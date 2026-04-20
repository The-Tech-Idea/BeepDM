using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// File-backed <see cref="IChainAnchorStore"/> persisting every
    /// known chain anchor in a single small JSON file. Atomic writes use
    /// the standard <c>tmp + fsync + rename</c> dance so a crash
    /// mid-write either keeps the prior file intact or commits the new
    /// one — never a half-formed state.
    /// </summary>
    /// <remarks>
    /// The file is rewritten in full on every <see cref="Write"/>; that
    /// is acceptable because the payload is only a few hundred bytes
    /// per chain. SQLite-backed and KeyVault-backed implementations
    /// can replace this when the anchor count grows large enough to
    /// matter (Phase 13).
    /// </remarks>
    public sealed class JsonChainAnchorStore : IChainAnchorStore
    {
        /// <summary>Default file name when the caller does not pass one.</summary>
        public const string DefaultFileName = "audit-chain-anchors.json";

        private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private readonly string _path;
        private readonly object _gate = new object();
        private Dictionary<string, ChainAnchor> _cache;

        /// <summary>Creates a store rooted at the supplied folder.</summary>
        public JsonChainAnchorStore(string directory, string fileName = DefaultFileName)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Directory must be non-empty.", nameof(directory));
            }
            string name = string.IsNullOrWhiteSpace(fileName) ? DefaultFileName : fileName;
            _path = System.IO.Path.Combine(directory, name);
            EnsureDirectory(directory);
            _cache = LoadFromDisk(_path);
        }

        /// <summary>Absolute path of the backing file.</summary>
        public string Path => _path;

        /// <inheritdoc/>
        public ChainAnchor TryRead(string chainId)
        {
            if (string.IsNullOrEmpty(chainId))
            {
                return null;
            }
            lock (_gate)
            {
                return _cache.TryGetValue(chainId, out ChainAnchor anchor) ? Clone(anchor) : null;
            }
        }

        /// <inheritdoc/>
        public void Write(ChainAnchor anchor)
        {
            if (anchor is null || string.IsNullOrEmpty(anchor.ChainId))
            {
                return;
            }
            lock (_gate)
            {
                _cache[anchor.ChainId] = Clone(anchor);
                SaveToDisk_NoLock();
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<ChainAnchor> ReadAll()
        {
            lock (_gate)
            {
                var list = new List<ChainAnchor>(_cache.Count);
                foreach (KeyValuePair<string, ChainAnchor> entry in _cache)
                {
                    list.Add(Clone(entry.Value));
                }
                return list;
            }
        }

        private void SaveToDisk_NoLock()
        {
            string tmp = _path + ".tmp";
            byte[] payload;
            try
            {
                payload = JsonSerializer.SerializeToUtf8Bytes(_cache, WriteOptions);
            }
            catch
            {
                return;
            }

            try
            {
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.Write(payload, 0, payload.Length);
                    fs.Flush(true);
                }
                if (File.Exists(_path))
                {
                    File.Replace(tmp, _path, null);
                }
                else
                {
                    File.Move(tmp, _path);
                }
            }
            catch
            {
                // A best-effort cleanup of the leftover tmp file; the next
                // successful write will overwrite it via FileMode.Create.
                try
                {
                    if (File.Exists(tmp))
                    {
                        File.Delete(tmp);
                    }
                }
                catch
                {
                }
            }
        }

        private static Dictionary<string, ChainAnchor> LoadFromDisk(string path)
        {
            if (!File.Exists(path))
            {
                return new Dictionary<string, ChainAnchor>(StringComparer.Ordinal);
            }
            try
            {
                byte[] raw = File.ReadAllBytes(path);
                if (raw.Length == 0)
                {
                    return new Dictionary<string, ChainAnchor>(StringComparer.Ordinal);
                }
                Dictionary<string, ChainAnchor> loaded =
                    JsonSerializer.Deserialize<Dictionary<string, ChainAnchor>>(raw)
                    ?? new Dictionary<string, ChainAnchor>(StringComparer.Ordinal);
                return new Dictionary<string, ChainAnchor>(loaded, StringComparer.Ordinal);
            }
            catch
            {
                return new Dictionary<string, ChainAnchor>(StringComparer.Ordinal);
            }
        }

        private static ChainAnchor Clone(ChainAnchor source)
        {
            return new ChainAnchor
            {
                ChainId = source.ChainId,
                LastSequence = source.LastSequence,
                LastHash = source.LastHash,
                LastUpdatedUtc = source.LastUpdatedUtc
            };
        }

        private static void EnsureDirectory(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch
            {
                // The first write attempt will surface a clear error if
                // the host genuinely forbids creating the directory.
            }
        }
    }
}
