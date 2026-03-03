using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Editor.Importing.Sync
{
    /// <summary>
    /// Persists watermarks as JSON files under <c>&lt;BeepRoot&gt;/Importing/Watermarks/</c>.
    /// Zero-config default — works without a local database driver.
    /// </summary>
    public sealed class FileWatermarkStore : IWatermarkStore
    {
        private readonly string _folder;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public FileWatermarkStore()
        {
            var root = EnvironmentService.CreateAppfolder("Importing");
            _folder = Path.Combine(root, "Watermarks");
            Directory.CreateDirectory(_folder);
        }

        public async Task SaveWatermarkAsync(string contextKey, object value, CancellationToken token = default)
        {
            await _lock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                var path = GetPath(contextKey);
                var json = JsonSerializer.Serialize(new WatermarkEntry { Value = value?.ToString() });
                await File.WriteAllTextAsync(path, json, token).ConfigureAwait(false);
            }
            finally { _lock.Release(); }
        }

        public async Task<object?> LoadWatermarkAsync(string contextKey, CancellationToken token = default)
        {
            var path = GetPath(contextKey);
            if (!File.Exists(path)) return null;

            var json = await File.ReadAllTextAsync(path, token).ConfigureAwait(false);
            var entry = JsonSerializer.Deserialize<WatermarkEntry>(json);
            return entry?.Value;
        }

        public Task ClearWatermarkAsync(string contextKey, CancellationToken token = default)
        {
            var path = GetPath(contextKey);
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }

        private string GetPath(string contextKey)
        {
            var safe = string.Join("_", contextKey.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_folder, $"{safe}.watermark.json");
        }

        private sealed class WatermarkEntry { public string? Value { get; set; } }
    }

    /// <summary>
    /// In-memory watermark store — suitable for unit tests and short-lived processes.
    /// Values are lost when the process exits.
    /// </summary>
    public sealed class InMemoryWatermarkStore : IWatermarkStore
    {
        private readonly ConcurrentDictionary<string, object> _store = new(StringComparer.OrdinalIgnoreCase);

        public Task SaveWatermarkAsync(string contextKey, object value, CancellationToken token = default)
        {
            _store[contextKey] = value;
            return Task.CompletedTask;
        }

        public Task<object?> LoadWatermarkAsync(string contextKey, CancellationToken token = default) =>
            Task.FromResult(_store.TryGetValue(contextKey, out var v) ? v : null);

        public Task ClearWatermarkAsync(string contextKey, CancellationToken token = default)
        {
            _store.TryRemove(contextKey, out _);
            return Task.CompletedTask;
        }
    }
}
