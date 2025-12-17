using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AI_Voice_Translator_SaaS.Interfaces;

namespace AI_Voice_Translator_SaaS.Services
{
    public class AzureTranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _endpoint;
        private readonly string? _subscriptionKey;
        private readonly string? _region;
        private readonly ILogger<AzureTranslationService> _logger;
        private readonly ICacheService _cacheService;

        public AzureTranslationService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<AzureTranslationService> logger, ICacheService cacheService)
        {
            _endpoint = configuration["AzureTranslator:Endpoint"];
            _subscriptionKey = configuration["AzureTranslator:Key"];
            _region = configuration["AzureTranslator:Region"];
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<(bool Success, string TranslatedText)> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_endpoint) || string.IsNullOrWhiteSpace(_subscriptionKey) || string.IsNullOrWhiteSpace(_region))
                {
                    return (false, "Azure Translator is not configured. Please set AzureTranslator:Endpoint, Key and Region in appsettings.json.");
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    return (false, "Empty text");
                }

                var cacheKey = GenerateCacheKey(text, sourceLanguage, targetLanguage);
                var cachedTranslation = await _cacheService.GetAsync<string>(cacheKey);
                if (!string.IsNullOrEmpty(cachedTranslation))
                {
                    _logger.LogInformation("AzureTranslator cache hit");
                    return (true, cachedTranslation);
                }

                _logger.LogInformation("AzureTranslator cache miss - calling Azure Translator API");

                var baseUrl = _endpoint.TrimEnd('/');
                string url;
                if (baseUrl.Contains("cognitiveservices.azure.com") && !baseUrl.Contains("/translator/text/", StringComparison.OrdinalIgnoreCase))
                {
                    url = $"{baseUrl}/translator/text/v3.0/translate?from={sourceLanguage}&to={targetLanguage}";
                }
                else
                {
                    url = $"{baseUrl}/translate?api-version=3.0&from={sourceLanguage}&to={targetLanguage}";
                }

                var requestBody = new[]
                {
                    new { Text = text }
                };

                var json = JsonSerializer.Serialize(requestBody);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
                _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", _region);

                using var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Azure Translator error: {StatusCode} - {Body}", response.StatusCode, responseString);
                    return (false, $"Azure Translator error: {response.StatusCode}");
                }

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                var translatedText = root[0].GetProperty("translations")[0].GetProperty("text").GetString();

                if (string.IsNullOrEmpty(translatedText))
                {
                    return (false, "Translation failed - empty response");
                }

                await _cacheService.SetAsync(cacheKey, translatedText, TimeSpan.FromHours(24));

                return (true, translatedText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating text with Azure Translator");
                return (false, $"Azure Translator error: {ex.Message}");
            }
        }

        private string GenerateCacheKey(string text, string sourceLang, string targetLang)
        {
            var input = $"{text}_{sourceLang}_{targetLang}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            return $"translation_azure_{hash}";
        }
    }
}


