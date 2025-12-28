using AI_Voice_Translator_SaaS.Data;
using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Repositories
{
    public class AudioFileRepository : Repository<AudioFile>, IAudioFileRepository
    {
        public AudioFileRepository(AivoiceTranslatorContext context) : base(context) { }

        public async Task<IEnumerable<AudioFile>> GetByUserIdAsync(Guid userId)
        {
            return await _context.AudioFiles.AsNoTracking().Where(a => a.UserId == userId).OrderByDescending(a => a.UploadedAt).ToListAsync();
        }

        public async Task<IEnumerable<AudioFile>> GetCompletedFilesAsync(Guid userId)
        {
            return await _dbSet.Where(a => a.UserId == userId && a.Status == "Completed").OrderByDescending(a => a.UploadedAt).ToListAsync();
        }

        public async Task<IEnumerable<AudioFile>> GetByStatusAsync(string status)
        {
            return await _dbSet.Where(a => a.Status == status).ToListAsync();
        }

        public async Task<AudioFile> GetWithTranscriptAsync(Guid id)
        {
            return await _dbSet.Include(a => a.Transcripts).FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<AudioFile> GetByIdWithRelatedAsync(Guid id)
        {
            return await _context.AudioFiles.AsNoTracking().Include(a => a.Transcripts).ThenInclude(t => t.Translations).ThenInclude(tr => tr.Outputs).AsSplitQuery().FirstOrDefaultAsync(a => a.Id == id);
        }

        // Optimized paginated query
        public async Task<(List<AudioFile> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, string status = null)
        {
            var query = _context.AudioFiles.AsNoTracking().Where(a => a.UserId == userId);

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(a => a.Status == status);
            }

            var totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.UploadedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }
    }
}