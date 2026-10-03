using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Persistence;

namespace TheTechIdea.Beep.Editor.Importing.Storage
{
    internal sealed class ImportJsonLinesStore<T> where T : class
    {
        private readonly string _folder;
        private readonly string _suffix;
        private readonly Func<T, string> _contextKey;
        private readonly Action<T> _validate;
        private readonly Action<IReadOnlyList<T>> _validateAll;
        private readonly Action<JsonElement> _validateJson;

        internal ImportJsonLinesStore(string folder, string suffix, Func<T, string> contextKey,
            Action<T> validate = null, Action<IReadOnlyList<T>> validateAll = null, Action<JsonElement> validateJson = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(folder);
            _folder = Path.GetFullPath(folder);
            _suffix = suffix;
            _contextKey = contextKey;
            _validate = validate; _validateAll = validateAll; _validateJson = validateJson;
            Directory.CreateDirectory(_folder);
        }

        internal Task AppendAsync(T record, CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(record);
            token.ThrowIfCancellationRequested();
            var key = _contextKey(record);
            var path = ImportStorePaths.Resolve(_folder, key, _suffix);
            var line = JsonSerializer.Serialize(record);
            Parse(line, key);
            return AtomicFileStore.UpdateTextAsync(path, current =>
            {
                Parse(current, key);
                var next = current + (string.IsNullOrEmpty(current) || current.EndsWith('\n') ? "" : "\n") + line + "\n";
                Parse(next, key);
                return next;
            }, token);
        }

        internal async Task<List<T>> ReadAsync(string key, CancellationToken token)
        {
            var content = await AtomicFileStore.ReadTextAsync(
                ImportStorePaths.Resolve(_folder, key, _suffix), token).ConfigureAwait(false);
            return Parse(content, key);
        }

        internal Task MutateAsync(string key, Action<List<T>> mutate, CancellationToken token) =>
            AtomicFileStore.UpdateTextAsync(ImportStorePaths.Resolve(_folder, key, _suffix), current =>
            {
                var records = Parse(current, key);
                mutate(records);
                foreach (var record in records) _validate?.Invoke(record);
                _validateAll?.Invoke(records);
                return string.Join("\n", records.Select(record => JsonSerializer.Serialize(record))) +
                    (records.Count == 0 ? "" : "\n");
            }, token);

        internal Task ClearAsync(string key, CancellationToken token) =>
            AtomicFileStore.DeleteValidatedAsync(ImportStorePaths.Resolve(_folder, key, _suffix), current => { Parse(current, key); }, token);

        internal Task ClearValidatedAsync(string key, Action<IReadOnlyList<T>> validate, CancellationToken token) =>
            AtomicFileStore.DeleteValidatedAsync(ImportStorePaths.Resolve(_folder, key, _suffix), current => validate(Parse(current, key)), token);

        private List<T> Parse(string content, string key)
        {
            var records = new List<T>();
            if (content == null) return records;
            using var reader = new StringReader(content);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (_validateJson != null)
                {
                    try
                    {
                        using var json = JsonDocument.Parse(line, new JsonDocumentOptions { MaxDepth = 64 });
                        _validateJson(json.RootElement);
                    }
                    catch (JsonException)
                    {
                        // Preserve the legacy public exception type without exposing corrupt payload bytes.
                        throw new JsonException("Import record JSON is malformed.");
                    }
                }
                var record = JsonSerializer.Deserialize<T>(line);
                if (record == null || !string.Equals(_contextKey(record), key, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Import record context does not match the requested store identity.");
                _validate?.Invoke(record);
                records.Add(record);
            }
            _validateAll?.Invoke(records);
            return records;
        }
    }
}
