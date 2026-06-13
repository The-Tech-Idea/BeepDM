using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Editor.Schema
{
    /// <summary>Contract for persisting and retrieving <see cref="SchemaSnapshot"/> instances.</summary>
    public interface ISchemaSnapshotStore
    {
        Task SaveAsync(SchemaSnapshot snapshot, CancellationToken token = default);
        Task<SchemaSnapshot?> LoadAsync(string contextKey, CancellationToken token = default);
        Task DeleteAsync(string contextKey, CancellationToken token = default);
        Task<IReadOnlyList<SchemaSnapshot>> ListHistoryAsync(string contextKey, CancellationToken token = default);
    }

    /// <summary>JSON-file backed schema snapshot store. Files under <c>&lt;BeepRoot&gt;/Schema/Snapshots/</c>.</summary>
    public sealed class FileSchemaSnapshotStore : ISchemaSnapshotStore
    {
        private readonly string _folder;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public FileSchemaSnapshotStore()
        {
            var root = EnvironmentService.CreateAppfolder("Schema");
            _folder = Path.Combine(root, "Snapshots");
            Directory.CreateDirectory(_folder);
        }

        public async Task SaveAsync(SchemaSnapshot snapshot, CancellationToken token = default)
        {
            await _lock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });

                var basePath = GetBasePath(snapshot.ContextKey);
                var versionedPath = $"{basePath}.v{DateTime.UtcNow.Ticks}.schema.json";
                await File.WriteAllTextAsync(versionedPath, json, token).ConfigureAwait(false);

                await File.WriteAllTextAsync($"{basePath}.schema.json", json, token).ConfigureAwait(false);
            }
            finally { _lock.Release(); }
        }

        public async Task<SchemaSnapshot?> LoadAsync(string contextKey, CancellationToken token = default)
        {
            var path = $"{GetBasePath(contextKey)}.schema.json";
            if (!File.Exists(path)) return null;
            var json = await File.ReadAllTextAsync(path, token).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SchemaSnapshot>(json);
        }

        public Task DeleteAsync(string contextKey, CancellationToken token = default)
        {
            var prefix = GetBasePath(contextKey);
            foreach (var file in Directory.GetFiles(_folder, $"{Path.GetFileName(prefix)}.*.schema.json"))
                File.Delete(file);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SchemaSnapshot>> ListHistoryAsync(string contextKey, CancellationToken token = default)
        {
            var prefix = $"{Path.GetFileName(GetBasePath(contextKey))}.";
            var results = new List<SchemaSnapshot>();

            foreach (var file in Directory.GetFiles(_folder, $"{prefix}v*.schema.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var snap = JsonSerializer.Deserialize<SchemaSnapshot>(json);
                    if (snap != null) results.Add(snap);
                }
                catch { }
            }

            results.Sort((a, b) => a.CapturedAt.CompareTo(b.CapturedAt));
            return Task.FromResult<IReadOnlyList<SchemaSnapshot>>(results);
        }

        private string GetBasePath(string contextKey)
        {
            var safe = string.Join("_", contextKey.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_folder, safe);
        }
    }

    /// <summary>In-memory schema snapshot store — for unit tests and ephemeral pipelines.</summary>
    public sealed class InMemorySchemaSnapshotStore : ISchemaSnapshotStore
    {
        private readonly ConcurrentDictionary<string, List<SchemaSnapshot>> _store = new(StringComparer.OrdinalIgnoreCase);

        public Task SaveAsync(SchemaSnapshot snapshot, CancellationToken token = default)
        {
            _store.AddOrUpdate(snapshot.ContextKey,
                _ => new List<SchemaSnapshot> { snapshot },
                (_, list) => { lock (list) { list.Add(snapshot); } return list; });
            return Task.CompletedTask;
        }

        public Task<SchemaSnapshot?> LoadAsync(string contextKey, CancellationToken token = default)
        {
            if (_store.TryGetValue(contextKey, out var list) && list.Count > 0)
                return Task.FromResult<SchemaSnapshot?>(list[list.Count - 1]);
            return Task.FromResult<SchemaSnapshot?>(null);
        }

        public Task DeleteAsync(string contextKey, CancellationToken token = default)
        {
            _store.TryRemove(contextKey, out _);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SchemaSnapshot>> ListHistoryAsync(string contextKey, CancellationToken token = default)
        {
            if (_store.TryGetValue(contextKey, out var list))
                return Task.FromResult<IReadOnlyList<SchemaSnapshot>>(list.AsReadOnly());
            return Task.FromResult<IReadOnlyList<SchemaSnapshot>>(Array.Empty<SchemaSnapshot>());
        }
    }
}
