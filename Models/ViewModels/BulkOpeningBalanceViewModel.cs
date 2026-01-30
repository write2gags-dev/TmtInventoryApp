using System;
using System.Collections.Generic;

namespace TmtInventoryApp.Models.ViewModels
{
    public class BulkOpeningBalanceViewModel
    {
        public int DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public DateTime AsOfDate { get; set; } = DateTime.Today;
        public List<VariantBalanceEntry> Variants { get; set; } = new();
    }

    public class VariantBalanceEntry
    {
        public int TmtVariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public decimal OpeningBalanceKg { get; set; }
        public string? Notes { get; set; }
        public int? ExistingId { get; set; } // If already exists
    }
}
