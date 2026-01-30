namespace TmtInventoryApp.Models.ViewModels
{
    public class DealerBillingStatus
    {
        public int DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public int TmtVariantId { get; set; }
        public string TmtVariantName { get; set; } = string.Empty;
        public decimal TotalSoldWeightKg { get; set; }
        public decimal TotalBilledWeightKg { get; set; }
        public decimal DifferenceWeightKg => TotalSoldWeightKg - TotalBilledWeightKg;
        public bool NeedsToBill => DifferenceWeightKg > 0;
        public bool OverBilled => DifferenceWeightKg < 0;
    }

    public class DealerBillingSummary
    {
        public Dealer Dealer { get; set; } = null!;
        public List<DealerBillingStatus> VariantStatus { get; set; } = new();
        public decimal TotalSoldWeightKg { get; set; }
        public decimal TotalBilledWeightKg { get; set; }
        public decimal TotalDifferenceWeightKg => TotalSoldWeightKg - TotalBilledWeightKg;
    }
}
