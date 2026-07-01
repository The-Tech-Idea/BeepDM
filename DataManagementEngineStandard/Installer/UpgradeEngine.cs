using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace TheTechIdea.Beep.Installer
{
    /// <summary>
    /// Handles version detection, backup, and upgrade migration for existing installations.
    /// </summary>
    public class UpgradeEngine
    {
        private readonly InstallLogger? _logger;

        public UpgradeEngine(InstallLogger? logger = null) { _logger = logger; }

        /// <summary>Detects an existing installation and returns its version and path, or null.</summary>
        public ExistingInstall? DetectExisting(string productName)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\TheTechIdea\{productName}");
                if (key == null) return null;

                var path = key.GetValue("InstallPath")?.ToString();
                var version = key.GetValue("Version")?.ToString();
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                    return null;

                return new ExistingInstall
                {
                    ProductName = productName,
                    InstalledVersion = version ?? "unknown",
                    InstallPath = path,
                    ManifestPath = Path.Combine(path, "install-manifest.json")
                };
            }
            catch { return null; }
        }

        /// <summary>Backs up the existing installation to a .backup directory.</summary>
        public string? Backup(string installPath, CancellationToken ct)
        {
            if (!Directory.Exists(installPath)) return null;

            var backupPath = installPath.TrimEnd('\\', '/') + ".backup";
            _logger?.Info("Upgrade", $"Backing up {installPath} → {backupPath}");

            try
            {
                // Remove old backup if exists
                if (Directory.Exists(backupPath))
                    Directory.Delete(backupPath, recursive: true);

                CopyDirectory(installPath, backupPath, ct);
                _logger?.Info("Upgrade", "Backup completed.");
                return backupPath;
            }
            catch (Exception ex)
            {
                _logger?.Error("Upgrade", $"Backup failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>Restores from backup if upgrade fails.</summary>
        public bool RestoreFromBackup(string backupPath, string installPath, CancellationToken ct)
        {
            if (!Directory.Exists(backupPath)) return false;

            _logger?.Info("Upgrade", $"Restoring from backup: {backupPath} → {installPath}");
            try
            {
                if (Directory.Exists(installPath))
                    Directory.Delete(installPath, recursive: true);
                CopyDirectory(backupPath, installPath, ct);
                Directory.Delete(backupPath, recursive: true);
                _logger?.Info("Upgrade", "Restore completed.");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("Upgrade", $"Restore failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>Compares two version strings. Returns true if newVersion is greater.</summary>
        public bool IsNewer(string? currentVersion, string newVersion)
        {
            if (string.IsNullOrWhiteSpace(currentVersion)) return true;
            try
            {
                var current = new Version(currentVersion);
                var newer = new Version(newVersion);
                return newer > current;
            }
            catch { return true; } // If parse fails, assume upgrade
        }

        /// <summary>Migrates user configuration files from old install to new.</summary>
        public void MigrateUserConfig(string oldPath, string newPath)
        {
            var configPatterns = new[] { "*.config", "*.json", "*.xml", "*.ini" };
            foreach (var pattern in configPatterns)
            {
                try
                {
                    foreach (var file in Directory.GetFiles(oldPath, pattern, SearchOption.TopDirectoryOnly))
                    {
                        var dest = Path.Combine(newPath, Path.GetFileName(file));
                        if (!File.Exists(dest))
                        {
                            File.Copy(file, dest, overwrite: false);
                            _logger?.Info("Upgrade", $"Migrated config: {Path.GetFileName(file)}");
                        }
                    }
                }
                catch { /* skip files that can't be read */ }
            }
        }

        public void RegisterInstall(InstallConfig config, string installPath)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey($@"SOFTWARE\TheTechIdea\{config.ProductName}");
                key?.SetValue("InstallPath", installPath);
                key?.SetValue("Version", config.ProductVersion);
                key?.SetValue("Publisher", config.Publisher);
                _logger?.Info("Upgrade", $"Registered install: {config.ProductName} v{config.ProductVersion}");
            }
            catch (Exception ex)
            {
                _logger?.Error("Upgrade", $"Failed to register: {ex.Message}");
            }
        }

        private static void CopyDirectory(string source, string dest, CancellationToken ct)
        {
            Directory.CreateDirectory(dest);
            foreach (var file in Directory.GetFiles(source))
            {
                ct.ThrowIfCancellationRequested();
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
            }
            foreach (var dir in Directory.GetDirectories(source))
            {
                ct.ThrowIfCancellationRequested();
                CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)), ct);
            }
        }
    }

    public class ExistingInstall
    {
        public string ProductName { get; set; } = "";
        public string InstalledVersion { get; set; } = "";
        public string InstallPath { get; set; } = "";
        public string ManifestPath { get; set; } = "";
    }
}
