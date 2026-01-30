using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Models;
using TmtInventoryApp.Models.DealerPortal;

namespace TmtInventoryApp.Data
{
    public class InventoryContext : DbContext
    {
        public InventoryContext(DbContextOptions<InventoryContext> options)
            : base(options)
        {
        }

        // Distributor-side entities
        public DbSet<TmtVariant> TmtVariants { get; set; }
        public DbSet<StockEntry> StockEntries { get; set; }
        public DbSet<Dealer> Dealers { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderItem> SalesOrderItems { get; set; }
        public DbSet<BillingRecord> BillingRecords { get; set; }
        public DbSet<BillingRecordItem> BillingRecordItems { get; set; }
        public DbSet<VirtualStock> VirtualStocks { get; set; }
        public DbSet<VirtualStockTransaction> VirtualStockTransactions { get; set; }
        public DbSet<PurchaseRecord> PurchaseRecords { get; set; }
        public DbSet<PurchaseRecordItem> PurchaseRecordItems { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<OpeningDealerBalance> OpeningDealerBalances { get; set; }

        // Dealer Portal entities
        public DbSet<DealerUser> DealerUsers { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<ProductBrand> ProductBrands { get; set; }
        public DbSet<DealerInventoryItem> DealerInventoryItems { get; set; }
        public DbSet<DealerInventoryTransaction> DealerInventoryTransactions { get; set; }
        public DbSet<EndCustomer> EndCustomers { get; set; }
        public DbSet<CustomerSale> CustomerSales { get; set; }
        public DbSet<CustomerSaleItem> CustomerSaleItems { get; set; }
        public DbSet<CustomerPayment> CustomerPayments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Seed initial data for standard TMT bars
            modelBuilder.Entity<TmtVariant>().HasData(
                new TmtVariant { Id = 1, Diameter = 8, WeightPerMeter = 0.395 },
                new TmtVariant { Id = 2, Diameter = 10, WeightPerMeter = 0.617 },
                new TmtVariant { Id = 3, Diameter = 12, WeightPerMeter = 0.888 },
                new TmtVariant { Id = 4, Diameter = 16, WeightPerMeter = 1.58 },
                new TmtVariant { Id = 5, Diameter = 20, WeightPerMeter = 2.47 },
                // Additional miscellaneous variants (wire, nails, rings)
                new TmtVariant { Id = 7, Diameter = 0, WeightPerMeter = 0.0, Name = "Wire - 20 Gauge" },
                new TmtVariant { Id = 8, Diameter = 0, WeightPerMeter = 0.0, Name = "Nail (BAG) - 2 Inch" },
                new TmtVariant { Id = 9, Diameter = 0, WeightPerMeter = 0.0, Name = "Nail (BAG) - 2.5 Inch" },
                new TmtVariant { Id = 10, Diameter = 0, WeightPerMeter = 0.0, Name = "Ring - 7/7" },
                new TmtVariant { Id = 11, Diameter = 0, WeightPerMeter = 0.0, Name = "Ring - 7/4" },
                new TmtVariant { Id = 12, Diameter = 0, WeightPerMeter = 0.0, Name = "Ring - 7/9" }
            );
        }
    }
}
