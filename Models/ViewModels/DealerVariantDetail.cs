namespace TmtInventoryApp.Models.ViewModels
{
    public class DealerVariantDetail
    {
        public int TmtVariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public decimal SoldWeightKg { get; set; }
        public decimal BilledWeightKg { get; set; }
        public decimal NetDifferenceKg => SoldWeightKg - BilledWeightKg;
        // Positive = Under Billed (Goods sent, not billed)
        // Negative = Over Billed (Billed, goods not sent)
    }
}
