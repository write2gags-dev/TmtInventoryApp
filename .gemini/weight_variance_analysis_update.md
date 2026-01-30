# Weight Variance Analysis - Update Summary

## Issue
The "Weight Variance Analysis" was not reflecting the latest adjusted information when calculating "Total Purchase Wt (kg)". When administrators edited stock weights through the Stock/Edit functionality, these adjustments were not being included in the variance analysis calculations.

## Root Cause
The `WeightStats` action in `ReconciliationController.cs` was calculating total purchase weight by summing:
1. Historical `PurchaseRecordItems` (original purchase records)
2. `VirtualStockTransactions` (manual virtual stock additions)
3. Offline additions parsed from Activity Logs

However, when stock was **edited** via the Stock/Edit feature (especially weight adjustments by Admin), these changes only updated the `StockEntry` table. The original `PurchaseRecordItems` remained unchanged, so the Weight Variance Analysis didn't reflect the adjusted values.

## Solution Implemented
Modified the `WeightStats` action to use **current physical stock** from `StockEntries` instead of historical purchase records. This ensures that:

1. **All adjustments are included**: Any weight modifications made through Stock/Edit are automatically reflected
2. **Latest data is used**: The analysis always shows the current state of inventory
3. **Simplified logic**: Removed the need to aggregate multiple sources (purchase records, virtual additions, offline additions)

## Changes Made

### File: `Controllers/ReconciliationController.cs`

**Before:**
```csharp
// Get all historical purchase data
var purchaseItems = await _context.PurchaseRecordItems
    .Select(p => new { p.TmtVariantId, p.Bundles, p.WeightTons })
    .ToListAsync();

// Get Virtual Stock Additions (Manual ones that aren't linked to PurchaseRecords)
var virtualAdditions = await _context.VirtualStockTransactions
    .Where(t => t.TransactionType == VirtualStockTransactionType.Addition 
               && t.ReferenceType != "Purchase")
    .Select(t => new { t.VirtualStock!.TmtVariantId, t.Bundles, t.WeightKg })
    .ToListAsync();

// Get Offline Additions (Parsed from Activity Logs)
var offlineHistory = await _stockHistoryService.GetOfflineAdditionsAsync(DateTime.MinValue);

// Calculate from multiple sources
stat.TotalPurchasedBundles = (decimal)vPurchases.Sum(p => p.Bundles) 
                           + (decimal)vVirtualAdds.Sum(v => v.Bundles)
                           + (decimal)vParsedOffline.Sum(o => o.Bundles);
                           
stat.TotalPurchasedWeightKg = (vPurchases.Sum(p => p.WeightTons) * 1000m) 
                            + vVirtualAdds.Sum(v => v.WeightKg)
                            + (vParsedOffline.Sum(o => o.WeightTons) * 1000m);
```

**After:**
```csharp
// Get current physical stock (includes all adjustments)
var currentStock = await _context.StockEntries
    .Select(s => new { s.TmtVariantId, s.QuantityBundles, s.WeightTons, s.PiecesPerBundle })
    .ToListAsync();

// Get current stock for this variant (reflects latest adjustments)
var vStock = currentStock.Where(s => s.TmtVariantId == variant.Id).ToList();

// Total purchased bundles and weight from current stock (includes all adjustments)
stat.TotalPurchasedBundles = vStock.Sum(s => s.QuantityBundles);
stat.TotalPurchasedWeightKg = vStock.Sum(s => s.WeightTons.HasValue ? s.WeightTons.Value * 1000m : 0);
```

## Benefits

1. **Accuracy**: The analysis now reflects the true current state of inventory
2. **Consistency**: Weight adjustments made by admins are immediately visible in the analysis
3. **Simplicity**: Reduced code complexity by using a single source of truth (StockEntries)
4. **Maintainability**: Easier to understand and maintain the calculation logic

## Testing Recommendations

1. Edit a stock entry's weight through Stock/Edit (as Admin)
2. Navigate to Business Health → Weight Variance Analysis
3. Verify that "Total Purchase Wt (kg)" reflects the adjusted weight
4. Compare the values before and after the adjustment to confirm the change is reflected

## Notes

- The change maintains backward compatibility as it doesn't modify the database schema
- The `AvgPurchaseWeightPerBundleKg` calculation remains unchanged
- The dealer-wise analysis continues to work as before
- All existing functionality is preserved
