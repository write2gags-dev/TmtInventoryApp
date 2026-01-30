using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TmtInventoryApp.Models.DealerPortal
{
    /// <summary>
    /// Represents inventory transactions (stock in/out)
    /// </summary>
    public class DealerInventoryTransaction
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Inventory Item")]
        public int DealerInventoryItemId { get; set; }
        public DealerInventoryItem? DealerInventoryItem { get; set; }

        [Required]
        [Display(Name = "Transaction Type")]
        public string TransactionType { get; set; } = string.Empty; // "Stock In", "Stock Out", "Adjustment"

        [Required]
        [Display(Name = "Quantity")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Display(Name = "Transaction Date")]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Display(Name = "Reference Number")]
        public string? ReferenceNumber { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }

        // Link to customer sale if this is a stock out transaction
        [Display(Name = "Customer Sale")]
        public int? CustomerSaleId { get; set; }
        public CustomerSale? CustomerSale { get; set; }
    }

    public static class TransactionTypes
    {
        public const string StockIn = "Stock In";
        public const string StockOut = "Stock Out";
        public const string Adjustment = "Adjustment";
        public const string Return = "Return";
    }
}
