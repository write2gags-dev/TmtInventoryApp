using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models.DealerPortal;

namespace TmtInventoryApp.Controllers.DealerPortal
{
    [Route("DealerPortal/[controller]")]
    public class CustomerController : Controller
    {
        private readonly InventoryContext _context;

        public CustomerController(InventoryContext context)
        {
            _context = context;
        }

        // GET: DealerPortal/Customer
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var customers = await _context.EndCustomers
                .Where(c => c.DealerId == dealerId.Value)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            return View(customers);
        }

        // GET: DealerPortal/Customer/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            return View();
        }

        // POST: DealerPortal/Customer/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EndCustomer customer)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            if (ModelState.IsValid)
            {
                customer.DealerId = dealerId.Value;
                customer.CreatedDate = DateTime.Now;
                customer.CurrentOutstanding = 0;
                customer.IsActive = true;

                _context.EndCustomers.Add(customer);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Customer created successfully.";
                return RedirectToAction("Index");
            }

            return View(customer);
        }

        // GET: DealerPortal/Customer/Edit/{id}
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var customer = await _context.EndCustomers
                .FirstOrDefaultAsync(c => c.Id == id && c.DealerId == dealerId.Value);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: DealerPortal/Customer/Edit/{id}
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EndCustomer customer)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            if (id != customer.Id)
            {
                return NotFound();
            }

            var existingCustomer = await _context.EndCustomers
                .FirstOrDefaultAsync(c => c.Id == id && c.DealerId == dealerId.Value);

            if (existingCustomer == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                existingCustomer.Name = customer.Name;
                existingCustomer.ContactPerson = customer.ContactPerson;
                existingCustomer.PhoneNumber = customer.PhoneNumber;
                existingCustomer.Email = customer.Email;
                existingCustomer.Address = customer.Address;
                existingCustomer.Gstin = customer.Gstin;
                existingCustomer.CreditLimit = customer.CreditLimit;
                existingCustomer.IsActive = customer.IsActive;
                existingCustomer.Notes = customer.Notes;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Customer updated successfully.";
                return RedirectToAction("Index");
            }

            return View(customer);
        }

        // GET: DealerPortal/Customer/Details/{id}
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var customer = await _context.EndCustomers
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Items)
                        .ThenInclude(i => i.ProductBrand)
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(c => c.Id == id && c.DealerId == dealerId.Value);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: DealerPortal/Customer/Ledger/{id}
        [HttpGet("Ledger/{id}")]
        public async Task<IActionResult> Ledger(int id)
        {
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                return RedirectToAction("Login", "DealerAuth");
            }

            var customer = await _context.EndCustomers
                .Include(c => c.Sales)
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(c => c.Id == id && c.DealerId == dealerId.Value);

            if (customer == null)
            {
                return NotFound();
            }

            // Combine sales and payments into a single ledger
            var ledgerEntries = new List<dynamic>();

            foreach (var sale in customer.Sales)
            {
                ledgerEntries.Add(new
                {
                    Date = sale.SaleDate,
                    Type = "Sale",
                    Reference = sale.InvoiceNumber ?? $"Sale #{sale.Id}",
                    Debit = sale.TotalAmount,
                    Credit = 0m,
                    Balance = 0m // Will be calculated
                });
            }

            foreach (var payment in customer.Payments)
            {
                ledgerEntries.Add(new
                {
                    Date = payment.PaymentDate,
                    Type = "Payment",
                    Reference = payment.ReferenceNumber ?? $"Payment #{payment.Id}",
                    Debit = 0m,
                    Credit = payment.Amount,
                    Balance = 0m // Will be calculated
                });
            }

            // Sort by date and calculate running balance
            var sortedEntries = ledgerEntries.OrderBy(e => e.Date).ToList();
            decimal runningBalance = 0;
            var finalEntries = new List<dynamic>();

            foreach (var entry in sortedEntries)
            {
                runningBalance += entry.Debit - entry.Credit;
                finalEntries.Add(new
                {
                    entry.Date,
                    entry.Type,
                    entry.Reference,
                    entry.Debit,
                    entry.Credit,
                    Balance = runningBalance
                });
            }

            ViewBag.Customer = customer;
            ViewBag.LedgerEntries = finalEntries;

            return View();
        }
    }
}
