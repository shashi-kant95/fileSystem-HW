using System;
namespace FileSystem_Honeywell.Services
{
	public interface IFileStorageService
	{
        Task<string> SaveFileAsync(Stream fileStream, string extension, CancellationToken cancellationToken = default);
        Task<Stream> GetFileAsync(string storedFileName, CancellationToken cancellationToken = default);
        Task DeleteFileAsync(string storedFileName, CancellationToken cancellationToken = default);
        string GetPhysicalPath(string storedFileName);
    }
}

