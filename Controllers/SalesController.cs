using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models;
using TmtInventoryApp.Services;

namespace TmtInventoryApp.Controllers
{
    public class SalesController : Controller
    {
        private readonly InventoryContext _context;
        private readonly ActivityLogger _activityLogger;

        public SalesController(InventoryContext context, ActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // GET: Sales
        public async Task<IActionResult> Index()
        {
            var orders = await _context.SalesOrders
                .Include(s => s.Dealer)
                .Include(s => s.Items)
                    .ThenInclude(i => i.TmtVariant)
                .OrderByDescending(s => s.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // GET: Sales/Create
        public IActionResult Create()
        {
            ViewData["DealerId"] = new SelectList(_context.Dealers, "Id", "Name");
            return View();
        }

        // POST: Sales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DealerId,OrderDate")] SalesOrder salesOrder)
        {
            if (ModelState.IsValid)
            {
                salesOrder.Status = OrderStatus.Pending;
                _context.Add(salesOrder);
                await _context.SaveChangesAsync();
                
                var dealer = await _context.Dealers.FindAsync(salesOrder.DealerId);
                await _activityLogger.LogActivityAsync(
                    "Sales",
                    "Created",
                    $"Sales Order #{salesOrder.Id} created for dealer: {dealer?.Name}",
                    User.Identity?.Name,
                    "SalesOrder",
                    salesOrder.Id,
                    $"Order Date: {salesOrder.OrderDate:yyyy-MM-dd}"
                );
                
                return RedirectToAction(nameof(Details), new { id = salesOrder.Id });
            }
            ViewData["DealerId"] = new SelectList(_context.Dealers, "Id", "Name", salesOrder.DealerId);
            return View(salesOrder);
        }

        // GET: Sales/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salesOrder = await _context.SalesOrders
                .Include(s => s.Dealer)
                .Include(s => s.Items)
                    .ThenInclude(i => i.TmtVariant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (salesOrder == null)
            {
                return NotFound();
            }

            var variants = _context.TmtVariants.ToList();
            ViewData["TmtVariantId"] = new SelectList(variants, "Id", "DisplayName");
            ViewData["TmtVariants"] = variants;

            // Build a mapping of variantId -> piecesPerBundle using the first available stock entry (FIFO)
            var piecesMap = new Dictionary<int, int>();
            var stockEntries = await _context.StockEntries.ToListAsync();
            foreach (var v in variants)
            {
                var firstStock = stockEntries.FirstOrDefault(s => s.TmtVariantId == v.Id);
                piecesMap[v.Id] = firstStock?.PiecesPerBundle ?? 0;
            }
            // Auto-lock if > 24 hours
            if (!salesOrder.IsLocked && salesOrder.CreatedAt < DateTime.Now.AddHours(-24))
            {
                salesOrder.IsLocked = true;
                _context.Update(salesOrder);
                await _context.SaveChangesAsync();
            }

            ViewData["PiecesPerBundle"] = piecesMap;
            return View(salesOrder);
        }

        // POST: Sales/LockOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockOrder(int id)
        {
            var salesOrder = await _context.SalesOrders.FindAsync(id);
            if (salesOrder == null) return NotFound();

            salesOrder.IsLocked = true;
            _context.Update(salesOrder);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order locked successfully!";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // POST: Sales/AddItems
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItems(int salesOrderId, List<TmtInventoryApp.Models.SalesItemInput> Items)
        {
            var salesOrder = await _context.SalesOrders
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == salesOrderId);

            if (salesOrder == null) return NotFound();

            if (salesOrder.IsLocked)
            {
                TempData["Error"] = "This order is locked and cannot be modified.";
                return RedirectToAction(nameof(Details), new { id = salesOrderId });
            }

            if (Items == null || !Items.Any())
            {
                TempData["Error"] = "No items submitted.";
                return RedirectToAction(nameof(Details), new { id = salesOrderId });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            var deductionDetails = new List<string>();
            try
            {
                // If items exist, revert stock and clear them (Update mode)
                if (salesOrder.Items != null && salesOrder.Items.Any())
                {
                    foreach (var existingItem in salesOrder.Items)
                    {
                        var stockToRestore = await _context.StockEntries
                            .Where(s => s.TmtVariantId == existingItem.TmtVariantId)
                            .OrderBy(s => s.Id) // FIFO restoration or just dump into first? 
                            // Ideally we should restore to the specific entries we took from, but we don't track that granularity.
                            // Simple approach: Add to the first available stock entry or create a new one if none?
                            // For now, add to the first found entry for that variant.
                            .FirstOrDefaultAsync();

                        if (stockToRestore != null)
                        {
                            stockToRestore.QuantityBundles += existingItem.QuantityBundles;
                            stockToRestore.LoosePieces += existingItem.LoosePieces;
                            // Restore weight if tracked
                            if (stockToRestore.WeightTons.HasValue && existingItem.WeightKg.HasValue)
                            {
                                stockToRestore.WeightTons += (decimal)(existingItem.WeightKg.Value / 1000.0);
                            }
                            _context.Update(stockToRestore);
                        }
                    }
                    _context.SalesOrderItems.RemoveRange(salesOrder.Items);
                    await _context.SaveChangesAsync();
                }

                foreach (var input in Items)
                {
                    if (input == null) continue;
                    if (input.QuantityBundles <= 0 && input.LoosePieces <= 0) continue;

                    var tmtVariantId = input.TmtVariantId;

                    // Check available stock
                    var stockEntries = await _context.StockEntries
                        .Where(s => s.TmtVariantId == tmtVariantId)
                        .OrderBy(s => s.Id)
                        .ToListAsync();

                    int totalAvailableBundles = stockEntries.Sum(s => s.QuantityBundles);
                    int totalAvailableLoosePieces = stockEntries.Sum(s => s.LoosePieces);

                    if (totalAvailableBundles < input.QuantityBundles)
                    {
                        TempData["Error"] = $"Insufficient stock for variant {tmtVariantId}. Available bundles: {totalAvailableBundles}";
                        await transaction.RollbackAsync();
                        return RedirectToAction(nameof(Details), new { id = salesOrderId });
                    }

                    var salesOrderItem = new SalesOrderItem
                    {
                        SalesOrderId = salesOrderId,
                        TmtVariantId = tmtVariantId,
                        QuantityBundles = input.QuantityBundles,
                        LoosePieces = input.LoosePieces,
                        // UnitPrice/SubTotal removed; only storing quantity and weight
                    };

                    // Determine weight: prefer posted weight if provided.
                    // Otherwise, prefer PredefinedBundleWeight if available: (bundles * PredefinedBundleWeight) + (loose * WeightPerRod).
                    if (input.WeightKg.HasValue && input.WeightKg.Value > 0)
                    {
                        salesOrderItem.WeightKg = input.WeightKg.Value;
                    }
                    else
                    {
                        var firstStock = stockEntries.FirstOrDefault();
                        int piecesPerBundle = firstStock?.PiecesPerBundle ?? 0;
                        var variant = await _context.TmtVariants.FindAsync(tmtVariantId);
                        double weightPerRod = variant?.WeightPerRod ?? 0.0;
                        double bundleWeight = variant?.PredefinedBundleWeight ?? 0.0;

                        if (bundleWeight > 0)
                        {
                            // Use predefined bundle weight for bundles plus loose pieces * weight per rod
                            double weight = (input.QuantityBundles * bundleWeight) + (input.LoosePieces * weightPerRod);
                            salesOrderItem.WeightKg = weight > 0 ? weight : (double?)null;
                        }
                        else
                        {
                            int totalPieces = (input.QuantityBundles * piecesPerBundle) + input.LoosePieces;
                            salesOrderItem.WeightKg = totalPieces > 0 ? totalPieces * weightPerRod : (double?)null;
                        }
                    }

                    var variantName = (await _context.TmtVariants.FindAsync(tmtVariantId))?.DisplayName ?? "Unknown";
                    if (salesOrderItem.WeightKg.HasValue || input.QuantityBundles > 0)
                    {
                         deductionDetails.Add($"{variantName}: {salesOrderItem.WeightKg?.ToString("0.##") ?? "0"} KG and {input.QuantityBundles} Bundles");
                    }

                    _context.SalesOrderItems.Add(salesOrderItem);

                    // Deduct stock
                    int remainingBundles = input.QuantityBundles;
                    int remainingLoose = input.LoosePieces;
                    
                    // Calculate total pieces involved in this sale item to determine proportions
                    int refPiecesPerBundle = stockEntries.FirstOrDefault()?.PiecesPerBundle ?? 10; 
                    long totalItemPieces = (long)input.QuantityBundles * refPiecesPerBundle + input.LoosePieces;
                    double totalItemWeightKg = salesOrderItem.WeightKg ?? 0;

                    foreach (var stockEntry in stockEntries)
                    {
                        if (remainingBundles <= 0 && remainingLoose <= 0) break;

                        int bundlesDeducted = 0;
                        int looseDeducted = 0;

                        if (remainingBundles > 0)
                        {
                            if (stockEntry.QuantityBundles >= remainingBundles)
                            {
                                bundlesDeducted = remainingBundles;
                                stockEntry.QuantityBundles -= remainingBundles;
                                remainingBundles = 0;
                            }
                            else
                            {
                                bundlesDeducted = stockEntry.QuantityBundles;
                                remainingBundles -= stockEntry.QuantityBundles;
                                stockEntry.QuantityBundles = 0;
                            }
                        }

                        if (remainingLoose > 0)
                        {
                            if (stockEntry.LoosePieces >= remainingLoose)
                            {
                                looseDeducted = remainingLoose;
                                stockEntry.LoosePieces -= remainingLoose;
                                remainingLoose = 0;
                            }
                            else
                            {
                                looseDeducted = stockEntry.LoosePieces;
                                remainingLoose -= stockEntry.LoosePieces;
                                stockEntry.LoosePieces = 0;
                            }
                        }

                        // Deduct proportional weight if WeightTons is tracked
                        if (stockEntry.WeightTons.HasValue && totalItemPieces > 0)
                        {
                            long piecesDeducted = (long)bundlesDeducted * refPiecesPerBundle + looseDeducted;
                            double weightDeductedKg = totalItemWeightKg * ((double)piecesDeducted / totalItemPieces);
                            decimal weightDeductedTons = (decimal)(weightDeductedKg / 1000.0);
                            stockEntry.WeightTons = stockEntry.WeightTons.Value - weightDeductedTons;
                            if (stockEntry.WeightTons < 0) stockEntry.WeightTons = 0;
                        }

                        _context.Update(stockEntry);
                    }

                    // No monetary totals to update
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var dealer = await _context.Dealers.FindAsync(salesOrder.DealerId);
                await _activityLogger.LogActivityAsync(
                    "Sales",
                    "Updated",
                    $"Sales Order #{salesOrder.Id} items submitted for dealer: {dealer?.Name}",
                    HttpContext.Session.GetString("DisplayName"),
                    "SalesOrder",
                    salesOrder.Id,
                    $"Stock deducted: {string.Join(", ", deductionDetails)}"
                );

                TempData["Success"] = "Stock reduced by " + string.Join(", ", deductionDetails);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Error adding items: " + ex.Message;
                return RedirectToAction(nameof(Details), new { id = salesOrderId });
            }
        }

        // NOTE: The legacy single-item AddItem endpoint was removed; use AddItems (matrix) instead.

        // POST: Sales/CompleteOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var salesOrder = await _context.SalesOrders.FindAsync(id);
            if (salesOrder == null)
            {
                return NotFound();
            }

            salesOrder.Status = OrderStatus.Completed;
            _context.Update(salesOrder);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order completed successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Sales/DeleteItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id, int salesOrderId)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can delete items from a sales order.";
                return RedirectToAction(nameof(Details), new { id = salesOrderId });
            }
            var item = await _context.SalesOrderItems
                .Include(i => i.SalesOrder)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            // Return stock (reverse the deduction)
            var firstStockEntry = await _context.StockEntries
                .FirstOrDefaultAsync(s => s.TmtVariantId == item.TmtVariantId);

            if (firstStockEntry != null)
            {
                firstStockEntry.QuantityBundles += item.QuantityBundles;
                firstStockEntry.LoosePieces += item.LoosePieces;
                // Restore weight
                if (firstStockEntry.WeightTons.HasValue && item.WeightKg.HasValue)
                {
                    firstStockEntry.WeightTons += (decimal)(item.WeightKg.Value / 1000.0);
                }
                _context.Update(firstStockEntry);
            }

            // No monetary totals to update

            var variantName = (await _context.TmtVariants.FindAsync(item.TmtVariantId))?.DisplayName ?? "Unknown";
            
            _context.SalesOrderItems.Remove(item);
            await _context.SaveChangesAsync();

            await _activityLogger.LogActivityAsync(
                "Sales",
                "Deleted",
                $"Sales item deleted from order #{salesOrderId}",
                HttpContext.Session.GetString("DisplayName"),
                "SalesOrderItem",
                id,
                $"Item: {variantName}, Qty: {item.QuantityBundles} bundles, Stock restored"
            );

            TempData["Success"] = "Item removed and stock restored!";
            return RedirectToAction(nameof(Details), new { id = salesOrderId });
        }

        // POST: Sales/UpdateBundleWeights
        [HttpPost]
        public async Task<IActionResult> UpdateBundleWeights([FromBody] List<BundleWeightUpdate> updates)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                return Json(new { success = false, message = "Only Admins can update bundle weights." });
            }
            try
            {
                foreach (var update in updates)
                {
                    var variant = await _context.TmtVariants.FindAsync(update.VariantId);
                    if (variant != null)
                    {
                        variant.PredefinedBundleWeight = update.Weight;
                        variant.UsePredefinedBundleWeight = update.UsePredefinedBundleWeight;
                        _context.Update(variant);
                    }
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class BundleWeightUpdate
        {
            public int VariantId { get; set; }
            public double Weight { get; set; }
            public bool UsePredefinedBundleWeight { get; set; }
        }
    }
}
