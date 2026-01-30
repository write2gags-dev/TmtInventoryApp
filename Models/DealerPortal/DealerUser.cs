using System.ComponentModel.DataAnnotations;

namespace TmtInventoryApp.Models.DealerPortal
{
    /// <summary>
    /// Represents a dealer user who can access the dealer portal
    /// </summary>
    public class DealerUser
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Password Hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Dealer")]
        public int DealerId { get; set; }
        public Dealer? Dealer { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Last Login")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "Contact Email")]
        [EmailAddress]
        public string? Email { get; set; }

        [Display(Name = "Contact Phone")]
        [Phone]
        public string? Phone { get; set; }
    }
}
