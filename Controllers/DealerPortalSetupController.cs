using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Services;

namespace TmtInventoryApp.Controllers
{
    /// <summary>
    /// Admin controller for setting up dealer portal users
    /// This should be protected and only accessible to administrators
    /// </summary>
    public class DealerPortalSetupController : Controller
    {
        private readonly InventoryContext _context;
        private readonly DealerPortalSeeder _seeder;

        public DealerPortalSetupController(InventoryContext context, DealerPortalSeeder seeder)
        {
            _context = context;
            _seeder = seeder;
        }

        // GET: DealerPortalSetup
        public async Task<IActionResult> Index()
        {
            // Check if user is admin (you may want to add proper authorization)
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var dealers = await _context.Dealers
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Category,
                    HasPortalAccess = _context.DealerUsers.Any(du => du.DealerId == d.Id)
                })
                .ToListAsync();

            ViewBag.Dealers = dealers;
            return View();
        }

        // POST: DealerPortalSetup/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(int dealerId, string username, string password)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                await _seeder.CreateDealerUserAsync(dealerId, username, password);
                TempData["Success"] = $"Dealer user '{username}' created successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: DealerPortalSetup/ViewUsers
        public async Task<IActionResult> ViewUsers()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var dealerUsers = await _context.DealerUsers
                .Include(du => du.Dealer)
                .OrderBy(du => du.Dealer.Name)
                .ToListAsync();

            return View(dealerUsers);
        }

        // POST: DealerPortalSetup/ToggleStatus/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var dealerUser = await _context.DealerUsers.FindAsync(id);
            if (dealerUser != null)
            {
                dealerUser.IsActive = !dealerUser.IsActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"User status updated to {(dealerUser.IsActive ? "Active" : "Inactive")}.";
            }

            return RedirectToAction("ViewUsers");
        }
    }
}
