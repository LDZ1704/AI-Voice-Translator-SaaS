using System;
using System.Threading.Tasks;
using AI_Voice_Translator_SaaS.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AI_Voice_Translator_SaaS.ViewComponents
{
    public class SubscriptionInfoViewComponent : ViewComponent
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionInfoViewComponent(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Content(string.Empty);
            }

            var userId = Guid.Parse(userIdStr);
            var plan = await _subscriptionService.GetCurrentPlanAsync(userId);
            var used = await _subscriptionService.GetUsedConversionsAsync(userId);
            var remaining = plan.ConversionLimit - used;
            var expiryDate = await _subscriptionService.GetSubscriptionExpiryDateAsync(userId);
            var isExpired = await _subscriptionService.IsSubscriptionExpiredAsync(userId);

            return View(new SubscriptionInfoViewModel
            {
                PlanName = plan.Name,
                PlanId = plan.Id,
                UsedConversions = used,
                TotalConversions = plan.ConversionLimit,
                RemainingConversions = remaining,
                ExpiryDate = expiryDate,
                IsExpired = isExpired
            });
        }
    }

    public class SubscriptionInfoViewModel
    {
        public string PlanName { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;
        public int UsedConversions { get; set; }
        public int TotalConversions { get; set; }
        public int RemainingConversions { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsExpired { get; set; }
    }
}

