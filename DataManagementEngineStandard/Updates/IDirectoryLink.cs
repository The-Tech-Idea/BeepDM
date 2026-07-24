using System;
using System.Diagnostics;
using System.IO;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// Points a stable directory path (the <c>current</c> link) at one of the side-by-side version
    /// folders. Abstracted so the applier's stage/verify/flip/rollback logic is testable without
    /// real filesystem junctions.
    /// </summary>
    public interface IDirectoryLink
    {
        /// <summary>Atomically re-points <paramref name="linkPath"/> at <paramref name="targetPath"/>, replacing any existing link.</summary>
        void Point(string linkPath, string targetPath);
    }

    /// <summary>
    /// Windows directory junction (<c>mklink /J</c>). Junctions are used rather than symlinks
    /// because they need no elevation or Developer Mode. Removing the junction removes only the
    /// reparse point, never the target's contents — the whole point of the side-by-side layout is
    /// that flipping the link never writes into a version folder.
    /// </summary>
    public sealed class JunctionLink : IDirectoryLink
    {
        public void Point(string linkPath, string targetPath)
        {
            // Remove the existing junction (reparse point only — recursive:false never follows it
            // into the target and deletes files there).
            if (Directory.Exists(linkPath))
                Directory.Delete(linkPath, recursive: false);

            var psi = new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{linkPath.TrimEnd('\\')}\" \"{targetPath.TrimEnd('\\')}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi) ?? throw new IOException("Could not start cmd.exe to create the junction.");
            p.WaitForExit(30_000);
            if (p.ExitCode != 0)
                throw new IOException($"Creating junction '{linkPath}' → '{targetPath}' failed: {p.StandardError.ReadToEnd().Trim()}");
        }
    }
}
