using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TmtInventoryApp.Models.DealerPortal
{
    /// <summary>
    /// Represents inventory items in dealer's stock
    /// </summary>
    public class DealerInventoryItem
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Dealer")]
        public int DealerId { get; set; }
        public Dealer? Dealer { get; set; }

        [Required]
        [Display(Name = "Product Brand")]
        public int ProductBrandId { get; set; }
        public ProductBrand? ProductBrand { get; set; }

        [Display(Name = "Current Stock Quantity")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentQuantity { get; set; } = 0;

        [Display(Name = "Minimum Stock Level")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumStockLevel { get; set; }

        [Display(Name = "Unit Price")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public List<DealerInventoryTransaction> Transactions { get; set; } = new List<DealerInventoryTransaction>();
    }
}
