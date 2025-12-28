using System.Collections.Generic;
using AI_Voice_Translator_SaaS.Models;

namespace AI_Voice_Translator_SaaS.Models.ViewModels
{
    public class BillingViewModel
    {
        public string CurrentPlanId { get; set; } = string.Empty;
        public string CurrentPlanName { get; set; } = string.Empty;
        public int CurrentPlanLimit { get; set; }
        public int UsedConversions { get; set; }
        public int RemainingConversions => CurrentPlanLimit - UsedConversions;
        public DateTime? SubscriptionExpiryDate { get; set; }
        public bool IsExpiringSoon { get; set; }
        public int DaysUntilExpiry { get; set; }
        public List<SubscriptionPlan> Plans { get; set; } = new();
    }

    public class BillingCheckoutViewModel
    {
        public string PlanId { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string PriceDisplay { get; set; } = string.Empty;
        public int ConversionLimit { get; set; }
        public bool IsTrial { get; set; }
        public string CurrentPlanName { get; set; } = string.Empty;
    }
}



