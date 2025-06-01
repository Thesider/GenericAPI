namespace GenericAPI.Models;

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public long FileSize { get; set; }
    public string? Error { get; set; }
    public string? Url { get; set; }
}
