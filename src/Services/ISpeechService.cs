namespace AI_Voice_Translator_SaaS.Services
{
    public interface ISpeechService
    {
        Task<(bool Success, string Text, string Language, decimal Confidence)> TranscribeAsync(string audioFileUrl);
    }
}