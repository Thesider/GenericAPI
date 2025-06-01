using GenericAPI.Models;
using Microsoft.AspNetCore.Http;

namespace GenericAPI.Services
{
    /// <summary>
    /// Interface for file upload service operations
    /// </summary>
    public interface IFileUploadService
    {
        /// <summary>
        /// Uploads a single file
        /// </summary>
        /// <param name="file">File to upload</param>
        /// <param name="folder">Target folder (optional)</param>
        /// <returns>Upload result</returns>
        Task<FileUploadResult> UploadFileAsync(IFormFile file, string? folder = null);

        /// <summary>
        /// Uploads multiple files
        /// </summary>
        /// <param name="files">Files to upload</param>
        /// <param name="folder">Target folder (optional)</param>
        /// <returns>List of upload results</returns>
        Task<List<FileUploadResult>> UploadFilesAsync(IList<IFormFile> files, string? folder = null);

        /// <summary>
        /// Deletes a file
        /// </summary>
        /// <param name="filePath">Path to file to delete</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Gets the URL for accessing a file
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Accessible URL</returns>
        string GetFileUrl(string filePath);

        /// <summary>
        /// Validates file before upload
        /// </summary>
        /// <param name="file">File to validate</param>
        /// <returns>Validation result</returns>
        (bool IsValid, string? Error) ValidateFile(IFormFile file);
    }
}
