namespace AI_Voice_Translator_SaaS.Interfaces
{
    public interface ISpeechService
    {
        Task<(bool Success, string Text, string Language, decimal Confidence)> TranscribeAsync(string audioFileUrl);
    }
}