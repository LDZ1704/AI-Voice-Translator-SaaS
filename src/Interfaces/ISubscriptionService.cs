using System;
using System.Threading.Tasks;
using AI_Voice_Translator_SaaS.Models;

namespace AI_Voice_Translator_SaaS.Interfaces
{
    public interface ISubscriptionService
    {
        Task<SubscriptionPlan> GetCurrentPlanAsync(Guid userId);
        Task<int> GetUsedConversionsAsync(Guid userId);
        Task<bool> HasRemainingConversionsAsync(Guid userId);
        Task<(bool Success, string Message)> EnsureCanUseConversionAsync(Guid userId);
        Task<(bool Success, string Message)> PurchasePlanAsync(Guid userId, string planId);
        Task<DateTime?> GetSubscriptionExpiryDateAsync(Guid userId);
        Task<bool> IsSubscriptionExpiredAsync(Guid userId);
        Task CheckAndDowngradeExpiredSubscriptionsAsync();
    }
}