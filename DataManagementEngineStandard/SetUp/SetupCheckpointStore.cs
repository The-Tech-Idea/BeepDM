using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace TheTechIdea.Beep.SetUp
{
    internal static class SetupCheckpointStore
    {
        private const int StateIoRetryCount = 5;
        private const int StateIoRetryDelayMs = 30;
        private static readonly ConcurrentDictionary<string, object> StateFileLocks =
            new(StringComparer.OrdinalIgnoreCase);
        private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

        public static void LoadPersistedState(
            SetupState state,
            SetupOptions opts,
            ILogger? logger,
            bool force = false)
        {
            if (string.IsNullOrEmpty(opts?.StateFilePath)) return;
            if (!force && state.CompletedStepIds.Count > 0) return;

            var statePath = opts.StateFilePath;
            var stateLock = StateFileLocks.GetOrAdd(statePath, _ => new object());

            lock (stateLock)
            {
                for (int attempt = 0; attempt < StateIoRetryCount; attempt++)
                {
                    try
                    {
                        if (!File.Exists(statePath)) return;

                        var json = ReadAllTextWithSharedDelete(statePath);
                        var loaded = JsonSerializer.Deserialize<SetupState>(json);
                        if (loaded == null) return;

                        state.RunId = loaded.RunId;
                        foreach (var id in loaded.CompletedStepIds) state.CompletedStepIds.Add(id);
                        foreach (var id in loaded.SkippedStepIds) state.SkippedStepIds.Add(id);
                        state.FailedStepId = loaded.FailedStepId;
                        state.SchemaHash = loaded.SchemaHash;
                        if (loaded.StartedAt.HasValue) state.StartedAt = loaded.StartedAt;
                        state.LastUpdatedAt = loaded.LastUpdatedAt;
                        state.CompletedSeederIds = loaded.CompletedSeederIds;
                        foreach (var kv in loaded.Metadata) state.Metadata[kv.Key] = kv.Value;
                        return;
                    }
                    catch (IOException) when (attempt < StateIoRetryCount - 1)
                    {
                        Thread.Sleep(StateIoRetryDelayMs);
                    }
                    catch (UnauthorizedAccessException) when (attempt < StateIoRetryCount - 1)
                    {
                        Thread.Sleep(StateIoRetryDelayMs);
                    }
                    catch
                    {
                        return;
                    }
                }
            }
        }

        public static void PersistState(SetupState state, SetupOptions opts, ILogger? logger)
        {
            if (string.IsNullOrEmpty(opts?.StateFilePath)) return;

            var statePath = opts.StateFilePath;
            var stateLock = StateFileLocks.GetOrAdd(statePath, _ => new object());

            lock (stateLock)
            {
                var dir = Path.GetDirectoryName(statePath);
                var targetDir = string.IsNullOrEmpty(dir) ? "." : dir;
                Directory.CreateDirectory(targetDir);

                var tmp = Path.Combine(targetDir, Path.GetRandomFileName() + ".tmp");

                try
                {
                    var json = JsonSerializer.Serialize(state,
                        new JsonSerializerOptions { WriteIndented = false });

                    File.WriteAllText(tmp, json, Utf8NoBom);

                    for (int attempt = 0; attempt < StateIoRetryCount; attempt++)
                    {
                        try
                        {
                            File.Move(tmp, statePath, overwrite: true);
                            return;
                        }
                        catch (IOException) when (attempt < StateIoRetryCount - 1)
                        {
                            Thread.Sleep(StateIoRetryDelayMs);
                        }
                        catch (UnauthorizedAccessException) when (attempt < StateIoRetryCount - 1)
                        {
                            Thread.Sleep(StateIoRetryDelayMs);
                        }
                    }
                }
                catch
                {
                    logger?.LogWarning(
                        "Failed to persist setup state to '{Path}'. Resume will not be available.",
                        statePath);
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tmp)) File.Delete(tmp);
                    }
                    catch { }
                }
            }
        }

        private static string ReadAllTextWithSharedDelete(string path)
        {
            using var fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return sr.ReadToEnd();
        }
    }
}
