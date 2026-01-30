using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TmtInventoryApp.Models.DealerPortal
{
    /// <summary>
    /// Represents individual items in a customer sale
    /// </summary>
    public class CustomerSaleItem
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Customer Sale")]
        public int CustomerSaleId { get; set; }
        public CustomerSale? CustomerSale { get; set; }

        [Required]
        [Display(Name = "Product Brand")]
        public int ProductBrandId { get; set; }
        public ProductBrand? ProductBrand { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Required]
        [Display(Name = "Unit Price")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        [Display(Name = "Total Price")]
        public decimal TotalPrice => Quantity * UnitPrice;

        [Display(Name = "Discount %")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountPercentage { get; set; }

        [NotMapped]
        [Display(Name = "Final Amount")]
        public decimal FinalAmount
        {
            get
            {
                var total = TotalPrice;
                if (DiscountPercentage.HasValue)
                {
                    total -= (total * DiscountPercentage.Value / 100);
                }
                return total;
            }
        }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}
