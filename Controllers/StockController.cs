using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models;
using TmtInventoryApp.Models.ViewModels;
using TmtInventoryApp.Services;

namespace TmtInventoryApp.Controllers
{
    public class StockController : Controller
    {
        private readonly InventoryContext _context;
        private readonly ActivityLogger _activityLogger;

        public StockController(InventoryContext context, ActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // GET: Stock
        public async Task<IActionResult> Index()
        {
            var inventoryContext = _context.StockEntries
                .Include(s => s.TmtVariant)
                .Where(s => s.TmtVariant != null && s.TmtVariant.Diameter != 25);
            return View(await inventoryContext.ToListAsync());
        }

        // GET: Stock/Create
        public async Task<IActionResult> Create()
        {
            var variants = await _context.TmtVariants.OrderBy(v => v.Id).ToListAsync();
            ViewData["TmtVariants"] = variants;
            return View();
        }

        // POST: Stock/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string? invoiceNumber,
            DateTime? invoiceDate,
            Dictionary<int, int> bundles,
            Dictionary<int, decimal> weights)
        {
            if (bundles == null || !bundles.Any())
            {
                ModelState.AddModelError("", "No stock data provided.");
                var variants = await _context.TmtVariants.OrderBy(v => v.Id).ToListAsync();
                ViewData["TmtVariants"] = variants;
                return View();
            }

            bool hasData = false;

            foreach (var variantId in bundles.Keys)
            {
                int quantity = bundles[variantId];
                decimal weight = weights.ContainsKey(variantId) ? weights[variantId] : 0;

                if (quantity > 0 || weight > 0)
                {
                    hasData = true;
                    
                    // Find existing stock entry for this variant
                    var stockEntry = await _context.StockEntries
                        .FirstOrDefaultAsync(s => s.TmtVariantId == variantId);

                    if (stockEntry != null)
                    {
                        // Update existing stock
                        stockEntry.QuantityBundles += quantity;
                        
                        // Update weight if provided (accumulate)
                        if (stockEntry.WeightTons.HasValue)
                        {
                            stockEntry.WeightTons += weight;
                        }
                        else
                        {
                            stockEntry.WeightTons = weight;
                        }

                        // Update invoice details to latest (or append?) - Overwriting for now as per current model design
                        stockEntry.InvoiceNumber = invoiceNumber;
                        stockEntry.InvoiceDate = invoiceDate;
                        
                        _context.Update(stockEntry);
                    }
                    else
                    {
                        // Create new stock entry
                        stockEntry = new StockEntry
                        {
                            TmtVariantId = variantId,
                            QuantityBundles = quantity,
                            WeightTons = weight,
                            InvoiceNumber = invoiceNumber,
                            InvoiceDate = invoiceDate,
                            PiecesPerBundle = 10, // Default assumption, can be edited later
                            LoosePieces = 0
                        };
                        _context.Add(stockEntry);
                    }
                }
            }

            if (hasData)
            {
                await _context.SaveChangesAsync();
                
                // Log the stock addition
                var stockDetails = new List<string>();
                foreach (var variantId in bundles.Keys)
                {
                    int quantity = bundles[variantId];
                    decimal weight = weights.ContainsKey(variantId) ? weights[variantId] : 0;
                    if (quantity > 0 || weight > 0)
                    {
                        var variant = await _context.TmtVariants.FindAsync(variantId);
                        stockDetails.Add($"{variant?.DisplayName}: {quantity} bundles, {weight} tons");
                    }
                }
                
                await _activityLogger.LogActivityAsync(
                    "Stock",
                    "Created",
                    $"Stock added",
                    HttpContext.Session.GetString("DisplayName"),
                    "StockEntry",
                    null,
                    $"Invoice: {invoiceNumber}, Items: {string.Join(", ", stockDetails)}"
                );
                
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", "Please enter at least one quantity or weight.");
                var variants = await _context.TmtVariants.OrderBy(v => v.Id).ToListAsync();
                ViewData["TmtVariants"] = variants;
                return View();
            }
        }

