using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TmtInventoryApp.Models.DealerPortal
{
    /// <summary>
    /// Represents sales made to end customers
    /// </summary>
    public class CustomerSale
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public int EndCustomerId { get; set; }
        public EndCustomer? EndCustomer { get; set; }

        [Required]
        [Display(Name = "Dealer")]
        public int DealerId { get; set; }
        public Dealer? Dealer { get; set; }

        [Required]
        [Display(Name = "Sale Date")]
        [DataType(DataType.Date)]
        public DateTime SaleDate { get; set; } = DateTime.Now;

        [Display(Name = "Invoice Number")]
        public string? InvoiceNumber { get; set; }

        [Display(Name = "Total Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Paid Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [NotMapped]
        [Display(Name = "Outstanding Amount")]
        public decimal OutstandingAmount => TotalAmount - PaidAmount;

        [Display(Name = "Payment Status")]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Partial, Paid

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }

        public List<CustomerSaleItem> Items { get; set; } = new List<CustomerSaleItem>();
        public List<DealerInventoryTransaction> InventoryTransactions { get; set; } = new List<DealerInventoryTransaction>();
    }

    public static class PaymentStatus
    {
        public const string Pending = "Pending";
        public const string Partial = "Partial";
        public const string Paid = "Paid";
    }
}
