using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmtInventoryApp.Data;
using TmtInventoryApp.Models;

namespace TmtInventoryApp.Controllers
{
    public class SettingsController : Controller
    {
        private readonly InventoryContext _context;

        public SettingsController(InventoryContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != UserRoles.Admin)
            {
                TempData["Error"] = "Only Admins can access settings.";
                return RedirectToAction("Index", "Home");
            }
            return View();
        }


    }
}
