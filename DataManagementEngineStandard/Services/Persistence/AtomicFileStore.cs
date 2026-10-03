using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Persistence
{
    /// <summary>
    /// Whole-file snapshot persistence for cooperating writers on a local filesystem.
    /// Updates are coordinated across instances/processes with a persistent sidecar lease.
    /// Replacement is atomic on filesystems supporting same-directory rename; this is
    /// not a distributed lock or a guarantee against filesystem/power-loss failures.
    /// </summary>
    public static class AtomicFileStore
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly TimeSpan LeaseTimeout = TimeSpan.FromSeconds(30);

        public static string ReadText(string path)
        {
            var fullPath = GetFullPath(path);
            try
            {
                var snapshot = ReadSnapshot(fullPath);
                if (snapshot != null || !File.Exists(fullPath + ".beep.lock")) return snapshot;
            }
            catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException) && File.Exists(fullPath + ".beep.lock"))
            {
                // Settle a cooperating replacement, never interpret its read error as empty state.
            }
            using var lease = AcquireLease(fullPath, default);
            return ReadSnapshot(fullPath);
        }

        public static void WriteText(string path, string content)
        {
            ArgumentNullException.ThrowIfNull(content);
            var fullPath = GetFullPath(path);
            using var lease = AcquireLease(fullPath, default);
            Replace(fullPath, content, default);
        }

        public static void UpdateText(string path, Func<string, string> update, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(update);
            var fullPath = GetFullPath(path);
            using var lease = AcquireLease(fullPath, token);
            var current = ReadSnapshot(fullPath);
            var content = update(current);
            token.ThrowIfCancellationRequested();
            if (string.Equals(current, content, StringComparison.Ordinal)) return;
            ArgumentNullException.ThrowIfNull(content);
            Replace(fullPath, content, token);
        }

        private static string ReadSnapshot(string path)
        {
            try
            {
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(file, Utf8, detectEncodingFromByteOrderMarks: true);
                return reader.ReadToEnd();
            }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }

        private static FileStream AcquireLease(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var watch = Stopwatch.StartNew();
            while (true)
            {
                token.ThrowIfCancellationRequested();
                try { return new FileStream(path + ".beep.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
                catch (IOException ex)
                {
                    if (watch.Elapsed >= LeaseTimeout)
                        throw new TimeoutException($"Unable to acquire persistence lease for '{path}'.", ex);
                    WaitForRetry(token);
                }
            }
        }

        private static void WaitForRetry(CancellationToken token)
        {
            if (token.CanBeCanceled) token.WaitHandle.WaitOne(20);
            else Thread.Sleep(20);
            token.ThrowIfCancellationRequested();
        }

        /// <summary>Returns null for a missing file, never for a read failure.</summary>
        public static async Task<string> ReadTextAsync(string path, CancellationToken token = default)
        {
            var fullPath = GetFullPath(path);
            token.ThrowIfCancellationRequested();
            try
            {
                var snapshot = await ReadSnapshotAsync(fullPath, token).ConfigureAwait(false);
                if (snapshot != null || !File.Exists(fullPath + ".beep.lock")) return snapshot;
            }
            catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException) && File.Exists(fullPath + ".beep.lock"))
            {
                // A Windows replacement can make an otherwise shareable snapshot
                // briefly inaccessible. Wait for the cooperating writer to settle.
            }
            using var lease = await AcquireLeaseAsync(fullPath, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return ReadSnapshot(fullPath);
        }

        private static async Task<string> ReadSnapshotAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
                using var reader = new StreamReader(file, Utf8, detectEncodingFromByteOrderMarks: true);
                return await reader.ReadToEndAsync(token).ConfigureAwait(false);
            }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }

        public static async Task WriteTextAsync(string path, string content, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            var fullPath = GetFullPath(path);
            token.ThrowIfCancellationRequested();
            using var lease = await AcquireLeaseAsync(fullPath, token).ConfigureAwait(false);
            Replace(fullPath, content, token);
        }

        /// <summary>
        /// Mutates a complete snapshot under the lease. The callback must not reenter
        /// this store for the same path. Callback/serialization failure preserves the old file.
        /// Returning the unchanged snapshot performs no write, including null for a missing file.
        /// </summary>
        public static async Task UpdateTextAsync(string path, Func<string, string> update,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(update);
            var fullPath = GetFullPath(path);
            token.ThrowIfCancellationRequested();
            using var lease = await AcquireLeaseAsync(fullPath, token).ConfigureAwait(false);
            // Never suspend while holding the lease: synchronous waiters can occupy
            // every pool thread needed by an asynchronous holder's I/O continuation.
            var current = ReadSnapshot(fullPath);
            var content = update(current);
            token.ThrowIfCancellationRequested();
            if (string.Equals(current, content, StringComparison.Ordinal)) return;
            ArgumentNullException.ThrowIfNull(content);
            Replace(fullPath, content, token);
        }

        public static async Task DeleteAsync(string path, CancellationToken token = default)
        {
            var fullPath = GetFullPath(path);
            token.ThrowIfCancellationRequested();
            using var lease = await AcquireLeaseAsync(fullPath, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            File.Delete(fullPath);
        }

        private static string GetFullPath(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            return Path.GetFullPath(path);
        }

        /// <summary>Validates the exact leased snapshot before deletion; validation failure preserves evidence.</summary>
        public static async Task DeleteValidatedAsync(string path, Action<string> validate, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(validate);
            var fullPath = GetFullPath(path);
            using var lease = await AcquireLeaseAsync(fullPath, token).ConfigureAwait(false);
            var current = ReadSnapshot(fullPath);
            if (current == null) return;
            validate(current);
            token.ThrowIfCancellationRequested();
            File.Delete(fullPath);
        }

        private static async Task<FileStream> AcquireLeaseAsync(string path, CancellationToken token)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var watch = Stopwatch.StartNew();
            while (true)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    // Never delete the lease file: unlinking it permits two owners of
                    // different handles with the same pathname (an ABA race).
                    return new FileStream(path + ".beep.lock", FileMode.OpenOrCreate,
                        FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException ex)
                {
                    if (watch.Elapsed >= LeaseTimeout)
                        throw new TimeoutException($"Unable to acquire persistence lease for '{path}'.", ex);
                    await Task.Delay(20, token).ConfigureAwait(false);
                }
            }
        }

        private static void Replace(string path, string content, CancellationToken token)
        {
            var bytes = Utf8.GetBytes(content);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write,
                        FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    token.ThrowIfCancellationRequested();
                    file.Write(bytes);
                    file.Flush(flushToDisk: true);
                }
                token.ThrowIfCancellationRequested();
                // Cancellation after this commit point does not turn a persisted write
                // into a cancelled acknowledgement.
                for (int attempt = 0; ; attempt++)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        if (File.Exists(path))
                            File.Replace(temporary, path, destinationBackupFileName: null);
                        else
                            File.Move(temporary, path);
                        break;
                    }
                    catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException) && attempt < 9)
                    {
                        // Windows can briefly deny replacement while a shared reader
                        // releases the previous snapshot. Never truncate as a fallback.
                        WaitForRetry(token);
                    }
                }
            }
            finally
            {
                try { File.Delete(temporary); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Debug.WriteLine($"[AtomicFileStore] Temporary file cleanup failed: {ex.Message}");
                }
            }
        }
    }
}
