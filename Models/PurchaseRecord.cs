using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TmtInventoryApp.Models
{
    public class PurchaseRecord
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Purchase Date")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; }

        [Display(Name = "Invoice #")]
        [StringLength(50)]
        public string? InvoiceNumber { get; set; }

        [Display(Name = "Vehicle #")]
        [StringLength(50)]
        public string? VehicleNumber { get; set; }

        [Display(Name = "Shortage (KG)")]
        public decimal Shortage { get; set; }

        [NotMapped]
        [Display(Name = "Total Weight (Tons)")]
        public decimal TotalWeight => Items?.Sum(i => i.WeightTons) ?? 0;

        public List<PurchaseRecordItem> Items { get; set; } = new List<PurchaseRecordItem>();
    }

    public class PurchaseRecordItem
    {
        public int Id { get; set; }

        public int PurchaseRecordId { get; set; }
        public PurchaseRecord? PurchaseRecord { get; set; }

        public int TmtVariantId { get; set; }
        public TmtVariant? TmtVariant { get; set; }

        public int Bundles { get; set; }

        [Display(Name = "Weight (Tons)")]
        public decimal WeightTons { get; set; }
    }
}
