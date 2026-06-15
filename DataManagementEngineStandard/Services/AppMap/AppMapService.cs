using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using AppMapModel = TheTechIdea.Beep.AppMap.AppMap;
using DiscoveryOptions = TheTechIdea.Beep.AppMap.DiscoveryOptions;
using ProjectDependencyGraph = TheTechIdea.Beep.AppMap.ProjectDependencyGraph;
using ProjectRoleAssignment = TheTechIdea.Beep.AppMap.ProjectRoleAssignment;

namespace TheTechIdea.Beep.Services.AppMap
{
    /// <summary>
    /// Implements IAppMapService: creates AppMap from solution discovery,
    /// detects roles via heuristics, allows manual override, and persists to JSON.
    /// </summary>
    public sealed class AppMapService : IAppMapService
    {
        private readonly IDMEEditor _editor;
        private AppMapModel? _appMap;
        private readonly string _persistPath;

        public AppMapService(IDMEEditor editor, string? persistPath = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _persistPath = persistPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "TheTechIdea", "BeepDMS", "app-map.json");
        }

        public async Task<AppMapModel?> CreateAppMapAsync(string solutionPath, DiscoveryOptions? options = null, CancellationToken token = default)
        {
            var discovery = new SolutionDiscoveryService(_editor);
            var solution = await discovery.DiscoverAsync(solutionPath, options, token).ConfigureAwait(false);
            if (solution == null) return null;

            _appMap = new AppMapModel { Solution = solution };

            foreach (var project in solution.Projects)
            {
                token.ThrowIfCancellationRequested();
                var (role, confidence, heuristic) = RoleDetectionHeuristics.Detect(project);
                _appMap.Projects.Add(new ProjectRoleAssignment
                {
                    Project = project,
                    Role = role,
                    Confidence = confidence,
                    MatchedHeuristic = heuristic,
                    AssignedAt = DateTime.UtcNow
                });
            }

            _appMap.UpdatedAt = DateTime.UtcNow;
            Log("AppMap created", $"{_appMap.Projects.Count} projects, roles auto-detected");
            return _appMap;
        }

        public AppMapModel? GetAppMap() => _appMap;

        public async Task<AppMapModel?> LoadAsync(CancellationToken token = default)
        {
            if (!File.Exists(_persistPath)) return null;

            var json = await File.ReadAllTextAsync(_persistPath, token).ConfigureAwait(false);
            _appMap = JsonSerializer.Deserialize<AppMapModel>(json, JsonOptions);
            Log("AppMap loaded", $"from {_persistPath}");
            return _appMap;
        }

        public void SetRole(string projectName, ProjectRole role)
        {
            if (_appMap == null) return;
            var assignment = _appMap.GetProject(projectName);
            if (assignment == null) return;

            assignment.Role = role;
            assignment.IsManualOverride = true;
            assignment.AssignedAt = DateTime.UtcNow;
            _appMap.UpdatedAt = DateTime.UtcNow;
            Log("Role overridden", $"{projectName} → {role}");
        }

        public List<ProjectRoleAssignment> GetProjectsByRole(ProjectRole role) =>
            _appMap?.GetProjectsByRole(role) ?? new List<ProjectRoleAssignment>();

        public List<string> GetProjectDependencies(string projectName)
        {
            var graph = _appMap != null
                ? new SolutionDiscoveryService(_editor).BuildDependencyGraph(_appMap.Solution.Projects)
                : new ProjectDependencyGraph();
            return graph.Adjacency.TryGetValue(projectName, out var deps) ? deps : new List<string>();
        }

        public async Task SaveAsync(CancellationToken token = default)
        {
            if (_appMap == null) return;

            _appMap.UpdatedAt = DateTime.UtcNow;
            var dir = Path.GetDirectoryName(_persistPath)!;
            Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_appMap, JsonOptions);
            await File.WriteAllTextAsync(_persistPath, json, token).ConfigureAwait(false);
            Log("AppMap saved", _persistPath);
        }

        public void RedetectRoles()
        {
            if (_appMap == null) return;
            foreach (var assignment in _appMap.Projects)
            {
                if (assignment.IsManualOverride) continue;
                var (role, confidence, heuristic) = RoleDetectionHeuristics.Detect(assignment.Project);
                assignment.Role = role;
                assignment.Confidence = confidence;
                assignment.MatchedHeuristic = heuristic;
            }
            _appMap.UpdatedAt = DateTime.UtcNow;
            Log("Roles redetected");
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private void Log(string message, string? detail = null)
        {
            _editor.AddLogMessage("AppMap",
                detail != null ? $"{message}: {detail}" : message,
                DateTime.Now, 0, null, Errors.Ok);
        }
    }
}
