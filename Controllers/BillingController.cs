using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models;
using TmtInventoryApp.Models.ViewModels;
using TmtInventoryApp.Services;

namespace TmtInventoryApp.Controllers
{
    public class BillingController : Controller
    {
        private readonly InventoryContext _context;
        private readonly ActivityLogger _activityLogger;

        public BillingController(InventoryContext context, ActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // GET: Billing
        public async Task<IActionResult> Index()
        {
            var billingRecords = await _context.BillingRecords
                .Include(b => b.Dealer)
                .Include(b => b.Items)
                    .ThenInclude(i => i.TmtVariant)
                .OrderByDescending(b => b.BillingDate)
                .ToListAsync();

            return View(billingRecords);
        }

        // GET: Billing/Create
        public IActionResult Create()
        {
            ViewData["DealerId"] = new SelectList(_context.Dealers, "Id", "Name");
            ViewData["TmtVariants"] = _context.TmtVariants.ToList();
            
            return View(new BillingRecord());
        }

        // POST: Billing/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DealerId,BillingDate,InvoiceNumber,Notes,Items")] BillingRecord billingRecord)
        {
            // Remove empty items (guard for null list)
            if (billingRecord.Items != null)
            {
                billingRecord.Items.RemoveAll(i => i.BilledWeightKg == null || i.BilledWeightKg <= 0);
            }

            // Validate virtual stock availability
            if (billingRecord.Items != null)
            {
                var billingGroups = billingRecord.Items.GroupBy(i => i.TmtVariantId);
                foreach (var group in billingGroups)
                {
                    var variantId = group.Key;
                    var totalBilledWeight = group.Sum(i => i.BilledWeightKg ?? 0);
                    
                    var virtualStock = await _context.VirtualStocks
                        .AsNoTracking()
                        .FirstOrDefaultAsync(vs => vs.TmtVariantId == variantId);
                        
                    if (virtualStock == null || virtualStock.AvailableWeightKg < totalBilledWeight)
                    {
                        var variant = await _context.TmtVariants.FindAsync(variantId);
                        ModelState.AddModelError("", $"Insufficient As per Kolkata Stock (Virtual) for {variant?.DisplayName}. Available: {virtualStock?.AvailableWeightKg ?? 0} kg, Required: {totalBilledWeight} kg");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(billingRecord);
                await _context.SaveChangesAsync();

                // Deduct from virtual stock
                await DeductFromVirtualStock(billingRecord);

                var dealer = await _context.Dealers.FindAsync(billingRecord.DealerId);
                await _activityLogger.LogActivityAsync(
                    "Billing",
                    "Created",
                    $"New billing record created for dealer: {dealer?.Name}",
                    HttpContext.Session.GetString("DisplayName"),
                    "BillingRecord",
                    billingRecord.Id,
                    $"Invoice: {billingRecord.InvoiceNumber}, Total Weight: {billingRecord.TotalWeight} kg"
                );

                return RedirectToAction(nameof(Index));
            }
            ViewData["DealerId"] = new SelectList(_context.Dealers, "Id", "Name", billingRecord.DealerId);
            ViewData["TmtVariants"] = _context.TmtVariants.ToList();
            return View(billingRecord);
        }

        // GET: Billing/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can edit billing records.";
                return RedirectToAction(nameof(Index));
            }

            if (id == null)
            {
                return NotFound();
            }

            var billingRecord = await _context.BillingRecords
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (billingRecord == null)
            {
                return NotFound();
            }

            ViewData["DealerId"] = new SelectList(_context.Dealers, "Id", "Name", billingRecord.DealerId);
            ViewData["TmtVariants"] = _context.TmtVariants.ToList();
            return View(billingRecord);
        }

        // POST: Billing/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DealerId,BillingDate,InvoiceNumber,Notes,Items")] BillingRecord billingRecord)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can edit billing records.";
                return RedirectToAction(nameof(Index));
            }

            if (id != billingRecord.Id)
            {
                return NotFound();
            }

            // Remove empty items (guard for null list)
            if (billingRecord.Items != null)
            {
                billingRecord.Items.RemoveAll(i => i.BilledWeightKg == null || i.BilledWeightKg <= 0);
            }

