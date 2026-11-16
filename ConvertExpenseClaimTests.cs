using System;
using System.Collections.Generic;
using System.Globalization;
using UiPath.CodedWorkflows;

namespace ExpenseClaimConverter
{
    public class ConvertExpenseClaimTests : CodedWorkflow
    {
        [TestCase]
        public void Execute()
        {
            // Arrange
            var today = DateTime.Today;
            var validDate = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var futureDate = today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // Act
            var roundedHalfUp = ExpenseCalculator.CalculateConvertedAmount(10.005m, 1m);
            var roundedDown = ExpenseCalculator.CalculateConvertedAmount(2.344m, 1m);
            var sameCurrencyRate = ExpenseCalculator.GetSameCurrencyRate(
                "EUR",
                "EUR",
                today);

            var negativeAmountValidation = ExpenseCalculator.Validate(
                new ExpenseLine
                {
                    LineId = "negative",
                    ExpenseDate = validDate,
                    Amount = "-5.00",
                    Currency = "USD"
                },
                today);

            var invalidCurrencyValidation = ExpenseCalculator.Validate(
                new ExpenseLine
                {
                    LineId = "invalid-currency",
                    ExpenseDate = validDate,
                    Amount = "10.00",
                    Currency = "US"
                },
                today);

            var futureDateValidation = ExpenseCalculator.Validate(
                new ExpenseLine
                {
                    LineId = "future-date",
                    ExpenseDate = futureDate,
                    Amount = "10.00",
                    Currency = "USD"
                },
                today);

            var total = ExpenseCalculator.CalculateTotal([
                new ExpenseLine
                {
                    LineId = "converted-1",
                    Status = "Converted",
                    ConvertedAmount = roundedHalfUp
                },
                new ExpenseLine
                {
                    LineId = "converted-2",
                    Status = "Converted",
                    ConvertedAmount = roundedDown
                },
                new ExpenseLine
                {
                    LineId = "failed",
                    Status = "Failed",
                    ConvertedAmount = 99.99m
                }
            ]);

            // Assert
            testing.VerifyExpression(
                roundedHalfUp == 10.01m,
                "10.005 must round to 10.01 using AwayFromZero.");
            testing.VerifyExpression(
                roundedDown == 2.34m,
                "2.344 must round to 2.34.");
            testing.VerifyExpression(
                sameCurrencyRate.Success && sameCurrencyRate.Rate == 1m,
                "Same currency must return success with rate 1.");
            testing.VerifyExpression(
                !negativeAmountValidation.IsValid &&
                negativeAmountValidation.Message == "Amount must be a number greater than 0.",
                "Negative amounts must be rejected with a clear message.");
            testing.VerifyExpression(
                !invalidCurrencyValidation.IsValid &&
                invalidCurrencyValidation.Message == "Currency must be exactly 3 letters.",
                "Invalid currency codes must be rejected with a clear message.");
            testing.VerifyExpression(
                !futureDateValidation.IsValid &&
                futureDateValidation.Message == "ExpenseDate must not be in the future.",
                "Future dates must be rejected with a clear message.");
            testing.VerifyExpression(
                total == 12.35m,
                "The total must include converted lines only.");
        }
    }
}
