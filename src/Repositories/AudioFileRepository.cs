using AI_Voice_Translator_SaaS.Data;
using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Repositories
{
    public class AudioFileRepository : Repository<AudioFile>, IAudioFileRepository
    {
        public AudioFileRepository(AivoiceTranslatorContext context) : base(context)
        {
        }

        public async Task<IEnumerable<AudioFile>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AudioFile>> GetCompletedFilesAsync(Guid userId)
        {
            return await _dbSet
                .Where(a => a.UserId == userId && a.Status == "Completed")
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AudioFile>> GetByStatusAsync(string status)
        {
            return await _dbSet
                .Where(a => a.Status == status)
                .ToListAsync();
        }

        public async Task<AudioFile> GetWithTranscriptAsync(Guid id)
        {
            return await _dbSet.Include(a => a.Transcripts).FirstOrDefaultAsync(a => a.Id == id);
        }
    }
}