using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TmtInventoryApp.Models
{
    public class SalesOrderItem
    {
        public int Id { get; set; }

        public int SalesOrderId { get; set; }
        public SalesOrder? SalesOrder { get; set; }

        [Required]
        [Display(Name = "TMT Variant")]
        public int TmtVariantId { get; set; }
        public TmtVariant? TmtVariant { get; set; }

        [Display(Name = "Quantity (Bundles)")]
        public int QuantityBundles { get; set; }

        [Display(Name = "Loose Pieces")]
        public int LoosePieces { get; set; }

        // UnitPrice and SubTotal removed — sales tracked by bundles and weight only

        [Display(Name = "Weight (kg)")]
        public double? WeightKg { get; set; }
    }
}
