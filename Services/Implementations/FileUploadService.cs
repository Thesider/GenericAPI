using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenericAPI.Services
{
    /// <summary>
    /// Local file system implementation of file upload service
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileUploadService> _logger;
        private readonly string _uploadsPath;
        private readonly long _maxFileSize;
        private readonly string[] _allowedExtensions;
        private readonly string _baseUrl;

        public FileUploadService(
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;

            _uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads");
            _maxFileSize = _configuration.GetValue<long>("FileUpload:MaxFileSize", 10 * 1024 * 1024); // 10MB default
            _allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>() 
                ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt" };
            _baseUrl = _configuration.GetValue<string>("FileUpload:BaseUrl", "/uploads/");

            // Ensure uploads directory exists
            Directory.CreateDirectory(_uploadsPath);
        }

        public async Task<FileUploadResult> UploadFileAsync(IFormFile file, string? folder = null)
        {
            var validation = ValidateFile(file);
            if (!validation.IsValid)
            {
                return new FileUploadResult
                {
                    Success = false,
                    Error = validation.Error
                };
            }

            try
            {
                var fileName = GenerateUniqueFileName(file.FileName);
                var targetFolder = string.IsNullOrEmpty(folder) ? _uploadsPath : Path.Combine(_uploadsPath, folder);
                
                // Ensure target folder exists
                Directory.CreateDirectory(targetFolder);
                
                var filePath = Path.Combine(targetFolder, fileName);
                var relativePath = Path.GetRelativePath(_uploadsPath, filePath).Replace('\\', '/');

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var result = new FileUploadResult
                {
                    Success = true,
                    FileName = fileName,
                    FilePath = relativePath,
                    FileSize = file.Length,
                    Url = GetFileUrl(relativePath)
                };

                _logger.LogInformation("File uploaded successfully: {FileName} ({FileSize} bytes)", fileName, file.Length);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
                return new FileUploadResult
                {
                    Success = false,
                    Error = "An error occurred while uploading the file."
                };
            }
        }

        public async Task<List<FileUploadResult>> UploadFilesAsync(IList<IFormFile> files, string? folder = null)
        {
            var results = new List<FileUploadResult>();

            foreach (var file in files)
            {
                var result = await UploadFileAsync(file, folder);
                results.Add(result);
            }

            return results;
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_uploadsPath, filePath);
                
                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                    return true;
                }

                _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public string GetFileUrl(string filePath)
        {
            return $"{_baseUrl.TrimEnd('/')}/{filePath.TrimStart('/')}";
        }

        public (bool IsValid, string? Error) ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "No file provided or file is empty.");
            }

            if (file.Length > _maxFileSize)
            {
                return (false, $"File size exceeds maximum allowed size of {_maxFileSize / 1024 / 1024} MB.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return (false, $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            // Additional security check - validate file content type
            var contentType = file.ContentType.ToLowerInvariant();
            if (!IsValidContentType(contentType, extension))
            {
                return (false, "File content type does not match file extension.");
            }

            return (true, null);
        }

        private string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var guid = Guid.NewGuid().ToString("N")[..8];
            
            return $"{nameWithoutExtension}_{timestamp}_{guid}{extension}";
        }

        private static bool IsValidContentType(string contentType, string extension)
        {
            var validMappings = new Dictionary<string, string[]>
            {
                { ".jpg", new[] { "image/jpeg" } },
                { ".jpeg", new[] { "image/jpeg" } },
                { ".png", new[] { "image/png" } },
                { ".gif", new[] { "image/gif" } },
                { ".pdf", new[] { "application/pdf" } },
                { ".doc", new[] { "application/msword" } },
                { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
                { ".txt", new[] { "text/plain" } }
            };

            if (validMappings.TryGetValue(extension, out var validTypes))
            {
                return validTypes.Contains(contentType);
            }

            return false;
        }
    }
}
