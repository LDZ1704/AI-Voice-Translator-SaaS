namespace AI_Voice_Translator_SaaS.Services
{
    public interface ITranslationService
    {
        Task<(bool Success, string TranslatedText)> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
    }
}