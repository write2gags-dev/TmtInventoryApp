using System.ComponentModel.DataAnnotations;

namespace TmtInventoryApp.Models
{
    public enum OrderStatus
    {
        Pending,
        Completed,
        Cancelled
    }

    public class SalesOrder
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Dealer")]
        public int DealerId { get; set; }

        public Dealer? Dealer { get; set; }

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // TotalAmount removed — not tracking money in sales orders

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public bool IsLocked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
    }
}
