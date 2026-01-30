using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models;
using TmtInventoryApp.Models.ViewModels;
using TmtInventoryApp.Services;

namespace TmtInventoryApp.Controllers
{
    public class ReconciliationController : Controller
    {
        private readonly InventoryContext _context;
        private readonly StockHistoryService _stockHistoryService;

        public ReconciliationController(InventoryContext context, StockHistoryService stockHistoryService)
        {
            _context = context;
            _stockHistoryService = stockHistoryService;
        }

        /// <summary>
        /// Shows reconciliation summary: Sales vs Billing vs Physical Stock
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var reconciliationItems = new List<ReconciliationItem>();

            // Get all dealers
            var dealers = await _context.Dealers.ToListAsync();
            var variants = await _context.TmtVariants.ToListAsync();

            foreach (var dealer in dealers)
            {
                foreach (var variant in variants)
                {
                    // Calculate total sold weight (from all sales orders, not just completed)
                    var totalSoldWeight = await _context.SalesOrderItems
                        .Where(soi => soi.SalesOrder!.DealerId == dealer.Id && 
                                     soi.TmtVariantId == variant.Id)
                        .SumAsync(soi => soi.WeightKg ?? 0);

                    // Calculate total billed weight
                    var totalBilledWeight = await _context.BillingRecordItems
                        .Where(bri => bri.BillingRecord!.DealerId == dealer.Id && 
                                     bri.TmtVariantId == variant.Id)
                        .SumAsync(bri => bri.BilledWeightKg ?? 0);

                    // Skip if both are zero (no transactions)
                    if (totalSoldWeight == 0 && totalBilledWeight == 0)
                        continue;

                    // Get physical stock
                    // Get opening balance for this dealer-variant (if exists)
                    var openingBalance = await _context.OpeningDealerBalances
                        .FirstOrDefaultAsync(ob => ob.DealerId == dealer.Id && ob.TmtVariantId == variant.Id);

                    var openingBalanceKg = openingBalance?.OpeningBalanceKg ?? 0;

                    var physicalStock = await _context.StockEntries
                        .Include(se => se.TmtVariant)
                        .Where(se => se.TmtVariantId == variant.Id)
                        .ToListAsync();

                    // Use manually entered WeightTons, falling back to 0 if not set (as we disabled predefined weights)
                    var physicalStockWeight = physicalStock.Sum(se => se.WeightTons.HasValue ? (decimal)(se.WeightTons.Value * 1000) : 0);
                    var physicalStockBundles = physicalStock.Sum(se => se.QuantityBundles);

                    // Get virtual stock (Shymasteel)
                    var virtualStock = await _context.VirtualStocks
                        .FirstOrDefaultAsync(vs => vs.TmtVariantId == variant.Id);

                    var item = new ReconciliationItem
                    {
                        DealerId = dealer.Id,
                        DealerName = dealer.Name,
                        TmtVariantId = variant.Id,
                        TmtVariantName = variant.DisplayName,
                        // Include opening balance in TotalSoldWeightKg
                        // Opening balance positive = dealer owes you (add to sold)
                        // Opening balance negative = you owe dealer (subtract from sold)
                        TotalSoldWeightKg = (decimal)totalSoldWeight + openingBalanceKg,
                        TotalBilledWeightKg = totalBilledWeight,
                        PhysicalStockWeightKg = physicalStockWeight,
                        PhysicalStockBundles = physicalStockBundles,
                        VirtualStockWeightKg = virtualStock?.AvailableWeightKg ?? 0,
                        VirtualStockBundles = virtualStock?.AvailableBundles ?? 0
                    };

                    reconciliationItems.Add(item);
                }
            }

            // Sort: Critical items first, then by dealer name
            reconciliationItems = reconciliationItems
                .OrderBy(item => item.RiskLevel == ReconciliationRiskLevel.Critical ? 0 : 1)
                .ThenBy(item => item.DealerName)
                .ThenBy(item => item.TmtVariantName)
                .ToList();

