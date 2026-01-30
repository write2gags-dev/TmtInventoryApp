using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TmtInventoryApp.Models.DealerPortal
{
    /// <summary>
    /// Represents end customers that dealers sell to
    /// </summary>
    public class EndCustomer
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Dealer")]
        public int DealerId { get; set; }
        public Dealer? Dealer { get; set; }

        [Required]
        [Display(Name = "Customer Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [Display(Name = "Phone Number")]
        [Phone]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        public string? Email { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "GSTIN")]
        public string? Gstin { get; set; }

        [Display(Name = "Credit Limit")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CreditLimit { get; set; }

        [Display(Name = "Current Outstanding")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentOutstanding { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public List<CustomerSale> Sales { get; set; } = new List<CustomerSale>();
        public List<CustomerPayment> Payments { get; set; } = new List<CustomerPayment>();
    }
}
