using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TmtInventoryApp.Models.DealerPortal
{
    /// <summary>
    /// Represents payments received from end customers
    /// </summary>
    public class CustomerPayment
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public int EndCustomerId { get; set; }
        public EndCustomer? EndCustomer { get; set; }

        [Required]
        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Cheque, Bank Transfer, UPI

        [Display(Name = "Reference Number")]
        public string? ReferenceNumber { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Recorded By")]
        public string? RecordedBy { get; set; }

        [Display(Name = "Customer Sale")]
        public int? CustomerSaleId { get; set; }
        public CustomerSale? CustomerSale { get; set; }
    }

    public static class PaymentMethods
    {
        public const string Cash = "Cash";
        public const string Cheque = "Cheque";
        public const string BankTransfer = "Bank Transfer";
        public const string UPI = "UPI";
        public const string Card = "Card";
    }
}
