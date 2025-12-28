using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models;
using AI_Voice_Translator_SaaS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AI_Voice_Translator_SaaS.Controllers
{
    public class BillingController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMoMoPaymentService _moMoPaymentService;
        private readonly ILogger<BillingController> _logger;

        public BillingController(ISubscriptionService subscriptionService, IUnitOfWork unitOfWork, IMoMoPaymentService moMoPaymentService, ILogger<BillingController> logger)
        {
            _subscriptionService = subscriptionService;
            _unitOfWork = unitOfWork;
            _moMoPaymentService = moMoPaymentService;
            _logger = logger;
        }

        // GET: /Billing
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để quản lý gói dịch vụ.";
                return RedirectToAction("Login", "Account");
            }

            var userId = Guid.Parse(userIdStr);
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Login", "Account");
            }

            var currentPlan = await _subscriptionService.GetCurrentPlanAsync(userId);
            var used = await _subscriptionService.GetUsedConversionsAsync(userId);
            var expiryDate = await _subscriptionService.GetSubscriptionExpiryDateAsync(userId);
            var isExpired = await _subscriptionService.IsSubscriptionExpiredAsync(userId);

            var daysUntilExpiry = 0;
            var isExpiringSoon = false;
            if (expiryDate.HasValue && !currentPlan.IsTrial)
            {
                daysUntilExpiry = (int)(expiryDate.Value - DateTime.UtcNow).TotalDays;
                isExpiringSoon = daysUntilExpiry <= 7 && daysUntilExpiry > 0;
            }

            var model = new BillingViewModel
            {
                CurrentPlanId = currentPlan.Id,
                CurrentPlanName = currentPlan.Name,
                CurrentPlanLimit = currentPlan.ConversionLimit,
                UsedConversions = used,
                SubscriptionExpiryDate = expiryDate,
                IsExpiringSoon = isExpiringSoon,
                DaysUntilExpiry = daysUntilExpiry,
                Plans = SubscriptionPlans.GetAll()
                    .OrderBy(p => p.IsTrial ? 0 : 1)
                    .ThenBy(p => p.ConversionLimit)
                    .ToList()
            };

            return View(model);
        }

        // GET: /Billing/Checkout?planId=Basic
        [HttpGet]
        public async Task<IActionResult> Checkout(string planId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để mua gói.";
                return RedirectToAction("Login", "Account");
            }

            var plan = SubscriptionPlans.Get(planId);

            var userId = Guid.Parse(userIdStr);
            var currentPlan = await _subscriptionService.GetCurrentPlanAsync(userId);

            var model = new BillingCheckoutViewModel
            {
                PlanId = plan.Id,
                PlanName = plan.Name,
                PriceDisplay = plan.PriceDisplay,
                ConversionLimit = plan.ConversionLimit,
                IsTrial = plan.IsTrial,
                CurrentPlanName = currentPlan.Name
            };

            return View(model);
        }

        // POST: /Billing/Purchase
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purchase(string planId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var userId = Guid.Parse(userIdStr);
            var plan = SubscriptionPlans.Get(planId);

            if (plan.IsTrial)
            {
                var result = await _subscriptionService.PurchasePlanAsync(userId, planId);
                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Index");
                }
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Index");
            }

            decimal amount = ParseAmountFromPriceDisplay(plan.PriceDisplay);

            var returnUrl = Url.Action("PaymentReturn", "Billing", new { planId }, Request.Scheme, Request.Host.Value);
            var notifyUrl = Url.Action("PaymentNotify", "Billing", null, Request.Scheme, Request.Host.Value);

            _logger.LogInformation($"MoMo Payment URLs - ReturnUrl: {returnUrl}, NotifyUrl: {notifyUrl}, Amount: {amount}");

            if (string.IsNullOrEmpty(returnUrl) || string.IsNullOrEmpty(notifyUrl))
            {
                TempData["ErrorMessage"] = "Không thể tạo URL thanh toán. Vui lòng thử lại.";
                _logger.LogError($"Failed to generate payment URLs. ReturnUrl: {returnUrl}, NotifyUrl: {notifyUrl}");
                return RedirectToAction("Index");
            }

            var paymentResult = await _moMoPaymentService.CreatePaymentRequestAsync(
                userId, planId, amount, returnUrl, notifyUrl);

            if (!paymentResult.Success)
            {
                TempData["ErrorMessage"] = "Không thể tạo yêu cầu thanh toán. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }

            HttpContext.Session.SetString($"MoMoOrder_{paymentResult.OrderId}", planId);

            return Redirect(paymentResult.PaymentUrl);
        }

        // GET: /Billing/PaymentReturn?orderId=xxx&resultCode=0&planId=Basic
        [HttpGet]
        public async Task<IActionResult> PaymentReturn(string orderId, string resultCode, string planId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn.";
                return RedirectToAction("Login", "Account");
            }

            var userId = Guid.Parse(userIdStr);
            var verifyResult = await _moMoPaymentService.VerifyPaymentAsync(orderId, resultCode);

            if (verifyResult.Success)
            {
                var purchaseResult = await _subscriptionService.PurchasePlanAsync(userId, planId);
                if (purchaseResult.Success)
                {
                    TempData["SuccessMessage"] = purchaseResult.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = purchaseResult.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = verifyResult.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /Billing/PaymentNotify
        [HttpPost]
        public async Task<IActionResult> PaymentNotify([FromBody] Dictionary<string, string> callbackData)
        {
            try
            {
                var ipnResult = await _moMoPaymentService.ProcessIpnCallbackAsync(callbackData);

                if (ipnResult.Success && !string.IsNullOrEmpty(ipnResult.OrderId))
                {
                    var planId = HttpContext.Session.GetString($"MoMoOrder_{ipnResult.OrderId}");
                    
                    if (!string.IsNullOrEmpty(planId))
                    {
                        // Tìm user từ orderId (có thể lưu mapping orderId -> userId trong database)
                        // Tạm thời, chúng ta sẽ xử lý trong PaymentReturn
                        // IPN chỉ để verify và log
                        _logger.LogInformation($"MoMo IPN verified successfully. OrderId={ipnResult.OrderId}, PlanId={planId}");
                    }
                }
                else
                {
                    _logger.LogWarning($"MoMo IPN verification failed. OrderId={ipnResult.OrderId}, Message={ipnResult.Message}");
                }

                return Ok(new { resultCode = "0", message = "Success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo IPN callback");
                return StatusCode(500, new { resultCode = "99", message = "Internal error" });
            }
        }

        private static decimal ParseAmountFromPriceDisplay(string priceDisplay)
        {
            if (string.IsNullOrWhiteSpace(priceDisplay) || priceDisplay.Contains("Miễn phí"))
                return 0;

            var numberPart = priceDisplay.Split('₫')[0].Replace(".", "").Trim();
            if (decimal.TryParse(numberPart, out var amount))
            {
                return amount;
            }

            return 0;
        }
    }
}


