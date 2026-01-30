using System.ComponentModel.DataAnnotations;

namespace TmtInventoryApp.Models
{
    public class Dealer
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Dealer Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Dealer Category")]
        public string Category { get; set; } = "My Dealers - Deogarh Circle";

        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [Display(Name = "Phone Number")]
        [Phone]
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        [Display(Name = "GSTIN")]
        public string? Gstin { get; set; }
    }
}
