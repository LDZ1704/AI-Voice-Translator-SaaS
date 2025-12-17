namespace AI_Voice_Translator_SaaS.Interfaces
{
    public interface IAudioDurationService
    {
        Task<int> GetDurationAsync(IFormFile file);
    }
}

