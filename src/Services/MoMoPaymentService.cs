using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AI_Voice_Translator_SaaS.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AI_Voice_Translator_SaaS.Services
{
    public class MoMoPaymentService : IMoMoPaymentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MoMoPaymentService> _logger;
        private readonly string _partnerCode;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _endpoint;
        private readonly string _requestType;
        private readonly string _returnUrl;
        private readonly string _notifyUrl;

        public MoMoPaymentService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<MoMoPaymentService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;

            _partnerCode = _configuration["MoMoPayment:PartnerCode"] ?? "MOMO";
            _accessKey = _configuration["MoMoPayment:AccessKey"] ?? "";
            _secretKey = _configuration["MoMoPayment:SecretKey"] ?? "";
            _endpoint = _configuration["MoMoPayment:MomoApiUrl"] ?? _configuration["MoMoPayment:Endpoint"] ?? "https://test-payment.momo.vn/gw_payment/transactionProcessor";
            _requestType = _configuration["MoMoPayment:RequestType"] ?? "captureMoMoWallet";
            _returnUrl = _configuration["MoMoPayment:ReturnUrl"] ?? "";
            _notifyUrl = _configuration["MoMoPayment:NotifyUrl"] ?? "";
        }

        public async Task<(bool Success, string PaymentUrl, string OrderId)> CreatePaymentRequestAsync(
            Guid userId,
            string planId,
            decimal amount,
            string returnUrl,
            string notifyUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(_accessKey))
                {
                    _logger.LogError("MoMo AccessKey is empty");
                    return (false, "", Guid.NewGuid().ToString());
                }

                var orderId = Guid.NewGuid().ToString();
                var requestId = Guid.NewGuid().ToString();
                var orderInfo = $"Thanh toán gói {planId} - User {userId}";
                var extraData = "";
                
                var finalReturnUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : _returnUrl;
                var finalNotifyUrl = !string.IsNullOrEmpty(notifyUrl) ? notifyUrl : _notifyUrl;

                if (string.IsNullOrEmpty(finalReturnUrl) || string.IsNullOrEmpty(finalNotifyUrl))
                {
                    _logger.LogError($"MoMo URLs are empty. ReturnUrl: {finalReturnUrl}, NotifyUrl: {finalNotifyUrl}");
                    return (false, "", orderId);
                }

                var amountString = ((long)amount).ToString();
                var storeId = "AI_Voice_Translator";
                var rawHash =
                    $"partnerCode={_partnerCode}" +
                    $"&accessKey={_accessKey}" +
                    $"&requestId={requestId}" +
                    $"&amount={amountString}" +
                    $"&orderId={orderId}" +
                    $"&orderInfo={orderInfo}" +
                    $"&returnUrl={finalReturnUrl}" +
                    $"&notifyUrl={finalNotifyUrl}" +
                    $"&extraData={extraData}" +
                    $"&storeId={storeId}";

                var signature = ComputeHmacSha256(rawHash, _secretKey);

                var requestBody = new
                {
                    partnerCode = _partnerCode,
                    accessKey = _accessKey,
                    partnerName = "AI Voice Translator",
                    storeId = storeId,
                    requestId = requestId,
                    amount = amountString,
                    orderId = orderId,
                    orderInfo = orderInfo,
                    returnUrl = finalReturnUrl,
                    notifyUrl = finalNotifyUrl,
                    lang = "vi",
                    extraData = extraData,
                    requestType = _requestType,
                    signature = signature
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.PostAsync(_endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (responseObj.TryGetProperty("payUrl", out var payUrlElement))
                    {
                        var payUrl = payUrlElement.GetString();
                        _logger.LogInformation($"MoMo payment request created: OrderId={orderId}, PayUrl={payUrl}");
                        return (true, payUrl ?? "", orderId);
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"MoMo payment request failed: {errorContent}");
                return (false, "", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MoMo payment request");
                return (false, "", Guid.NewGuid().ToString());
            }
        }

        public async Task<(bool Success, string Message)> VerifyPaymentAsync(string orderId, string resultCode)
        {
            if (resultCode == "0")
            {
                return (true, "Thanh toán thành công");
            }

            return (false, $"Thanh toán thất bại. Mã lỗi: {resultCode}");
        }

        public async Task<(bool Success, string OrderId, string ResultCode, string Message)> ProcessIpnCallbackAsync(
            Dictionary<string, string> callbackData)
        {
            try
            {
                if (!callbackData.TryGetValue("orderId", out var orderId) ||
                    !callbackData.TryGetValue("resultCode", out var resultCode) ||
                    !callbackData.TryGetValue("signature", out var signature))
                {
                    _logger.LogWarning("MoMo IPN callback missing required fields");
                    return (false, "", "", "Thiếu thông tin bắt buộc trong callback");
                }

                var rawHash = BuildSignatureString(callbackData);
                var computedSignature = ComputeHmacSha256(rawHash, _secretKey);

                if (computedSignature != signature)
                {
                    _logger.LogWarning($"MoMo IPN signature mismatch. OrderId={orderId}");
                    return (false, orderId, resultCode, "Chữ ký không hợp lệ");
                }

                if (resultCode == "0")
                {
                    _logger.LogInformation($"MoMo payment successful via IPN. OrderId={orderId}");
                    return (true, orderId, resultCode, "Thanh toán thành công");
                }
                else
                {
                    _logger.LogWarning($"MoMo payment failed via IPN. OrderId={orderId}, ResultCode={resultCode}");
                    return (false, orderId, resultCode, $"Thanh toán thất bại. Mã lỗi: {resultCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo IPN callback");
                return (false, "", "", "Lỗi xử lý callback");
            }
        }

        private string BuildSignatureString(Dictionary<string, string> data)
        {
            var fields = new[] { "accessKey", "amount", "extraData", "message", "orderId", "orderInfo", "orderType", "partnerCode", "payType", "requestId", "responseTime", "resultCode", "transId" };
            
            var signatureParts = new List<string>();
            foreach (var field in fields)
            {
                if (data.TryGetValue(field, out var value) && !string.IsNullOrEmpty(value))
                {
                    signatureParts.Add($"{field}={value}");
                }
            }

            return string.Join("&", signatureParts);
        }

        private static string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmacsha256 = new HMACSHA256(keyBytes);
            var hashMessage = hmacsha256.ComputeHash(messageBytes);
            return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
        }
    }
}

