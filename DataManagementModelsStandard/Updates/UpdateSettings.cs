using TheTechIdea.Beep.Installer;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// The installed app's update configuration, stamped next to the app as
    /// <c>update-settings.json</c> at build time (from the project's <c>AppUpdatesURL</c> /
    /// <c>AppUpdateMode</c>) and read by <c>AddBeepAppUpdates()</c> by default. Reuses the
    /// installer's <see cref="UpdateMode"/> rather than duplicating the enum.
    /// </summary>
    public sealed class UpdateSettings
    {
        /// <summary>Stable URL of <c>feed.json</c> (HTTPS, or a local/UNC folder path for LAN feeds).</summary>
        public string FeedUrl { get; set; } = "";

        public string Channel { get; set; } = "stable";

        /// <summary><see cref="UpdateMode.Required"/> blocks the app until applied; <see cref="UpdateMode.Optional"/> notifies.</summary>
        public UpdateMode Mode { get; set; } = UpdateMode.Optional;

        /// <summary>
        /// The version currently installed, used as the baseline for the feed comparison.
        /// Normally the running assembly's version; overridable for tests and the CLI.
        /// </summary>
        public string CurrentVersion { get; set; } = "";

        /// <summary>
        /// Root under which side-by-side <c>app-&lt;version&gt;</c> directories live and the
        /// <c>current</c> junction points (Stage 11.B). Defaults to the app's own directory.
        /// </summary>
        public string? InstallRoot { get; set; }

        /// <summary>Directory the app's NuGet modules install under (<c>&lt;dir&gt;/&lt;id&gt;/&lt;version&gt;/</c>). Optional.</summary>
        public string? ModuleDirectory { get; set; }

        /// <summary>
        /// Managed-environment opt-out: when true the service never checks or applies. Mirrors the
        /// <c>BEEP_NO_UPDATE</c> environment override honoured by the policy layer.
        /// </summary>
        public bool Disabled { get; set; }
    }
}
