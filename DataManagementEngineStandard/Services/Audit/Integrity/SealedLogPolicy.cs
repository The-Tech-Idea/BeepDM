using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Marks a freshly rotated audit file as read-only so accidental
    /// in-place edits become visible. On Linux/macOS the policy
    /// additionally drops the file's mode to <c>0440</c> (owner+group
    /// read, no write) for stricter compliance footprints.
    /// </summary>
    /// <remarks>
    /// Wire <see cref="Seal"/> to <c>FileRollingSink.Rolled</c> in the
    /// DI extension so every closed audit file is sealed automatically.
    /// The policy never throws — failure to chmod or set the read-only
    /// attribute is swallowed because a sealed-log breach is a
    /// configuration concern, not a hot-path concern.
    /// </remarks>
    public sealed class SealedLogPolicy
    {
        /// <summary>
        /// Optional callback invoked after a successful seal. Useful
        /// for Phase 11 self-observability so operators can confirm
        /// rotated files were sealed end-to-end.
        /// </summary>
        public Action<string> OnSealed { get; set; }

        /// <summary>
        /// Optional callback invoked when sealing fails. Receives the
        /// file path and the surfaced exception. Callers should treat
        /// this as a high-priority operator alert.
        /// </summary>
        public Action<string, Exception> OnFailed { get; set; }

        /// <summary>Seals the supplied file path.</summary>
        public void Seal(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                FileAttributes attrs = File.GetAttributes(path);
                if ((attrs & FileAttributes.ReadOnly) == 0)
                {
                    File.SetAttributes(path, attrs | FileAttributes.ReadOnly);
                }

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    SetUnixReadOnlyMode(path);
                }

                OnSealed?.Invoke(path);
            }
            catch (Exception ex)
            {
                OnFailed?.Invoke(path, ex);
            }
        }

#if NET8_0_OR_GREATER
        [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
        private static void SetUnixReadOnlyMode(string path)
        {
            try
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.GroupRead);
            }
            catch
            {
                // chmod can fail on filesystems that don't honour POSIX
                // bits (e.g. exFAT). The Windows-style read-only bit
                // already set above remains in effect.
            }
        }
#else
        private static void SetUnixReadOnlyMode(string path)
        {
            // No-op on pre-net8 targets. The read-only attribute is
            // already set above.
        }
#endif
    }
}
