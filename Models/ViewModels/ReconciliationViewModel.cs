namespace TmtInventoryApp.Models.ViewModels
{
    /// <summary>
    /// View model for reconciliation summary showing billing vs sales vs physical stock
    /// </summary>
    public class ReconciliationSummaryViewModel
    {
        public List<ReconciliationItem> Items { get; set; } = new();
        public decimal TotalOverBilledWeightKg { get; set; }
        public decimal TotalUnderBilledWeightKg { get; set; }
        public bool HasCriticalShortages { get; set; }
        public List<VariantReconciliationSummary> VariantSummaries { get; set; } = new();
    }

    public class ReconciliationItem
    {
        public int DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public int TmtVariantId { get; set; }
        public string TmtVariantName { get; set; } = string.Empty;
        
        // Sales data
        public decimal TotalSoldWeightKg { get; set; }
        public decimal TotalSoldWeightTons => TotalSoldWeightKg / 1000m;
        
        // Billing data
        public decimal TotalBilledWeightKg { get; set; }
        public decimal TotalBilledWeightTons => TotalBilledWeightKg / 1000m;
        
        // Difference (Billed - Sold)
        // Positive: Billed more than sold (Stock Liability / Pending Delivery)
        // Negative: Sold more than billed (Pending Billing)
        public decimal DifferenceWeightKg => TotalBilledWeightKg - TotalSoldWeightKg;
        public decimal DifferenceWeightTons => DifferenceWeightKg / 1000m;
        
        // Stock availability
        public decimal PhysicalStockWeightKg { get; set; }
        public decimal PhysicalStockWeightTons => PhysicalStockWeightKg / 1000m;
        public int PhysicalStockBundles { get; set; }
        
        // Virtual stock (Shymasteel)
        public decimal VirtualStockWeightKg { get; set; }
        public decimal VirtualStockWeightTons => VirtualStockWeightKg / 1000m;
        public int VirtualStockBundles { get; set; }
        
        // Status flags
        public bool IsOverBilled => DifferenceWeightKg > 0; // Billed more than sold (Liability)
        public bool IsUnderBilled => DifferenceWeightKg < 0; // Sold more than billed
        public bool IsBalanced => Math.Abs(DifferenceWeightKg) < 0.01m; // Within 10 grams
        
        // Critical check: If over-billed (Positive Difference), do we have enough physical stock?
        public bool HasSufficientPhysicalStock => !IsOverBilled || PhysicalStockWeightKg >= DifferenceWeightKg;
        public bool HasSufficientVirtualStock => !IsOverBilled || VirtualStockWeightKg >= DifferenceWeightKg;
        
        // Shortage amount if any
        public decimal PhysicalStockShortageKg => IsOverBilled && !HasSufficientPhysicalStock 
            ? DifferenceWeightKg - PhysicalStockWeightKg 
            : 0;
        public decimal PhysicalStockShortageTons => PhysicalStockShortageKg / 1000m;
        
        public decimal VirtualStockShortageKg => IsOverBilled && !HasSufficientVirtualStock 
            ? DifferenceWeightKg - VirtualStockWeightKg 
            : 0;
        public decimal VirtualStockShortageTons => VirtualStockShortageKg / 1000m;
        
        // Risk level
        public ReconciliationRiskLevel RiskLevel
        {
            get
            {
                if (IsBalanced) return ReconciliationRiskLevel.Balanced;
                if (IsUnderBilled) return ReconciliationRiskLevel.UnderBilled;
                // Critical only if Physical Stock is insufficient (User Request)
                if (IsOverBilled && HasSufficientPhysicalStock) 
                    return ReconciliationRiskLevel.OverBilledWithStock;
                return ReconciliationRiskLevel.Critical; // Over-billed without sufficient PHYSICAL stock
            }
        }
    }

    public enum ReconciliationRiskLevel
    {
        Balanced,           // No discrepancy
        UnderBilled,        // Sold more than billed (dealer owes money)
        OverBilledWithStock,// Billed more than sold but stock is available
        Critical            // Billed more than sold AND insufficient stock (LOSS RISK)
    }

    public class VariantReconciliationSummary
    {
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public decimal TotalSoldKg { get; set; }
        public decimal TotalBilledKg { get; set; }
        // Net Difference from dealer perspective: Sold - Billed
        // Positive = Dealers owe you (you gave more than billed)
        // Negative = You owe dealers (you billed more than sold)
        public decimal NetDifferenceKg => TotalSoldKg - TotalBilledKg;
        public decimal PhysicalStockKg { get; set; }
        public decimal VirtualStockKg { get; set; } // Kolkata Virtual Stock (already included in Physical)
        
        // Total amount owed to dealers (over-billed amount - negative NetDifference)
        // This is the stock liability you need to fulfill
        public decimal TotalOwedToDealersKg => NetDifferenceKg < 0 ? Math.Abs(NetDifferenceKg) : 0;
        
        // Surplus/Loss: (Physical - Kolkata) + Net Difference
        // Add positive (dealers owe you), Subtract negative (you owe dealers)
        // Positive = Surplus, Negative = Loss/Shortage
        public decimal SurplusLossKg => (PhysicalStockKg - VirtualStockKg) + NetDifferenceKg;
        
        // Net Available after fulfilling obligations
        // This shows what you actually have left after giving dealers what you owe them
        // If SurplusLossKg is positive but you owe dealers, this shows the real available stock
        public decimal NetAvailableAfterObligationsKg => SurplusLossKg - TotalOwedToDealersKg;
    }
}
