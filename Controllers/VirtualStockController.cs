using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models;

namespace TmtInventoryApp.Controllers
{
    public class VirtualStockController : Controller
    {
        private readonly InventoryContext _context;

        public VirtualStockController(InventoryContext context)
        {
            _context = context;
        }

        // GET: VirtualStock
        public async Task<IActionResult> Index()
        {
            var virtualStocks = await _context.VirtualStocks
                .Include(vs => vs.TmtVariant)
                .OrderBy(vs => vs.TmtVariant!.Diameter)
                .ThenBy(vs => vs.TmtVariant!.Name)
                .ToListAsync();

            return View(virtualStocks);
        }

        // GET: VirtualStock/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can adjust virtual stock.";
                return RedirectToAction(nameof(Index));
            }

            if (id == null)
            {
                return NotFound();
            }

            var virtualStock = await _context.VirtualStocks
                .Include(vs => vs.TmtVariant)
                .FirstOrDefaultAsync(vs => vs.Id == id);

            if (virtualStock == null)
            {
                return NotFound();
            }

            return View(virtualStock);
        }

        // POST: VirtualStock/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, decimal newWeightTons, int newBundles, string? notes)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can adjust virtual stock.";
                return RedirectToAction(nameof(Index));
            }

            var virtualStock = await _context.VirtualStocks
                .Include(vs => vs.TmtVariant)
                .FirstOrDefaultAsync(vs => vs.Id == id);
            
            if (virtualStock == null)
            {
                return NotFound();
            }

            var oldWeight = virtualStock.AvailableWeightKg;
            var oldBundles = virtualStock.AvailableBundles;
            var newWeightKg = newWeightTons * 1000m; // Convert Tons to KG
            var weightDifference = newWeightKg - oldWeight;
            var bundleDifference = newBundles - oldBundles;

            // Update virtual stock
            virtualStock.AvailableWeightKg = newWeightKg;
            virtualStock.AvailableBundles = newBundles;
            virtualStock.LastUpdated = DateTime.Now;

            // Update TmtVariant PredefinedBundleWeight if applicable
            // REMOVED: User wants sales weight/bundle to be independent of stock received weight
            /*
            if (newBundles > 0 && newWeightKg > 0 && virtualStock.TmtVariant != null)
            {
                // Calculate weight per bundle based on this input
                // This ensures consistency between Virtual Stock and Physical Stock calculations
                var weightPerBundle = (double)(newWeightKg / newBundles);
                
                // Update the variant to use this new weight per bundle
                virtualStock.TmtVariant.PredefinedBundleWeight = weightPerBundle;
                virtualStock.TmtVariant.UsePredefinedBundleWeight = true;
                _context.Update(virtualStock.TmtVariant);
                
                // IMPORTANT: Save the variant changes NOW so SyncToPhysicalStock can use the updated weight
                await _context.SaveChangesAsync();
            }
            */

            // Record transaction
            var transaction = new VirtualStockTransaction
            {
                VirtualStockId = virtualStock.Id,
                TransactionType = weightDifference >= 0 ? VirtualStockTransactionType.Addition : VirtualStockTransactionType.Deduction,
                WeightKg = Math.Abs(weightDifference),
                Bundles = Math.Abs(bundleDifference),
                ReferenceType = "ManualAdjustment",
                Notes = notes ?? "Manual adjustment",
                TransactionDate = DateTime.Now
            };
            _context.VirtualStockTransactions.Add(transaction);

            // Sync with physical stock
            // The variant now has the correct PredefinedBundleWeight saved in the database
            await SyncToPhysicalStock(virtualStock.TmtVariantId, bundleDifference, weightDifference, notes);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: VirtualStock/AddStock/5
        public async Task<IActionResult> AddStock(int? id)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can add virtual stock.";
                return RedirectToAction(nameof(Index));
            }

            if (id == null)
            {
                return NotFound();
            }

            var virtualStock = await _context.VirtualStocks
                .Include(vs => vs.TmtVariant)
                .FirstOrDefaultAsync(vs => vs.Id == id);

            if (virtualStock == null)
            {
                return NotFound();
            }

            return View(virtualStock);
        }

        // POST: VirtualStock/AddStock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStock(int id, decimal addedWeightTons, int addedBundles, string? notes)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can add virtual stock.";
                return RedirectToAction(nameof(Index));
            }
            var virtualStock = await _context.VirtualStocks
                .Include(vs => vs.TmtVariant)
                .FirstOrDefaultAsync(vs => vs.Id == id);
            
            if (virtualStock == null)
            {
                return NotFound();
            }

            var addedWeightKg = addedWeightTons * 1000m; // Convert Tons to KG

            // Update virtual stock (Addition)
            virtualStock.AvailableWeightKg += addedWeightKg;
            virtualStock.AvailableBundles += addedBundles;
            virtualStock.LastUpdated = DateTime.Now;

            // Update TmtVariant PredefinedBundleWeight logic
            // REMOVED: User wants sales weight/bundle to be independent of stock received weight
            /*
            // If user provides specific weight and bundles, we should use this to establish the weight-per-bundle
            // This ensures that Physical Stock (calculated from bundles) matches the weight entered here.
            if (addedBundles > 0 && addedWeightKg > 0 && virtualStock.TmtVariant != null)
            {
                 var newWeightPerBundle = (double)(addedWeightKg / addedBundles);
                 
                 // Update the variant to use this new weight per bundle
                 virtualStock.TmtVariant.PredefinedBundleWeight = newWeightPerBundle;
                 virtualStock.TmtVariant.UsePredefinedBundleWeight = true;
                 _context.Update(virtualStock.TmtVariant);
                 
                 // IMPORTANT: Save the variant changes NOW so SyncToPhysicalStock can use the updated weight
                 await _context.SaveChangesAsync();
            }
            */

            // Record transaction
            var transaction = new VirtualStockTransaction
            {
                VirtualStockId = virtualStock.Id,
                TransactionType = VirtualStockTransactionType.Addition,
                WeightKg = addedWeightKg,
                Bundles = addedBundles,
                ReferenceType = "GRN/Addition",
                Notes = notes ?? "Stock received from Shyamsteel",
                TransactionDate = DateTime.Now
            };
            _context.VirtualStockTransactions.Add(transaction);

            // Sync with physical stock (Always Add)
            // The variant now has the correct PredefinedBundleWeight saved in the database
            await SyncToPhysicalStock(virtualStock.TmtVariantId, addedBundles, addedWeightKg, notes);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Syncs virtual stock changes to physical stock
        /// </summary>
        private async Task SyncToPhysicalStock(int tmtVariantId, int bundleDifference, decimal weightDifferenceKg, string? notes)
        {
            if (bundleDifference == 0 && weightDifferenceKg == 0)
                return;

            // Get or create physical stock entry
            var stockEntry = await _context.StockEntries
                .FirstOrDefaultAsync(se => se.TmtVariantId == tmtVariantId);

            if (stockEntry == null)
            {
                // Get variant to check properties
                var variant = await _context.TmtVariants.FindAsync(tmtVariantId);
                var piecesPerBundle = (variant != null && variant.UsePredefinedBundleWeight) ? 1 : 10;

                // Create new stock entry
                stockEntry = new StockEntry
                {
                    TmtVariantId = tmtVariantId,
                    QuantityBundles = bundleDifference > 0 ? bundleDifference : 0,
                    PiecesPerBundle = piecesPerBundle, // 1 for bags/coils, 10 for bars
                    LoosePieces = 0,
                    WeightTons = weightDifferenceKg / 1000m
                };
                _context.StockEntries.Add(stockEntry);
            }
            else
            {
                // Update existing stock entry
                stockEntry.QuantityBundles += bundleDifference;
                stockEntry.WeightTons = (stockEntry.WeightTons ?? 0) + (weightDifferenceKg / 1000m);
                
                // Ensure bundles don't go negative
                if (stockEntry.QuantityBundles < 0)
                {
                    stockEntry.QuantityBundles = 0;
                }
            }
        }

        // GET: VirtualStock/Transactions/5
        public async Task<IActionResult> Transactions(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var virtualStock = await _context.VirtualStocks
                .Include(vs => vs.TmtVariant)
                .FirstOrDefaultAsync(vs => vs.Id == id);

            if (virtualStock == null)
            {
                return NotFound();
            }

            var transactions = await _context.VirtualStockTransactions
                .Where(t => t.VirtualStockId == id)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            ViewBag.VirtualStock = virtualStock;
            return View(transactions);
        }

        // POST: VirtualStock/Initialize
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Initialize()
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can initialize virtual stock.";
                return RedirectToAction(nameof(Index));
            }
            // Get all TMT variants
            var variants = await _context.TmtVariants.ToListAsync();

            foreach (var variant in variants)
            {
                // Check if virtual stock already exists
                var exists = await _context.VirtualStocks
                    .AnyAsync(vs => vs.TmtVariantId == variant.Id);

                if (!exists)
                {
                    var virtualStock = new VirtualStock
                    {
                        TmtVariantId = variant.Id,
                        AvailableWeightKg = 0,
                        LastUpdated = DateTime.Now
                    };
                    _context.VirtualStocks.Add(virtualStock);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
