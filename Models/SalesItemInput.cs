namespace TmtInventoryApp.Models
{
    public class SalesItemInput
    {
        public int TmtVariantId { get; set; }
        public int QuantityBundles { get; set; }
        public int LoosePieces { get; set; }
        // UnitPrice removed — not used; weight and bundles only
        public double? WeightKg { get; set; }
    }
}