            // Aggregate per-variant totals for quick overview
            var variantSummaries = reconciliationItems
                .GroupBy(i => new { i.TmtVariantId, i.TmtVariantName })
                .Select(g => new VariantReconciliationSummary
                {
                    VariantId = g.Key.TmtVariantId,
                    VariantName = g.Key.TmtVariantName,
                    TotalSoldKg = g.Sum(x => x.TotalSoldWeightKg),
                    TotalBilledKg = g.Sum(x => x.TotalBilledWeightKg),
                    PhysicalStockKg = g.Max(x => x.PhysicalStockWeightKg),
                    VirtualStockKg = g.Max(x => x.VirtualStockWeightKg) // Kolkata Virtual Stock
                })
                .OrderBy(v => v.VariantId)
                .ToList();

            var viewModel = new ReconciliationSummaryViewModel
            {
                Items = reconciliationItems,
                TotalOverBilledWeightKg = reconciliationItems
                    .Where(i => i.IsOverBilled)
                    .Sum(i => i.DifferenceWeightKg), // Difference is positive for over-billed
                TotalUnderBilledWeightKg = reconciliationItems
                    .Where(i => i.IsUnderBilled)
                    .Sum(i => Math.Abs(i.DifferenceWeightKg)), // Make positive for display
                HasCriticalShortages = reconciliationItems
                    .Any(i => i.RiskLevel == ReconciliationRiskLevel.Critical),
                VariantSummaries = variantSummaries
            };

            return View(viewModel);
        }

        /// <summary>
        /// Shows detailed reconciliation for a specific dealer
        /// </summary>
        public async Task<IActionResult> DealerDetails(int dealerId)
        {
            var dealer = await _context.Dealers.FindAsync(dealerId);
            if (dealer == null)
                return NotFound();

            var reconciliationItems = new List<ReconciliationItem>();
            var variants = await _context.TmtVariants.ToListAsync();

            foreach (var variant in variants)
            {
                // Calculate total sold weight (from all sales orders, not just completed)
                var totalSoldWeight = await _context.SalesOrderItems
                    .Where(soi => soi.SalesOrder!.DealerId == dealerId && 
                                 soi.TmtVariantId == variant.Id)
                    .SumAsync(soi => soi.WeightKg ?? 0);

                // Calculate total billed weight
                var totalBilledWeight = await _context.BillingRecordItems
                    .Where(bri => bri.BillingRecord!.DealerId == dealerId && 
                                 bri.TmtVariantId == variant.Id)
                    .SumAsync(bri => bri.BilledWeightKg ?? 0);

                // Skip if both are zero
                if (totalSoldWeight == 0 && totalBilledWeight == 0)
                    continue;

                // Get physical stock
                var physicalStock = await _context.StockEntries
                    .Include(se => se.TmtVariant)
                    .Where(se => se.TmtVariantId == variant.Id)
                    .ToListAsync();

                // Use manually entered WeightTons
                var physicalStockWeight = physicalStock.Sum(se => se.WeightTons.HasValue ? (decimal)(se.WeightTons.Value * 1000) : 0);
                var physicalStockBundles = physicalStock.Sum(se => se.QuantityBundles);

                // Get virtual stock
                var virtualStock = await _context.VirtualStocks
                    .FirstOrDefaultAsync(vs => vs.TmtVariantId == variant.Id);

                var item = new ReconciliationItem
                {
                    DealerId = dealer.Id,
                    DealerName = dealer.Name,
                    TmtVariantId = variant.Id,
                    TmtVariantName = variant.DisplayName,
                    TotalSoldWeightKg = (decimal)totalSoldWeight,
                    TotalBilledWeightKg = totalBilledWeight,
                    PhysicalStockWeightKg = physicalStockWeight,
                    PhysicalStockBundles = physicalStockBundles,
                    VirtualStockWeightKg = virtualStock?.AvailableWeightKg ?? 0,
                    VirtualStockBundles = virtualStock?.AvailableBundles ?? 0
                };

                reconciliationItems.Add(item);
            }

            ViewData["DealerName"] = dealer.Name;
            
            var viewModel = new ReconciliationSummaryViewModel
            {
                Items = reconciliationItems
                    .OrderBy(i => i.RiskLevel == ReconciliationRiskLevel.Critical ? 0 : 1)
                    .ThenBy(i => i.TmtVariantName)
                    .ToList(),
                TotalOverBilledWeightKg = reconciliationItems
                    .Where(i => i.IsOverBilled)
                    .Sum(i => i.DifferenceWeightKg), // Difference is positive for over-billed
                TotalUnderBilledWeightKg = reconciliationItems
                    .Where(i => i.IsUnderBilled)
                    .Sum(i => Math.Abs(i.DifferenceWeightKg)), // Make positive for display
                HasCriticalShortages = reconciliationItems
                    .Any(i => i.RiskLevel == ReconciliationRiskLevel.Critical)
            };

            return View("Index", viewModel);
        }


