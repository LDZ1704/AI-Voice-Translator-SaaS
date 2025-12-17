using System.Diagnostics;

namespace AI_Voice_Translator_SaaS.Middleware
{
    public class PerformanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMiddleware> _logger;

        public PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();

            await _next(context);

            sw.Stop();

            var elapsedMs = sw.ElapsedMilliseconds;

            if (elapsedMs > 1000)
            {
                _logger.LogWarning($"Slow request: {context.Request.Method} {context.Request.Path} took {elapsedMs}ms");
            }
        }
    }
}