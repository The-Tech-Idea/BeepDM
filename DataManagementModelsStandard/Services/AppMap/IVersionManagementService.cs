using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Services.AppMap
{
    /// <summary>
    /// Tracks database schema versions and application versions.
    /// </summary>
    public interface IVersionManagementService
    {
        /// <summary>Record a database version after a successful migration.</summary>
        void RecordDatabaseVersion(DatabaseVersion version);

        /// <summary>Get version history for a datasource.</summary>
        List<DatabaseVersion> GetVersionHistory(string datasourceName);

        /// <summary>Get the latest version for a datasource.</summary>
        DatabaseVersion? GetLatestVersion(string datasourceName);

        /// <summary>Compare two versions and produce a change log.</summary>
        VersionComparison CompareVersions(DatabaseVersion v1, DatabaseVersion v2);

        /// <summary>Record an application version deployment.</summary>
        void RecordAppVersion(AppVersion version);

        /// <summary>Get all recorded application versions.</summary>
        List<AppVersion> GetAppVersionHistory();

        /// <summary>Persist all version data to JSON.</summary>
        Task SaveAsync(CancellationToken token = default);

        /// <summary>Load version data from JSON.</summary>
        Task LoadAsync(CancellationToken token = default);
    }
}