        /// <summary>
        /// Shows detailed weight reconciliation: Purchase Avg vs Sales Actual to determine Savings/Loss
        /// </summary>
        public async Task<IActionResult> WeightStats(int? selectedDealerId = null)
        {
            var variants = await _context.TmtVariants.ToListAsync();
            
            // Get current physical stock (includes all adjustments)
            var currentStock = await _context.StockEntries
                .Select(s => new { s.TmtVariantId, s.QuantityBundles, s.WeightTons, s.PiecesPerBundle })
                .ToListAsync();

            // Get all sales data
            var salesItems = await _context.SalesOrderItems
                .Include(s => s.SalesOrder)
                .Select(s => new 
                { 
                    s.TmtVariantId, 
                    s.QuantityBundles, 
                    s.LoosePieces, 
                    s.WeightKg, 
                    DealerId = s.SalesOrder!.DealerId,
                    DealerName = s.SalesOrder.Dealer != null ? s.SalesOrder.Dealer.Name : "Unknown"
                })
                .ToListAsync();

            // Get all billing data
            var billingItems = await _context.BillingRecordItems
                .Include(b => b.BillingRecord)
                .Select(b => new 
                { 
                    b.TmtVariantId, 
                    b.BilledWeightKg, 
                    DealerId = b.BillingRecord!.DealerId,
                    DealerName = b.BillingRecord.Dealer != null ? b.BillingRecord.Dealer.Name : "Unknown"
                })
                .ToListAsync();

            var model = new WeightStatisticsViewModel
            {
                Variants = new List<VariantWeightStat>()
            };

            foreach (var variant in variants)
            {
                var stat = new VariantWeightStat
                {
                    VariantId = variant.Id,
                    VariantName = variant.DisplayName
                };

                // Get current stock for this variant (reflects latest adjustments)
                var vStock = currentStock.Where(s => s.TmtVariantId == variant.Id).ToList();
                
                // Total purchased bundles and weight from current stock (includes all adjustments)
                stat.TotalPurchasedBundles = vStock.Sum(s => s.QuantityBundles);
                stat.TotalPurchasedWeightKg = vStock.Sum(s => s.WeightTons.HasValue ? s.WeightTons.Value * 1000m : 0);

                // Sales Stats (Global)
                var vSales = salesItems.Where(s => s.TmtVariantId == variant.Id).ToList();
                stat.TotalSoldBundles = vSales.Sum(s => s.QuantityBundles);
                stat.TotalSoldLoose = vSales.Sum(s => s.LoosePieces);
                stat.TotalSoldWeightKg = (decimal)vSales.Sum(s => s.WeightKg ?? 0);

                // Billing Stats (Global)
                var vBilling = billingItems.Where(b => b.TmtVariantId == variant.Id).ToList();
                stat.TotalBilledWeightKg = vBilling.Sum(b => b.BilledWeightKg ?? 0);

                // Dealer Breakdown & Calculation
                var dealerIds = vSales.Select(s => s.DealerId)
                                .Union(vBilling.Select(b => b.DealerId))
                                .Distinct();

                foreach (var dealerId in dealerIds)
                {
                    var dSales = vSales.Where(s => s.DealerId == dealerId).ToList();
                    var dBilling = vBilling.Where(b => b.DealerId == dealerId).ToList();
                    
                    var dealerStat = new DealerWeightStat
                    {
                        DealerId = dealerId,
                        DealerName = dSales.FirstOrDefault()?.DealerName ?? dBilling.FirstOrDefault()?.DealerName ?? "Unknown",
                        SoldBundles = dSales.Sum(s => s.QuantityBundles),
                        SoldLoose = dSales.Sum(s => s.LoosePieces),
                        SoldWeightKg = (decimal)dSales.Sum(s => s.WeightKg ?? 0),
                        BilledWeightKg = dBilling.Sum(b => b.BilledWeightKg ?? 0)
                    };
                    stat.DealerStats.Add(dealerStat);
                }

                if (stat.TotalPurchasedBundles > 0 && stat.AvgPurchaseWeightPerBundleKg > 0)
                {
                    // Find standard pieces per bundle from stock (fallback to 10 if not found)
                    var piecesPerBundle = vStock.FirstOrDefault()?.PiecesPerBundle;
                    if (!piecesPerBundle.HasValue || piecesPerBundle == 0) piecesPerBundle = 10; // Default fallback

                    decimal bundlesEquivalent = stat.TotalSoldBundles + (decimal)stat.TotalSoldLoose / piecesPerBundle.Value;
                    
                    // Expected weight = Sold Quantity (in bundles) * Avg Purchase Weight per Bundle
                    stat.ExpectedSoldWeightKg = bundlesEquivalent * stat.AvgPurchaseWeightPerBundleKg;
                    
                    stat.WeightSavingKg = stat.TotalSoldWeightKg - stat.ExpectedSoldWeightKg;
                }
                else
                {
                    // No purchase history to compare against
                    stat.ExpectedSoldWeightKg = 0;
                    stat.WeightSavingKg = 0; 
                }

                model.Variants.Add(stat);
            }

            // Calculate Dealer-wise totals
            var allDealerIds = salesItems.Select(s => s.DealerId)
                            .Union(billingItems.Select(b => b.DealerId))
                            .Distinct();

            foreach (var dealerId in allDealerIds)
            {
                var dSales = salesItems.Where(s => s.DealerId == dealerId).Sum(s => s.WeightKg ?? 0);
                var dBilling = billingItems.Where(b => b.DealerId == dealerId).Sum(b => b.BilledWeightKg ?? 0);
                
                // Get dealer name from one of the lists
                var dName = salesItems.FirstOrDefault(s => s.DealerId == dealerId)?.DealerName 
                          ?? billingItems.FirstOrDefault(b => b.DealerId == dealerId)?.DealerName 
                          ?? "Unknown";

                model.Dealers.Add(new DealerGlobalStat
                {
                    DealerId = dealerId,
                    DealerName = dName,
                    TotalSoldWeightKg = (decimal)dSales,
                    TotalBilledWeightKg = dBilling
                });
            }
            
            
            model.Dealers = model.Dealers.OrderBy(d => d.DealerName).ToList();

            // Handle Selected Dealer Details
            if (selectedDealerId.HasValue)
            {
                model.SelectedDealerId = selectedDealerId;
                foreach (var variant in variants)
                {
                    // Filter raw items by this dealer and variant
                    var dSales = salesItems
                        .Where(s => s.DealerId == selectedDealerId.Value && s.TmtVariantId == variant.Id)
                        .Sum(s => s.WeightKg ?? 0);
                        
                    var dBilling = billingItems
                        .Where(b => b.DealerId == selectedDealerId.Value && b.TmtVariantId == variant.Id)
                        .Sum(b => b.BilledWeightKg ?? 0);

                    // Only add if there is activity
                    if (dSales > 0 || dBilling > 0)
                    {
                        model.SelectedDealerDetails.Add(new DealerVariantDetail
                        {
                            TmtVariantId = variant.Id,
                            VariantName = variant.DisplayName,
                            SoldWeightKg = (decimal)dSales,
                            BilledWeightKg = dBilling
                        });
                    }
                }
            }

            return View(model);
        }
        /// <summary>
        /// Shows detailed reconciliation analysis for business health (Sales vs Billing vs Virtual vs Physical)
        /// Per user request: Comparing Physical Stock Requirements against Obligations
        /// </summary>
        public async Task<IActionResult> Analysis()
        {
            var variants = await _context.TmtVariants.OrderBy(v => v.Id).ToListAsync();
            var dealers = await _context.Dealers.ToListAsync();
            
            // Fetch all data in bulk to minimize DB roundtrips
            var salesItems = await _context.SalesOrderItems
                .Include(s => s.SalesOrder)
                .Select(s => new { s.TmtVariantId, s.WeightKg, DealerId = s.SalesOrder!.DealerId })
                .ToListAsync();

            var billingItems = await _context.BillingRecordItems
                .Include(b => b.BillingRecord)
                .Select(b => new { b.TmtVariantId, b.BilledWeightKg, DealerId = b.BillingRecord!.DealerId })
                .ToListAsync();

            var stockEntries = await _context.StockEntries
                .Select(s => new { s.TmtVariantId, s.WeightTons })
                .ToListAsync();

            var virtualStocks = await _context.VirtualStocks
                .Select(v => new { v.TmtVariantId, v.AvailableWeightKg })
                .ToListAsync();

            // Fetch Opening Balances
            var openingBalances = await _context.OpeningDealerBalances
                .Select(o => new { o.TmtVariantId, o.OpeningBalanceKg })
                .ToListAsync();

            var model = new ReconciliationAnalysisViewModel();

            foreach (var variant in variants)
            {
                var variantAnalysis = new VariantAnalysis
                {
                    VariantId = variant.Id,
                    VariantName = variant.DisplayName
                };

                // Virtual Stock
                var vs = virtualStocks.FirstOrDefault(v => v.TmtVariantId == variant.Id);
                variantAnalysis.VirtualStockBalanceKg = vs?.AvailableWeightKg ?? 0;

                // Physical Stock
                var phys = stockEntries.Where(s => s.TmtVariantId == variant.Id).Sum(s => s.WeightTons.HasValue ? (decimal)(s.WeightTons.Value * 1000) : 0);
                variantAnalysis.CurrentPhysicalStockKg = phys;

                // Sales & Billing sums (Aggregates)
                // Note: Sales weight is stored as double, need to cast to decimal for currency/precise calculations
                var sumSales = (decimal)salesItems.Where(s => s.TmtVariantId == variant.Id).Sum(s => s.WeightKg ?? 0);
                var sumBilling = billingItems.Where(b => b.TmtVariantId == variant.Id).Sum(b => b.BilledWeightKg ?? 0);
                
                // Opening Balance Sum for this variant (Sum of all dealers)
                // Positive = Dealer Owes You. Negative = You Owe Dealer.
                var vOpening = openingBalances.Where(o => o.TmtVariantId == variant.Id).Sum(o => o.OpeningBalanceKg);

                variantAnalysis.SumSoldKg = sumSales;
                variantAnalysis.SumBilledKg = sumBilling;
                variantAnalysis.SumOpeningBalanceKg = vOpening;

                // Net Obligation (Liability Perspective): Billed - Sold - Opening
                variantAnalysis.SumOverUnderKg = (sumBilling - sumSales) - vOpening;

                // Dealer Data for this variant (Lists for detailed view if needed later)
                // Note: The original code used these lists below, so we keep variable names but ensure no conflict above
                var vSales = salesItems.Where(s => s.TmtVariantId == variant.Id).ToList();
                var vBilling = billingItems.Where(b => b.TmtVariantId == variant.Id).ToList();

                // Find dealers with activity
                var activeDealerIds = vSales.Select(s => s.DealerId)
                    .Union(vBilling.Select(b => b.DealerId))
                    .Distinct();

                foreach (var dealerId in activeDealerIds)
                {
                    var dealerName = dealers.FirstOrDefault(d => d.Id == dealerId)?.Name ?? "Unknown";
                    var sold = vSales.Where(s => s.DealerId == dealerId).Sum(s => (decimal)(s.WeightKg ?? 0));
                    var billed = vBilling.Where(b => b.DealerId == dealerId).Sum(b => b.BilledWeightKg ?? 0);

                    variantAnalysis.Dealers.Add(new DealerAnalysis
                    {
                        DealerId = dealerId,
                        DealerName = dealerName,
                        SoldKg = sold,
                        BilledKg = billed
                    });
                }

                // Calculate Summaries
                variantAnalysis.SumSoldKg = variantAnalysis.Dealers.Sum(d => d.SoldKg);
                variantAnalysis.SumBilledKg = variantAnalysis.Dealers.Sum(d => d.BilledKg);
                variantAnalysis.SumOverUnderKg = variantAnalysis.Dealers.Sum(d => d.OverUnderKg);

                model.Variants.Add(variantAnalysis);
            }

            return View(model);
        }
    }
}
