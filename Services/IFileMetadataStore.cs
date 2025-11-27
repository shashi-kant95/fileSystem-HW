using System;
using FileSystem_Honeywell.Model;

namespace FileSystem_Honeywell.Services
{
    public interface IFileMetadataStore
    {
        Task<FileRecord> AddAsync(FileRecord record, CancellationToken cancellationToken = default);
        Task<FileRecord?> GetAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FileRecord>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}