        // GET: Stock/History
        public async Task<IActionResult> History()
        {
            var cutoffDate = DateTime.Now.AddMonths(-12);
            var logs = await _context.ActivityLogs
                .Where(l => l.ActivityType == "Stock" 
                           && l.Action == "Created" 
                           && l.Timestamp >= cutoffDate)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            var history = new List<StockHistoryViewModel>();

            foreach (var log in logs)
            {
                if (string.IsNullOrEmpty(log.AdditionalDetails)) continue;

                var record = new StockHistoryViewModel
                {
                    Date = log.Timestamp,
                    UserName = log.UserName ?? "Unknown"
                };

                // Parse AdditionalDetails
                // Format: "Invoice: {inv}, Items: {item1}, {item2}..."
                // Item format: "{Variant}: {bundles} bundles, {weight} tons"
                
                try 
                {
                    string details = log.AdditionalDetails;
                    int itemsIndex = details.IndexOf("Items: ");
                    if (itemsIndex != -1)
                    {
                        var invPart = details.Substring(0, itemsIndex).Replace("Invoice: ", "").Trim().TrimEnd(',');
                        record.InvoiceNumber = invPart;

                        var itemsPart = details.Substring(itemsIndex + 7); // Skip "Items: "
                        
                        // Split by ", " but we have to be careful as comma is used within item too
                        // Items end with "tons". So "tons, " is the delimiter between items.
                        var rawItems = itemsPart.Split(new[] { "tons, " }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var rawItem in rawItems)
                        {
                            // "10mm: 50 bundles, 2.5" (last "tons" might be stripped or retained depending on split)
                            // Clean up
                            var itemStr = rawItem.Trim();
                            if (!itemStr.EndsWith("tons")) itemStr += " tons";

                            // Now: "10mm: 50 bundles, 2.5 tons"
                            var parts = itemStr.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                var variantName = parts[0].Trim();
                                var qtyParts = parts[1].Split(new[] { " bundles, " }, StringSplitOptions.RemoveEmptyEntries);
                                if (qtyParts.Length >= 2)
                                {
                                    int bundles = 0;
                                    decimal weight = 0;
                                    
                                    int.TryParse(qtyParts[0].Trim(), out bundles);
                                    var weightStr = qtyParts[1].Replace(" tons", "").Trim();
                                    decimal.TryParse(weightStr, out weight);

                                    record.Items.Add(new StockHistoryItem
                                    {
                                        VariantName = variantName,
                                        Bundles = bundles,
                                        WeightTons = weight
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fallback/Legacy format? Just store raw if parsing fails? 
                        // For now, if no "Items:", we ignore or handle gracefully
                    }
                }
                catch
                {
                    // Parsing failed, maybe malformed log
                    continue; 
                }

                if (record.Items.Any())
                {
                    history.Add(record);
                }
            }

            return View(history);
        }

        // GET: Stock/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stockEntry = await _context.StockEntries.FindAsync(id);
            if (stockEntry == null)
            {
                return NotFound();
            }

            // Check if entry is older than 24 hours for non-admin users
            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin" && stockEntry.CreatedAt < DateTime.Now.AddHours(-24))
            {
                TempData["Error"] = "This stock entry is older than 24 hours and cannot be edited. Please contact the administrator.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["TmtVariantId"] = new SelectList(_context.TmtVariants.ToList(), "Id", "DisplayName", stockEntry.TmtVariantId);
            return View(stockEntry);
        }

        // POST: Stock/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TmtVariantId,QuantityBundles,PiecesPerBundle,LoosePieces,WeightTons,CreatedAt")] StockEntry stockEntry)
        {
            if (id != stockEntry.Id)
            {
                return NotFound();
            }

            // Check if entry is older than 24 hours for non-admin users
            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin" && stockEntry.CreatedAt < DateTime.Now.AddHours(-24))
            {
                TempData["Error"] = "This stock entry is older than 24 hours and cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // If user is not admin, preserve the existing WeightTons value
                    if (userRole != "Admin")
                    {
                        var existingEntry = await _context.StockEntries.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                        if (existingEntry != null)
                        {
                            stockEntry.WeightTons = existingEntry.WeightTons;
                        }
                    }
                    
                    var variant = await _context.TmtVariants.FindAsync(stockEntry.TmtVariantId);
                    
                    _context.Update(stockEntry);
                    await _context.SaveChangesAsync();
                    
                    await _activityLogger.LogActivityAsync(
                        "Stock",
                        "Updated",
                        $"Stock entry updated for {variant?.DisplayName}",
                        HttpContext.Session.GetString("DisplayName"),
                        "StockEntry",
                        stockEntry.Id,
                        userRole == "Admin" 
                            ? $"Bundles: {stockEntry.QuantityBundles}, Loose: {stockEntry.LoosePieces}, Weight: {stockEntry.WeightTons} tons"
                            : $"Bundles: {stockEntry.QuantityBundles}, Loose: {stockEntry.LoosePieces}"
                    );
                    
                    TempData["Success"] = "Stock entry updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StockEntryExists(stockEntry.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["TmtVariantId"] = new SelectList(_context.TmtVariants.ToList(), "Id", "DisplayName", stockEntry.TmtVariantId);
            return View(stockEntry);
        }


        // Delete functionality removed - stock should only be adjusted through proper reconciliation

        private bool StockEntryExists(int id)
        {
            return _context.StockEntries.Any(e => e.Id == id);
        }
    }
}
