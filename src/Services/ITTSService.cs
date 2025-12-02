namespace AI_Voice_Translator_SaaS.Services
{
    public interface ITTSService
    {
        Task<(bool Success, string AudioFileUrl)> GenerateSpeechAsync(string text, string language, string voiceType);
    }
}