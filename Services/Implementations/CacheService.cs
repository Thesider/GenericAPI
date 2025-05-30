using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GenericAPI.Services
{
    /// <summary>
    /// Redis-based caching service with memory cache fallback
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;
        private readonly IDatabase _database;
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(30);

        public CacheService(
            IConnectionMultiplexer redis,
            IMemoryCache memoryCache,
            ILogger<CacheService> logger)
        {
            _redis = redis;
            _memoryCache = memoryCache;
            _logger = logger;
            _database = _redis.GetDatabase();
        }        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                // Try Redis first
                var redisValue = await _database.StringGetAsync(key);
                if (redisValue.HasValue)
                {
                    var value = JsonSerializer.Deserialize<T>(redisValue!);
                    _logger.LogDebug("Cache hit for key {Key} from Redis", key);
                    return value;
                }

                // Fallback to memory cache
                if (_memoryCache.TryGetValue(key, out T? cachedValue))
                {
                    _logger.LogDebug("Cache hit for key {Key} from Memory", key);
                    return cachedValue;
                }

                _logger.LogDebug("Cache miss for key {Key}", key);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache value for key {Key}", key);
                
                // Fallback to memory cache on Redis error
                if (_memoryCache.TryGetValue(key, out T? fallbackValue))
                {
                    return fallbackValue;
                }
                
                return default(T);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var exp = expiration ?? _defaultExpiration;
            
            try
            {
                // Set in Redis
                var serializedValue = JsonSerializer.Serialize(value);
                await _database.StringSetAsync(key, serializedValue, exp);
                _logger.LogDebug("Set cache value for key {Key} in Redis with expiration {Expiration}", key, exp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting Redis cache for key {Key}", key);
            }

            // Always set in memory cache as fallback
            _memoryCache.Set(key, value, exp);
            _logger.LogDebug("Set cache value for key {Key} in Memory with expiration {Expiration}", key, exp);
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _database.KeyDeleteAsync(key);
                _logger.LogDebug("Removed cache key {Key} from Redis", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing Redis cache for key {Key}", key);
            }

            _memoryCache.Remove(key);
            _logger.LogDebug("Removed cache key {Key} from Memory", key);
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: pattern);
                
                foreach (var key in keys)
                {
                    await _database.KeyDeleteAsync(key);
                }
                
                _logger.LogDebug("Removed cache keys matching pattern {Pattern} from Redis", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing Redis cache by pattern {Pattern}", pattern);
            }

            // Memory cache doesn't support pattern removal, so we skip it
            _logger.LogWarning("Pattern removal not supported for memory cache");
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var exists = await _database.KeyExistsAsync(key);
                if (exists)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Redis cache existence for key {Key}", key);
            }

            // Fallback to memory cache
            return _memoryCache.TryGetValue(key, out _);
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var value = await factory();
            await SetAsync(key, value, expiration);
            return value;
        }
    }
}
