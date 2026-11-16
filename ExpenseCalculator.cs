using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ExpenseClaimConverter
{
    public static class ExpenseCalculator
    {
        private static readonly Regex CurrencyCodePattern = new Regex(
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

        public static ValidationResult Validate(ExpenseLine line, DateTime today)
        {
            if (line == null)
            {
                return new ValidationResult { Message = "Line is required." };
            }

            decimal amount;
            if (!decimal.TryParse(
                    (line.Amount ?? string.Empty).Trim(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out amount) || amount <= 0m)
            {
                return new ValidationResult { Message = "Amount must be a number greater than 0." };
            }

            var normalizedCurrency = NormalizeCurrency(line.Currency);
            if (!IsValidCurrency(normalizedCurrency))
            {
                return new ValidationResult { Message = "Currency must be exactly 3 letters." };
            }

            DateTime expenseDate;
            if (!DateTime.TryParseExact(
                    (line.ExpenseDate ?? string.Empty).Trim(),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out expenseDate))
            {
                return new ValidationResult { Message = "Invalid date format, expected yyyy-MM-dd" };
            }

            expenseDate = expenseDate.Date;
            if (expenseDate > today.Date)
            {
                return new ValidationResult { Message = "ExpenseDate must not be in the future." };
            }

            return new ValidationResult
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

    public sealed class RateLookupResult
    {
        public RateLookupResult(bool success, decimal rate, DateTime rateDate, string errorMessage)
        {
            Success = success;
            Rate = rate;
            RateDate = rateDate;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public bool Success { get; }
        public decimal Rate { get; }
        public DateTime RateDate { get; }
        public string ErrorMessage { get; }
    }
}
