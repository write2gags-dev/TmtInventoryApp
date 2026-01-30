using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models.DealerPortal;

namespace TmtInventoryApp.Controllers.DealerPortal
{
    [Route("DealerPortal/Sales")]
    public class DealerSalesController : Controller
    {
        private readonly InventoryContext _context;

        public DealerSalesController(InventoryContext context)
        {
            _context = context;
        }

        // GET: DealerPortal/Sales
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var sales = await _context.CustomerSales
                .Include(s => s.EndCustomer)
                .Include(s => s.Items)
                .Where(s => s.DealerId == dealerId.Value)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return View(sales);
        }

        // GET: DealerPortal/Sales/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            ViewBag.Customers = await _context.EndCustomers
                .Where(c => c.DealerId == dealerId.Value && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.ProductBrands = await _context.ProductBrands
                .Include(pb => pb.ProductCategory)
                .Where(pb => pb.IsActive)
                .OrderBy(pb => pb.ProductCategory.Name)
                    .ThenBy(pb => pb.Name)
                .ToListAsync();

            ViewBag.InventoryItems = await _context.DealerInventoryItems
                .Include(i => i.ProductBrand)
                    .ThenInclude(pb => pb.ProductCategory)
                .Where(i => i.DealerId == dealerId.Value)
                .ToDictionaryAsync(i => i.ProductBrandId, i => i.CurrentQuantity);

            return View();
        }

        // POST: DealerPortal/Sales/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerSale sale, List<CustomerSaleItem> items, string customerName, string customerPhone, string customerAddress, decimal initialPaidAmount)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            if (items == null || !items.Any())
            {
                TempData["Error"] = "Please add at least one item to the sale.";
                return RedirectToAction("Create");
            }

            // Find or create customer
            if (string.IsNullOrWhiteSpace(customerName))
            {
                 TempData["Error"] = "Customer Name is required.";
                 return RedirectToAction("Create");
            }

            EndCustomer? customer = null;
            
            // Try to find by phone if provided
            if (!string.IsNullOrWhiteSpace(customerPhone))
            {
                customer = await _context.EndCustomers
                    .FirstOrDefaultAsync(c => c.DealerId == dealerId.Value && c.PhoneNumber == customerPhone);
            }
            // Fallback: try to find by exact name match if no phone
            else
            {
                 customer = await _context.EndCustomers
                    .FirstOrDefaultAsync(c => c.DealerId == dealerId.Value && c.Name == customerName);
            }

            if (customer == null)
            {
                customer = new EndCustomer
                {
                    DealerId = dealerId.Value,
                    Name = customerName,
                    PhoneNumber = customerPhone,
                    Address = customerAddress,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };
                _context.EndCustomers.Add(customer);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Optional: Update address if provided and currently empty?
                if (string.IsNullOrWhiteSpace(customer.Address) && !string.IsNullOrWhiteSpace(customerAddress))
                {
                    customer.Address = customerAddress;
                     await _context.SaveChangesAsync();
                }
            }

            // Validate stock availability
            foreach (var item in items)
            {
                // Remove existing logic that checked validation. Since we're re-implementing, just keep stock check.
                 var inventoryItem = await _context.DealerInventoryItems
                    .FirstOrDefaultAsync(i => i.DealerId == dealerId.Value && i.ProductBrandId == item.ProductBrandId);

                if (inventoryItem == null || inventoryItem.CurrentQuantity < item.Quantity)
                {
                    TempData["Error"] = $"Insufficient stock for product brand ID {item.ProductBrandId}.";
                    return RedirectToAction("Create");
                }
            }

            // Create sale
            sale.DealerId = dealerId.Value;
            sale.EndCustomerId = customer.Id; // Link to the found/created customer
            sale.SaleDate = DateTime.Now;
            sale.TotalAmount = items.Sum(i => i.FinalAmount);
            sale.PaidAmount = initialPaidAmount;
            
            if (sale.PaidAmount >= sale.TotalAmount)
            {
                sale.PaymentStatus = PaymentStatus.Paid;
            }
            else if (sale.PaidAmount > 0)
            {
                sale.PaymentStatus = PaymentStatus.Partial;
            }
            else
            {
                sale.PaymentStatus = PaymentStatus.Pending;
            }

            sale.CreatedBy = HttpContext.Session.GetString("DealerUsername");

            // Clear navigation property to avoid validation issues if any
            sale.EndCustomer = null;
            ModelState.Remove("EndCustomer"); 

            _context.CustomerSales.Add(sale);
            await _context.SaveChangesAsync();

