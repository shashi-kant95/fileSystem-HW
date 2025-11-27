using System;
using FileSystem_Honeywell.Model;
using System.Text.Json;

namespace FileSystem_Honeywell.Services
{
    public class JsonFileMetadataStore : IFileMetadataStore
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private List<FileRecord> _cache = new();
        private bool _loaded = false;

        public JsonFileMetadataStore(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<FileRecord> AddAsync(FileRecord record, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                await EnsureLoadedAsync(cancellationToken);

                var nextId = _cache.Count == 0 ? 1 : _cache.Max(f => f.Id) + 1;
                record.Id = nextId;

                _cache.Add(record);
                await SaveAsync(cancellationToken);

                return record;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<FileRecord?> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                await EnsureLoadedAsync(cancellationToken);
                return _cache.FirstOrDefault(f => f.Id == id);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<IReadOnlyList<FileRecord>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                await EnsureLoadedAsync(cancellationToken);
                return _cache
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToList();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                await EnsureLoadedAsync(cancellationToken);
                var record = _cache.FirstOrDefault(f => f.Id == id);
                if (record == null) return false;

                _cache.Remove(record);
                await SaveAsync(cancellationToken);
                return true;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
        {
            if (_loaded) return;

            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var list = JsonSerializer.Deserialize<List<FileRecord>>(json);
                    if (list != null)
                        _cache = list;
                }
            }

            _loaded = true;
        }

        private async Task SaveAsync(CancellationToken cancellationToken)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(_cache, options);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);
        }
    }
}

