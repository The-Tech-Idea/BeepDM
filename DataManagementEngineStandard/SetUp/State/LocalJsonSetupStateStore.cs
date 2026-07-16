using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.SetUp.State;

namespace TheTechIdea.Beep.SetUp.State
{
    /// <summary>
    /// Solo-default <see cref="ISetupStateStore"/>: one JSON file per key, plus a sibling
    /// <c>.lock</c> file for a cross-process lease.
    /// </summary>
    /// <remarks>
    /// Ports the atomic write (temp file + <see cref="File.Move(string,string,bool)"/> with retries)
    /// from the former <c>SetupCheckpointStore</c>, and adds the lease that store never had — its
    /// lock was an in-process <c>ConcurrentDictionary</c>, so two processes sharing a path interleaved.
    /// </remarks>
    public sealed class LocalJsonSetupStateStore : ISetupStateStore
    {
        private const int IoRetryCount = 5;
        private const int IoRetryDelayMs = 30;
        private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

        private readonly string _root;
        private readonly string _explicitFile;
        private readonly ILogger _logger;

        /// <param name="root">
        /// Base directory for key-derived state files. State lands at
        /// <c>{root}/setup/{appId|_}/{environment}/{wizardId}.state.json</c>.
        /// </param>
        public LocalJsonSetupStateStore(string root, ILogger logger = null)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _logger = logger;
        }

        private LocalJsonSetupStateStore(string explicitFile, bool _, ILogger logger)
        {
            _explicitFile = explicitFile;
            _logger = logger;
        }

        /// <summary>
        /// Legacy-compatible store that reads and writes exactly <paramref name="stateFilePath"/>,
        /// ignoring the key. Backs <c>SetupOptions.StateFilePath</c>.
        /// </summary>
        /// <remarks>
        /// An explicit path is literal, so it does <b>not</b> get per-wizard isolation — two wizards
        /// pointed at the same file still collide, exactly as before. The key-based constructor is
        /// the one that fixes that.
        /// </remarks>
        public static LocalJsonSetupStateStore ForExplicitFile(string stateFilePath, ILogger logger = null)
            => new(stateFilePath, false, logger);

        private string StatePathFor(SetupStateKey key)
        {
            if (_explicitFile != null) return _explicitFile;

            var appSegment = key.AppId is { } id ? Sanitize(id) : "_";
            return Path.Combine(_root, "setup", appSegment, Sanitize(key.Environment),
                Sanitize(key.WizardId) + ".state.json");
        }

        private static string LockPathFor(string statePath) => statePath + ".lock";

        // ── Load ─────────────────────────────────────────────────────────────

        public Task<SetupState> LoadAsync(SetupStateKey key, CancellationToken token = default)
        {
            var statePath = StatePathFor(key);
            if (!File.Exists(statePath)) return Task.FromResult<SetupState>(null);

            for (int attempt = 0; attempt < IoRetryCount; attempt++)
            {
                try
                {
                    var json = ReadShared(statePath);
                    return Task.FromResult(JsonSerializer.Deserialize<SetupState>(json));
                }
                catch (IOException) when (attempt < IoRetryCount - 1) { Thread.Sleep(IoRetryDelayMs); }
                catch (UnauthorizedAccessException) when (attempt < IoRetryCount - 1) { Thread.Sleep(IoRetryDelayMs); }
                catch (JsonException ex)
                {
                    // Don't silently treat an unreadable checkpoint as "fresh" — on a live DB a
                    // re-run is a re-migration, not a reset.
                    _logger?.LogError(ex,
                        "Setup state at {Path} is unreadable and will be ignored; this run will start " +
                        "fresh. Move or delete the file to silence this.", statePath);
                    return Task.FromResult<SetupState>(null);
                }
            }
            return Task.FromResult<SetupState>(null);
        }

        // ── Save ─────────────────────────────────────────────────────────────

