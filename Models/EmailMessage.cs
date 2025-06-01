namespace GenericAPI.Models;

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public List<string> CC { get; set; } = new();
    public List<string> BCC { get; set; } = new();
    public Dictionary<string, byte[]> Attachments { get; set; } = new();
}
