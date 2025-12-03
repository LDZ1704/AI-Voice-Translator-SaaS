namespace AI_Voice_Translator_SaaS.Interfaces
{
    public interface ITranslationService
    {
        Task<(bool Success, string TranslatedText)> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
    }
}