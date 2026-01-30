using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models;
using TmtInventoryApp.Models.ViewModels;
using TmtInventoryApp.Services;

namespace TmtInventoryApp.Controllers
{
    public class OpeningBalanceController : Controller
    {
        private readonly InventoryContext _context;
        private readonly ActivityLogger _activityLogger;

        public OpeningBalanceController(InventoryContext context, ActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // GET: OpeningBalance
        public async Task<IActionResult> Index()
        {
            var balances = await _context.OpeningDealerBalances
                .Include(o => o.Dealer)
                .Include(o => o.TmtVariant)
                .OrderBy(o => o.Dealer!.Name)
                .ThenBy(o => o.TmtVariant!.Id)
                .ToListAsync();

            return View(balances);
        }

        // GET: OpeningBalance/Create
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can create opening balances.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["DealerId"] = new SelectList(_context.Dealers.OrderBy(d => d.Name), "Id", "Name");
            ViewData["TmtVariantId"] = new SelectList(_context.TmtVariants.OrderBy(v => v.Id), "Id", "DisplayName");
            return View();
        }

        // POST: OpeningBalance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DealerId,TmtVariantId,OpeningBalanceKg,AsOfDate,Notes")] OpeningDealerBalance openingBalance)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can create opening balances.";
                return RedirectToAction(nameof(Index));
            }
            if (ModelState.IsValid)
            {
                // Check if opening balance already exists for this dealer-variant combination
                var existing = await _context.OpeningDealerBalances
                    .FirstOrDefaultAsync(o => o.DealerId == openingBalance.DealerId && 
                                            o.TmtVariantId == openingBalance.TmtVariantId);

                if (existing != null)
                {
                    ModelState.AddModelError("", "Opening balance already exists for this dealer and variant. Please edit the existing entry.");
                    ViewData["DealerId"] = new SelectList(_context.Dealers.OrderBy(d => d.Name), "Id", "Name", openingBalance.DealerId);
                    ViewData["TmtVariantId"] = new SelectList(_context.TmtVariants.OrderBy(v => v.Id), "Id", "DisplayName", openingBalance.TmtVariantId);
                    return View(openingBalance);
                }

                openingBalance.CreatedDate = DateTime.Now;
                _context.Add(openingBalance);
                await _context.SaveChangesAsync();

                var dealer = await _context.Dealers.FindAsync(openingBalance.DealerId);
                var variant = await _context.TmtVariants.FindAsync(openingBalance.TmtVariantId);
                await _activityLogger.LogActivityAsync(
                    "OpeningBalance",
                    "Created",
                    $"Opening balance set for {dealer?.Name} - {variant?.DisplayName}",
                    HttpContext.Session.GetString("DisplayName"),
                    "OpeningDealerBalance",
                    openingBalance.Id,
                    $"Weight: {openingBalance.OpeningBalanceKg} kg, As of: {openingBalance.AsOfDate:yyyy-MM-dd}"
                );

                TempData["SuccessMessage"] = "Opening balance created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["DealerId"] = new SelectList(_context.Dealers.OrderBy(d => d.Name), "Id", "Name", openingBalance.DealerId);
            ViewData["TmtVariantId"] = new SelectList(_context.TmtVariants.OrderBy(v => v.Id), "Id", "DisplayName", openingBalance.TmtVariantId);
            return View(openingBalance);
        }

