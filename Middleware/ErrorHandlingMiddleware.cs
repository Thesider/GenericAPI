using System.Net;
using Microsoft.EntityFrameworkCore;

namespace GenericAPI.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = GetErrorDetails(exception);
        context.Response.StatusCode = statusCode;

        var response = new
        {
            error = new
            {
                message = message,
                details = _env.IsDevelopment() ? exception.ToString() : null
            },
            statusCode
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private (int StatusCode, string Message) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            ResourceNotFoundException ex => (
                (int)HttpStatusCode.NotFound,
                ex.Message
            ),
            ValidationException ex => (
                (int)HttpStatusCode.BadRequest,
                ex.Message
            ),
            BusinessRuleException ex => (
                (int)HttpStatusCode.BadRequest,
                ex.Message
            ),
            UnauthorizedAccessException _ => (
                (int)HttpStatusCode.Unauthorized,
                "You are not authorized to perform this action"
            ),
            DbUpdateConcurrencyException _ => (
                (int)HttpStatusCode.Conflict,
                "The resource was modified by another user"
            ),
            DbUpdateException _ => (
                (int)HttpStatusCode.BadRequest,
                "An error occurred while updating the database"
            ),
            ArgumentException or InvalidOperationException => (
                (int)HttpStatusCode.BadRequest,
                exception.Message
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "An unexpected error occurred"
            )
        };
    }
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
