using System.Collections.Generic;

namespace AI_Voice_Translator_SaaS.Interfaces
{
    public interface IMoMoPaymentService
    {
        Task<(bool Success, string PaymentUrl, string OrderId)> CreatePaymentRequestAsync(
            Guid userId,
            string planId,
            decimal amount,
            string returnUrl,
            string notifyUrl);

        Task<(bool Success, string Message)> VerifyPaymentAsync(string orderId, string resultCode);
        
        Task<(bool Success, string OrderId, string ResultCode, string Message)> ProcessIpnCallbackAsync(Dictionary<string, string> callbackData);
    }
}

