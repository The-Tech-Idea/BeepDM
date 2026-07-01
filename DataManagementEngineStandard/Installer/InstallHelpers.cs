using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace TheTechIdea.Beep.Installer
{
    /// <summary>Shared install utilities: hash verification, restart manager, firewall, restore point, file associations, env broadcast.</summary>
    public static class InstallHelpers
    {
        #region Hash Verification

        /// <summary>Computes SHA256 hash of a file. Returns null on error.</summary>
        public static string? ComputeFileHash(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                var hash = SHA256.HashData(stream);
                return Convert.ToHexString(hash);
            }
            catch { return null; }
        }

        /// <summary>Verifies a file matches the expected SHA256 hash.</summary>
        public static bool VerifyFileHash(string filePath, string expectedHash)
        {
            var actual = ComputeFileHash(filePath);
            return string.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Restart Manager (Locked Files)

        /// <summary>
        /// Schedules a file to be replaced/removed on next reboot.
        /// Uses MoveFileEx with MOVEFILE_DELAY_UNTIL_REBOOT.
        /// </summary>
        public static bool ScheduleFileForRestart(string sourcePath, string? destPath)
        {
            return MoveFileEx(sourcePath, destPath, MoveFileFlags.DelayUntilReboot);
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, MoveFileFlags dwFlags);

        [Flags]
        private enum MoveFileFlags
        {
            ReplaceExisting = 1,
            CopyAllowed = 2,
            DelayUntilReboot = 4,
            WriteThrough = 8
        }

        /// <summary>Checks if a file is currently locked by another process.</summary>
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch (IOException) { return true; }
            catch { return false; }
        }

        #endregion

        #region System Restore Point

        /// <summary>Creates a Windows System Restore point before installation.</summary>
        public static bool CreateSystemRestorePoint(string description)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore");
                key?.SetValue("SystemRestorePointCreationFrequency", 0);

                var type = Type.GetTypeFromCLSID(new Guid("{f51d50f0-2f82-4e5e-a5d2-3e4b3e5f0a1b}"));
                if (type == null) return false;

                dynamic? restorePoint = Activator.CreateInstance(type);
                if (restorePoint == null) return false;

                // SRSetRestorePoint via P/Invoke is safer:
                return SRSetRestorePoint(description, 0, 0);
            }
            catch { return false; }
        }

        [DllImport("Srclient.dll", SetLastError = true)]
        private static extern bool SRSetRestorePoint(
            [MarshalAs(UnmanagedType.LPWStr)] string description,
            int eventType, int restorePointType);

        #endregion

        #region Firewall Rules

        /// <summary>Adds a Windows Firewall inbound rule.</summary>
        public static bool AddFirewallRule(string name, string programPath, string port = "", string protocol = "TCP")
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = string.IsNullOrEmpty(port)
                            ? $"advfirewall firewall add rule name=\"{name}\" dir=in action=allow program=\"{programPath}\" enable=yes"
                            : $"advfirewall firewall add rule name=\"{name}\" dir=in action=allow protocol={protocol} localport={port} enable=yes",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(10000);
                return process.ExitCode == 0;
            }
            catch { return false; }
        }

        /// <summary>Removes a Windows Firewall rule.</summary>
        public static bool RemoveFirewallRule(string name)
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"advfirewall firewall delete rule name=\"{name}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(10000);
                return process.ExitCode == 0;
            }
            catch { return false; }
        }

        #endregion

        #region File Type Associations

        /// <summary>Registers a file extension association with an application.</summary>
        public static void RegisterFileAssociation(string extension, string progId, string description, string iconPath, string openCommand)
        {
            // Register ProgID
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
            {
                key?.SetValue("", description);
                using var iconKey = key?.CreateSubKey("DefaultIcon");
                iconKey?.SetValue("", iconPath);
                using var cmdKey = key?.CreateSubKey(@"shell\open\command");
                cmdKey?.SetValue("", openCommand);
            }

            // Associate extension
            using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}"))
            {
                extKey?.SetValue("", progId);
            }

            // Notify shell
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>Removes a file extension association.</summary>
        public static void UnregisterFileAssociation(string extension, string progId)
        {
            try { Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{progId}", throwOnMissingSubKey: false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{extension}", throwOnMissingSubKey: false); } catch { }
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

        #endregion

        #region Environment Broadcast

        /// <summary>Broadcasts environment variable changes to the system.</summary>
        public static void BroadcastEnvironmentChange()
        {
            SendMessageTimeout(
                new IntPtr(0xFFFF), // HWND_BROADCAST
                0x001A,             // WM_SETTINGCHANGE
                IntPtr.Zero,
                "Environment",
                0x0002,             // SMTO_ABORTIFHUNG
                5000,
                out _);
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd, uint Msg, IntPtr wParam, string lParam,
            uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        #endregion

        #region Per-User vs Per-Machine

        /// <summary>Gets the appropriate registry root for the install scope.</summary>
        public static RegistryKey GetRegistryRoot(bool perUser)
            => perUser ? Registry.CurrentUser : Registry.LocalMachine;

        /// <summary>Gets the default install path for the given scope.</summary>
        public static string GetDefaultInstallPath(string productName, bool perUser)
            => perUser
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", productName)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), productName);

        #endregion
    }
}
