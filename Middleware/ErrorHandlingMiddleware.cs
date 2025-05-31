using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using GenericAPI.Services.Interfaces;

namespace GenericAPI.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IServiceProvider _serviceProvider;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IWebHostEnvironment env,
        IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _env = env;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await LogErrorAsync(context, ex, correlationId, stopwatch.ElapsedMilliseconds);
            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        const string correlationIdHeaderName = "X-Correlation-ID";
        
        if (context.Request.Headers.TryGetValue(correlationIdHeaderName, out var correlationId))
        {
            return correlationId.ToString();
        }
        
        var newCorrelationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = newCorrelationId;
        context.Response.Headers.Add(correlationIdHeaderName, newCorrelationId);
        
        return newCorrelationId;
    }

    private async Task LogErrorAsync(HttpContext context, Exception exception, string correlationId, long elapsedMs)
    {
        var errorDetails = new
        {
            CorrelationId = correlationId,
            RequestPath = context.Request.Path,
            RequestMethod = context.Request.Method,
            QueryString = context.Request.QueryString.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            UserId = context.User?.Identity?.Name,
            ClientIP = GetClientIpAddress(context),
            ElapsedMilliseconds = elapsedMs,
            ExceptionType = exception.GetType().Name,
            ExceptionMessage = exception.Message,
            StackTrace = _env.IsDevelopment() ? exception.StackTrace : null,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogError(exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {RequestPath}, Method: {RequestMethod}, UserId: {UserId}, ClientIP: {ClientIP}, ElapsedMs: {ElapsedMs}",
            correlationId, context.Request.Path, context.Request.Method, 
            context.User?.Identity?.Name, GetClientIpAddress(context), elapsedMs);

        // Use scoped services safely
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            var metricsService = scope.ServiceProvider.GetService<IMetricsService>();
            metricsService?.IncrementErrorCounter(exception.GetType().Name, context.Request.Path);
            metricsService?.RecordRequestDuration(context.Request.Path, context.Request.Method, 500, (double)elapsedMs);

            // Audit log for security-related errors
            if (IsSecurityRelatedError(exception))
            {
                var auditLogService = scope.ServiceProvider.GetService<IAuditLogService>();
                if (auditLogService != null)
                {
                    await auditLogService.LogSecurityEventAsync(
                        "SecurityException",
                        $"Security-related error occurred: {exception.GetType().Name}. Details: {JsonSerializer.Serialize(errorDetails)}",
                        context.User?.Identity?.Name,
                        GetClientIpAddress(context));
                }
            }
        }
        catch (Exception logException)
        {
            _logger.LogError(logException, "Error occurred while logging the original exception");
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors) = GetErrorDetails(exception);
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            CorrelationId = correlationId,
            Message = message,
            StatusCode = statusCode,
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path,
            Method = context.Request.Method,
            Errors = errors,
            Details = _env.IsDevelopment() ? exception.ToString() : null
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _env.IsDevelopment()
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private (int StatusCode, string Message, Dictionary<string, string[]>? Errors) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            FluentValidation.ValidationException validationEx => (
                (int)HttpStatusCode.BadRequest,
                "One or more validation errors occurred.",
                validationEx.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ),
            ResourceNotFoundException ex => (
                (int)HttpStatusCode.NotFound,
                ex.Message,
                null
            ),
            ValidationException ex => (
                (int)HttpStatusCode.BadRequest,
                ex.Message,
                null
            ),
            BusinessRuleException ex => (
                (int)HttpStatusCode.BadRequest,
                ex.Message,
                null
            ),
            UnauthorizedAccessException _ => (
                (int)HttpStatusCode.Unauthorized,
                "You are not authorized to perform this action.",
                null
            ),
            SecurityException _ => (
                (int)HttpStatusCode.Forbidden,
                "Access denied due to security policy.",
                null
            ),
            DbUpdateConcurrencyException _ => (
                (int)HttpStatusCode.Conflict,
                "The resource was modified by another user. Please refresh and try again.",
                null
            ),
            DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("UNIQUE constraint") == true => (
                (int)HttpStatusCode.Conflict,
                "A record with the same information already exists.",
                null
            ),
            DbUpdateException _ => (
                (int)HttpStatusCode.BadRequest,
                "An error occurred while updating the database.",
                null
            ),
            TimeoutException _ => (
                (int)HttpStatusCode.RequestTimeout,
                "The operation timed out. Please try again.",
                null
            ),
            ArgumentException or ArgumentNullException => (
                (int)HttpStatusCode.BadRequest,
                "Invalid request parameters.",
                null
            ),
            InvalidOperationException when exception.Message.Contains("authentication") => (
                (int)HttpStatusCode.Unauthorized,
                "Authentication failed.",
                null
            ),
            NotSupportedException _ => (
                (int)HttpStatusCode.MethodNotAllowed,
                "The requested operation is not supported.",
                null
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please contact support if the problem persists.",
                null
            )
        };
    }

    private string GetClientIpAddress(HttpContext context)
    {
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            // X-Forwarded-For can contain multiple IPs, take the first one
            ipAddress = ipAddress.Split(',')[0].Trim();
        }
        
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = context.Connection.RemoteIpAddress?.ToString();
        }
        
        return ipAddress ?? "Unknown";
    }

    private bool IsSecurityRelatedError(Exception exception)
    {
        return exception is UnauthorizedAccessException ||
               exception is SecurityException ||
               exception is InvalidOperationException && exception.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase);
    }
}

public class ErrorResponse
{
    public string CorrelationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
    public string? Details { get; set; }
}

public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}

// Custom exceptions
public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException() : base("The requested resource was not found")
    {
    }

    public ResourceNotFoundException(string message) : base(message)
    {
    }

    public ResourceNotFoundException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class ValidationException : Exception
{
    public ValidationException() : base("Validation failed")
    {
    }

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class BusinessRuleException : Exception
{
    public BusinessRuleException() : base("A business rule was violated")
    {
    }

    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class SecurityException : Exception
{
    public SecurityException() : base("A security violation occurred")
    {
    }

    public SecurityException(string message) : base(message)
    {
    }

    public SecurityException(string message, Exception inner) : base(message, inner)
    {
    }
}
