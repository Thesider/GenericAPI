namespace GenericAPI.Services.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(AuditLogEntry entry);
    Task LogUserActionAsync(string userId, string action, string? details = null, string? ipAddress = null);
    Task LogSecurityEventAsync(string eventType, string details, string? userId = null, string? ipAddress = null);
    Task LogSystemEventAsync(string eventType, string details);
    Task<IEnumerable<AuditLogEntry>> GetAuditLogsAsync(DateTime? from = null, DateTime? to = null, string? userId = null, string? action = null, int page = 1, int pageSize = 50);
    Task<bool> VerifyIntegrityAsync(string entryId);
}

public class AuditLogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Severity { get; set; } = "Information";
    public string Category { get; set; } = "UserAction";
    public string? TenantId { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}

public enum AuditSeverity
{
    Information,
    Warning,
    Error,
    Critical
}

public enum AuditCategory
{
    UserAction,
    SecurityEvent,
    SystemEvent,
    DataAccess,
    Authentication,
    Authorization
}
