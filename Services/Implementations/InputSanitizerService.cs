using System.Text.RegularExpressions;
using Ganss.Xss;
using GenericAPI.Services.Interfaces;

namespace GenericAPI.Services.Implementations;

public class InputSanitizerService : IInputSanitizerService
{
    private readonly HtmlSanitizer _htmlSanitizer;
    private readonly ILogger<InputSanitizerService> _logger;

    // SQL injection patterns
    private static readonly string[] SqlInjectionPatterns = {
        @"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE){0,1}|INSERT( +INTO){0,1}|MERGE|SELECT|UPDATE|UNION( +ALL){0,1})\b)",
        @"(\b(AND|OR)\b.{1,6}?(=|>|<|\!|\<|\>))",
        @"(\b(CHAR|NCHAR|VARCHAR|NVARCHAR)\s*\(\s*\d+\s*\))",
        @"(\bCAST\s*\()",
        @"(\bCONVERT\s*\()",
        @"(\bSUBSTRING\s*\()",
        @"(\bDATALENGTH\s*\()",
        @"(\bLEN\s*\()",
        @"(\bISNULL\s*\()",
        @"(\bSYS\.)",
        @"(\bINFORMATION_SCHEMA\.)",
        @"(\bSYSOBJECTS\b)",
        @"(\bSYSCOLUMNS\b)",
        @"(\bxp_cmdshell\b)",
        @"(\bsp_\w+)",
        @"(\b0x[0-9A-Fa-f]+)",
        @"(\b'[^']*')",
        @"(\b--)",
        @"(\b/\*.*?\*/)",
        @"(\bunion\b.*\bselect\b)",
        @"(\bdrop\b.*\btable\b)",
        @"(\binsert\b.*\binto\b)",
        @"(\bdelete\b.*\bfrom\b)",
        @"(\bupdate\b.*\bset\b)"
    };

    // XSS patterns
    private static readonly string[] XssPatterns = {
        @"<script[^>]*>.*?</script>",
        @"javascript:",
        @"vbscript:",
        @"onload\s*=",
        @"onerror\s*=",
        @"onclick\s*=",
        @"onmouseover\s*=",
        @"onfocus\s*=",
        @"onblur\s*=",
        @"onchange\s*=",
        @"onsubmit\s*=",
        @"<iframe[^>]*>.*?</iframe>",
        @"<object[^>]*>.*?</object>",
        @"<embed[^>]*>",
        @"<link[^>]*>",
        @"<meta[^>]*>",
        @"<style[^>]*>.*?</style>",
        @"expression\s*\(",
        @"url\s*\(",
        @"@import",
        @"alert\s*\(",
        @"confirm\s*\(",
        @"prompt\s*\(",
        @"document\.",
        @"window\.",
        @"eval\s*\(",
        @"setTimeout\s*\(",
        @"setInterval\s*\("
    };

    public InputSanitizerService(ILogger<InputSanitizerService> logger)
    {
        _logger = logger;
        _htmlSanitizer = new HtmlSanitizer();
        
        // Configure allowed tags and attributes
        _htmlSanitizer.AllowedTags.Clear();
        _htmlSanitizer.AllowedTags.Add("p");
        _htmlSanitizer.AllowedTags.Add("br");
        _htmlSanitizer.AllowedTags.Add("strong");
        _htmlSanitizer.AllowedTags.Add("em");
        _htmlSanitizer.AllowedTags.Add("u");
        _htmlSanitizer.AllowedTags.Add("b");
        _htmlSanitizer.AllowedTags.Add("i");
        
        _htmlSanitizer.AllowedAttributes.Clear();
        _htmlSanitizer.AllowedAttributes.Add("class");
        
        _htmlSanitizer.AllowedCssProperties.Clear();
    }

    public string SanitizeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        try
        {
            var sanitized = _htmlSanitizer.Sanitize(input);
            _logger.LogDebug("HTML sanitized: {Original} -> {Sanitized}", input, sanitized);
            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing HTML input: {Input}", input);
            return string.Empty;
        }
    }

    public string SanitizeText(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove null characters
        input = input.Replace("\0", string.Empty);
        
        // Remove control characters except common whitespace
        input = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);
        
        // Normalize whitespace
        input = Regex.Replace(input, @"\s+", " ");
        
        // Trim
        input = input.Trim();
        
        return input;
    }

    public string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;

        // Remove path traversal attempts
        fileName = fileName.Replace("..", "").Replace("/", "").Replace("\\", "");
        
        // Remove invalid file name characters
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var invalidChar in invalidChars)
        {
            fileName = fileName.Replace(invalidChar, '_');
        }
        
        // Limit length
        if (fileName.Length > 255)
        {
            var extension = Path.GetExtension(fileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var maxNameLength = 255 - extension.Length;
            fileName = nameWithoutExtension[..Math.Min(nameWithoutExtension.Length, maxNameLength)] + extension;
        }
        
        return fileName;
    }

    public string SanitizeUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        try
        {
            var uri = new Uri(url);
            
            // Only allow HTTP and HTTPS schemes
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                _logger.LogWarning("Blocked URL with invalid scheme: {Url}", url);
                return string.Empty;
            }
            
            return uri.ToString();
        }
        catch (UriFormatException)
        {
            _logger.LogWarning("Invalid URL format: {Url}", url);
            return string.Empty;
        }
    }

    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        try
        {
            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            return emailRegex.IsMatch(email);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    public bool ContainsSqlInjectionPatterns(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        foreach (var pattern in SqlInjectionPatterns)
        {
            try
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    _logger.LogWarning("SQL injection pattern detected: {Pattern} in input: {Input}", pattern, input);
                    return true;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                _logger.LogWarning("Regex timeout for pattern: {Pattern}", pattern);
                continue;
            }
        }

        return false;
    }

    public bool ContainsXssPatterns(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        foreach (var pattern in XssPatterns)
        {
            try
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    _logger.LogWarning("XSS pattern detected: {Pattern} in input: {Input}", pattern, input);
                    return true;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                _logger.LogWarning("Regex timeout for pattern: {Pattern}", pattern);
                continue;
            }
        }

        return false;
    }

    public string RemoveSpecialCharacters(string input, string allowedCharacters = "")
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var pattern = @"[^a-zA-Z0-9\s" + Regex.Escape(allowedCharacters) + "]";
        return Regex.Replace(input, pattern, string.Empty);
    }

    public string EscapeForSql(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Replace single quotes with double single quotes
        return input.Replace("'", "''");
    }
}
