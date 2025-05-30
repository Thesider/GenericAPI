using System.Security.Cryptography;
using System.Text;

namespace GenericAPI.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger, SecurityHeadersOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate nonce for CSP
        var nonce = GenerateNonce();
        context.Items["csp-nonce"] = nonce;

        // Add security headers
        AddSecurityHeaders(context, nonce);

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context, string nonce)
    {
        var response = context.Response;

        // Content Security Policy
        if (_options.EnableCSP)
        {
            var csp = BuildCSP(nonce);
            response.Headers.Append("Content-Security-Policy", csp);
            _logger.LogDebug("Added CSP header: {CSP}", csp);
        }

        // HTTP Strict Transport Security
        if (_options.EnableHSTS)
        {
            response.Headers.Append("Strict-Transport-Security", _options.HSTSValue);
        }

        // X-Frame-Options
        if (_options.EnableXFrameOptions)
        {
            response.Headers.Append("X-Frame-Options", _options.XFrameOptionsValue);
        }

        // X-Content-Type-Options
        if (_options.EnableXContentTypeOptions)
        {
            response.Headers.Append("X-Content-Type-Options", "nosniff");
        }

        // X-XSS-Protection
        if (_options.EnableXXSSProtection)
        {
            response.Headers.Append("X-XSS-Protection", "1; mode=block");
        }

        // Referrer Policy
        if (_options.EnableReferrerPolicy)
        {
            response.Headers.Append("Referrer-Policy", _options.ReferrerPolicyValue);
        }

        // Permissions Policy
        if (_options.EnablePermissionsPolicy)
        {
            response.Headers.Append("Permissions-Policy", _options.PermissionsPolicyValue);
        }

        // Remove server information
        if (_options.RemoveServerHeader)
        {
            response.Headers.Remove("Server");
        }

        // Add custom headers
        foreach (var header in _options.CustomHeaders)
        {
            response.Headers.Append(header.Key, header.Value);
        }
    }

    private string BuildCSP(string nonce)
    {
        var cspBuilder = new StringBuilder();
        
        cspBuilder.Append("default-src 'self'; ");
        cspBuilder.Append($"script-src 'self' 'nonce-{nonce}' 'strict-dynamic'; ");
        cspBuilder.Append("style-src 'self' 'unsafe-inline'; ");
        cspBuilder.Append("img-src 'self' data: https:; ");
        cspBuilder.Append("font-src 'self' https:; ");
        cspBuilder.Append("connect-src 'self'; ");
        cspBuilder.Append("media-src 'self'; ");
        cspBuilder.Append("object-src 'none'; ");
        cspBuilder.Append("child-src 'none'; ");
        cspBuilder.Append("frame-ancestors 'none'; ");
        cspBuilder.Append("form-action 'self'; ");
        cspBuilder.Append("base-uri 'self'; ");
        cspBuilder.Append("manifest-src 'self'; ");
        
        if (_options.CSPReportUri != null)
        {
            cspBuilder.Append($"report-uri {_options.CSPReportUri}; ");
        }

        return cspBuilder.ToString().TrimEnd();
    }

    private static string GenerateNonce()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public class SecurityHeadersOptions
{
    public bool EnableCSP { get; set; } = true;
    public bool EnableHSTS { get; set; } = true;
    public bool EnableXFrameOptions { get; set; } = true;
    public bool EnableXContentTypeOptions { get; set; } = true;
    public bool EnableXXSSProtection { get; set; } = true;
    public bool EnableReferrerPolicy { get; set; } = true;
    public bool EnablePermissionsPolicy { get; set; } = true;
    public bool RemoveServerHeader { get; set; } = true;

    public string HSTSValue { get; set; } = "max-age=31536000; includeSubDomains; preload";
    public string XFrameOptionsValue { get; set; } = "DENY";
    public string ReferrerPolicyValue { get; set; } = "strict-origin-when-cross-origin";
    public string PermissionsPolicyValue { get; set; } = "camera=(), microphone=(), geolocation=(), interest-cohort=()";
    public string? CSPReportUri { get; set; }

    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder, Action<SecurityHeadersOptions>? configure = null)
    {
        var options = new SecurityHeadersOptions();
        configure?.Invoke(options);
        
        return builder.UseMiddleware<SecurityHeadersMiddleware>(options);
    }
}
