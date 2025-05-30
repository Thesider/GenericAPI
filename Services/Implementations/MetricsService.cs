using GenericAPI.Services.Interfaces;
using Prometheus;
using System.Diagnostics;

namespace GenericAPI.Services.Implementations;

public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;

    // Prometheus metrics
    private static readonly Counter RequestCounter = Metrics
        .CreateCounter("http_requests_total", "Total HTTP requests", new[] { "method", "endpoint", "status_code" });

    private static readonly Histogram RequestDuration = Metrics
        .CreateHistogram("http_request_duration_seconds", "HTTP request duration in seconds", new[] { "method", "endpoint" });

    private static readonly Gauge RequestSize = Metrics
        .CreateGauge("http_request_size_bytes", "HTTP request size in bytes", new[] { "endpoint" });

    private static readonly Gauge ResponseSize = Metrics
        .CreateGauge("http_response_size_bytes", "HTTP response size in bytes", new[] { "endpoint" });

    // Business metrics
    private static readonly Counter UserRegistrations = Metrics
        .CreateCounter("user_registrations_total", "Total user registrations");

    private static readonly Counter SuccessfulLogins = Metrics
        .CreateCounter("successful_logins_total", "Total successful logins");

    private static readonly Counter FailedLogins = Metrics
        .CreateCounter("failed_logins_total", "Total failed logins");

    private static readonly Histogram OrderAmount = Metrics
        .CreateHistogram("order_amount", "Order amounts", new[] { "currency" });

    private static readonly Counter ProductViews = Metrics
        .CreateCounter("product_views_total", "Total product views", new[] { "product_id" });

    private static readonly Counter CacheHits = Metrics
        .CreateCounter("cache_hits_total", "Total cache hits", new[] { "cache_type" });

    private static readonly Counter CacheMisses = Metrics
        .CreateCounter("cache_misses_total", "Total cache misses", new[] { "cache_type" });

    // System metrics
    private static readonly Histogram DatabaseQueryDuration = Metrics
        .CreateHistogram("database_query_duration_seconds", "Database query duration in seconds", new[] { "operation" });

    private static readonly Gauge DatabaseConnections = Metrics
        .CreateGauge("database_connections_active", "Active database connections");

    private static readonly Gauge MemoryUsage = Metrics
        .CreateGauge("memory_usage_bytes", "Memory usage in bytes");

    private static readonly Gauge CpuUsage = Metrics
        .CreateGauge("cpu_usage_percent", "CPU usage percentage");

    // Error metrics
    private static readonly Counter ErrorCounter = Metrics
        .CreateCounter("errors_total", "Total errors", new[] { "error_type", "source" });

    private static readonly Counter ExceptionCounter = Metrics
        .CreateCounter("exceptions_total", "Total exceptions", new[] { "exception_type", "context" });

    // Security metrics
    private static readonly Counter SecurityEvents = Metrics
        .CreateCounter("security_events_total", "Total security events", new[] { "event_type" });

    private static readonly Counter SuspiciousActivity = Metrics
        .CreateCounter("suspicious_activity_total", "Total suspicious activities", new[] { "activity_type" });

    private static readonly Counter RateLimitHits = Metrics
        .CreateCounter("rate_limit_hits_total", "Total rate limit hits", new[] { "endpoint" });

    // Health metrics
    private static readonly Gauge HealthCheckResults = Metrics
        .CreateGauge("health_check_status", "Health check status (1 = healthy, 0 = unhealthy)", new[] { "health_check_name" });

    private static readonly Histogram HealthCheckDuration = Metrics
        .CreateHistogram("health_check_duration_seconds", "Health check duration in seconds", new[] { "health_check_name" });

    private static readonly Gauge ServiceAvailability = Metrics
        .CreateGauge("service_availability", "Service availability (1 = available, 0 = unavailable)", new[] { "service_name" });

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
    }

    public void RecordRequestDuration(string endpoint, string method, int statusCode, double durationMs)
    {
        try
        {
            RequestDuration.WithLabels(method, endpoint).Observe(durationMs / 1000.0);
            _logger.LogDebug("Recorded request duration: {Endpoint} {Method} {StatusCode} {Duration}ms", 
                endpoint, method, statusCode, durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record request duration metric");
        }
    }

    public void IncrementRequestCounter(string endpoint, string method, int statusCode)
    {
        try
        {
            RequestCounter.WithLabels(method, endpoint, statusCode.ToString()).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment request counter");
        }
    }

    public void RecordRequestSize(string endpoint, long bytes)
    {
        try
        {
            RequestSize.WithLabels(endpoint).Set(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record request size");
        }
    }

    public void RecordResponseSize(string endpoint, long bytes)
    {
        try
        {
            ResponseSize.WithLabels(endpoint).Set(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record response size");
        }
    }

    public void IncrementUserRegistrations()
    {
        try
        {
            UserRegistrations.Inc();
            _logger.LogDebug("User registration metric incremented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment user registrations");
        }
    }

    public void IncrementSuccessfulLogins()
    {
        try
        {
            SuccessfulLogins.Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment successful logins");
        }
    }

    public void IncrementFailedLogins()
    {
        try
        {
            FailedLogins.Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment failed logins");
        }
    }

    public void RecordOrderCreated(decimal amount)
    {
        try
        {
            OrderAmount.WithLabels("USD").Observe((double)amount);
            _logger.LogDebug("Order amount recorded: {Amount}", amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record order amount");
        }
    }

    public void RecordProductViewed(string productId)
    {
        try
        {
            ProductViews.WithLabels(productId).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record product view");
        }
    }

    public void RecordCacheHit(string cacheKey)
    {
        try
        {
            var cacheType = GetCacheType(cacheKey);
            CacheHits.WithLabels(cacheType).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record cache hit");
        }
    }

    public void RecordCacheMiss(string cacheKey)
    {
        try
        {
            var cacheType = GetCacheType(cacheKey);
            CacheMisses.WithLabels(cacheType).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record cache miss");
        }
    }

    public void RecordDatabaseQueryDuration(string operation, double durationMs)
    {
        try
        {
            DatabaseQueryDuration.WithLabels(operation).Observe(durationMs / 1000.0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record database query duration");
        }
    }

    public void IncrementDatabaseConnections()
    {
        try
        {
            DatabaseConnections.Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment database connections");
        }
    }

    public void DecrementDatabaseConnections()
    {
        try
        {
            DatabaseConnections.Dec();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrement database connections");
        }
    }

    public void RecordMemoryUsage(long bytes)
    {
        try
        {
            MemoryUsage.Set(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record memory usage");
        }
    }

    public void RecordCpuUsage(double percentage)
    {
        try
        {
            CpuUsage.Set(percentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record CPU usage");
        }
    }

    public void IncrementErrorCounter(string errorType, string? source = null)
    {
        try
        {
            ErrorCounter.WithLabels(errorType, source ?? "unknown").Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment error counter");
        }
    }

    public void RecordExceptionOccurred(Exception exception, string? context = null)
    {
        try
        {
            ExceptionCounter.WithLabels(exception.GetType().Name, context ?? "unknown").Inc();
            _logger.LogDebug("Exception metric recorded: {ExceptionType} in {Context}", 
                exception.GetType().Name, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record exception metric");
        }
    }

    public void IncrementSecurityEvent(string eventType)
    {
        try
        {
            SecurityEvents.WithLabels(eventType).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment security event");
        }
    }

    public void RecordSuspiciousActivity(string activityType, string? details = null)
    {
        try
        {
            SuspiciousActivity.WithLabels(activityType).Inc();
            _logger.LogWarning("Suspicious activity recorded: {ActivityType} - {Details}", activityType, details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record suspicious activity");
        }
    }

    public void IncrementRateLimitHit(string endpoint)
    {
        try
        {
            RateLimitHits.WithLabels(endpoint).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment rate limit hit");
        }
    }

    public void RecordCustomMetric(string name, double value, Dictionary<string, string>? tags = null)
    {
        try
        {
            // For custom metrics, you might want to use a different approach
            // This is a simplified implementation
            _logger.LogInformation("Custom metric: {Name} = {Value} {@Tags}", name, value, tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record custom metric");
        }
    }

    public void IncrementCustomCounter(string name, Dictionary<string, string>? tags = null)
    {
        try
        {
            _logger.LogInformation("Custom counter incremented: {Name} {@Tags}", name, tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment custom counter");
        }
    }

    public void RecordHealthCheckResult(string healthCheckName, bool isHealthy, double durationMs)
    {
        try
        {
            HealthCheckResults.WithLabels(healthCheckName).Set(isHealthy ? 1 : 0);
            HealthCheckDuration.WithLabels(healthCheckName).Observe(durationMs / 1000.0);
            
            _logger.LogDebug("Health check result recorded: {HealthCheck} = {Status} in {Duration}ms", 
                healthCheckName, isHealthy ? "Healthy" : "Unhealthy", durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record health check result");
        }
    }

    public void RecordServiceAvailability(string serviceName, bool isAvailable)
    {
        try
        {
            ServiceAvailability.WithLabels(serviceName).Set(isAvailable ? 1 : 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record service availability");
        }
    }

    private static string GetCacheType(string cacheKey)
    {
        if (cacheKey.StartsWith("user_"))
            return "user";
        if (cacheKey.StartsWith("product_"))
            return "product";
        if (cacheKey.StartsWith("order_"))
            return "order";
        
        return "general";
    }
}
