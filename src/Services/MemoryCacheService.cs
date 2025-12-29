using AI_Voice_Translator_SaaS.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace AI_Voice_Translator_SaaS.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                if (_cache.TryGetValue(key, out T? value))
                {
                    _logger.LogDebug("Cache HIT: {Key}", key);
                    return value;
                }

                _logger.LogDebug("Cache MISS: {Key}", key);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đọc cache key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1),

                    Priority = CacheItemPriority.Normal,

                    PostEvictionCallbacks =
                    {
                        new PostEvictionCallbackRegistration
                        {
                            EvictionCallback = (k, v, reason, state) =>
                            {
                                if (reason != EvictionReason.Replaced)
                                {
                                    _logger.LogDebug("Cache evicted: {Key}, Reason: {Reason}", k, reason);
                                }
                            }
                        }
                    }
                };

                _cache.Set(key, value, options);
                _logger.LogDebug("Cache SET: {Key}, Expiration: {Expiration}m",
                    key, options.AbsoluteExpirationRelativeToNow?.TotalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu cache key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _cache.Remove(key);
                _logger.LogDebug("Cache REMOVED: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa cache key: {Key}", key);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return _cache.TryGetValue(key, out _);
        }
    }
}