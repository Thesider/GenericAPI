using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace GenericAPI.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly IConnectionMultiplexer _redis;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestStore;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IConnectionMultiplexer redis,
        IMemoryCache cache,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _redis = redis;
        _cache = cache;
        _logger = logger;
        _requestStore = new ConcurrentDictionary<string, Queue<DateTime>>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var endpoint = context.Request.Path.ToString();
        var key = $"rate_limit:{clientIp}:{endpoint}";

        var permitLimit = _configuration.GetValue<int>("RateLimiting:PermitLimit", 100);
        var window = _configuration.GetValue<int>("RateLimiting:Window", 60);
        var queueLimit = _configuration.GetValue<int>("RateLimiting:QueueLimit", 2);

        try
        {
            if (await IsRateLimitExceededAsync(key, permitLimit, window))
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsJsonAsync(new { message = "Rate limit exceeded. Please try again later." });
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in rate limiting middleware");
            throw;
        }
    }

    private async Task<bool> IsRateLimitExceededAsync(string key, int permitLimit, int window)
    {
        try
        {
            var db = _redis.GetDatabase();
            var now = DateTime.UtcNow;

            // Use Redis sorted set to track requests
            var transaction = db.CreateTransaction();
            var result = await transaction.ExecuteAsync();

            // Add current timestamp
            await db.SortedSetAddAsync(key, now.Ticks, now.Ticks);

            // Remove old entries
            await db.SortedSetRemoveRangeByScoreAsync(key, 0, (now.AddSeconds(-window)).Ticks);

            // Get count of requests in window
            var requestCount = await db.SortedSetLengthAsync(key);

            // Set expiry on the key
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(window));

            return requestCount > permitLimit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing Redis for rate limiting. Falling back to in-memory cache.");
            return IsRateLimitExceededInMemory(key, permitLimit, window);
        }
    }

    private bool IsRateLimitExceededInMemory(string key, int permitLimit, int window)
    {
        var queue = _requestStore.GetOrAdd(key, _ => new Queue<DateTime>());
        var now = DateTime.UtcNow;

        // Remove old timestamps
        while (queue.Count > 0 && queue.Peek() < now.AddSeconds(-window))
        {
            queue.Dequeue();
        }

        if (queue.Count >= permitLimit)
        {
            return true;
        }

        queue.Enqueue(now);
        return false;
    }
}

// Extension method for middleware registration
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
