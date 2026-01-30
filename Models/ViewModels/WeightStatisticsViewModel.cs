using System;
using System.Collections.Generic;
using System.Linq;

namespace TmtInventoryApp.Models.ViewModels
{
    public class WeightStatisticsViewModel
    {
        public List<VariantWeightStat> Variants { get; set; } = new();
        public List<DealerGlobalStat> Dealers { get; set; } = new();
        
        // Detailed View Selection
        public int? SelectedDealerId { get; set; }
        public List<DealerVariantDetail> SelectedDealerDetails { get; set; } = new();
    }

    public class VariantWeightStat
    {
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;

        // Purchase Stats
        public decimal TotalPurchasedBundles { get; set; }
        public int TotalPurchasedLoose { get; set; }
        // Helper to get total pieces approximately if needed, but primary metrics are weight and bundles
        public decimal TotalPurchasedWeightKg { get; set; }
        
        // Derived
        public decimal AvgPurchaseWeightPerBundleKg => TotalPurchasedBundles > 0 
            ? TotalPurchasedWeightKg / TotalPurchasedBundles 
            : 0;

        // Sales Stats (Aggregated)
        public int TotalSoldBundles { get; set; }
        public int TotalSoldLoose { get; set; }
        public decimal TotalSoldWeightKg { get; set; }

        // Billing Stats
        public decimal TotalBilledWeightKg { get; set; }

        // Stock Saving / Loss Calculation
        // Expected Weight based on Purchase Avg
        public decimal ExpectedSoldWeightKg { get; set; }
        
        public decimal WeightSavingKg { get; set; }
        
        // Breakdown by Dealer
        public List<DealerWeightStat> DealerStats { get; set; } = new();
    }

    public class DealerWeightStat
    {
        public int DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;

        public int SoldBundles { get; set; }
        public int SoldLoose { get; set; }
        public decimal SoldWeightKg { get; set; }

        public decimal BilledWeightKg { get; set; }

        public decimal UnderBilledKg => SoldWeightKg - BilledWeightKg; // Positive = Under Billed (Dealer owes us billing)
        // If Sold 610, Billed 0 -> UnderBilled 610.
        // If Sold 610, Billed 600 -> UnderBilled 10.
    }

    public class DealerGlobalStat
    {
        public int DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public decimal TotalSoldWeightKg { get; set; }
        public decimal TotalBilledWeightKg { get; set; }
        public decimal NetDifferenceKg => TotalSoldWeightKg - TotalBilledWeightKg;
        // Positive = Under Billed (Goods sent, not billed)
        // Negative = Over Billed (Billed, goods not sent?)
    }
}