        public Task SaveAsync(SetupStateKey key, SetupState state, ISetupStateLease lease = null,
            CancellationToken token = default)
        {
            var statePath = StatePathFor(key);

            // If a lease is held, it must still be ours — otherwise another runner reclaimed the key
            // and our view is stale.
            if (lease is FileLease fl && !fl.IsStillHeld())
                throw new SetupStateConflictException(
                    $"Lease on '{key}' was lost (expired and reclaimed by another runner); refusing to save.");

            state.Revision++;

            var dir = Path.GetDirectoryName(statePath);
            var targetDir = string.IsNullOrEmpty(dir) ? "." : dir;
            Directory.CreateDirectory(targetDir);

            var tmp = Path.Combine(targetDir, Path.GetRandomFileName() + ".tmp");
            try
            {
                File.WriteAllText(tmp, JsonSerializer.Serialize(state), Utf8NoBom);

                for (int attempt = 0; attempt < IoRetryCount; attempt++)
                {
                    try { File.Move(tmp, statePath, overwrite: true); return Task.CompletedTask; }
                    catch (IOException) when (attempt < IoRetryCount - 1) { Thread.Sleep(IoRetryDelayMs); }
                    catch (UnauthorizedAccessException) when (attempt < IoRetryCount - 1) { Thread.Sleep(IoRetryDelayMs); }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex,
                    "Failed to persist setup state to '{Path}'. Resume will not be available.", statePath);
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
            return Task.CompletedTask;
        }

        // ── Lease ────────────────────────────────────────────────────────────

        public Task<ISetupStateLease> TryAcquireLeaseAsync(SetupStateKey key, TimeSpan ttl,
            CancellationToken token = default)
        {
            var statePath = StatePathFor(key);
            var lockPath = LockPathFor(statePath);
            Directory.CreateDirectory(Path.GetDirectoryName(lockPath) is { Length: > 0 } d ? d : ".");

            var existing = ReadLock(lockPath);
            if (existing != null && existing.ExpiresAt > DateTimeOffset.UtcNow)
            {
                // Held by a live runner.
                _logger?.LogWarning(
                    "Setup state '{Key}' is locked by run {RunId} until {Expiry:o}; not acquiring.",
                    key, existing.RunId, existing.ExpiresAt);
                return Task.FromResult<ISetupStateLease>(null);
            }

            if (existing != null)
                _logger?.LogInformation(
                    "Reclaiming expired setup lease on '{Key}' (previous run {RunId} expired {Expiry:o}).",
                    key, existing.RunId, existing.ExpiresAt);

            var runId = Guid.NewGuid().ToString("N");
            var record = new LockRecord { RunId = runId, ExpiresAt = DateTimeOffset.UtcNow.Add(ttl) };
            if (!WriteLock(lockPath, record))
                return Task.FromResult<ISetupStateLease>(null);

            ISetupStateLease lease = new FileLease(key, lockPath, record, ttl, this);
            return Task.FromResult(lease);
        }

        // ── lock file IO ──────────────────────────────────────────────────────

        private LockRecord ReadLock(string lockPath)
        {
            try
            {
                if (!File.Exists(lockPath)) return null;
                return JsonSerializer.Deserialize<LockRecord>(ReadShared(lockPath));
            }
            catch { return null; }
        }

        private bool WriteLock(string lockPath, LockRecord record)
        {
            try
            {
                File.WriteAllText(lockPath, JsonSerializer.Serialize(record), Utf8NoBom);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not write setup lease file '{Path}'.", lockPath);
                return false;
            }
        }

        private void DeleteLock(string lockPath, string runId)
        {
            // Only delete if it's still ours — never stomp a lease that was reclaimed.
            var current = ReadLock(lockPath);
            if (current == null || current.RunId != runId) return;
            try { File.Delete(lockPath); } catch { }
        }

        private static string ReadShared(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return sr.ReadToEnd();
        }

        private static string Sanitize(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var c in value)
                sb.Append(char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-');
            return sb.Length == 0 ? "_" : sb.ToString();
        }

        private sealed class LockRecord
        {
            public string RunId { get; set; }
            public DateTimeOffset ExpiresAt { get; set; }
        }

        private sealed class FileLease : ISetupStateLease
        {
            private readonly string _lockPath;
            private readonly TimeSpan _ttl;
            private readonly LocalJsonSetupStateStore _store;
            private LockRecord _record;
            private bool _released;

            public FileLease(SetupStateKey key, string lockPath, LockRecord record, TimeSpan ttl,
                LocalJsonSetupStateStore store)
            {
                Key = key;
                _lockPath = lockPath;
                _record = record;
                _ttl = ttl;
                _store = store;
            }

            public SetupStateKey Key { get; }
            public string RunId => _record.RunId;
            public DateTimeOffset ExpiresAt => _record.ExpiresAt;

            /// <summary>The lock file still exists and still names this run.</summary>
            public bool IsStillHeld()
            {
                if (_released) return false;
                var current = _store.ReadLock(_lockPath);
                return current != null && current.RunId == _record.RunId
                                       && current.ExpiresAt > DateTimeOffset.UtcNow;
            }

            public Task<bool> RenewAsync(CancellationToken token = default)
            {
                if (!IsStillHeld()) return Task.FromResult(false);
                var renewed = new LockRecord { RunId = _record.RunId, ExpiresAt = DateTimeOffset.UtcNow.Add(_ttl) };
                if (!_store.WriteLock(_lockPath, renewed)) return Task.FromResult(false);
                _record = renewed;
                return Task.FromResult(true);
            }

            public ValueTask DisposeAsync()
            {
                if (_released) return ValueTask.CompletedTask;
                _released = true;
                _store.DeleteLock(_lockPath, _record.RunId);
                return ValueTask.CompletedTask;
            }
        }
    }
}
