using System.ComponentModel.DataAnnotations;

namespace TmtInventoryApp.Models
{
    public class BillingRecord
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Dealer")]
        public int DealerId { get; set; }
        public Dealer? Dealer { get; set; }

        [Required]
        [Display(Name = "Billing Date")]
        [DataType(DataType.Date)]
        public DateTime BillingDate { get; set; } = DateTime.Now;

        [Display(Name = "Invoice Number")]
        public string? InvoiceNumber { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public List<BillingRecordItem> Items { get; set; } = new List<BillingRecordItem>();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public decimal TotalWeight => Items?.Sum(i => i.BilledWeightKg ?? 0) ?? 0;
    }

    public class BillingRecordItem
    {
        public int Id { get; set; }

        public int BillingRecordId { get; set; }
        public BillingRecord? BillingRecord { get; set; }

        [Required]
        [Display(Name = "TMT Variant")]
        public int TmtVariantId { get; set; }
        public TmtVariant? TmtVariant { get; set; }

        [Display(Name = "Billed Weight (KG)")]
        public decimal? BilledWeightKg { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        [Display(Name = "Billed Weight (Tons)")]
        public decimal? BilledWeightTons
        {
            get
            {
                return BilledWeightKg.HasValue ? BilledWeightKg.Value / 1000m : null;
            }
            set
            {
                BilledWeightKg = value.HasValue ? value.Value * 1000m : null;
            }
        }
    }
}