            // Validate virtual stock availability
            if (billingRecord.Items != null)
            {
                var billingGroups = billingRecord.Items.GroupBy(i => i.TmtVariantId);
                foreach (var group in billingGroups)
                {
                    var variantId = group.Key;
                    var newTotalBilledWeight = group.Sum(i => i.BilledWeightKg ?? 0);
                    
                    // Get current available stock
                    var virtualStock = await _context.VirtualStocks
                        .AsNoTracking()
                        .FirstOrDefaultAsync(vs => vs.TmtVariantId == variantId);
                    var currentAvailable = virtualStock?.AvailableWeightKg ?? 0;
                    
                    // Add back what is currently billed in this record (since we are replacing it)
                    var previousBilledWeight = await _context.BillingRecordItems
                        .Where(i => i.BillingRecordId == id && i.TmtVariantId == variantId)
                        .SumAsync(i => i.BilledWeightKg) ?? 0;
                        
                    if ((currentAvailable + previousBilledWeight) < newTotalBilledWeight)
                    {
                        var variant = await _context.TmtVariants.FindAsync(variantId);
                        ModelState.AddModelError("", $"Insufficient As per Kolkata Stock (Virtual) for {variant?.DisplayName}. Available: {(currentAvailable + previousBilledWeight)} kg, Required: {newTotalBilledWeight} kg");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update parent
                    var existingRecord = await _context.BillingRecords
                        .Include(b => b.Items)
                        .FirstOrDefaultAsync(b => b.Id == id);

                    if (existingRecord == null)
                    {
                        return NotFound();
                    }

                    // Restore virtual stock from old billing items before updating
                    await RestoreVirtualStock(existingRecord);

                    existingRecord.DealerId = billingRecord.DealerId;
                    existingRecord.BillingDate = billingRecord.BillingDate;
                    existingRecord.InvoiceNumber = billingRecord.InvoiceNumber;
                    existingRecord.Notes = billingRecord.Notes;

                    // Update items: remove existing and add new items explicitly so EF tracking is correct
                    if (existingRecord.Items != null && existingRecord.Items.Any())
                    {
                        _context.BillingRecordItems.RemoveRange(existingRecord.Items);
                    }

                    existingRecord.Items = new List<BillingRecordItem>();
                    if (billingRecord.Items != null)
                    {
                        foreach (var item in billingRecord.Items)
                        {
                            // ensure FK is set
                            item.BillingRecordId = existingRecord.Id;
                            existingRecord.Items.Add(item);
                            _context.BillingRecordItems.Add(item);
                        }
                    }

                    await _context.SaveChangesAsync();

                    // Deduct from virtual stock with new billing items
                    await DeductFromVirtualStock(existingRecord);

                    await _activityLogger.LogActivityAsync(
                        "Billing",
                        "Updated",
                        $"Billing record updated for dealer: {existingRecord.Dealer?.Name}",
                        HttpContext.Session.GetString("DisplayName"),
                        "BillingRecord",
                        existingRecord.Id,
                        $"Invoice: {existingRecord.InvoiceNumber}, New Total Weight: {existingRecord.TotalWeight} kg"
                    );

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BillingRecordExists(billingRecord.Id))
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
            ViewData["DealerId"] = new SelectList(_context.Dealers, "Id", "Name", billingRecord.DealerId);
            ViewData["TmtVariants"] = _context.TmtVariants.ToList();
            return View(billingRecord);
        }

        // GET: Billing/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can delete billing records.";
                return RedirectToAction(nameof(Index));
            }

            if (id == null)
            {
                return NotFound();
            }

            var billingRecord = await _context.BillingRecords
                .Include(b => b.Dealer)
                .Include(b => b.Items)
                    .ThenInclude(i => i.TmtVariant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (billingRecord == null)
            {
                return NotFound();
            }

            return View(billingRecord);
        }

        // POST: Billing/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can delete billing records.";
                return RedirectToAction(nameof(Index));
            }

