using System;

namespace ExpenseClaimConverter
{
    public sealed class ExpenseLine
    {
        public string LineId { get; set; } = string.Empty;
        public string ExpenseDate { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;

        public decimal? Rate { get; set; }
        public DateTime? RateDate { get; set; }
        public decimal? ConvertedAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
