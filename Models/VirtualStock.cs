using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TmtInventoryApp.Models
{
    /// <summary>
    /// Represents virtual stock maintained by Shymasteel company
    /// This stock is deducted when billing is done
    /// </summary>
    public class VirtualStock
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "TMT Variant")]
        public int TmtVariantId { get; set; }
        public TmtVariant? TmtVariant { get; set; }

        [Display(Name = "Available Weight (KG)")]
        public decimal AvailableWeightKg { get; set; }

        [NotMapped]
        [Display(Name = "Available Weight (Tons)")]
        public decimal AvailableWeightTons
        {
            get => AvailableWeightKg / 1000m;
            set => AvailableWeightKg = value * 1000m;
        }

        [Display(Name = "Available Bundles")]
        public int AvailableBundles { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Tracks transactions against virtual stock
    /// </summary>
    public class VirtualStockTransaction
    {
        public int Id { get; set; }

        [Required]
        public int VirtualStockId { get; set; }
        public VirtualStock? VirtualStock { get; set; }

        [Display(Name = "Transaction Type")]
        public VirtualStockTransactionType TransactionType { get; set; }

        [Display(Name = "Weight (KG)")]
        public decimal WeightKg { get; set; }

        [Display(Name = "Bundles")]
        public int Bundles { get; set; }

        [Display(Name = "Reference Type")]
        public string? ReferenceType { get; set; } // e.g., "BillingRecord", "Adjustment"

        [Display(Name = "Reference ID")]
        public int? ReferenceId { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Transaction Date")]
        public DateTime TransactionDate { get; set; } = DateTime.Now;
    }

    public enum VirtualStockTransactionType
    {
        Addition,
        Deduction,
        Adjustment
    }
}
