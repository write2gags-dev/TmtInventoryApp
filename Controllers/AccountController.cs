using Microsoft.AspNetCore.Mvc;
using TmtInventoryApp.Services;
using TmtInventoryApp.Models.ViewModels;

namespace TmtInventoryApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        public AccountController(AuthService authService)
        {
            _authService = authService;
        }

        // GET: Account/Login
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password, string? returnUrl = null)
        {
            var user = _authService.Authenticate(username, password);
            
            if (user != null)
            {
                // Store user info in session
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("DisplayName", user.DisplayName);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            TempData["Error"] = "Invalid username or password.";
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Welcome", "Home");
        }

        // GET: Account/ChangePassword
        public IActionResult ChangePassword()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }

            if (_authService.ChangePassword(username, model.CurrentPassword, model.NewPassword))
            {
                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("CurrentPassword", "The current password is incorrect.");
            return View(model);
        }
    }
}
