using System;

namespace FileSystem_Honeywell.Services
{
	public class FileStorageService :IFileStorageService
	{
        private readonly string _path;

        public FileStorageService(string path)
        {
            _path = path;
        }

        public Task DeleteFileAsync(string storedFileName, CancellationToken cancellationToken = default)
        {
            var filePath = GetPhysicalPath(storedFileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return Task.CompletedTask;
        }

        public Task<Stream> GetFileAsync(string storedFileName, CancellationToken cancellationToken = default)
        {
            var filePath = GetPhysicalPath(storedFileName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException();

            Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(stream);
        }

        public string GetPhysicalPath(string storedFileName)
        {
            var filePath = Path.Combine(_path, storedFileName);
            filePath = Path.GetFullPath(filePath);
            if (!filePath.StartsWith(_path))
                throw new InvalidOperationException("Invalid file path.");

            return filePath;
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string extension, CancellationToken cancellationToken = default)
        {
            var uniqueName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(_path, uniqueName);

            filePath = Path.GetFullPath(filePath);
            if (!filePath.StartsWith(_path))
                throw new InvalidOperationException("Invalid file path.");

            await using var fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
            await fileStream.CopyToAsync(fs, cancellationToken);

            return uniqueName;
        }
    }
}

