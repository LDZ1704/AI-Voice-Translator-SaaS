namespace AI_Voice_Translator_SaaS.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IAudioFileRepository AudioFiles { get; }
        IRepository<Models.Transcript> Transcripts { get; }
        IRepository<Models.Translation> Translations { get; }
        IRepository<Models.Output> Outputs { get; }
        IRepository<Models.AuditLog> AuditLogs { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}