            // Add items and update inventory
            foreach (var item in items)
            {
                item.CustomerSaleId = sale.Id;
                _context.CustomerSaleItems.Add(item);

                // Update inventory
                var inventoryItem = await _context.DealerInventoryItems
                    .FirstOrDefaultAsync(i => i.DealerId == dealerId.Value && i.ProductBrandId == item.ProductBrandId);

                if (inventoryItem != null)
                {
                    inventoryItem.CurrentQuantity -= item.Quantity;
                    inventoryItem.LastUpdated = DateTime.Now;

                    // Create inventory transaction
                    var transaction = new DealerInventoryTransaction
                    {
                        DealerInventoryItemId = inventoryItem.Id,
                        TransactionType = TransactionTypes.StockOut,
                        Quantity = -item.Quantity,
                        TransactionDate = DateTime.Now,
                        ReferenceNumber = sale.InvoiceNumber,
                        Notes = $"Sale to {customerName}",
                        CreatedBy = HttpContext.Session.GetString("DealerUsername"),
                        CustomerSaleId = sale.Id
                    };

                    _context.DealerInventoryTransactions.Add(transaction);
                }
            }

            // Update customer outstanding
            // If full payment, outstanding doesn't increase. If partial, it increases by (Total - Paid)
            // Actually, we usually add Total to Outstanding, and then subtract Payment from Outstanding.
            // But here "PaidAmount" is just on the Sale record. We should probably also record a Payment transaction if PaidAmount > 0?
            // For simplicity, let's just update Outstanding based on the difference.
            
            // Re-fetch customer to ensure we have latest context
            var customerToUpdate = await _context.EndCustomers.FindAsync(customer.Id);
            if (customerToUpdate != null)
            {
                customerToUpdate.CurrentOutstanding += (sale.TotalAmount - sale.PaidAmount);
            }
            
            // If PaidAmount > 0, we should arguably create a CustomerPayment record too, to keep the ledger clean.
            if (sale.PaidAmount > 0)
            {
                var payment = new CustomerPayment
                {
                    EndCustomerId = sale.EndCustomerId,
                    CustomerSaleId = sale.Id,
                    PaymentDate = DateTime.Now,
                    Amount = sale.PaidAmount,
                    PaymentMethod = "Cash", // Default or add field if needed
                    RecordedBy = HttpContext.Session.GetString("DealerUsername"),
                    Notes = "Initial payment at time of sale"
                };
                _context.CustomerPayments.Add(payment);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Sale created successfully.";
            return RedirectToAction("Details", new { id = sale.Id });
        }

        // GET: DealerPortal/Sales/Details/{id}
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var sale = await _context.CustomerSales
                .Include(s => s.EndCustomer)
                .Include(s => s.Items)
                    .ThenInclude(i => i.ProductBrand)
                        .ThenInclude(pb => pb.ProductCategory)
                .FirstOrDefaultAsync(s => s.Id == id && s.DealerId == dealerId.Value);

            if (sale == null)
            {
                return NotFound();
            }

            return View(sale);
        }

        // GET: DealerPortal/Sales/RecordPayment/{id}
        [HttpGet("RecordPayment/{id}")]
        public async Task<IActionResult> RecordPayment(int id)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var sale = await _context.CustomerSales
                .Include(s => s.EndCustomer)
                .FirstOrDefaultAsync(s => s.Id == id && s.DealerId == dealerId.Value);

            if (sale == null)
            {
                return NotFound();
            }

            ViewBag.Sale = sale;
            return View();
        }

        // POST: DealerPortal/Sales/RecordPayment/{id}
        [HttpPost("RecordPayment/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(int id, decimal amount, string paymentMethod, string? referenceNumber, string? notes)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var sale = await _context.CustomerSales
                .Include(s => s.EndCustomer)
                .FirstOrDefaultAsync(s => s.Id == id && s.DealerId == dealerId.Value);

            if (sale == null)
            {
                return NotFound();
            }

            if (amount <= 0 || amount > sale.OutstandingAmount)
            {
                TempData["Error"] = "Invalid payment amount.";
                return RedirectToAction("RecordPayment", new { id });
            }

            // Create payment record
            var payment = new CustomerPayment
            {
                EndCustomerId = sale.EndCustomerId,
                PaymentDate = DateTime.Now,
                Amount = amount,
                PaymentMethod = paymentMethod,
                ReferenceNumber = referenceNumber,
                Notes = notes,
                RecordedBy = HttpContext.Session.GetString("DealerUsername"),
                CustomerSaleId = sale.Id
            };

            _context.CustomerPayments.Add(payment);

            // Update sale
            sale.PaidAmount += amount;
            if (sale.PaidAmount >= sale.TotalAmount)
            {
                sale.PaymentStatus = PaymentStatus.Paid;
            }
            else if (sale.PaidAmount > 0)
            {
                sale.PaymentStatus = PaymentStatus.Partial;
            }

            // Update customer outstanding
            var customer = await _context.EndCustomers.FindAsync(sale.EndCustomerId);
            if (customer != null)
            {
                customer.CurrentOutstanding -= amount;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment recorded successfully.";
            return RedirectToAction("Details", new { id });
        }
    }
}
