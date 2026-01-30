using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models;
using TmtInventoryApp.Models.ViewModels;
using TmtInventoryApp.Services;

namespace TmtInventoryApp.Controllers
{
    public class PurchaseController : Controller
    {
        private readonly InventoryContext _context;
        private readonly ActivityLogger _activityLogger;

        public PurchaseController(InventoryContext context, ActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // GET: Purchase
        public async Task<IActionResult> Index()
        {
            // Fetch variants for the grid columns - ordered by Id to maintain custom sequence
            var variants = await _context.TmtVariants.OrderBy(v => v.Id).ToListAsync();
            ViewBag.Variants = variants;

            // Fetch current Virtual Stock balances
            var virtualStocks = await _context.VirtualStocks
                .Include(vs => vs.TmtVariant)
                .OrderBy(vs => vs.TmtVariantId)
                .ToListAsync();
            ViewBag.VirtualStocks = virtualStocks;

            // Fetch purchases from the last 12 months
            var cutoffDate = DateTime.Now.AddMonths(-12);
            var purchases = await _context.PurchaseRecords
                .Include(p => p.Items)
                .Where(p => p.PurchaseDate >= cutoffDate)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            return View(purchases);
        }

        // POST: Purchase/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DateTime purchaseDate, string invoiceNumber, string vehicleNumber, decimal shortage, Dictionary<int, int> bundles, Dictionary<int, decimal> weights)
        {
            // Allow any logged in user (Admin or Operator) to record purchases
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Role")))
            {
                 return RedirectToAction("Login", "Account");
            }
            if (bundles == null || weights == null)
            {
                return BadRequest("Invalid data");
            }

            // Create the purchase record
            var purchaseRecord = new PurchaseRecord
            {
                PurchaseDate = purchaseDate,
                InvoiceNumber = invoiceNumber,
                VehicleNumber = vehicleNumber,
                Shortage = shortage,
                Items = new List<PurchaseRecordItem>()
            };

            bool hasItems = false;

            // Iterate through variants to find entered data
            var variants = await _context.TmtVariants.ToListAsync();
            foreach (var variant in variants)
            {
                int qtyBundles = bundles.ContainsKey(variant.Id) ? bundles[variant.Id] : 0;
                decimal weightTons = weights.ContainsKey(variant.Id) ? weights[variant.Id] : 0;

                if (qtyBundles > 0 || weightTons > 0)
                {
                    hasItems = true;
                    var item = new PurchaseRecordItem
                    {
                        TmtVariantId = variant.Id,
                        Bundles = qtyBundles,
                        WeightTons = weightTons
                    };
                    purchaseRecord.Items.Add(item);

                    // Update Virtual Stock
                    await UpdateVirtualStock(variant.Id, qtyBundles, weightTons, invoiceNumber);
                }
            }

            if (hasItems)
            {
                _context.PurchaseRecords.Add(purchaseRecord);
                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogger.LogActivityAsync(
                    "Purchase",
                    "Created",
                    $"New stock received entry: Invoice {invoiceNumber}",
                    HttpContext.Session.GetString("DisplayName"),
                    "PurchaseRecord",
                    purchaseRecord.Id,
                    $"Vehicle: {vehicleNumber}, Total Weight: {purchaseRecord.TotalWeight} tons"
                );

                TempData["Success"] = "Stock received and Kolkata Stock (Virtual) updated.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Purchase/Adjust/5
        public async Task<IActionResult> Adjust(int? id)
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

        // POST: Purchase/Adjust/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(int id, decimal newWeightTons, int newBundles, string? notes)
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
            await _context.SaveChangesAsync();
            
            await _activityLogger.LogActivityAsync(
                "VirtualStock",
                "Adjusted",
                $"Virtual stock manually adjusted for {virtualStock.TmtVariant?.DisplayName}",
                HttpContext.Session.GetString("DisplayName"),
                "VirtualStock",
                virtualStock.Id,
                $"Old Weight: {oldWeight / 1000m} tons, New Weight: {newWeightTons} tons, Old Bundles: {oldBundles}, New Bundles: {newBundles}, Notes: {notes}"
            );

            TempData["Success"] = "Kolkata Stock (Virtual) adjusted successfully.";

            // Sync with physical stock
            await SyncToPhysicalStock(virtualStock.TmtVariantId, bundleDifference, notes);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Helper method to sync changes to physical stock
        private async Task SyncToPhysicalStock(int tmtVariantId, int bundleDifference, string? notes)
        {
            var stockEntry = await _context.StockEntries
                .FirstOrDefaultAsync(s => s.TmtVariantId == tmtVariantId);

            if (stockEntry == null && bundleDifference > 0)
            {
                // Create new entry if adding stock
                stockEntry = new StockEntry
                {
                    TmtVariantId = tmtVariantId,
                    QuantityBundles = bundleDifference,
                    PiecesPerBundle = 10,
                    LoosePieces = 0
                };
                _context.StockEntries.Add(stockEntry);
            }
            else if (stockEntry != null)
            {
                // Update existing entry
                stockEntry.QuantityBundles += bundleDifference;
                
                // If quantity becomes zero or negative, remove the entry
                if (stockEntry.QuantityBundles <= 0)
                {
                    _context.StockEntries.Remove(stockEntry);
                }
            }
        }

        private async Task UpdateVirtualStock(int tmtVariantId, int addedBundles, decimal addedWeightTons, string invoiceNumber)
        {
            var virtualStock = await _context.VirtualStocks
                .Include(vs => vs.TmtVariant)
                .FirstOrDefaultAsync(vs => vs.TmtVariantId == tmtVariantId);

            if (virtualStock == null)
            {
                // Create if not exists (should exist from seeding, but safety check)
                virtualStock = new VirtualStock
                {
                    TmtVariantId = tmtVariantId,
                    AvailableBundles = 0,
                    AvailableWeightKg = 0
                };
                _context.VirtualStocks.Add(virtualStock);
            }

            decimal addedWeightKg = addedWeightTons * 1000m;

            // Update totals
            virtualStock.AvailableBundles += addedBundles;
            virtualStock.AvailableWeightKg += addedWeightKg;
            virtualStock.LastUpdated = DateTime.Now;

            // Update PredefinedBundleWeight logic (CRITICAL for sync)
            // REMOVED: User wants sales weight/bundle to be independent of stock received weight
            /*
            if (addedBundles > 0 && addedWeightKg > 0)
            {
                var weightPerBundle = (double)(addedWeightKg / addedBundles);
                
                // Always update the variant's weight definition based on this latest purchase
                if (virtualStock.TmtVariant != null)
                {
                    virtualStock.TmtVariant.PredefinedBundleWeight = weightPerBundle;
                    virtualStock.TmtVariant.UsePredefinedBundleWeight = true;
                    _context.Update(virtualStock.TmtVariant);
                    
                    // Save NOW so SyncToPhysicalStock uses the correct weight
                    await _context.SaveChangesAsync();
                }
            }
            */

            // Record Transaction
            var transaction = new VirtualStockTransaction
            {
                VirtualStock = virtualStock,
                TransactionType = VirtualStockTransactionType.Addition,
                Bundles = addedBundles,
                WeightKg = addedWeightKg,
                ReferenceType = "Purchase",
                // ReferenceId = invoiceNumber, // Cannot assign string to int?
                Notes = $"Purchase Invoice: {invoiceNumber}",
                TransactionDate = DateTime.Now
            };
            _context.VirtualStockTransactions.Add(transaction);

            // Sync to Physical Stock
            // We need to call the logic from VirtualStockController. 
            // Since we are in a different controller, I'll duplicate the simple sync logic here or instantiate the controller.
            // Duplicating the simple logic is safer than cross-controller calls.
            
            var stockEntry = await _context.StockEntries
                .FirstOrDefaultAsync(s => s.TmtVariantId == tmtVariantId);

            if (stockEntry == null)
            {
                stockEntry = new StockEntry
                {
                    TmtVariantId = tmtVariantId,
                    QuantityBundles = 0,
                    PiecesPerBundle = 10, // Default
                    LoosePieces = 0
                };
                _context.StockEntries.Add(stockEntry);
            }

            stockEntry.QuantityBundles += addedBundles;
            // We don't manually set weight on StockEntry, it's calculated from PredefinedBundleWeight which we just updated.
            
            // However, we also added WeightTons and Invoice fields to StockEntry recently.
            // We should update those too for the record.
            stockEntry.WeightTons = (stockEntry.WeightTons ?? 0) + addedWeightTons;
            stockEntry.InvoiceNumber = invoiceNumber;
            stockEntry.InvoiceDate = DateTime.Now; // Or purchaseDate if passed down

            // _context.Update(stockEntry); // Removed to fix InvalidOperationException
        }
    }
}
