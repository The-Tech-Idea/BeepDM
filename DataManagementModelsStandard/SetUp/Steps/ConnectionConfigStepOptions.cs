namespace TheTechIdea.Beep.SetUp.Steps
{
    /// <summary>
    /// Configuration options for <see cref="ConnectionConfigStep"/>.
    /// </summary>
    public class ConnectionConfigStepOptions
    {
        /// <summary>
        /// Connection properties to register and open.
        /// At minimum <see cref="TheTechIdea.Beep.ConfigUtil.ConnectionProperties.ConnectionName"/>
        /// and <see cref="TheTechIdea.Beep.ConfigUtil.ConnectionProperties.DatabaseType"/> must be set.
        /// </summary>
        public TheTechIdea.Beep.ConfigUtil.ConnectionProperties ConnectionProperties { get; set; }

        /// <summary>
        /// When true the step skips validation of the raw connection string structure
        /// (useful when the connection string will be built from individual fields by
        /// <see cref="TheTechIdea.Beep.Helpers.ConnectionHelper.ReplaceValueFromConnectionString"/>).
        /// Default: false.
        /// </summary>
        public bool SkipConnectionStringValidation { get; set; } = false;

        /// <summary>
        /// Base path used by
        /// <see cref="TheTechIdea.Beep.Helpers.ConnectionHelpers.ConnectionHelper.NormalizeFilePath"/>
        /// to resolve relative file paths (file-based datasources only).
        /// When null, <see cref="System.AppContext.BaseDirectory"/> is used.
        /// </summary>
        public string BaseDirectory { get; set; }

        /// <summary>
        /// When true, attempt to open the datasource at the end of Execute and surface
        /// any connection error as a step failure.
        /// Default: true.
        /// </summary>
        public bool OpenConnection { get; set; } = true;

        /// <summary>
        /// Step ids this step depends on. When null, defaults to the single bare
        /// <c>"driver-provision"</c> id.
        /// <para>
        /// A wizard carrying one driver step per package has per-package ids
        /// (<c>"driver-provision:SQLite"</c>), so the connection step must name them all —
        /// a hard-coded single id would leave the other drivers unordered.
        /// </para>
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<string> DependsOnStepIds { get; set; }
    }
}
