using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models.DealerPortal;
using System.Security.Cryptography;
using System.Text;

namespace TmtInventoryApp.Controllers.DealerPortal
{
    [Route("DealerPortal/[controller]")]
    public class DealerAuthController : Controller
    {
        private readonly InventoryContext _context;
        private readonly TmtInventoryApp.Services.AuthService _authService;

        public DealerAuthController(InventoryContext context, TmtInventoryApp.Services.AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        // GET: DealerPortal/DealerAuth/Login
        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }

        // POST: DealerPortal/DealerAuth/Login
        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            var passwordHash = HashPassword(password);
            var dealerUser = await _context.DealerUsers
                .Include(du => du.Dealer)
                .FirstOrDefaultAsync(du => du.Username == username && du.PasswordHash == passwordHash && du.IsActive);

            if (dealerUser != null)
            {
                // Normal Dealer Login
                dealerUser.LastLogin = DateTime.Now;
                await _context.SaveChangesAsync();

                HttpContext.Session.SetInt32("DealerUserId", dealerUser.Id);
                HttpContext.Session.SetInt32("DealerId", dealerUser.DealerId);
                HttpContext.Session.SetString("DealerUsername", dealerUser.Username);
                HttpContext.Session.SetString("DealerName", dealerUser.Dealer?.Name ?? "Dealer");
                return RedirectToAction("Index", "DealerDashboard");
            }

            // check if it's an admin from the main system
            var systemUser = _authService.Authenticate(username, password);
            if (systemUser != null && systemUser.Role == TmtInventoryApp.Models.UserRoles.Admin)
            {
                // Admin login to Dealer Portal - let them see the first dealer's data
                var firstDealer = await _context.Dealers.OrderBy(d => d.Id).FirstOrDefaultAsync();
                if (firstDealer != null)
                {
                    HttpContext.Session.SetInt32("DealerUserId", -1); // special ID for admin
                    HttpContext.Session.SetInt32("DealerId", firstDealer.Id);
                    HttpContext.Session.SetString("DealerUsername", systemUser.Username);
                    HttpContext.Session.SetString("DealerName", $"{firstDealer.Name} (Admin View)");
                    return RedirectToAction("Index", "DealerDashboard");
                }
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        // GET: DealerPortal/DealerAuth/Logout
        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Welcome", "Home");
        }

        // Helper method to hash passwords
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
