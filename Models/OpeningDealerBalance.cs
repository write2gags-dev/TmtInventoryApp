using System.ComponentModel.DataAnnotations;

namespace TmtInventoryApp.Models
{
    /// <summary>
    /// Represents opening balances for dealers (pre-system historical data)
    /// Positive = Dealer owes you weight
    /// Negative = You owe dealer weight
    /// </summary>
    public class OpeningDealerBalance
    {
        public int Id { get; set; }

        [Required]
        public int DealerId { get; set; }
        public Dealer? Dealer { get; set; }

        [Required]
        public int TmtVariantId { get; set; }
        public TmtVariant? TmtVariant { get; set; }

        /// <summary>
        /// Opening balance in KG
        /// Positive = Dealer owes you (you gave more than billed)
        /// Negative = You owe dealer (you billed more than given)
        /// </summary>
        [Required]
        [Display(Name = "Opening Balance (kg)")]
        public decimal OpeningBalanceKg { get; set; }

        [Display(Name = "As of Date")]
        [DataType(DataType.Date)]
        public DateTime AsOfDate { get; set; } = DateTime.Today;

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string? Notes { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Last Modified")]
        public DateTime? LastModified { get; set; }
    }
}
