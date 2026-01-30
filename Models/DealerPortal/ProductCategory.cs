using System.ComponentModel.DataAnnotations;

namespace TmtInventoryApp.Models.DealerPortal
{
    /// <summary>
    /// Represents product categories (Cement, Asbestos, TMT, etc.)
    /// </summary>
    public class ProductCategory
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Unit of Measurement")]
        public string UnitOfMeasurement { get; set; } = "KG"; // KG, Bags, Pieces, etc.

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public List<ProductBrand> Brands { get; set; } = new List<ProductBrand>();
    }
}
