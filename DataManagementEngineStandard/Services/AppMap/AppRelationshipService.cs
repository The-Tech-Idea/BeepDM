using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Services.AppMap
{
    public interface IAppRelationshipService
    {
        List<AppEnvironmentDatasource> GetAll();
        List<AppEnvironmentDatasource> GetByApp(string appName);
        List<AppEnvironmentDatasource> GetByEnvironment(string environmentId);
        AppEnvironmentDatasource? Get(string appName, string environmentId);
        void Upsert(AppEnvironmentDatasource link);
        bool Remove(string id);
        bool Remove(string appName, string environmentId);
        List<AppMatrixRow> GetMatrix(List<string> environmentIds);
        List<string> GetAppNames();
        Task SaveAsync(CancellationToken token = default);
        Task LoadAsync(CancellationToken token = default);
    }

    public class AppRelationshipService : IAppRelationshipService
    {
        private readonly IDMEEditor _editor;
        private readonly string _persistPath;
        private List<AppEnvironmentDatasource> _links = new();
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AppRelationshipService(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _persistPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BeepDM", "app-relationships.json");
            _ = LoadAsync();
        }

        public List<AppEnvironmentDatasource> GetAll() => _links.ToList();

        public List<AppEnvironmentDatasource> GetByApp(string appName) =>
            _links.Where(l => l.AppName.Equals(appName, StringComparison.OrdinalIgnoreCase)).ToList();

        public List<AppEnvironmentDatasource> GetByEnvironment(string environmentId) =>
            _links.Where(l => l.EnvironmentId.Equals(environmentId, StringComparison.OrdinalIgnoreCase)).ToList();

        public AppEnvironmentDatasource? Get(string appName, string environmentId) =>
            _links.FirstOrDefault(l =>
                l.AppName.Equals(appName, StringComparison.OrdinalIgnoreCase) &&
                l.EnvironmentId.Equals(environmentId, StringComparison.OrdinalIgnoreCase));

        public void Upsert(AppEnvironmentDatasource link)
        {
            if (link == null || string.IsNullOrWhiteSpace(link.AppName) || string.IsNullOrWhiteSpace(link.EnvironmentId))
                return;

            var existing = _links.FirstOrDefault(l =>
                l.AppName.Equals(link.AppName, StringComparison.OrdinalIgnoreCase) &&
                l.EnvironmentId.Equals(link.EnvironmentId, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                var createdAt = existing.CreatedAt;
                _links.Remove(existing);
                link.CreatedAt = createdAt;
            }
            link.UpdatedAt = DateTimeOffset.UtcNow;
            _links.Add(link);

            if (link.IsPrimary)
            {
                foreach (var other in _links.Where(l =>
                    l.AppName.Equals(link.AppName, StringComparison.OrdinalIgnoreCase) &&
                    l.EnvironmentId.Equals(link.EnvironmentId, StringComparison.OrdinalIgnoreCase) &&
                    l.Id != link.Id))
                {
                    other.IsPrimary = false;
                }
            }
        }

        public bool Remove(string id)
        {
            var link = _links.FirstOrDefault(l => l.Id == id);
            if (link == null) return false;
            _links.Remove(link);
            return true;
        }

        public bool Remove(string appName, string environmentId)
        {
            var link = Get(appName, environmentId);
            if (link == null) return false;
            return _links.Remove(link);
        }

        public List<AppMatrixRow> GetMatrix(List<string> environmentIds)
        {
            var appNames = _links.Select(l => l.AppName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var rows = new List<AppMatrixRow>();

            foreach (var appName in appNames)
            {
                var row = new AppMatrixRow { AppName = appName };
                foreach (var envId in environmentIds)
                {
                    var link = Get(appName, envId);
                    row.Cells.Add(new AppMatrixCell
                    {
                        EnvironmentId = envId,
                        EnvironmentName = link?.EnvironmentName ?? envId,
                        DatasourceName = link?.DatasourceName,
                        DatasourceCategory = link?.DatasourceCategory,
                        IsPrimary = link?.IsPrimary ?? false
                    });
                }
                rows.Add(row);
            }

            return rows;
        }

        public List<string> GetAppNames() =>
            _links.Select(l => l.AppName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();

        public async Task SaveAsync(CancellationToken token = default)
        {
            try
            {
                var dir = Path.GetDirectoryName(_persistPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(_persistPath, JsonSerializer.Serialize(_links, JsonOpts), token);
            }
            catch { }
        }

        public async Task LoadAsync(CancellationToken token = default)
        {
            try
            {
                if (File.Exists(_persistPath))
                {
                    var json = await File.ReadAllTextAsync(_persistPath, token);
                    _links = JsonSerializer.Deserialize<List<AppEnvironmentDatasource>>(json, JsonOpts) ?? new();
                }
            }
            catch { _links = new(); }
        }
    }
}
