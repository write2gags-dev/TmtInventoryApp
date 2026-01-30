using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models;
using TmtInventoryApp.Models.ViewModels;

namespace TmtInventoryApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly InventoryContext _context;

    public HomeController(ILogger<HomeController> logger, InventoryContext context)
    {
        _logger = logger;
        _context = context;
    }

        public IActionResult Welcome()
    {
        return View();
    }

    public async Task<IActionResult> Index()
        {
            var stockEntries = await _context.StockEntries
                .Include(s => s.TmtVariant)
                .Where(s => s.TmtVariant != null && s.TmtVariant.Diameter != 25)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                // Only use manually entered WeightTons (convert to kg)
                TotalWeightKg = stockEntries.Sum(s => s.WeightTons.HasValue ? (double)(s.WeightTons.Value * 1000) : 0),
                TotalBundles = stockEntries.Sum(s => s.QuantityBundles),
                StockSummary = stockEntries
                    .GroupBy(s => s.TmtVariant)
                    .Select(g => new StockSummaryItem
                    {
                        VariantId = g.Key?.Id ?? 0,
                        VariantName = g.Key != null && g.Key.Diameter > 0
                            ? $"{g.Key.Diameter} mm"
                            : g.Key?.DisplayName ?? "Unknown variant",
                        QuantityBundles = g.Sum(s => s.QuantityBundles),
                        // Only use manually entered WeightTons (convert to kg)
                        TotalWeight = g.Sum(s => s.WeightTons.HasValue ? (double)(s.WeightTons.Value * 1000) : 0)
                    })
                    .OrderBy(i => i.VariantId) // Order by Id to match Stock Received sequence
                    .ToList()
            };

            return View(viewModel);
        }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
