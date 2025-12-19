using System;
using System.Threading.Tasks;
using AI_Voice_Translator_SaaS.Interfaces;
using Microsoft.Extensions.Logging;

namespace AI_Voice_Translator_SaaS.Jobs
{
    public class SubscriptionExpiryJob
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionExpiryJob> _logger;

        public SubscriptionExpiryJob(ISubscriptionService subscriptionService, ILogger<SubscriptionExpiryJob> logger)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        public async Task CheckAndDowngradeExpiredSubscriptions()
        {
            try
            {
                _logger.LogInformation("Starting subscription expiry check job...");
                await _subscriptionService.CheckAndDowngradeExpiredSubscriptionsAsync();
                _logger.LogInformation("Subscription expiry check job completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in subscription expiry check job");
                throw;
            }
        }
    }
}

