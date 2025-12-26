using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = new AuthenticationProperties 
            { 
                RedirectUri = redirectUrl,
                Items = { { "scheme", provider } }
            };
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"Lỗi từ nhà cung cấp: {remoteError}";
                return RedirectToAction("Login");
            }

            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                TempData["ErrorMessage"] = "Không thể xác thực từ nhà cung cấp.";
                return RedirectToAction("Login");
            }

            var claims = authenticateResult.Principal.Claims.ToList();
            var provider = authenticateResult.Properties.Items.ContainsKey("scheme") 
                ? authenticateResult.Properties.Items["scheme"] 
                : "Unknown";

            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value 
                ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                ?? email?.Split('@')[0] ?? "User";
            var providerKey = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerKey))
            {
                TempData["ErrorMessage"] = "Không thể lấy thông tin từ nhà cung cấp.";
                return RedirectToAction("Login");
            }

            var user = await _authService.GetOrCreateOAuthUserAsync(provider, providerKey, email, name);

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", user.DisplayName);
            HttpContext.Session.SetString("UserRole", user.Role);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["SuccessMessage"] = "Đăng nhập thành công!";
            return RedirectToLocal(returnUrl);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }
    }
}