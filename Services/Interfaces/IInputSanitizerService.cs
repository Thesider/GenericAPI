namespace GenericAPI.Services.Interfaces;

public interface IInputSanitizerService
{
    string SanitizeHtml(string input);
    string SanitizeText(string input);
    string SanitizeFileName(string fileName);
    string SanitizeUrl(string url);
    bool IsValidEmail(string email);
    bool ContainsSqlInjectionPatterns(string input);
    bool ContainsXssPatterns(string input);
    string RemoveSpecialCharacters(string input, string allowedCharacters = "");
    string EscapeForSql(string input);
}
