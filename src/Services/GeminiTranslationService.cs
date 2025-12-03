using AI_Voice_Translator_SaaS.Interfaces;
using Mscc.GenerativeAI;

namespace AI_Voice_Translator_SaaS.Services
{
    public class GeminiTranslationService : ITranslationService
    {
        private readonly string _apiKey;
        private readonly ILogger<GeminiTranslationService> _logger;

        public GeminiTranslationService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<GeminiTranslationService> logger)
        {
            _apiKey = configuration["Gemini:Credentials:ApiKey"];
            _logger = logger;
        }

        public async Task<(bool Success, string TranslatedText)> TranslateAsync(
            string text,
            string sourceLanguage,
            string targetLanguage)
        {
            try
            {
                var languageNames = new Dictionary<string, string>
                {
                    { "en", "English" },
                    { "vi", "Vietnamese" },
                    { "ja", "Japanese" },
                    { "zh", "Chinese" },
                    { "fr", "French" }
                };

                var sourceLang = languageNames.GetValueOrDefault(sourceLanguage, sourceLanguage);
                var targetLang = languageNames.GetValueOrDefault(targetLanguage, targetLanguage);

                var prompt = $@"Translate the following text from {sourceLang} to {targetLang}. 
                                Only provide the translation, no explanations or additional text.

                                Text to translate:
                                {text}

                                Translation:";

                var model = new GenerativeModel { ApiKey = _apiKey };

                var response = await model.GenerateContent(prompt);

                var translatedText = response.Text?.Trim();

                if (string.IsNullOrEmpty(translatedText))
                {
                    return (false, "No translation returned");
                }

                _logger.LogInformation($"Translation completed: {sourceLanguage} -> {targetLanguage}");

                return (true, translatedText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating text");
                return (false, ex.Message);
            }
        }
    }
}