using System.ComponentModel.DataAnnotations;

namespace TmtInventoryApp.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = string.Empty; // Sales, Billing, Stock, Dealer, etc.

        [Required]
        [Display(Name = "Action")]
        public string Action { get; set; } = string.Empty; // Created, Updated, Deleted

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "User")]
        public string? UserName { get; set; }

        [Display(Name = "Entity Type")]
        public string? EntityType { get; set; } // SalesOrder, BillingRecord, StockEntry, etc.

        [Display(Name = "Entity ID")]
        public int? EntityId { get; set; }

        [Display(Name = "Additional Details")]
        public string? AdditionalDetails { get; set; }
    }
}
