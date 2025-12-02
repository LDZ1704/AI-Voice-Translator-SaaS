using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models;

namespace AI_Voice_Translator_SaaS.Interfaces
{
    public interface IAudioFileRepository : IRepository<AudioFile>
    {
        Task<IEnumerable<AudioFile>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<AudioFile>> GetCompletedFilesAsync(Guid userId);
        Task<IEnumerable<AudioFile>> GetByStatusAsync(string status);
        Task<AudioFile> GetWithTranscriptAsync(Guid id);
    }
}