            var billingRecord = await _context.BillingRecords
                .Include(b => b.Items)
                .Include(b => b.Dealer)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (billingRecord != null)
            {
                var invoiceNum = billingRecord.InvoiceNumber;
                var dealerName = billingRecord.Dealer?.Name;

                // Restore virtual stock before deleting
                await RestoreVirtualStock(billingRecord);

                _context.BillingRecords.Remove(billingRecord);
                await _context.SaveChangesAsync();

                await _activityLogger.LogActivityAsync(
                    "Billing",
                    "Deleted",
                    $"Billing record deleted: Invoice {invoiceNum}",
                    HttpContext.Session.GetString("DisplayName"),
                    "BillingRecord",
                    id,
                    $"Dealer: {dealerName}"
                );
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Billing/DealerStatus
        public async Task<IActionResult> DealerStatus()
        {
            var dealers = await _context.Dealers.ToListAsync();
            var summaries = new List<DealerBillingSummary>();

            foreach (var dealer in dealers)
            {
                var summary = new DealerBillingSummary
                {
                    Dealer = dealer
                };

                // Get all TMT variants involved
                var variantIds = await _context.SalesOrderItems
                    .Where(i => i.SalesOrder!.DealerId == dealer.Id)
                    .Select(i => i.TmtVariantId)
                    .Union(_context.BillingRecordItems
                        .Where(i => i.BillingRecord!.DealerId == dealer.Id)
                        .Select(i => i.TmtVariantId))
                    .Distinct()
                    .ToListAsync();

                foreach (var variantId in variantIds)
                {
                    var variant = await _context.TmtVariants.FindAsync(variantId);
                    if (variant == null) continue;

                    // Calculate total sold weight
                    var soldItems = await _context.SalesOrderItems
                        .Where(i => i.SalesOrder!.DealerId == dealer.Id && 
                                   i.TmtVariantId == variantId)
                        .ToListAsync();

                    decimal totalSoldKg = 0;
                    foreach (var item in soldItems)
                    {
                        // Use the actual WeightKg stored in the sales order item
                        // This accurately reflects what was sold (including predefined bundle weights)
                        totalSoldKg += (decimal)(item.WeightKg ?? 0);
                    }

                    // Calculate total billed weight
                    var totalBilledKg = await _context.BillingRecordItems
                        .Where(i => i.BillingRecord!.DealerId == dealer.Id && i.TmtVariantId == variantId)
                        .SumAsync(i => i.BilledWeightKg) ?? 0;

                    summary.VariantStatus.Add(new DealerBillingStatus
                    {
                        DealerId = dealer.Id,
                        DealerName = dealer.Name,
                        TmtVariantId = variantId,
                        TmtVariantName = variant.DisplayName,
                        TotalSoldWeightKg = totalSoldKg,
                        TotalBilledWeightKg = totalBilledKg
                    });

                    summary.TotalSoldWeightKg += totalSoldKg;
                    summary.TotalBilledWeightKg += totalBilledKg;
                }

                if (summary.VariantStatus.Any())
                {
                    summaries.Add(summary);
                }
            }

            return View(summaries);
        }

        // GET: Billing/DealerDetails/5
        public async Task<IActionResult> DealerDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dealer = await _context.Dealers.FindAsync(id);
            if (dealer == null)
            {
                return NotFound();
            }

            var summary = new DealerBillingSummary
            {
                Dealer = dealer
            };

            // Get all TMT variants involved
            var variantIds = await _context.SalesOrderItems
                .Where(i => i.SalesOrder!.DealerId == dealer.Id)
                .Select(i => i.TmtVariantId)
                .Union(_context.BillingRecordItems
                    .Where(i => i.BillingRecord!.DealerId == dealer.Id)
                    .Select(i => i.TmtVariantId))
                .Distinct()
                .ToListAsync();

            foreach (var variantId in variantIds)
            {
                var variant = await _context.TmtVariants.FindAsync(variantId);
                if (variant == null) continue;

                // Calculate total sold weight
                var soldItems = await _context.SalesOrderItems
                    .Where(i => i.SalesOrder!.DealerId == dealer.Id && 
                               i.TmtVariantId == variantId)
                    .ToListAsync();

                decimal totalSoldKg = 0;
                foreach (var item in soldItems)
                {
                    // Use the actual WeightKg stored in the sales order item
                    // This accurately reflects what was sold (including predefined bundle weights)
                    totalSoldKg += (decimal)(item.WeightKg ?? 0);
                }

                // Calculate total billed weight
                var totalBilledKg = await _context.BillingRecordItems
                    .Where(i => i.BillingRecord!.DealerId == dealer.Id && i.TmtVariantId == variantId)
                    .SumAsync(i => i.BilledWeightKg) ?? 0;

                summary.VariantStatus.Add(new DealerBillingStatus
                {
                    DealerId = dealer.Id,
                    DealerName = dealer.Name,
                    TmtVariantId = variantId,
                    TmtVariantName = variant.DisplayName,
                    TotalSoldWeightKg = totalSoldKg,
                    TotalBilledWeightKg = totalBilledKg
                });

                summary.TotalSoldWeightKg += totalSoldKg;
                summary.TotalBilledWeightKg += totalBilledKg;
            }

            // Get billing history
            ViewBag.BillingHistory = await _context.BillingRecords
                .Include(b => b.Items)
                    .ThenInclude(i => i.TmtVariant)
                .Where(b => b.DealerId == dealer.Id)
                .OrderByDescending(b => b.BillingDate)
                .ToListAsync();

            return View(summary);
        }

