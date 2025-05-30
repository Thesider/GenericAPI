using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GenericAPI.HealthChecks
{
    /// <summary>
    /// Redis cache health check
    /// </summary>
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisHealthCheck> _logger;

        public RedisHealthCheck(IConnectionMultiplexer redis, ILogger<RedisHealthCheck> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var database = _redis.GetDatabase();
                
                // Test Redis connection with a simple ping
                var ping = await database.PingAsync();
                
                if (ping.TotalMilliseconds < 1000) // Less than 1 second
                {
                    _logger.LogDebug("Redis health check passed - Ping: {PingTime}ms", ping.TotalMilliseconds);
                    return HealthCheckResult.Healthy($"Redis is accessible - Ping: {ping.TotalMilliseconds}ms");
                }
                else
                {
                    _logger.LogWarning("Redis health check degraded - Slow response: {PingTime}ms", ping.TotalMilliseconds);
                    return HealthCheckResult.Degraded($"Redis is slow - Ping: {ping.TotalMilliseconds}ms");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return HealthCheckResult.Unhealthy("Redis is not accessible", ex);
            }
        }
    }
}
