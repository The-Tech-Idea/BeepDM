using System;
using System.IO;

namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Single source of truth for "where can we safely write on this OS / host?".
    /// Returns directories using only portable BCL APIs so the same code works
    /// on Windows, Linux, macOS, MAUI, and (with the Phase 12 override) Blazor.
    /// </summary>
    /// <remarks>
    /// Blazor WebAssembly cannot write to a real filesystem; in that target
    /// Phase 12 swaps the active provider for an IndexedDB-backed shim and
    /// the path returned here becomes a virtual identifier consumed by the
    /// JS interop sink. All other targets receive a real, creatable path.
    /// </remarks>
    public static class PlatformPaths
    {
        /// <summary>Default app folder name when callers do not pass one.</summary>
        public const string DefaultAppName = "Beep";

        /// <summary>
        /// Per-user, per-app local data root. Uses
        /// <see cref="Environment.SpecialFolder.LocalApplicationData"/>
        /// with <see cref="Environment.SpecialFolderOption.Create"/> so the
        /// directory is created on demand by the BCL.
        /// </summary>
        public static string AppDataRoot(string appName = DefaultAppName)
        {
            string baseDir = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create);
            string root = Path.Combine(baseDir, string.IsNullOrWhiteSpace(appName) ? DefaultAppName : appName);
            EnsureDirectory(root);
            return root;
        }

        /// <summary>
        /// Logs directory for the supplied app name. Defaults to
        /// <c>{AppDataRoot}/logs</c>. Created on demand.
        /// </summary>
        public static string LogsDir(string appName = DefaultAppName, string subfolder = "logs")
        {
            string dir = Path.Combine(AppDataRoot(appName), string.IsNullOrWhiteSpace(subfolder) ? "logs" : subfolder);
            EnsureDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Audit directory for the supplied app name. Defaults to
        /// <c>{AppDataRoot}/audit</c>. Created on demand.
        /// </summary>
        public static string AuditDir(string appName = DefaultAppName, string subfolder = "audit")
        {
            string dir = Path.Combine(AppDataRoot(appName), string.IsNullOrWhiteSpace(subfolder) ? "audit" : subfolder);
            EnsureDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Returns a sanitized file-name fragment from <paramref name="raw"/>
        /// safe to use across every supported filesystem. Replaces any
        /// invalid character with <c>_</c>.
        /// </summary>
        public static string SanitizeFileName(string raw, string fallback = "beep")
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            char[] buffer = raw.ToCharArray();
            for (int i = 0; i < buffer.Length; i++)
            {
                if (Array.IndexOf(invalid, buffer[i]) >= 0)
                {
                    buffer[i] = '_';
                }
            }
            return new string(buffer);
        }

        private static void EnsureDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch
            {
                // Sandboxed hosts (e.g. tightened service accounts) may forbid
                // creation. Sinks that try to open files will surface a clear
                // error; we deliberately do not throw from a path helper.
            }
        }
    }
}
