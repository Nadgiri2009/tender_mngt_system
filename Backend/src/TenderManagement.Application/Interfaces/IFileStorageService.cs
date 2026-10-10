namespace TenderManagement.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<(string FileName, string FilePath)> SaveFileAsync(Stream fileStream, string originalFileName, string subFolder);
        Task DeleteFileAsync(string filePath);
    }
}
