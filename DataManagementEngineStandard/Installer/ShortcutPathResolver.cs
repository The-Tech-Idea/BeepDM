using System;
using System.IO;

namespace TheTechIdea.Beep.Installer
{
    /// <summary>
    /// Resolves where a <see cref="ShortcutDefinition"/> lives on disk.
    ///
    /// This exists as one shared function because ShortcutCreateStep and UninstallStep each
    /// had their own copy, and the copies disagreed: the create side fell back to
    /// <see cref="InstallConfig.StartMenuFolder"/> when no subfolder was set while the remove
    /// side did not, and the remove side appended ".lnk" unconditionally where the create side
    /// appended it only when missing. Either mismatch orphaned the shortcut at uninstall.
    /// Neither copy was scope-aware, so a per-machine install still wrote to the current
    /// user's Start Menu and Desktop.
    /// </summary>
    public static class ShortcutPathResolver
    {
        /// <summary>
        /// Absolute path for <paramref name="shortcut"/>, or an empty string when the location
        /// is not supported.
        /// </summary>
        /// <param name="perUser">
        /// True for a per-user install (the current user's folders), false for per-machine
        /// (the all-users / "Common" folders). Quick Launch is inherently per-user.
        /// </param>
        public static string Resolve(ShortcutDefinition shortcut, InstallConfig? config, bool perUser)
        {
            if (shortcut == null) return "";

            var name = shortcut.Name ?? "";
            if (!name.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) name += ".lnk";

            return shortcut.Location switch
            {
                ShortcutLocation.Desktop => Path.Combine(Folder(
                    perUser ? Environment.SpecialFolder.DesktopDirectory
                            : Environment.SpecialFolder.CommonDesktopDirectory), name),

                ShortcutLocation.StartMenu => Path.Combine(
                    Folder(perUser ? Environment.SpecialFolder.Programs
                                   : Environment.SpecialFolder.CommonPrograms),
                    StartMenuSubfolder(shortcut, config),
                    name),

                ShortcutLocation.Startup => Path.Combine(Folder(
                    perUser ? Environment.SpecialFolder.Startup
                            : Environment.SpecialFolder.CommonStartup), name),

                // Quick Launch has no all-users equivalent.
                ShortcutLocation.QuickLaunch => Path.Combine(
                    Folder(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Internet Explorer\Quick Launch", name),

                _ => ""
            };
        }

        /// <summary>
        /// Group folder for a Start Menu shortcut: its own subfolder, else the product's
        /// configured folder, else the Programs root.
        /// </summary>
        private static string StartMenuSubfolder(ShortcutDefinition shortcut, InstallConfig? config)
            => !string.IsNullOrWhiteSpace(shortcut.StartMenuSubfolder)
                ? shortcut.StartMenuSubfolder
                : (config?.StartMenuFolder ?? "");

        private static string Folder(Environment.SpecialFolder folder)
            => Environment.GetFolderPath(folder) ?? "";
    }
}
