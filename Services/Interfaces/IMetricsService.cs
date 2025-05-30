namespace GenericAPI.Services.Interfaces;

public interface IMetricsService
{
    // Request metrics
    void RecordRequestDuration(string endpoint, string method, int statusCode, double durationMs);
    void IncrementRequestCounter(string endpoint, string method, int statusCode);
    void RecordRequestSize(string endpoint, long bytes);
    void RecordResponseSize(string endpoint, long bytes);

    // Business metrics
    void IncrementUserRegistrations();
    void IncrementSuccessfulLogins();
    void IncrementFailedLogins();
    void RecordOrderCreated(decimal amount);
    void RecordProductViewed(string productId);
    void RecordCacheHit(string cacheKey);
    void RecordCacheMiss(string cacheKey);

    // System metrics
    void RecordDatabaseQueryDuration(string operation, double durationMs);
    void IncrementDatabaseConnections();
    void DecrementDatabaseConnections();
    void RecordMemoryUsage(long bytes);
    void RecordCpuUsage(double percentage);

    // Error metrics
    void IncrementErrorCounter(string errorType, string? source = null);
    void RecordExceptionOccurred(Exception exception, string? context = null);

    // Security metrics
    void IncrementSecurityEvent(string eventType);
    void RecordSuspiciousActivity(string activityType, string? details = null);
    void IncrementRateLimitHit(string endpoint);

    // Custom metrics
    void RecordCustomMetric(string name, double value, Dictionary<string, string>? tags = null);
    void IncrementCustomCounter(string name, Dictionary<string, string>? tags = null);

    // Health and availability
    void RecordHealthCheckResult(string healthCheckName, bool isHealthy, double durationMs);
    void RecordServiceAvailability(string serviceName, bool isAvailable);
}

public class MetricEntry
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = "Gauge"; // Counter, Gauge, Histogram, Summary
}
