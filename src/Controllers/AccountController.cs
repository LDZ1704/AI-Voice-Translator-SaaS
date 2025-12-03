using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AI_Voice_Translator_SaaS.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        //GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        //POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.RegisterAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            // Store user in session
            HttpContext.Session.SetString("UserId", result.User.Id.ToString());
            HttpContext.Session.SetString("UserEmail", result.User.Email);
            HttpContext.Session.SetString("UserName", result.User.DisplayName);
            HttpContext.Session.SetString("UserRole", result.User.Role);

            TempData["SuccessMessage"] = "Đăng ký thành công!";
            return RedirectToAction("Index", "Dashboard");
        }

        //GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        //POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.LoginAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            // Store user in session
            HttpContext.Session.SetString("UserId", result.User.Id.ToString());
            HttpContext.Session.SetString("UserEmail", result.User.Email);
            HttpContext.Session.SetString("UserName", result.User.DisplayName);
            HttpContext.Session.SetString("UserRole", result.User.Role);

            TempData["SuccessMessage"] = "Đăng nhập thành công!";
            return RedirectToAction("Index", "Dashboard");
        }

        //GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Login");
        }
    }
}