using Microsoft.Win32;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer
{
    /// <summary>
    /// Resolves registry hive/view and program-file location for the active install scope
    /// (Track A3.1 — 64-bit install mode). Pure helpers shared by the install/uninstall steps.
    /// </summary>
    public static class InstallScope
    {
        /// <summary>Per-user installs write under HKCU; per-machine under HKLM.</summary>
        public static RegistryHive HiveFor(bool perUser)
            => perUser ? RegistryHive.CurrentUser : RegistryHive.LocalMachine;

        /// <summary>64-bit installs use the 64-bit registry view; 32-bit use WOW6432 (Registry32).</summary>
        public static RegistryView ViewFor(bool prefer64Bit)
            => prefer64Bit ? RegistryView.Registry64 : RegistryView.Registry32;

        /// <summary>True when the current run is a per-user install (reads the runtime flag).</summary>
        public static bool IsPerUser(SetupContext context)
            => context.Properties.TryGetValue("PerUser", out var v) && v is bool b && b;

        /// <summary>True when the config targets 64-bit (defaults to true when unset).</summary>
        public static bool Is64Bit(InstallConfig? config)
            => config?.Prefer64Bit ?? true;

        /// <summary>Opens the base key for the active scope (hive + bitness view).</summary>
        public static RegistryKey OpenBaseKey(SetupContext context, InstallConfig? config)
        {
            var perUser = IsPerUser(context);
            var prefer64 = Is64Bit(config);
            // On a 32-bit OS there is no WOW6432 view; fall back to default to avoid failures.
            var view = prefer64 ? RegistryView.Registry64 : RegistryView.Registry32;
            if (!prefer64 && System.Environment.Is64BitOperatingSystem == false)
                view = RegistryView.Default;
            return RegistryKey.OpenBaseKey(HiveFor(perUser), view);
        }
    }
}
