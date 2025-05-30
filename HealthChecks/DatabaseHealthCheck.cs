using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using GenericAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace GenericAPI.HealthChecks
{
    /// <summary>
    /// Database health check
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(ApplicationDbContext context, ILogger<DatabaseHealthCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to execute a simple query
                await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
                
                _logger.LogDebug("Database health check passed");
                return HealthCheckResult.Healthy("Database is accessible");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy("Database is not accessible", ex);
            }
        }
    }
}
