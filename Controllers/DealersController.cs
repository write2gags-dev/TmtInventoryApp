using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models;
using TmtInventoryApp.Services;

namespace TmtInventoryApp.Controllers
{
    public class DealersController : Controller
    {
        private readonly InventoryContext _context;
        private readonly ActivityLogger _activityLogger;

        public DealersController(InventoryContext context, ActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // GET: Dealers
        public async Task<IActionResult> Index()
        {
            return View(await _context.Dealers.ToListAsync());
        }

        // GET: Dealers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dealer = await _context.Dealers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dealer == null)
            {
                return NotFound();
            }

            return View(dealer);
        }

        // GET: Dealers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Dealers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Category,ContactPerson,PhoneNumber,Address,Gstin")] Dealer dealer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(dealer);
                await _context.SaveChangesAsync();
                
                await _activityLogger.LogActivityAsync(
                    "Dealer",
                    "Created",
                    $"New dealer added: {dealer.Name}",
                    HttpContext.Session.GetString("DisplayName"),
                    "Dealer",
                    dealer.Id,
                    $"Category: {dealer.Category}, Contact: {dealer.ContactPerson}, Phone: {dealer.PhoneNumber}"
                );
                
                return RedirectToAction(nameof(Index));
            }
            return View(dealer);
        }

        // GET: Dealers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can edit dealers.";
                return RedirectToAction(nameof(Index));
            }

            if (id == null)
            {
                return NotFound();
            }

            var dealer = await _context.Dealers.FindAsync(id);
            if (dealer == null)
            {
                return NotFound();
            }
            return View(dealer);
        }

        // POST: Dealers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Category,ContactPerson,PhoneNumber,Address,Gstin")] Dealer dealer)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can edit dealers.";
                return RedirectToAction(nameof(Index));
            }

            if (id != dealer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dealer);
                    await _context.SaveChangesAsync();
                    
                    await _activityLogger.LogActivityAsync(
                        "Dealer",
                        "Updated",
                        $"Dealer updated: {dealer.Name}",
                        HttpContext.Session.GetString("DisplayName"),
                        "Dealer",
                        dealer.Id,
                        $"Category: {dealer.Category}"
                    );
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DealerExists(dealer.Id))
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
            return View(dealer);
        }

        // GET: Dealers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can delete dealers.";
                return RedirectToAction(nameof(Index));
            }

            if (id == null)
            {
                return NotFound();
            }

            var dealer = await _context.Dealers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dealer == null)
            {
                return NotFound();
            }

            return View(dealer);
        }

        // POST: Dealers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can delete dealers.";
                return RedirectToAction(nameof(Index));
            }

            var dealer = await _context.Dealers.FindAsync(id);
            if (dealer != null)
            {
                var dealerName = dealer.Name;
                var dealerCategory = dealer.Category;
                
                _context.Dealers.Remove(dealer);
                await _context.SaveChangesAsync();
                
                await _activityLogger.LogActivityAsync(
                    "Dealer",
                    "Deleted",
                    $"Dealer deleted: {dealerName}",
                    HttpContext.Session.GetString("DisplayName"),
                    "Dealer",
                    id,
                    $"Category: {dealerCategory}"
                );
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DealerExists(int id)
        {
            return _context.Dealers.Any(e => e.Id == id);
        }
    }
}
