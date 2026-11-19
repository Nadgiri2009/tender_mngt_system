namespace TenderManagement.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<(string FileName, string FilePath)> SaveFileAsync(Stream fileStream, string originalFileName, string subFolder);
        Task<(string FileName, string FilePath)> SavePrivateFileAsync(Stream fileStream, string originalFileName, string subFolder);
        Task<Stream?> OpenPrivateFileAsync(string filePath);
        Task DeleteFileAsync(string filePath);
    }
}