        private bool BillingRecordExists(int id)
        {
            return _context.BillingRecords.Any(e => e.Id == id);
        }

        /// <summary>
        /// Deducts billed weight from virtual stock (Shymasteel stock)
        /// </summary>
        private async Task DeductFromVirtualStock(BillingRecord billingRecord)
        {
            if (billingRecord.Items == null || !billingRecord.Items.Any())
                return;

            foreach (var item in billingRecord.Items)
            {
                if (item.BilledWeightKg == null || item.BilledWeightKg <= 0)
                    continue;

                // Get or create virtual stock entry for this variant
                var virtualStock = await _context.VirtualStocks
                    .FirstOrDefaultAsync(vs => vs.TmtVariantId == item.TmtVariantId);

                if (virtualStock == null)
                {
                    // Create new virtual stock entry with zero balance
                    virtualStock = new VirtualStock
                    {
                        TmtVariantId = item.TmtVariantId,
                        AvailableWeightKg = 0,
                        LastUpdated = DateTime.Now
                    };
                    _context.VirtualStocks.Add(virtualStock);
                    await _context.SaveChangesAsync();
                }

                // Deduct from virtual stock
                virtualStock.AvailableWeightKg -= item.BilledWeightKg.Value;
                virtualStock.LastUpdated = DateTime.Now;

                // Record transaction
                var transaction = new VirtualStockTransaction
                {
                    VirtualStockId = virtualStock.Id,
                    TransactionType = VirtualStockTransactionType.Deduction,
                    WeightKg = item.BilledWeightKg.Value,
                    ReferenceType = "BillingRecord",
                    ReferenceId = billingRecord.Id,
                    Notes = $"Billing deduction for {billingRecord.Dealer?.Name ?? "Dealer"}",
                    TransactionDate = DateTime.Now
                };
                _context.VirtualStockTransactions.Add(transaction);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Restores virtual stock when editing/deleting billing records
        /// </summary>
        private async Task RestoreVirtualStock(BillingRecord billingRecord)
        {
            if (billingRecord.Items == null || !billingRecord.Items.Any())
                return;

            foreach (var item in billingRecord.Items)
            {
                if (item.BilledWeightKg == null || item.BilledWeightKg <= 0)
                    continue;

                var virtualStock = await _context.VirtualStocks
                    .FirstOrDefaultAsync(vs => vs.TmtVariantId == item.TmtVariantId);

                if (virtualStock != null)
                {
                    // Restore to virtual stock
                    virtualStock.AvailableWeightKg += item.BilledWeightKg.Value;
                    virtualStock.LastUpdated = DateTime.Now;

                    // Record transaction
                    var transaction = new VirtualStockTransaction
                    {
                        VirtualStockId = virtualStock.Id,
                        TransactionType = VirtualStockTransactionType.Addition,
                        WeightKg = item.BilledWeightKg.Value,
                        ReferenceType = "BillingRecord",
                        ReferenceId = billingRecord.Id,
                        Notes = $"Billing edit/delete restoration for {billingRecord.Dealer?.Name ?? "Dealer"}",
                        TransactionDate = DateTime.Now
                    };
                    _context.VirtualStockTransactions.Add(transaction);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
