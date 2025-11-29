using AI_Voice_Translator_SaaS.Data;
using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace AI_Voice_Translator_SaaS.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AivoiceTranslatorContext _context;
        private IDbContextTransaction _transaction;

        public IUserRepository Users { get; }
        public IAudioFileRepository AudioFiles { get; }
        public IRepository<Transcript> Transcripts { get; }
        public IRepository<Translation> Translations { get; }
        public IRepository<Output> Outputs { get; }
        public IRepository<AuditLog> AuditLogs { get; }

        public UnitOfWork(AivoiceTranslatorContext context)
        {
            _context = context;

            Users = new UserRepository(_context);
            AudioFiles = new AudioFileRepository(_context);
            Transcripts = new Repository<Transcript>(_context);
            Translations = new Repository<Translation>(_context);
            Outputs = new Repository<Output>(_context);
            AuditLogs = new Repository<AuditLog>(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                await _transaction?.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            await _transaction?.RollbackAsync();
            _transaction?.Dispose();
            _transaction = null;
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}