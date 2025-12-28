using AI_Voice_Translator_SaaS.Data;
using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AI_Voice_Translator_SaaS.Services
{
    public class AuditService : IAuditService
    {
        private readonly AivoiceTranslatorContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(AivoiceTranslatorContext context, IHttpContextAccessor httpContextAccessor, ILogger<AuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogAsync(Guid userId, string action, string? details = null)
        {
            try
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                var log = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Audit log created: UserId={UserId}, Action={Action}", userId, action);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - audit logging should not break the main flow
                _logger.LogError(ex, "Failed to create audit log: UserId={UserId}, Action={Action}", userId, action);
            }
        }
    }
}

