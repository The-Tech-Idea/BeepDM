using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// Applies an app update side-by-side: it materializes the new version into its own
    /// <c>app-&lt;version&gt;</c> folder and only then flips the <c>current</c> junction. The running
    /// version is never written to, so the locked-file problem disappears and an interrupted apply
    /// is harmless — the junction still points at the old version until the very last step.
    /// Rollback is just flipping the junction back.
    /// </summary>
    public sealed class SideBySideApplier
    {
        public const string CurrentLinkName = "current";
        private const string StateFileName = ".sxs-state.json";
        private const string StagingPrefix = ".staging-";

        private readonly IDirectoryLink _link;

        public SideBySideApplier(IDirectoryLink? link = null) => _link = link ?? new JunctionLink();

        public sealed class Request
        {
            public string InstallRoot { get; init; } = "";
            public string NewVersion { get; init; } = "";
            public DeltaPlan Plan { get; init; } = new();

            /// <summary>Resolves a blob's bytes by its SHA-256 (local store first, else download).</summary>
            public Func<string, CancellationToken, Task<byte[]>> FetchBlob { get; init; } = (_, _) => Task.FromResult(Array.Empty<byte>());

            /// <summary>The live version whose files seed a delta. Null → materialize fully from blobs.</summary>
            public string? CurrentVersion { get; init; }

            /// <summary>Invoked with the new version once the flip has committed (e.g. to record it in the version ledger).</summary>
            public Action<string>? OnApplied { get; init; }
        }

        public async Task<IErrorsInfo> ApplyAsync(Request req, IProgress<PassedArgs>? progress = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(req);
            if (string.IsNullOrWhiteSpace(req.InstallRoot)) return Fail("InstallRoot is required.");
            if (string.IsNullOrWhiteSpace(req.NewVersion)) return Fail("NewVersion is required.");

            Directory.CreateDirectory(req.InstallRoot);

            // Crash-safety: a previous run may have died mid-stage. Its staging dir is scratch —
            // clear all leftover staging before we begin (the running version is never in staging).
            CleanStaleStaging(req.InstallRoot);

            var newDir = Path.Combine(req.InstallRoot, "app-" + req.NewVersion);
            var staging = Path.Combine(req.InstallRoot, StagingPrefix + req.NewVersion);
            if (Directory.Exists(staging)) TryDeleteDir(staging);
            Directory.CreateDirectory(staging);

            var full = req.Plan.IsFullInstall || string.IsNullOrWhiteSpace(req.CurrentVersion);

            try
            {
                // Seed a delta from the current version's files; a full install starts empty.
                if (!full)
                {
                    var currentDir = Path.Combine(req.InstallRoot, "app-" + req.CurrentVersion);
                    if (Directory.Exists(currentDir)) CopyDirectory(currentDir, staging);
                }

                foreach (var rel in req.Plan.FilesToDelete)
                {
                    var f = Path.Combine(staging, Normalize(rel));
                    if (File.Exists(f)) File.Delete(f);
                }

                // Write changed/new files, verifying every blob before it lands. A mismatch aborts
                // here — before any move or flip — so the current junction is never touched.
                var cache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                int written = 0, total = req.Plan.FilesToWrite.Count;
                foreach (var entry in req.Plan.FilesToWrite)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!cache.TryGetValue(entry.Blob, out var bytes))
                    {
                        bytes = await req.FetchBlob(entry.Blob, ct).ConfigureAwait(false);
                        var actual = Convert.ToHexString(SHA256.HashData(bytes));
                        if (!actual.Equals(entry.Blob, StringComparison.OrdinalIgnoreCase))
                        {
                            TryDeleteDir(staging);
                            return Fail($"Blob for '{entry.Path}' failed SHA-256 verification " +
                                        $"(expected {entry.Blob}, got {actual}); update aborted before any version flip.");
                        }
                        cache[entry.Blob] = bytes;
                    }

                    var dest = Path.Combine(staging, Normalize(entry.Path));
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    await File.WriteAllBytesAsync(dest, bytes, ct).ConfigureAwait(false);

                    progress?.Report(new PassedArgs { Messege = $"Staging {entry.Path}", ParameterInt1 = (int)((++written) * 100.0 / Math.Max(1, total)) });
                }
            }
            catch (OperationCanceledException)
            {
                TryDeleteDir(staging);
                throw;
            }
            catch (Exception ex)
            {
                TryDeleteDir(staging);
                return Fail($"Staging the new version failed: {ex.Message}", ex);
            }

            // Commit: promote the staged tree to its version folder, then flip the junction. The
            // flip is the single point of no return; everything before it is discardable scratch.
            if (Directory.Exists(newDir)) TryDeleteDir(newDir);
            Directory.Move(staging, newDir);

            try
            {
                _link.Point(Path.Combine(req.InstallRoot, CurrentLinkName), newDir);
            }
            catch (Exception ex)
            {
                return Fail($"New version staged at {newDir} but the '{CurrentLinkName}' link could not be flipped: {ex.Message}. " +
                            "The running version is unchanged.", ex);
            }

            var state = LoadState(req.InstallRoot);
            SaveState(req.InstallRoot, new State { Current = req.NewVersion, Previous = state.Current });

            req.OnApplied?.Invoke(req.NewVersion);
            return Ok($"Updated to {req.NewVersion} ({(full ? "full" : "delta")}). Previous version kept for rollback.");
        }

        /// <summary>Flips <c>current</c> back to the previously-installed version.</summary>
        public IErrorsInfo Rollback(string installRoot)
        {
            if (string.IsNullOrWhiteSpace(installRoot)) return Fail("InstallRoot is required.");
            var state = LoadState(installRoot);
            if (string.IsNullOrWhiteSpace(state.Previous))
                return Fail("There is no previous version to roll back to.");

            var prevDir = Path.Combine(installRoot, "app-" + state.Previous);
            if (!Directory.Exists(prevDir))
                return Fail($"The previous version folder is missing: {prevDir}.");

            try
            {
                _link.Point(Path.Combine(installRoot, CurrentLinkName), prevDir);
            }
            catch (Exception ex) { return Fail($"Rollback flip failed: {ex.Message}", ex); }

            // The rolled-back-from version becomes the new "previous" so a redo is possible.
            SaveState(installRoot, new State { Current = state.Previous, Previous = state.Current });
            return Ok($"Rolled back to {state.Previous}.");
        }

        /// <summary>Deletes version folders other than the current and previous (post-retirement cleanup).</summary>
        public int RetireOldVersions(string installRoot)
        {
            var state = LoadState(installRoot);
            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(state.Current)) keep.Add("app-" + state.Current);
            if (!string.IsNullOrWhiteSpace(state.Previous)) keep.Add("app-" + state.Previous);

            int removed = 0;
            foreach (var dir in Directory.EnumerateDirectories(installRoot, "app-*"))
            {
                if (keep.Contains(Path.GetFileName(dir))) continue;
                if (TryDeleteDir(dir)) removed++;
            }
            return removed;
        }

        // ── helpers ──

        private void CleanStaleStaging(string root)
        {
            foreach (var dir in Directory.EnumerateDirectories(root, StagingPrefix + "*"))
                TryDeleteDir(dir);
        }

        private static string Normalize(string rel) => rel.Replace('/', Path.DirectorySeparatorChar);

        private static void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(source, file);
                var target = Path.Combine(dest, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
            }
        }

        private static bool TryDeleteDir(string dir)
        {
            try { if (Directory.Exists(dir)) { Directory.Delete(dir, recursive: true); } return true; }
            catch { return false; }
        }

        private sealed class State
        {
            public string? Current { get; set; }
            public string? Previous { get; set; }
        }

        private static State LoadState(string root)
        {
            var path = Path.Combine(root, StateFileName);
            if (!File.Exists(path)) return new State();
            try { return JsonSerializer.Deserialize<State>(File.ReadAllText(path)) ?? new State(); }
            catch { return new State(); }
        }

        private static void SaveState(string root, State state)
        {
            var path = Path.Combine(root, StateFileName);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(state));
            File.Move(tmp, path, overwrite: true);
        }

        private static IErrorsInfo Ok(string msg) => new ErrorsInfo { Flag = Errors.Ok, Message = msg };
        private static IErrorsInfo Fail(string msg, Exception? ex = null) => new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };
    }
}
