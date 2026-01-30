using System.ComponentModel.DataAnnotations;

namespace TmtInventoryApp.Models
{
    public class TmtVariant
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Diameter (mm)")]
        public int Diameter { get; set; } // e.g., 8, 10, 12, 16, 20, 25

        [Display(Name = "Name")]
        public string? Name { get; set; }

        [Required]
        [Display(Name = "Weight per Meter (kg)")]
        public double WeightPerMeter { get; set; }

        [Required]
        [Display(Name = "Standard Length (m)")]
        public double StandardLength { get; set; } = 12.0; // Default to 12m

        public double WeightPerRod => WeightPerMeter * StandardLength;

        public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : $"{Diameter}mm";

        [Display(Name = "Predefined Bundle Weight (kg)")]
        public double PredefinedBundleWeight { get; set; }

        [Display(Name = "Use Predefined Bundle Weight")]
        public bool UsePredefinedBundleWeight { get; set; } = false;
    }
}
