using TenderManagement.Application.Interfaces;

namespace TenderManagement.Infrastructure.Services
{
    /// <summary>
    /// Local-disk file storage under wwwroot/uploads. Swap this implementation for an
    /// Azure Blob / S3 client in production without touching the Application layer.
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly string _rootPath;

        public FileStorageService(string webRootPath)
        {
            _rootPath = Path.Combine(webRootPath, "uploads");
        }

        public async Task<(string FileName, string FilePath)> SaveFileAsync(Stream fileStream, string originalFileName, string subFolder)
        {
            var safeExtension = Path.GetExtension(originalFileName);
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
            if (!allowedExtensions.Contains(safeExtension.ToLowerInvariant()))
            {
                throw new Application.Common.BusinessRuleException($"File type '{safeExtension}' is not permitted.");
            }

            var folder = Path.Combine(_rootPath, subFolder);
            Directory.CreateDirectory(folder);

            var storedFileName = $"{Guid.NewGuid()}{safeExtension}";
            var fullPath = Path.Combine(folder, storedFileName);

            using var output = new FileStream(fullPath, FileMode.Create);
            await fileStream.CopyToAsync(output);

            var relativePath = Path.Combine("uploads", subFolder, storedFileName).Replace("\\", "/");
            return (storedFileName, relativePath);
        }

        public Task DeleteFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.CompletedTask;
            }

            var normalized = filePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            if (normalized.StartsWith($"uploads{Path.DirectorySeparatorChar}"))
            {
                normalized = normalized[("uploads" + Path.DirectorySeparatorChar).Length..];
            }

            var fullPath = Path.Combine(_rootPath, normalized);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }
    }
}
