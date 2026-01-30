using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models.DealerPortal;

namespace TmtInventoryApp.Controllers.DealerPortal
{
    [Route("DealerPortal/[controller]")]
    public class DealerDashboardController : Controller
    {
        private readonly InventoryContext _context;

        public DealerDashboardController(InventoryContext context)
        {
            _context = context;
        }

        // GET: DealerPortal/DealerDashboard
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            // Get dashboard statistics
            var totalCustomers = await _context.EndCustomers
                .Where(c => c.DealerId == dealerId.Value && c.IsActive)
                .CountAsync();

            var totalOutstanding = await _context.EndCustomers
                .Where(c => c.DealerId == dealerId.Value && c.IsActive)
                .SumAsync(c => c.CurrentOutstanding);

            var totalInventoryValue = await _context.DealerInventoryItems
                .Where(i => i.DealerId == dealerId.Value)
                .SumAsync(i => i.CurrentQuantity * (i.UnitPrice ?? 0));

            var lowStockItems = await _context.DealerInventoryItems
                .Include(i => i.ProductBrand)
                    .ThenInclude(pb => pb.ProductCategory)
                .Where(i => i.DealerId == dealerId.Value && 
                           i.MinimumStockLevel.HasValue && 
                           i.CurrentQuantity <= i.MinimumStockLevel.Value)
                .ToListAsync();

            var recentSales = await _context.CustomerSales
                .Include(s => s.EndCustomer)
                .Where(s => s.DealerId == dealerId.Value)
                .OrderByDescending(s => s.SaleDate)
                .Take(10)
                .ToListAsync();

            var pendingPayments = await _context.CustomerSales
                .Include(s => s.EndCustomer)
                .Where(s => s.DealerId == dealerId.Value && s.PaymentStatus != PaymentStatus.Paid)
                .OrderBy(s => s.SaleDate)
                .Take(10)
                .ToListAsync();

            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalOutstanding = totalOutstanding;
            ViewBag.TotalInventoryValue = totalInventoryValue;
            ViewBag.LowStockItems = lowStockItems;
            ViewBag.RecentSales = recentSales;
            ViewBag.PendingPayments = pendingPayments;
            ViewBag.DealerName = HttpContext.Session.GetString("DealerName");

            return View();
        }
    }
}
