using System;
using System.Collections.Generic;

namespace TmtInventoryApp.Models.ViewModels
{
    public class StockHistoryViewModel
    {
        public DateTime Date { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<StockHistoryItem> Items { get; set; } = new();
    }

    public class StockHistoryItem
    {
        public string VariantName { get; set; } = string.Empty;
        public int Bundles { get; set; }
        public decimal WeightTons { get; set; }
    }
}
