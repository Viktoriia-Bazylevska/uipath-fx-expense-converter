#pragma warning disable CS0436 // FX.Rates 1.0.2 exports source types in the consumer namespace; local project types are intentional.

using System;
using System.Collections.Generic;
using System.IO;
using UiPath.CodedWorkflows;
using UiPath.Core;

namespace ExpenseClaimConverter
{
    public class ConvertExpenseClaim : CodedWorkflow
    {
        /// <summary>
        /// Converts expense claims from a CSV file into the selected target currency and writes an enriched CSV result.
        /// Invalid expense lines are marked as failed and excluded from the converted total.
        /// </summary>
        /// <param name="in_InputPath">Path to the source expenses CSV file. Provide an absolute path or a path relative to the robot's working directory.</param>
        /// <param name="in_OutputPath">Path where the converted expenses CSV file is written. Provide an absolute path or a path relative to the robot's working directory.</param>
        /// <param name="in_TargetCurrency">Three-letter ISO currency code used for conversion. Defaults to EUR.</param>
        [Workflow]
        public void Execute(
            [System.ComponentModel.Description("Path to the source expenses CSV file. Provide an absolute path or a path relative to the robot's working directory.")] string in_InputPath,
            [System.ComponentModel.Description("Path where the converted expenses CSV file is written. Provide an absolute path or a path relative to the robot's working directory.")] string in_OutputPath,
            [System.ComponentModel.Description("Three-letter ISO currency code used for conversion. Defaults to EUR.")] string in_TargetCurrency = "EUR")
        {
            int linesRead;
            var convertedCount = 0;
            var failedCount = 0;
            decimal total;

            try
            {
                var targetCurrency = ExpenseCalculator.NormalizeCurrency(in_TargetCurrency);
                if (!ExpenseCalculator.IsValidCurrency(targetCurrency))
                {
                    throw new BusinessRuleException("Invalid target currency: must be exactly 3 letters.");
                }

                var inputPath = ResolvePath(in_InputPath, "in_InputPath");
                var outputPath = ResolvePath(in_OutputPath, "in_OutputPath");
                Log("Expense conversion started: input=" + Path.GetFileName(inputPath) +
                    "; output=" + Path.GetFileName(outputPath) +
                    "; target=" + targetCurrency);

                var lines = ExpenseCsv.Read(inputPath);
                linesRead = lines.Count;
                var rateCache = new Dictionary<string, RateLookupResult>(StringComparer.Ordinal);

                foreach (var line in lines)
                {
                    var validation = ExpenseCalculator.Validate(line, DateTime.Today);
                    if (!validation.IsValid)
                    {
                        MarkFailed(line, validation.Message);
                        failedCount++;
                        Log("Line failed: LineId=" + line.LineId + "; reason=" + validation.Message, LogLevel.Warn);
                        continue;
                    }

                    RateLookupResult lookup;
                    if (ExpenseCalculator.IsSameCurrency(
                            validation.NormalizedCurrency,
                            targetCurrency))
                    {
                        lookup = ExpenseCalculator.GetSameCurrencyRate(
                            validation.NormalizedCurrency,
                            targetCurrency,
                            validation.ExpenseDate);
                    }
                    else
                    {
                        var cacheKey = ExpenseCalculator.CreateRateCacheKey(
                            validation.NormalizedCurrency,
                            validation.ExpenseDate);

                        if (!rateCache.TryGetValue(cacheKey, out lookup))
                        {
                            var (success, rate, rateDate, errorMessage) = fXRates.GetExchangeRate(
                                validation.NormalizedCurrency,
                                targetCurrency,
                                validation.ExpenseDate);

                            lookup = new RateLookupResult(
                                success,
                                rate,
                                rateDate,
                                errorMessage ?? string.Empty);
                            rateCache.Add(cacheKey, lookup);
                        }
                    }

                    if (!lookup.Success)
                    {
                        MarkFailed(line, lookup.ErrorMessage);
                        failedCount++;
                        Log("Line failed: LineId=" + line.LineId + "; reason=" + lookup.ErrorMessage, LogLevel.Warn);
                        continue;
                    }

                    line.Rate = lookup.Rate;
                    line.RateDate = lookup.RateDate;
                    line.ConvertedAmount = ExpenseCalculator.CalculateConvertedAmount(validation.Amount, lookup.Rate);
                    line.Status = "Converted";
                    line.Message = string.Empty;
                    convertedCount++;
                }

                total = ExpenseCalculator.CalculateTotal(lines);
                ExpenseCsv.Write(outputPath, lines, total, targetCurrency);
                Log("Expense conversion ended: linesRead=" + linesRead +
                    "; converted=" + convertedCount +
                    "; failed=" + failedCount +
                    "; total=" + total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                    " " + targetCurrency);
            }
            catch (BusinessRuleException exception)
            {
                Log("Expense conversion configuration error: " + exception.Message, LogLevel.Warn);
                throw;
            }
            catch (Exception exception)
            {
                Log("Expense conversion unexpected error: " + exception.Message, LogLevel.Error);
                throw;
            }
        }

        private static string ResolvePath(string path, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new BusinessRuleException(argumentName + " is required.");
            }

            return Path.GetFullPath(path);
        }

        private static void MarkFailed(ExpenseLine line, string message)
        {
            line.Rate = null;
            line.RateDate = null;
            line.ConvertedAmount = null;
            line.Status = "Failed";
            line.Message = message ?? "Conversion failed.";
        }
    }
}
