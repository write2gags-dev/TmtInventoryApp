using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models.DealerPortal;

namespace TmtInventoryApp.Controllers.DealerPortal
{
    [Route("DealerPortal/[controller]")]
    public class DealerInventoryController : Controller
    {
        private readonly InventoryContext _context;

        public DealerInventoryController(InventoryContext context)
        {
            _context = context;
        }

        // GET: DealerPortal/DealerInventory
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var inventoryItems = await _context.DealerInventoryItems
                .Include(i => i.ProductBrand)
                    .ThenInclude(pb => pb.ProductCategory)
                .Where(i => i.DealerId == dealerId.Value)
                .OrderBy(i => i.ProductBrand.ProductCategory.Name)
                    .ThenBy(i => i.ProductBrand.Name)
                .ToListAsync();

            return View(inventoryItems);
        }

        // GET: DealerPortal/DealerInventory/AddStock
        [HttpGet("AddStock")]
        public async Task<IActionResult> AddStock()
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            ViewBag.ProductBrands = await _context.ProductBrands
                .Include(pb => pb.ProductCategory)
                .Where(pb => pb.IsActive)
                .OrderBy(pb => pb.ProductCategory.Name)
                    .ThenBy(pb => pb.Name)
                .ToListAsync();

            return View();
        }

        // POST: DealerPortal/DealerInventory/AddStock
        [HttpPost("AddStock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStock(int productBrandId, decimal quantity, string? referenceNumber, string? notes)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            if (quantity <= 0)
            {
                TempData["Error"] = "Quantity must be greater than zero.";
                return RedirectToAction("AddStock");
            }

            // Find or create inventory item
            var inventoryItem = await _context.DealerInventoryItems
                .FirstOrDefaultAsync(i => i.DealerId == dealerId.Value && i.ProductBrandId == productBrandId);

            if (inventoryItem == null)
            {
                inventoryItem = new DealerInventoryItem
                {
                    DealerId = dealerId.Value,
                    ProductBrandId = productBrandId,
                    CurrentQuantity = 0
                };
                _context.DealerInventoryItems.Add(inventoryItem);
                await _context.SaveChangesAsync();
            }

            // Update quantity
            inventoryItem.CurrentQuantity += quantity;
            inventoryItem.LastUpdated = DateTime.Now;

            // Create transaction
            var transaction = new DealerInventoryTransaction
            {
                DealerInventoryItemId = inventoryItem.Id,
                TransactionType = TransactionTypes.StockIn,
                Quantity = quantity,
                TransactionDate = DateTime.Now,
                ReferenceNumber = referenceNumber,
                Notes = notes,
                CreatedBy = HttpContext.Session.GetString("DealerUsername")
            };

            _context.DealerInventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Stock added successfully.";
            return RedirectToAction("Index");
        }

        // GET: DealerPortal/DealerInventory/AdjustStock/{id}
        [HttpGet("AdjustStock/{id}")]
        public async Task<IActionResult> AdjustStock(int id)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var inventoryItem = await _context.DealerInventoryItems
                .Include(i => i.ProductBrand)
                    .ThenInclude(pb => pb.ProductCategory)
                .FirstOrDefaultAsync(i => i.Id == id && i.DealerId == dealerId.Value);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            return View(inventoryItem);
        }

        // POST: DealerPortal/DealerInventory/AdjustStock/{id}
        [HttpPost("AdjustStock/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int id, decimal newQuantity, string? notes)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var inventoryItem = await _context.DealerInventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DealerId == dealerId.Value);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            var difference = newQuantity - inventoryItem.CurrentQuantity;
            
            // Create adjustment transaction
            var transaction = new DealerInventoryTransaction
            {
                DealerInventoryItemId = inventoryItem.Id,
                TransactionType = TransactionTypes.Adjustment,
                Quantity = difference,
                TransactionDate = DateTime.Now,
                Notes = notes,
                CreatedBy = HttpContext.Session.GetString("DealerUsername")
            };

            inventoryItem.CurrentQuantity = newQuantity;
            inventoryItem.LastUpdated = DateTime.Now;

            _context.DealerInventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Stock adjusted successfully.";
            return RedirectToAction("Index");
        }

        // GET: DealerPortal/DealerInventory/Transactions/{id}
        [HttpGet("Transactions/{id}")]
        public async Task<IActionResult> Transactions(int id)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var inventoryItem = await _context.DealerInventoryItems
                .Include(i => i.ProductBrand)
                    .ThenInclude(pb => pb.ProductCategory)
                .Include(i => i.Transactions)
                    .ThenInclude(t => t.CustomerSale)
                        .ThenInclude(cs => cs.EndCustomer)
                .FirstOrDefaultAsync(i => i.Id == id && i.DealerId == dealerId.Value);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            return View(inventoryItem);
        }

        // GET: DealerPortal/DealerInventory/Edit/{id}
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var inventoryItem = await _context.DealerInventoryItems
                .Include(i => i.ProductBrand)
                    .ThenInclude(pb => pb.ProductCategory)
                .FirstOrDefaultAsync(i => i.Id == id && i.DealerId == dealerId.Value);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            return View(inventoryItem);
        }

        // POST: DealerPortal/DealerInventory/Edit/{id}
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, decimal? minimumStockLevel, decimal? unitPrice, string? notes)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var inventoryItem = await _context.DealerInventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.DealerId == dealerId.Value);

            if (inventoryItem == null)
            {
                return NotFound();
            }

            inventoryItem.MinimumStockLevel = minimumStockLevel;
            inventoryItem.UnitPrice = unitPrice;
            inventoryItem.Notes = notes;
            inventoryItem.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Inventory item updated successfully.";
            return RedirectToAction("Index");
        }
    }
}
