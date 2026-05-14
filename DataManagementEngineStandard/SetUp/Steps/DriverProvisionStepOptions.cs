using System.Collections.Generic;

namespace TheTechIdea.Beep.SetUp.Steps
{
    /// <summary>
    /// Configuration options for <see cref="DriverProvisionStep"/>.
    /// </summary>
    public class DriverProvisionStepOptions
    {
        /// <summary>
        /// The <see cref="TheTechIdea.Beep.ConfigUtil.ConnectionDriversConfig.PackageName"/>
        /// of the driver to ensure is loaded.
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// Optional specific NuGet package version to download. When null, the version
        /// stored in <see cref="TheTechIdea.Beep.ConfigUtil.ConnectionDriversConfig.NuggetVersion"/>
        /// is used first; if that is also empty the latest stable version is fetched.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Additional NuGet package sources to try when downloading. Appended to any
        /// source already stored in <see cref="TheTechIdea.Beep.ConfigUtil.ConnectionDriversConfig.NuggetSource"/>.
        /// </summary>
        public IList<string> NuGetSources { get; set; } = new List<string>();

        /// <summary>
        /// Override the local installation path for the downloaded package.
        /// When null the assemblyHandler default path is used.
        /// </summary>
        public string AppInstallPath { get; set; }
    }
}
