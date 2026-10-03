using System;
using System.Collections.Concurrent;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Services.Persistence;
using TheTechIdea.Beep.Editor.Importing.Storage;

namespace TheTechIdea.Beep.Editor.Importing.Sync
{
    /// <summary>
    /// Persists watermarks as JSON files under <c>&lt;BeepRoot&gt;/Importing/Watermarks/</c>.
    /// Zero-config default — works without a local database driver.
    /// </summary>
    public sealed class FileWatermarkStore : IWatermarkStore
    {
        private readonly string _folder;

        public FileWatermarkStore() : this(Path.Combine(EnvironmentService.CreateAppfolder("Importing"), "Watermarks"))
        {
        }

        public FileWatermarkStore(string folder)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(folder);
            _folder = Path.GetFullPath(folder);
            Directory.CreateDirectory(_folder);
        }

        public Task SaveWatermarkAsync(string contextKey, object value, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            var path = GetPath(contextKey);
            var json = JsonSerializer.Serialize(new WatermarkEntry
            {
                Version = 1, ContextKey = contextKey, Value = TypedCursorCodec.Encode(value, 0)
            });
            return AtomicFileStore.UpdateTextAsync(path, current =>
            {
                if (current != null) DecodeEntry(current, contextKey);
                return json;
            }, token);
        }

        public async Task<object?> LoadWatermarkAsync(string contextKey, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            var json = await AtomicFileStore.ReadTextAsync(GetPath(contextKey), token).ConfigureAwait(false);
            return json == null ? null : DecodeEntry(json, contextKey);
        }

        public Task ClearWatermarkAsync(string contextKey, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return AtomicFileStore.DeleteAsync(GetPath(contextKey), token);
        }

        private string GetPath(string contextKey) => ImportStorePaths.Resolve(_folder, contextKey, ".watermark.json");

        private static object DecodeEntry(string json, string contextKey)
        {
            var entry = JsonSerializer.Deserialize<WatermarkEntry>(json);
            if (entry?.Version != 1 || entry.Value == null)
                throw new InvalidDataException("Unsupported or missing watermark envelope version.");
            if (!string.Equals(entry.ContextKey, contextKey, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Watermark context identity does not match the requested store.");
            return TypedCursorCodec.Decode(entry.Value, 0);
        }

        private sealed class WatermarkEntry
        {
            public int Version { get; set; }
            public string ContextKey { get; set; }
            public TypedCursorValue Value { get; set; }
        }


    }

    /// <summary>
    /// In-memory watermark store — suitable for unit tests and short-lived processes.
    /// Values are lost when the process exits.
    /// </summary>
    public sealed class InMemoryWatermarkStore : IWatermarkStore
    {
        private readonly ConcurrentDictionary<string, object> _store = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object NullValue = new();

        public Task SaveWatermarkAsync(string contextKey, object value, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(contextKey);
            _store[contextKey] = value ?? NullValue;
            return Task.CompletedTask;
        }

        public Task<object?> LoadWatermarkAsync(string contextKey, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(contextKey);
            return Task.FromResult(_store.TryGetValue(contextKey, out var value) && !ReferenceEquals(value, NullValue) ? value : null);
        }

        public Task ClearWatermarkAsync(string contextKey, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(contextKey);
            _store.TryRemove(contextKey, out _);
            return Task.CompletedTask;
        }
    }
}
