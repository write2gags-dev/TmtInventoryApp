using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models.DealerPortal;

namespace TmtInventoryApp.Controllers.DealerPortal
{
    [Route("DealerPortal/[controller]")]
    public class ProductManagementController : Controller
    {
        private readonly InventoryContext _context;

        public ProductManagementController(InventoryContext context)
        {
            _context = context;
        }

        // GET: DealerPortal/ProductManagement
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var categories = await _context.ProductCategories
                .Include(c => c.Brands)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        // GET: DealerPortal/ProductManagement/Brands/{categoryId}
        [HttpGet("Brands/{categoryId}")]
        public async Task<IActionResult> Brands(int categoryId)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var category = await _context.ProductCategories
                .Include(c => c.Brands)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
            {
                return NotFound();
            }

            ViewBag.Category = category;
            return View(category.Brands);
        }

        // GET: DealerPortal/ProductManagement/CreateBrand/{categoryId}
        [HttpGet("CreateBrand/{categoryId}")]
        public async Task<IActionResult> CreateBrand(int categoryId)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var category = await _context.ProductCategories.FindAsync(categoryId);
            if (category == null)
            {
                return NotFound();
            }

            var brand = new ProductBrand { ProductCategoryId = categoryId };
            ViewBag.CategoryName = category.Name;
            return View(brand);
        }

        // POST: DealerPortal/ProductManagement/CreateBrand
        [HttpPost("CreateBrand")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBrand(ProductBrand brand)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            // Remove navigation property validation errors
            ModelState.Remove("ProductCategory");
            ModelState.Remove("InventoryItems");

            if (ModelState.IsValid)
            {
                _context.ProductBrands.Add(brand);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Brand added successfully!";
                return RedirectToAction(nameof(Brands), new { categoryId = brand.ProductCategoryId });
            }

            var category = await _context.ProductCategories.FindAsync(brand.ProductCategoryId);
            ViewBag.CategoryName = category?.Name;
            return View(brand);
        }
    }
}
