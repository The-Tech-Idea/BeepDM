using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.Installer
{
    /// <summary>
    /// Complete configuration for a Beep application installation.
    /// </summary>
    public class InstallConfig
    {
        /// <summary>
        /// Schema version of the install-config.json contract. The builder stamps the
        /// current version; the runtime warns on mismatch (P0-3 handshake).
        /// </summary>
        public const string CurrentSchemaVersion = "1.0";

        /// <summary>Schema version stamped by the generator. Defaults to current.</summary>
        public string SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>
        /// Runtime-only: directory the install-config.json was loaded from. Used to
        /// resolve rebased (relative) payload paths. Not serialized.
        /// </summary>
        [JsonIgnore]
        public string? ConfigDirectory { get; set; }

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

        /// <summary>
        /// When true (default), per-machine installs resolve to the 64-bit Program Files and the
        /// 64-bit registry view (non-WOW6432). Set false for 32-bit (x86) installers.
        /// Stamped by the builder from <c>BuildOptions.Architecture</c>.
        /// </summary>
        public bool Prefer64Bit { get; set; } = true;
        public string LicenseText { get; set; }
        public string BannerImagePath { get; set; }
        public string ProductIconPath { get; set; }
        public string SupportUrl { get; set; }
        public string UpdateUrl { get; set; }

        /// <summary>How the on-launch updater behaves when a newer version is available (Track B3).</summary>
        public UpdateMode UpdateMode { get; set; } = UpdateMode.Optional;
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
        public List<ComRegistration> ComRegistrations { get; set; } = new();
        public List<GacAssembly> GacAssemblies { get; set; } = new();

        /// <summary>
        /// Conditions that must all evaluate true for this component to be selectable/visible
        /// on the components page (OS/arch/registry/etc.). Evaluated via InstallConditionEvaluator.
        /// Empty/null means always available.
        /// </summary>
        public List<InstallCondition> Conditions { get; set; } = new();
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

        /// <summary>
        /// When true, the file participates in MSI-style shared-file reference counting
        /// (SharedDLLs). Each install increments the count; uninstall only removes the file
        /// when the count reaches zero, so a DLL shared between products survives the first
        /// uninstall. (Track A3.2.)
        /// </summary>
        public bool SharedCount { get; set; }
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

    /// <summary>
    /// COM in-process server registration (Track A3.3). Written to HKCR\CLSID\{Clsid}
    /// (+ ProgId). DllPath may use the <c>{InstallPath}</c> macro. Scope-aware: per-user
    /// registrations go to HKCU\Software\Classes, per-machine to HKLM\Software\Classes.
    /// </summary>
    public class ComRegistration
    {
        public string Clsid { get; set; } = "";          // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
        public string ProgId { get; set; } = "";
        public string Description { get; set; } = "";
        public string ThreadingModel { get; set; } = "Apartment"; // Apartment|Free|Both|Neutral
        public string DllPath { get; set; } = "";         // {InstallPath}\server.dll or absolute
    }

    /// <summary>An assembly to install/remove from the GAC (Track A3.3).</summary>
    public class GacAssembly
    {
        public string Path { get; set; } = "";            // {InstallPath}\Lib.dll or absolute
        public string StrongName { get; set; } = "";       // used for /u (e.g. "Lib, Version=1.0.0.0, ...")
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

        /// <summary>Installed files under SharedDLLs reference counting (kept until count hits 0).</summary>
        public List<string> SharedFiles { get; set; } = new();

        /// <summary>COM servers registered during install (reversed on uninstall).</summary>
        public List<ComRegistration> ComRegistrations { get; set; } = new();

        /// <summary>Assemblies installed to the GAC during install (removed on uninstall).</summary>
        public List<GacAssembly> GacAssemblies { get; set; } = new();
    }

public enum InstallationType { Typical, Custom, Complete }
public enum ShortcutLocation { Desktop, StartMenu, Startup, QuickLaunch }
public enum UpdateMode { Optional, Required }

    /// <summary>Condition evaluated by InstallConditionEvaluator to gate component visibility.</summary>
    public class InstallCondition
    {
        public ConditionType Type { get; set; }
        public string? Value { get; set; }
        public string? Value2 { get; set; }
        public string Operator { get; set; } = "==";
    }

    public enum ConditionType
    {
        AlwaysTrue, AlwaysFalse, OsVersion, Architecture,
        RegistryExists, RegistryValue, FileExists, DirectoryExists,
        CommandReturns, IsAdmin
    }
}
