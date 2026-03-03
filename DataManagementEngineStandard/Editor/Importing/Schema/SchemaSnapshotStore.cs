using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Editor.Importing.Schema
{
    /// <summary>
    /// Contract for persisting and retrieving <see cref="SchemaSnapshot"/> instances.
    /// </summary>
    public interface ISchemaSnapshotStore
    {
        Task SaveAsync(SchemaSnapshot snapshot, CancellationToken token = default);
        Task<SchemaSnapshot?> LoadAsync(string contextKey, CancellationToken token = default);
        Task DeleteAsync(string contextKey, CancellationToken token = default);
    }

    // -----------------------------------------------------------------------

    /// <summary>
    /// JSON-file backed schema snapshot store.
    /// Files written to <c>&lt;BeepRoot&gt;/Importing/SchemaSnapshots/</c>.
    /// </summary>
    public sealed class FileSchemaSnapshotStore : ISchemaSnapshotStore
    {
        private readonly string _folder;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public FileSchemaSnapshotStore()
        {
            var root = EnvironmentService.CreateAppfolder("Importing");
            _folder = Path.Combine(root, "SchemaSnapshots");
            Directory.CreateDirectory(_folder);
        }

        public async Task SaveAsync(SchemaSnapshot snapshot, CancellationToken token = default)
        {
            await _lock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(GetPath(snapshot.ContextKey), json, token).ConfigureAwait(false);
            }
            finally { _lock.Release(); }
        }

        public async Task<SchemaSnapshot?> LoadAsync(string contextKey, CancellationToken token = default)
        {
            var path = GetPath(contextKey);
            if (!File.Exists(path)) return null;
            var json = await File.ReadAllTextAsync(path, token).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SchemaSnapshot>(json);
        }

        public Task DeleteAsync(string contextKey, CancellationToken token = default)
        {
            var path = GetPath(contextKey);
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }

        private string GetPath(string contextKey)
        {
            var safe = string.Join("_", contextKey.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_folder, $"{safe}.schema.json");
        }
    }

    // -----------------------------------------------------------------------

    /// <summary>
    /// In-memory schema snapshot store — for unit tests and ephemeral pipelines.
    /// </summary>
    public sealed class InMemorySchemaSnapshotStore : ISchemaSnapshotStore
    {
        private readonly ConcurrentDictionary<string, SchemaSnapshot> _store = new(StringComparer.OrdinalIgnoreCase);

        public Task SaveAsync(SchemaSnapshot snapshot, CancellationToken token = default)
        {
            _store[snapshot.ContextKey] = snapshot;
            return Task.CompletedTask;
        }

        public Task<SchemaSnapshot?> LoadAsync(string contextKey, CancellationToken token = default) =>
            Task.FromResult(_store.TryGetValue(contextKey, out var s) ? s : null);

        public Task DeleteAsync(string contextKey, CancellationToken token = default)
        {
            _store.TryRemove(contextKey, out _);
            return Task.CompletedTask;
        }
    }
}
