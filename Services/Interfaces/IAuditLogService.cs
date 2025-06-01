using GenericAPI.Models;

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
