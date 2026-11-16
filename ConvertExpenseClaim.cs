using System;
using System.Collections.Generic;
using System.IO;
using UiPath.CodedWorkflows;
using UiPath.Core;

namespace ExpenseClaimConverter
{
    public class ConvertExpenseClaim : CodedWorkflow
    {
        [Workflow]
        public void Execute(
            string in_InputPath = "Data/Input/expenses.csv",
            string in_OutputPath = "Data/Output/expenses_converted.csv",
            string in_TargetCurrency = "EUR")
        {
            int linesRead;
            var convertedCount = 0;
            var failedCount = 0;
            var total = 0m;

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
                    if (validation.NormalizedCurrency == targetCurrency)
                    {
                        lookup = new RateLookupResult(true, 1m, validation.ExpenseDate, string.Empty);
                    }
                    else
                    {
                        var cacheKey = ExpenseCalculator.CreateRateCacheKey(
                            validation.NormalizedCurrency,
                            validation.ExpenseDate);

                        if (!rateCache.TryGetValue(cacheKey, out lookup))
                        {
                            var exchangeRateOutputs = RunWorkflow(
                                "GetExchangeRate.xaml",
                                new Dictionary<string, object>
                                {
                                    { "in_BaseCurrency", validation.NormalizedCurrency },
                                    { "in_QuoteCurrency", targetCurrency },
                                    { "in_RateDate", validation.ExpenseDate }
                                });

                            lookup = new RateLookupResult(
                                Convert.ToBoolean(exchangeRateOutputs["out_Success"]),
                                Convert.ToDecimal(exchangeRateOutputs["out_Rate"], System.Globalization.CultureInfo.InvariantCulture),
                                Convert.ToDateTime(exchangeRateOutputs["out_RateDate"], System.Globalization.CultureInfo.InvariantCulture),
                                exchangeRateOutputs["out_ErrorMessage"]?.ToString() ?? string.Empty);
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
                    total += line.ConvertedAmount.Value;
                    convertedCount++;
                }

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
