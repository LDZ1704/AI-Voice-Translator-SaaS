namespace AI_Voice_Translator_SaaS.Interfaces
{
    public interface ITTSService
    {
        Task<(bool Success, string AudioFileUrl)> GenerateSpeechAsync(string text, string language, string voiceType);
    }
}