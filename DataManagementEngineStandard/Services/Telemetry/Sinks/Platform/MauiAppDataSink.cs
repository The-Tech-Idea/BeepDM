using System;
using TheTechIdea.Beep.Services.Telemetry.Presets;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks.Platform
{
    /// <summary>
    /// Factory that produces a <see cref="FileRollingSink"/> tuned
    /// for MAUI hosts (Android / iOS / Mac Catalyst / Windows).
    /// Implemented as a static factory rather than a subclass because
    /// <see cref="FileRollingSink"/> is sealed (its internal write
    /// path holds an exclusive file handle and cannot be safely
    /// inherited).
    /// </summary>
    /// <remarks>
    /// MAUI quirks the defaults compensate for:
    /// <list type="bullet">
    ///   <item>Smaller <c>MaxFileBytes</c> (1 MB) so flush latency
    ///         stays low on slow flash storage.</item>
    ///   <item>Shorter rotation interval (15 min) so the OS gets a
    ///         chance to evict / index closed files between rolls.</item>
    /// </list>
    /// The library never references <c>Microsoft.Maui.Storage</c>
    /// directly so the core stays cross-target. Callers pass a
    /// delegate that returns
    /// <c>FileSystem.AppDataDirectory</c> at startup.
    /// </remarks>
    public static class MauiAppDataSink
    {
        /// <summary>Default MAUI rotation interval (15 minutes).</summary>
        public static readonly TimeSpan DefaultMauiRollInterval = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Creates a <see cref="FileRollingSink"/> rooted at
        /// <c>{appDataDirectoryProvider()}/{appName}/{subfolder}</c>.
        /// </summary>
        /// <param name="appDataDirectoryProvider">
        /// Delegate returning <c>FileSystem.AppDataDirectory</c> (or any
        /// other writable per-user directory). Required.
        /// </param>
        /// <param name="appName">Per-app folder under the OS app-data root.</param>
        /// <param name="subfolder">Sink subfolder (e.g. <c>logs</c> or <c>audit</c>).</param>
        /// <param name="prefix">File-name prefix.</param>
        /// <param name="maxFileBytes">Rotation size cap.</param>
        /// <param name="rollInterval">Rotation wall-clock cap.</param>
        /// <param name="name">Sink name surfaced in <c>SinkHealth</c>.</param>
        public static FileRollingSink Create(
            Func<string> appDataDirectoryProvider,
            string appName = PlatformPaths.DefaultAppName,
            string subfolder = "logs",
            string prefix = "beep",
            long maxFileBytes = PlatformBudgets.MauiMaxFileBytes,
            TimeSpan? rollInterval = null,
            string name = "maui-appdata")
        {
            string directory = ResolveDirectory(appDataDirectoryProvider, appName, subfolder);
            return new FileRollingSink(
                directory: directory,
                prefix: prefix,
                extension: ".ndjson",
                maxFileBytes: maxFileBytes,
                rollInterval: rollInterval ?? DefaultMauiRollInterval,
                name: name);
        }

        private static string ResolveDirectory(
            Func<string> appDataDirectoryProvider,
            string appName,
            string subfolder)
        {
            if (appDataDirectoryProvider is null)
            {
                throw new ArgumentNullException(nameof(appDataDirectoryProvider));
            }
            string root = appDataDirectoryProvider();
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new InvalidOperationException(
                    "MAUI app-data directory provider returned an empty path.");
            }
            string app = string.IsNullOrWhiteSpace(appName) ? PlatformPaths.DefaultAppName : appName;
            string sub = string.IsNullOrWhiteSpace(subfolder) ? "logs" : subfolder;
            string full = System.IO.Path.Combine(root, app, sub);
            try
            {
                if (!System.IO.Directory.Exists(full))
                {
                    System.IO.Directory.CreateDirectory(full);
                }
            }
            catch
            {
                // Sandboxed mobile hosts may forbid creation outside
                // the app sandbox. The base sink will surface a clear
                // error on first write; we deliberately do not throw
                // from a directory resolver.
            }
            return full;
        }
    }
}
