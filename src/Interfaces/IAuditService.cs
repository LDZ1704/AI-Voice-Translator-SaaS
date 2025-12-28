namespace AI_Voice_Translator_SaaS.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(Guid userId, string action, string? details = null);
    }
}

