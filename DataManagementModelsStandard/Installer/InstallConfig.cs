using System.Collections.Generic;

namespace TheTechIdea.Beep.Installer
{
    /// <summary>
    /// Complete configuration for a Beep application installation.
    /// </summary>
    public class InstallConfig
    {
        public string ProductName { get; set; } = "Beep Application";
        public string ProductVersion { get; set; } = "1.0.0";
        public string Publisher { get; set; } = "The Tech Idea";
        public string DefaultInstallPath { get; set; }
        public string StartMenuFolder { get; set; }
        public List<InstallComponent> Components { get; set; } = new();
        public List<Prerequisite> Prerequisites { get; set; } = new();
        public List<ShortcutDefinition> Shortcuts { get; set; } = new();
        public List<EnvironmentVariableOp> EnvironmentVariables { get; set; } = new();
        public List<RegistryOperation> RegistryEntries { get; set; } = new();
        public InstallationType DefaultInstallType { get; set; } = InstallationType.Typical;
        public bool RequireAdminPrivileges { get; set; } = true;
        public string LicenseText { get; set; }
        public string BannerImagePath { get; set; }
        public string ProductIconPath { get; set; }
        public string SupportUrl { get; set; }
        public string UpdateUrl { get; set; }
    }

    /// <summary>Selectable feature in the installer.</summary>
    public class InstallComponent
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public long SizeBytes { get; set; }
        public bool Required { get; set; }
        public bool Selected { get; set; } = true;
        public InstallationType IncludedIn { get; set; } = InstallationType.Typical;
        public List<string> DependsOn { get; set; } = new();
        public List<string> ConflictsWith { get; set; } = new();
        public List<FileCopyOperation> Files { get; set; } = new();
        public List<RegistryOperation> Registry { get; set; } = new();
        public List<ShortcutDefinition> Shortcuts { get; set; } = new();
    }

    /// <summary>File copy definition for installation.</summary>
    public class FileCopyOperation
    {
        public string SourcePath { get; set; } = "";
        public string DestinationPath { get; set; } = "";
        public bool Overwrite { get; set; } = true;
        public bool SkipIfNewer { get; set; }
        public bool IsRequired { get; set; } = true;
        public string Description { get; set; } = "";
    }

    /// <summary>Registry key/value operation.</summary>
    public class RegistryOperation
    {
        public string KeyPath { get; set; } = "";
        public string ValueName { get; set; } = "";
        public string Value { get; set; } = "";
        public Microsoft.Win32.RegistryValueKind ValueKind { get; set; } = Microsoft.Win32.RegistryValueKind.String;
        public bool CreateIfNotExists { get; set; } = true;
    }

    /// <summary>Shortcut/link definition.</summary>
    public class ShortcutDefinition
    {
        public string Name { get; set; } = "";
        public string TargetPath { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";
        public string IconPath { get; set; } = "";
        public ShortcutLocation Location { get; set; } = ShortcutLocation.StartMenu;
        public string StartMenuSubfolder { get; set; } = "";
    }

    /// <summary>Environment variable operation.</summary>
    public class EnvironmentVariableOp
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public System.EnvironmentVariableTarget Scope { get; set; } = System.EnvironmentVariableTarget.Machine;
    }

    /// <summary>Required prerequisite for installation.</summary>
    public class Prerequisite
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string VersionRequired { get; set; } = "";
        public string DetectionCommand { get; set; } = "";
        public string DetectionPattern { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string DownloadUrlX86 { get; set; } = "";
        public string SilentInstallArgs { get; set; } = "";
        public bool IsMandatory { get; set; } = true;
        public string HelpUrl { get; set; } = "";
    }

    /// <summary>Record of all items installed for clean uninstall.</summary>
    public class UninstallManifest
    {
        public string ProductName { get; set; } = "";
        public string ProductVersion { get; set; } = "";
        public System.DateTime InstalledAt { get; set; } = System.DateTime.Now;
        public string InstallPath { get; set; } = "";
        public List<string> InstalledFiles { get; set; } = new();
        public List<string> CreatedDirectories { get; set; } = new();
        public List<RegistryOperation> RegistryEntries { get; set; } = new();
        public List<ShortcutDefinition> Shortcuts { get; set; } = new();
        public List<EnvironmentVariableOp> EnvironmentVariables { get; set; } = new();
    }

    public enum InstallationType { Typical, Custom, Complete }
    public enum ShortcutLocation { Desktop, StartMenu, Startup, QuickLaunch }
}
