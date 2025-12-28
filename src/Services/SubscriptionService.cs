using System;
using System.Linq;
using System.Threading.Tasks;
using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models;

namespace AI_Voice_Translator_SaaS.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public SubscriptionService(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<SubscriptionPlan> GetCurrentPlanAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return SubscriptionPlans.Get("Trial");
            }

            return SubscriptionPlans.Get(user.SubscriptionTier);
        }

        public async Task<int> GetUsedConversionsAsync(Guid userId)
        {
            var files = await _unitOfWork.AudioFiles.GetByUserIdAsync(userId);
            return files.Count();
        }

        public async Task<bool> HasRemainingConversionsAsync(Guid userId)
        {
            var plan = await GetCurrentPlanAsync(userId);
            var used = await GetUsedConversionsAsync(userId);
            return used < plan.ConversionLimit;
        }

        public async Task<(bool Success, string Message)> EnsureCanUseConversionAsync(Guid userId)
        {
            var plan = await GetCurrentPlanAsync(userId);
            var used = await GetUsedConversionsAsync(userId);

            if (used < plan.ConversionLimit)
            {
                return (true, string.Empty);
            }

            var message =
                $"Bạn đã dùng hết {plan.ConversionLimit} lượt chuyển đổi của gói {plan.Name}. " +
                "Vui lòng nâng cấp gói để tiếp tục sử dụng.";

            return (false, message);
        }

        public async Task<(bool Success, string Message)> PurchasePlanAsync(Guid userId, string planId)
        {
            var plan = SubscriptionPlans.Get(planId);

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return (false, "Không tìm thấy người dùng.");
            }

            user.SubscriptionTier = plan.Id;
            
            if (plan.IsTrial)
            {
                user.SubscriptionExpiryDate = null;
            }
            else
            {
                user.SubscriptionExpiryDate = DateTime.UtcNow.AddMonths(1);
            }

            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAsync(userId, "PurchasePlan", $"User purchased plan: {plan.Name}");

            return (true, $"Thanh toán thành công. Bạn đang sử dụng gói {plan.Name} với {plan.ConversionLimit} lượt chuyển đổi.");
        }

        public async Task<DateTime?> GetSubscriptionExpiryDateAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            return user?.SubscriptionExpiryDate;
        }

        public async Task<bool> IsSubscriptionExpiredAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return true;

            var plan = SubscriptionPlans.Get(user.SubscriptionTier);
            if (plan.IsTrial) return false;

            if (user.SubscriptionExpiryDate == null) return false;

            return user.SubscriptionExpiryDate.Value <= DateTime.UtcNow;
        }

        public async Task CheckAndDowngradeExpiredSubscriptionsAsync()
        {
            var allUsers = await _unitOfWork.Users.GetAllAsync();
            var expiredUsers = allUsers.Where(u =>
            {
                if (u.SubscriptionTier == "Trial") return false;
                if (u.SubscriptionExpiryDate == null) return false;
                return u.SubscriptionExpiryDate.Value <= DateTime.UtcNow;
            }).ToList();

            foreach (var user in expiredUsers)
            {
                user.SubscriptionTier = "Trial";
                user.SubscriptionExpiryDate = null;
                await _auditService.LogAsync(user.Id, "SubscriptionExpired", $"Subscription expired, downgraded to Trial");
            }

            if (expiredUsers.Any())
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}



