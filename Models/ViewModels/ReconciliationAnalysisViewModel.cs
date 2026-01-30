using System;
using System.Collections.Generic;

namespace TmtInventoryApp.Models.ViewModels
{
    public class ReconciliationAnalysisViewModel
    {
        public List<VariantAnalysis> Variants { get; set; } = new();
    }

    public class VariantAnalysis
    {
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        
        // "Kolkata Virtual stock" - Current Balance from VirtualStock table
        public decimal VirtualStockBalanceKg { get; set; } 
        public decimal VirtualStockBalanceTons => VirtualStockBalanceKg / 1000m;

        // Summary of all dealers
        public decimal SumSoldKg { get; set; }
        public decimal SumSoldTons => SumSoldKg / 1000m;

        public decimal SumBilledKg { get; set; }
        public decimal SumBilledTons => SumBilledKg / 1000m;

        // Sum of all Opening Balances for this variant
        public decimal SumOpeningBalanceKg { get; set; }
        public decimal SumOpeningBalanceTons => SumOpeningBalanceKg / 1000m;

        public decimal SumOverUnderKg { get; set; } // Sum of (Billed - Sold) for all dealers
        public decimal SumOverUnderTons => SumOverUnderKg / 1000m;

        // List of Dealer stats for this variant
        public List<DealerAnalysis> Dealers { get; set; } = new();

        // Footer Calculations
        // "Minimum physical stock should be Owed + Balance"
        public decimal MinPhysicalStockKg => SumOverUnderKg + VirtualStockBalanceKg;
        public decimal MinPhysicalStockTons => MinPhysicalStockKg / 1000m;

        // Current Physical Stock
        public decimal CurrentPhysicalStockKg { get; set; }
        public decimal CurrentPhysicalStockTons => CurrentPhysicalStockKg / 1000m;

        // Difference: Current - Minimum
        // If Positive: Surplus
        // If Negative: Shortage
        public decimal PhysicalStockDifferenceKg => CurrentPhysicalStockKg - MinPhysicalStockKg;
        public decimal PhysicalStockDifferenceTons => PhysicalStockDifferenceKg / 1000m;
        
        // Explicitly show what is owed to dealers (Over-billed)
        // SumOverUnderKg = Billed - Sold. If Positive, we owe dealers.
        public decimal OwedToDealersKg => SumOverUnderKg > 0 ? SumOverUnderKg : 0;
        public decimal OwedToDealersTons => OwedToDealersKg / 1000m;

        // Alias for clarity - this IS the real available stock
        public decimal NetAvailableAfterObligationsKg => PhysicalStockDifferenceKg;
        public decimal NetAvailableAfterObligationsTons => PhysicalStockDifferenceTons;

        public bool IsShortage => PhysicalStockDifferenceKg < -10; // Tolerance of 10kg
    }

    public class DealerAnalysis
    {
        public int DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;
        
        public decimal SoldKg { get; set; }
        public decimal BilledKg { get; set; }
        
        public decimal OverUnderKg => BilledKg - SoldKg;
    }
}
