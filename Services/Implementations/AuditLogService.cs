using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GenericAPI.Data;
using GenericAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GenericAPI.Services.Implementations;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;

    public AuditLogService(ApplicationDbContext context, ILogger<AuditLogService> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _secretKey = _configuration["AuditLog:SecretKey"] ?? "DefaultSecretKeyForAuditLogging";
    }

    public async Task LogAsync(AuditLogEntry entry)
    {
        try
        {
            // Generate hash for integrity verification
            entry.Hash = GenerateHash(entry);

            // Store in database (you'll need to create AuditLogs table)
            await StoreAuditLogAsync(entry);

            // Also log to structured logging
            _logger.LogInformation("Audit: {Action} by {UserId} on {EntityType}:{EntityId} - {Details}",
                entry.Action, entry.UserId, entry.EntityType, entry.EntityId, entry.Details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry: {@Entry}", entry);
        }
    }

    public async Task LogUserActionAsync(string userId, string action, string? details = null, string? ipAddress = null)
    {
        var entry = new AuditLogEntry
        {
            UserId = userId,
            Action = action,
            Details = details ?? string.Empty,
            IpAddress = ipAddress ?? string.Empty,
            Category = AuditCategory.UserAction.ToString(),
            Severity = AuditSeverity.Information.ToString()
        };

        await LogAsync(entry);
    }

    public async Task LogSecurityEventAsync(string eventType, string details, string? userId = null, string? ipAddress = null)
    {
        var entry = new AuditLogEntry
        {
            UserId = userId ?? "System",
            Action = eventType,
            Details = details,
            IpAddress = ipAddress ?? string.Empty,
            Category = AuditCategory.SecurityEvent.ToString(),
            Severity = AuditSeverity.Warning.ToString()
        };

        await LogAsync(entry);
    }

    public async Task LogSystemEventAsync(string eventType, string details)
    {
        var entry = new AuditLogEntry
        {
            UserId = "System",
            Action = eventType,
            Details = details,
            Category = AuditCategory.SystemEvent.ToString(),
            Severity = AuditSeverity.Information.ToString()
        };

        await LogAsync(entry);
    }

    public async Task<IEnumerable<AuditLogEntry>> GetAuditLogsAsync(DateTime? from = null, DateTime? to = null, string? userId = null, string? action = null, int page = 1, int pageSize = 50)
    {
        try
        {
            // This would query from your AuditLogs table
            // For now, returning empty collection as table doesn't exist yet
            await Task.CompletedTask;
            return new List<AuditLogEntry>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit logs");
            return new List<AuditLogEntry>();
        }
    }

    public async Task<bool> VerifyIntegrityAsync(string entryId)
    {
        try
        {
            // This would verify the hash of a specific audit log entry
            // For now, returning true as table doesn't exist yet
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify audit log integrity for entry: {EntryId}", entryId);
            return false;
        }
    }

    private string GenerateHash(AuditLogEntry entry)
    {
        var hashInput = $"{entry.Id}{entry.UserId}{entry.Action}{entry.EntityType}{entry.EntityId}" +
                       $"{entry.Details}{entry.Timestamp:O}{_secretKey}";
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToBase64String(hashBytes);
    }

    private async Task StoreAuditLogAsync(AuditLogEntry entry)
    {
        try
        {
            // For now, we'll just log to file/console since we don't have the table yet
            // In a real implementation, you would:
            // _context.AuditLogs.Add(entry);
            // await _context.SaveChangesAsync();
            
            var serializedEntry = JsonSerializer.Serialize(entry, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            
            _logger.LogInformation("AUDIT_LOG: {SerializedEntry}", serializedEntry);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store audit log entry");
            throw;
        }
    }
}
