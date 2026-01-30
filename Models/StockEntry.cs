using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TmtInventoryApp.Models
{
    public class StockEntry
    {
        public int Id { get; set; }

        [Required]
        public int TmtVariantId { get; set; }

        public TmtVariant? TmtVariant { get; set; }

        [Display(Name = "Quantity (Bundles)")]
        public int QuantityBundles { get; set; }

        [Display(Name = "Pieces per Bundle")]
        public int PiecesPerBundle { get; set; }

        [Display(Name = "Loose Pieces")]
        public int LoosePieces { get; set; }

        [Display(Name = "Weight (Tons)")]
        public decimal? WeightTons { get; set; }

        [Display(Name = "Invoice Number")]
        [StringLength(100)]
        public string? InvoiceNumber { get; set; }

        [Display(Name = "Invoice Date")]
        [DataType(DataType.Date)]
        public DateTime? InvoiceDate { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public int TotalPieces => (QuantityBundles * PiecesPerBundle) + LoosePieces;

        [NotMapped]
        [Display(Name = "Total Weight (kg)")]
        public double TotalWeight 
        {
            get
            {
                if (TmtVariant == null) return 0;
                if (TmtVariant.UsePredefinedBundleWeight)
                {
                    return QuantityBundles * TmtVariant.PredefinedBundleWeight;
                }
                return TotalPieces * TmtVariant.WeightPerRod;
            }
        }
    }
}
