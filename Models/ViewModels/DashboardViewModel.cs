using TmtInventoryApp.Models;

namespace TmtInventoryApp.Models.ViewModels
{
    public class DashboardViewModel
    {
        public double TotalWeightKg { get; set; }
        public int TotalBundles { get; set; }
        public List<StockSummaryItem> StockSummary { get; set; } = new List<StockSummaryItem>();
    }

    public class StockSummaryItem
    {
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public int QuantityBundles { get; set; }
        public double TotalWeight { get; set; }
    }
}
