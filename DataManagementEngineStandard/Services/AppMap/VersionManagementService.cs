using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Services.AppMap
{
    public sealed class VersionManagementService : IVersionManagementService
    {
        private readonly IDMEEditor _editor;
        private readonly string _persistPath;
        private List<DatabaseVersion> _dbVersions = new();
        private List<AppVersion> _appVersions = new();

        public VersionManagementService(IDMEEditor editor, string? persistPath = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _persistPath = persistPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "TheTechIdea", "BeepDMS", "version-history.json");
        }

        public void RecordDatabaseVersion(DatabaseVersion version)
        {
            version.AppliedAt = DateTime.UtcNow;
            _dbVersions.Add(version);
            Log("DB version recorded", $"{version.DatasourceName} v{version.VersionString}");
        }

        public List<DatabaseVersion> GetVersionHistory(string datasourceName) =>
            _dbVersions.Where(v => v.DatasourceName.Equals(datasourceName, StringComparison.OrdinalIgnoreCase))
                       .OrderByDescending(v => v.Major).ThenByDescending(v => v.Minor).ThenByDescending(v => v.Patch)
                       .ToList();

        public DatabaseVersion? GetLatestVersion(string datasourceName) =>
            GetVersionHistory(datasourceName).FirstOrDefault();

        public VersionComparison CompareVersions(DatabaseVersion v1, DatabaseVersion v2)
        {
            var comparison = new VersionComparison { Version1 = v1, Version2 = v2 };
            // Placeholder — real implementation would diff entity structures
            comparison.Changes = new List<VersionChangeEntry>
            {
                new() { EntityName = "(comparison)", ChangeType = VersionChangeType.Modified,
                    FromVersion = v1.VersionString, ToVersion = v2.VersionString, IsBreaking = v1.Major != v2.Major }
            };
            return comparison;
        }

        public void RecordAppVersion(AppVersion version)
        {
            version.BuildDate = DateTime.UtcNow;
            _appVersions.Add(version);
            Log("App version recorded", version.Version);
        }

        public List<AppVersion> GetAppVersionHistory() =>
            _appVersions.OrderByDescending(v => v.BuildDate).ToList();

        public async Task SaveAsync(CancellationToken token = default)
        {
            var dir = Path.GetDirectoryName(_persistPath)!;
            Directory.CreateDirectory(dir);
            var wrapper = new PersistWrapper { DatabaseVersions = _dbVersions, AppVersions = _appVersions };
            var json = JsonSerializer.Serialize(wrapper, JsonOpts);
            await File.WriteAllTextAsync(_persistPath, json, token).ConfigureAwait(false);
        }

        public async Task LoadAsync(CancellationToken token = default)
        {
            if (!File.Exists(_persistPath)) return;
            var json = await File.ReadAllTextAsync(_persistPath, token).ConfigureAwait(false);
            var wrapper = JsonSerializer.Deserialize<PersistWrapper>(json, JsonOpts);
            if (wrapper != null) { _dbVersions = wrapper.DatabaseVersions ?? new(); _appVersions = wrapper.AppVersions ?? new(); }
        }

        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private void Log(string m, string? d) => _editor.AddLogMessage("Version", d != null ? $"{m}: {d}" : m, DateTime.Now, 0, null, Errors.Ok);

        private class PersistWrapper { public List<DatabaseVersion>? DatabaseVersions { get; set; } public List<AppVersion>? AppVersions { get; set; } }
    }
}