        // GET: OpeningBalance/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can edit opening balances.";
                return RedirectToAction(nameof(Index));
            }

            if (id == null)
            {
                return NotFound();
            }

            var openingBalance = await _context.OpeningDealerBalances.FindAsync(id);
            if (openingBalance == null)
            {
                return NotFound();
            }

            ViewData["DealerId"] = new SelectList(_context.Dealers.OrderBy(d => d.Name), "Id", "Name", openingBalance.DealerId);
            ViewData["TmtVariantId"] = new SelectList(_context.TmtVariants.OrderBy(v => v.Id), "Id", "DisplayName", openingBalance.TmtVariantId);
            return View(openingBalance);
        }

        // POST: OpeningBalance/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DealerId,TmtVariantId,OpeningBalanceKg,AsOfDate,Notes,CreatedDate")] OpeningDealerBalance openingBalance)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can edit opening balances.";
                return RedirectToAction(nameof(Index));
            }

            if (id != openingBalance.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    openingBalance.LastModified = DateTime.Now;
                    _context.Update(openingBalance);
                    await _context.SaveChangesAsync();

                    var dealer = await _context.Dealers.FindAsync(openingBalance.DealerId);
                    var variant = await _context.TmtVariants.FindAsync(openingBalance.TmtVariantId);
                    await _activityLogger.LogActivityAsync(
                        "OpeningBalance",
                        "Updated",
                        $"Opening balance updated for {dealer?.Name} - {variant?.DisplayName}",
                        HttpContext.Session.GetString("DisplayName"),
                        "OpeningDealerBalance",
                        openingBalance.Id,
                        $"New Weight: {openingBalance.OpeningBalanceKg} kg"
                    );
                    TempData["SuccessMessage"] = "Opening balance updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OpeningBalanceExists(openingBalance.Id))
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

            ViewData["DealerId"] = new SelectList(_context.Dealers.OrderBy(d => d.Name), "Id", "Name", openingBalance.DealerId);
            ViewData["TmtVariantId"] = new SelectList(_context.TmtVariants.OrderBy(v => v.Id), "Id", "DisplayName", openingBalance.TmtVariantId);
            return View(openingBalance);
        }

        // GET: OpeningBalance/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can delete opening balances.";
                return RedirectToAction(nameof(Index));
            }

            if (id == null)
            {
                return NotFound();
            }

            var openingBalance = await _context.OpeningDealerBalances
                .Include(o => o.Dealer)
                .Include(o => o.TmtVariant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (openingBalance == null)
            {
                return NotFound();
            }

            return View(openingBalance);
        }

        // POST: OpeningBalance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can delete opening balances.";
                return RedirectToAction(nameof(Index));
            }
            var openingBalance = await _context.OpeningDealerBalances.FindAsync(id);
            if (openingBalance != null)
            {
                var dealer = await _context.Dealers.FindAsync(openingBalance.DealerId);
                var variant = await _context.TmtVariants.FindAsync(openingBalance.TmtVariantId);
                var weight = openingBalance.OpeningBalanceKg;

                _context.OpeningDealerBalances.Remove(openingBalance);
                await _context.SaveChangesAsync();

                await _activityLogger.LogActivityAsync(
                    "OpeningBalance",
                    "Deleted",
                    $"Opening balance removed for {dealer?.Name} - {variant?.DisplayName}",
                    HttpContext.Session.GetString("DisplayName"),
                    "OpeningDealerBalance",
                    id,
                    $"Weight was: {weight} kg"
                );
                TempData["SuccessMessage"] = "Opening balance deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: OpeningBalance/BulkEntry
        public async Task<IActionResult> BulkEntry(int? dealerId)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can use bulk entry.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["Dealers"] = new SelectList(await _context.Dealers.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
            
            if (dealerId == null)
            {
                return View(new BulkOpeningBalanceViewModel());
            }

            var dealer = await _context.Dealers.FindAsync(dealerId);
            if (dealer == null)
            {
                return NotFound();
            }

            var allVariants = await _context.TmtVariants.OrderBy(v => v.Id).ToListAsync();
            var existingBalances = await _context.OpeningDealerBalances
                .Where(ob => ob.DealerId == dealerId)
                .ToListAsync();

            var model = new BulkOpeningBalanceViewModel
            {
                DealerId = dealerId.Value,
                DealerName = dealer.Name,
                AsOfDate = DateTime.Today,
                Variants = allVariants.Select(v =>
                {
                    var existing = existingBalances.FirstOrDefault(eb => eb.TmtVariantId == v.Id);
                    return new VariantBalanceEntry
                    {
                        TmtVariantId = v.Id,
                        VariantName = v.DisplayName,
                        OpeningBalanceKg = existing?.OpeningBalanceKg ?? 0,
                        Notes = existing?.Notes,
                        ExistingId = existing?.Id
                    };
                }).ToList()
            };

            return View(model);
        }

        // POST: OpeningBalance/BulkEntry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkEntry(BulkOpeningBalanceViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can use bulk entry.";
                return RedirectToAction(nameof(Index));
            }
            if (ModelState.IsValid)
            {
                var dealer = await _context.Dealers.FindAsync(model.DealerId);
                if (dealer == null)
                {
                    return NotFound();
                }

                int addedCount = 0;
                int updatedCount = 0;
                bool hasChanges = false;

                foreach (var variant in model.Variants.Where(v => v.OpeningBalanceKg != 0)) // Only process non-zero entries
                {
                    if (variant.ExistingId.HasValue)
                    {
                        // Update existing
                        var existing = await _context.OpeningDealerBalances.FindAsync(variant.ExistingId.Value);
                        if (existing != null)
                        {
                            existing.OpeningBalanceKg = variant.OpeningBalanceKg;
                            existing.AsOfDate = model.AsOfDate;
                            existing.Notes = variant.Notes;
                            existing.LastModified = DateTime.Now;
                            _context.Update(existing);
                            updatedCount++;
                            hasChanges = true;
                        }
                    }
                    else
                    {
                        // Create new
                        var newBalance = new OpeningDealerBalance
                        {
                            DealerId = model.DealerId,
                            TmtVariantId = variant.TmtVariantId,
                            OpeningBalanceKg = variant.OpeningBalanceKg,
                            AsOfDate = model.AsOfDate,
                            Notes = variant.Notes,
                            CreatedDate = DateTime.Now
                        };
                        _context.Add(newBalance);
                        addedCount++;
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    await _context.SaveChangesAsync();
                    await _activityLogger.LogActivityAsync(
                        "OpeningBalance",
                        "BulkEntry",
                        $"Bulk opening balances updated for dealer: {dealer.Name}",
                        HttpContext.Session.GetString("DisplayName"),
                        "Dealer",
                        model.DealerId,
                        $"Updated balances for multiple variants. Added: {addedCount}, Updated: {updatedCount}"
                    );
                    TempData["SuccessMessage"] = $"Successfully saved! {addedCount} added, {updatedCount} updated for {dealer.Name}.";
                }
                else
                {
                    TempData["InfoMessage"] = "No changes were made to the opening balances.";
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["Dealers"] = new SelectList(await _context.Dealers.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", model.DealerId);
            return View(model);
        }

        private bool OpeningBalanceExists(int id)
        {
            return _context.OpeningDealerBalances.Any(e => e.Id == id);
        }
    }
}
