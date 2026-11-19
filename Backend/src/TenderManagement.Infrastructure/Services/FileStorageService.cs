using TenderManagement.Application.Interfaces;
using TenderManagement.Application.Common;

namespace TenderManagement.Infrastructure.Services
{
    /// <summary>
    /// Local-disk file storage for public tender files and private application documents.
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly string _publicRootPath;
        private readonly string _privateRootPath;

        public FileStorageService(string webRootPath, string contentRootPath)
        {
            _publicRootPath = Path.Combine(webRootPath, "uploads");
            _privateRootPath = Path.Combine(contentRootPath, "App_Data", "uploads");
        }

        public async Task<(string FileName, string FilePath)> SaveFileAsync(Stream fileStream, string originalFileName, string subFolder)
        {
            var safeExtension = Path.GetExtension(originalFileName);
            if (!DocumentUploadRules.AllowedExtensions.Contains(safeExtension))
            {
                throw new Application.Common.BusinessRuleException($"File type '{safeExtension}' is not permitted.");
            }

            var folder = Path.Combine(_publicRootPath, subFolder);
            Directory.CreateDirectory(folder);

            var storedFileName = $"{Guid.NewGuid()}{safeExtension}";
            var fullPath = Path.Combine(folder, storedFileName);

            using var output = new FileStream(fullPath, FileMode.Create);
            await fileStream.CopyToAsync(output);

            var relativePath = Path.Combine("uploads", subFolder, storedFileName).Replace("\\", "/");
            return (storedFileName, relativePath);
        }

        public async Task<(string FileName, string FilePath)> SavePrivateFileAsync(
            Stream fileStream, string originalFileName, string subFolder)
        {
            var safeExtension = Path.GetExtension(originalFileName);
            if (!DocumentUploadRules.AllowedExtensions.Contains(safeExtension))
            {
                throw new BusinessRuleException($"File type '{safeExtension}' is not permitted.");
            }

            var folder = Path.Combine(_privateRootPath, subFolder);
            Directory.CreateDirectory(folder);

            var storedFileName = $"{Guid.NewGuid()}{safeExtension}";
            var fullPath = Path.Combine(folder, storedFileName);
            await using (var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await fileStream.CopyToAsync(output);
            }

            var relativePath = Path.Combine("private-uploads", subFolder, storedFileName).Replace("\\", "/");
            return (storedFileName, relativePath);
        }

        public Task<Stream?> OpenPrivateFileAsync(string filePath)
        {
            var relativePath = filePath.Replace('/', Path.DirectorySeparatorChar);
            const string privatePrefix = "private-uploads";
            if (!relativePath.StartsWith(privatePrefix + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The requested file is not a private application document.");
            }

            var normalized = relativePath[(privatePrefix.Length + 1)..];
            var fullPath = Path.GetFullPath(Path.Combine(_privateRootPath, normalized));
            var rootPrefix = Path.GetFullPath(_privateRootPath) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The requested file path is invalid.");
            }

            return Task.FromResult<Stream?>(File.Exists(fullPath)
                ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                : null);
        }

        public Task DeleteFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.CompletedTask;
            }

            var normalized = filePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            string rootPath;
            if (normalized.StartsWith($"private-uploads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[("private-uploads" + Path.DirectorySeparatorChar).Length..];
                rootPath = _privateRootPath;
            }
            else if (normalized.StartsWith($"uploads{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[("uploads" + Path.DirectorySeparatorChar).Length..];
                rootPath = _publicRootPath;
            }
            else
                return Task.CompletedTask;

            var fullPath = Path.GetFullPath(Path.Combine(rootPath, normalized));
            var rootPrefix = Path.GetFullPath(rootPath) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }
    }
}
