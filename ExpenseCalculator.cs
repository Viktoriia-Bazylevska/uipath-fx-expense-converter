using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ExpenseClaimConverter
{
    public static class ExpenseCalculator
    {
        private static readonly Regex CurrencyCodePattern = new(
            "^[A-Z]{3}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static string NormalizeCurrency(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        public static bool IsValidCurrency(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && CurrencyCodePattern.IsMatch(value);
        }

        public static bool IsSameCurrency(string baseCurrency, string quoteCurrency)
        {
            return string.Equals(
                NormalizeCurrency(baseCurrency),
                NormalizeCurrency(quoteCurrency),
                StringComparison.Ordinal);
        }

        public static RateLookupResult GetSameCurrencyRate(
            string baseCurrency,
            string quoteCurrency,
            DateTime rateDate)
        {
            if (!IsSameCurrency(baseCurrency, quoteCurrency))
            {
                return new(
                    false,
                    0m,
                    rateDate,
                    "Currencies differ.");
            }

            return new(
                true,
                1m,
                rateDate.Date,
                string.Empty);
        }

        public static ValidationResult Validate(ExpenseLine line, DateTime today)
        {
            if (line == null)
            {
                return new() { Message = "Line is required." };
            }

            if (!decimal.TryParse(
                    (line.Amount ?? string.Empty).Trim(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var amount) || amount <= 0m)
            {
                return new ValidationResult { Message = "Amount must be a number greater than 0." };
            }

            var normalizedCurrency = NormalizeCurrency(line.Currency);
            if (!IsValidCurrency(normalizedCurrency))
            {
                return new() { Message = "Currency must be exactly 3 letters." };
            }

            if (!DateTime.TryParseExact(
                    (line.ExpenseDate ?? string.Empty).Trim(),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var expenseDate))
            {
                return new() { Message = "Invalid date format, expected yyyy-MM-dd" };
            }

            expenseDate = expenseDate.Date;
            if (expenseDate > today.Date)
            {
                return new() { Message = "ExpenseDate must not be in the future." };
            }

            return new()
            {
                IsValid = true,
                Amount = amount,
                NormalizedCurrency = normalizedCurrency,
                ExpenseDate = expenseDate,
                Message = string.Empty
            };
        }

        public static decimal CalculateConvertedAmount(decimal amount, decimal rate)
        {
            return Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero);
        }

        public static decimal CalculateTotal(IReadOnlyList<ExpenseLine> lines)
        {
            var total = 0m;
            if (lines == null)
            {
                return total;
            }

            foreach (var line in lines)
            {
                if (line != null &&
                    string.Equals(line.Status, "Converted", StringComparison.Ordinal) &&
                    line.ConvertedAmount.HasValue)
                {
                    total += line.ConvertedAmount.Value;
                }
            }

            return total;
        }

        public static string CreateRateCacheKey(string normalizedCurrency, DateTime expenseDate)
        {
            return (normalizedCurrency ?? string.Empty) + "|" +
                   expenseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }

    public sealed class ValidationResult
    {
        public bool IsValid { get; set; }
        public decimal Amount { get; set; }
        public string NormalizedCurrency { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class RateLookupResult(
        bool success,
        decimal rate,
        DateTime rateDate,
        string errorMessage)
    {
        public bool Success { get; } = success;
        public decimal Rate { get; } = rate;
        public DateTime RateDate { get; } = rateDate;
        public string ErrorMessage { get; } = errorMessage ?? string.Empty;
    }
}
