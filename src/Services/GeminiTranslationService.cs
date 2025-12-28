using AI_Voice_Translator_SaaS.Interfaces;
using Mscc.GenerativeAI;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AI_Voice_Translator_SaaS.Services
{
    public class GeminiTranslationService : ITranslationService
    {
        private readonly string _apiKey;
        private readonly ILogger<GeminiTranslationService> _logger;
        private readonly ICacheService _cacheService;

        public GeminiTranslationService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<GeminiTranslationService> logger, ICacheService cacheService)
        {
            _apiKey = configuration["Gemini:Credentials:ApiKey"];
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<(bool Success, string TranslatedText)> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            try
            {
                var cacheKey = GenerateCacheKey(text, sourceLanguage, targetLanguage);
                var cachedTranslation = await _cacheService.GetAsync<string>(cacheKey);
                if (!string.IsNullOrEmpty(cachedTranslation))
                {
                    _logger.LogInformation("Cache hit for translation");
                    return (true, cachedTranslation);
                }

                _logger.LogInformation("Cache miss - calling Gemini API");

                var model = new GenerativeModel { ApiKey = _apiKey };
                var prompt = $@"Translate from {sourceLanguage} to {targetLanguage}. Only output translation: {text}";
                var response = await model.GenerateContent(prompt);
                var translatedText = response?.Text?.Trim();

                if (string.IsNullOrEmpty(translatedText))
                {
                    return (false, "Translation failed - empty response");
                }

                await _cacheService.SetAsync(cacheKey, translatedText, TimeSpan.FromHours(24));

                return (true, translatedText);
            }
            catch (Mscc.GenerativeAI.GeminiApiTimeoutException ex)
            {
                if (ex.Message.Contains("429") || ex.Message.Contains("quota"))
                {
                    _logger.LogError("Quota exceeded - NO RETRY");
                    return (false, "Translation quota exceeded. Please try again later.");
                }

                _logger.LogError(ex, "Gemini API timeout");
                return (false, $"Translation timeout: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Gemini translation task canceled (likely timeout)");
                return (false, "Translation took too long and was cancelled. Please try again with a shorter audio or later.");
            }
            catch (Exception ex)
            {
                var message = ex.Message ?? string.Empty;
                if (message.Contains("429") || message.Contains("quota", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError(ex, "Gemini API quota exceeded");
                    return (false, "Translation quota exceeded. Please check your Gemini quota or try again later.");
                }

                _logger.LogError(ex, "Error translating text");
                return (false, $"Translation error: {ex.Message}");
            }
        }

        private string GenerateCacheKey(string text, string sourceLang, string targetLang)
        {
            var input = $"{text}_{sourceLang}_{targetLang}";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            return $"translation_{hash}";
        }
    }
}