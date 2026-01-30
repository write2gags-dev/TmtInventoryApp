using System.ComponentModel.DataAnnotations;

namespace TmtInventoryApp.Models.DealerPortal
{
    /// <summary>
    /// Represents brands within each product category
    /// </summary>
    public class ProductBrand
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Brand Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product Category")]
        public int ProductCategoryId { get; set; }
        public ProductCategory? ProductCategory { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public List<DealerInventoryItem> InventoryItems { get; set; } = new List<DealerInventoryItem>();
    }
}
