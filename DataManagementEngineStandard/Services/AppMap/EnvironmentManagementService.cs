using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Environments;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Services.AppMap
{
    /// <summary>
    /// Manages environment profiles per project with standard tier seeding,
    /// promote workflows, and environment-wide switching.
    /// </summary>
    public sealed class EnvironmentManagementService : IEnvironmentManagementService
    {
        private readonly IDMEEditor _editor;
        private readonly string _persistPath;
        private List<ProjectEnvironmentProfile> _profiles = new();
        private List<AppEnvironment> _tiers;

        public EnvironmentManagementService(IDMEEditor editor, string? persistPath = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _persistPath = persistPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "TheTechIdea", "BeepDMS", "environment-profiles.json");
            _tiers = CreateStandardTiers();
        }

        public List<AppEnvironment> GetStandardTiers() => _tiers;

        public List<ProjectEnvironmentProfile> GetAllProfilesForProject(string projectName) =>
            _profiles.Where(p => p.ProjectName.Equals(projectName, StringComparison.OrdinalIgnoreCase)).ToList();

        public ProjectEnvironmentProfile? GetProjectProfile(string projectName, string environmentId) =>
            _profiles.FirstOrDefault(p =>
                p.ProjectName.Equals(projectName, StringComparison.OrdinalIgnoreCase) &&
                p.EnvironmentId.Equals(environmentId, StringComparison.OrdinalIgnoreCase));

        public void SetProjectProfile(ProjectEnvironmentProfile profile)
        {
            var existing = GetProjectProfile(profile.ProjectName, profile.EnvironmentId);
            if (existing != null) _profiles.Remove(existing);
            profile.UpdatedAt = DateTime.UtcNow;
            _profiles.Add(profile);
            Log("Profile set", $"{profile.ProjectName}/{profile.EnvironmentId}");
        }

        public void ApplyStandardProfile(string projectName)
        {
            foreach (var tier in _tiers)
            {
                var servers = GetDefaultServers(projectName, tier.Tier);
                SetProjectProfile(new ProjectEnvironmentProfile
                {
                    ProjectName = projectName,
                    EnvironmentId = tier.Id,
                    Servers = servers,
                    Overrides = new List<EnvironmentConfigOverride>(),
                    IsPrimary = tier.Tier == EnvironmentTier.Dev,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            Log("Standard profile applied", projectName);
        }

        public ProjectEnvironmentProfile PromoteConfig(string projectName, EnvironmentTier fromTier, EnvironmentTier toTier)
        {
            var fromEnv = _tiers.First(t => t.Tier == fromTier);
            var toEnv = _tiers.First(t => t.Tier == toTier);
            var source = GetProjectProfile(projectName, fromEnv.Id);
            if (source == null) throw new InvalidOperationException($"No profile for {projectName}/{fromEnv.Id}");

            var clone = new ProjectEnvironmentProfile
            {
                ProjectName = projectName,
                EnvironmentId = toEnv.Id,
                Servers = source.Servers.Select(s => new EnvironmentServerConfig
                {
                    ServerType = s.ServerType,
                    Url = PromoteUrl(s.Url, fromTier, toTier),
                    HealthCheckEndpoint = s.HealthCheckEndpoint,
                    IsRequired = s.IsRequired
                }).ToList(),
                Overrides = source.Overrides.Select(o => new EnvironmentConfigOverride
                {
                    Key = o.Key, Value = o.Value, Description = o.Description
                }).ToList(),
                IsPrimary = toEnv.IsProduction,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            SetProjectProfile(clone);
            Log("Promoted config", $"{projectName}: {fromTier} → {toTier}");
            return clone;
        }

        public List<ProjectEnvironmentProfile> SwitchEnvironment(List<string> projectNames, string fromEnvId, string toEnvId)
        {
            var results = new List<ProjectEnvironmentProfile>();
            foreach (var name in projectNames)
            {
                var profile = GetProjectProfile(name, toEnvId);
                if (profile == null)
                {
                    var fromProfile = GetProjectProfile(name, fromEnvId);
                    if (fromProfile != null)
                    {
                        profile = new ProjectEnvironmentProfile
                        {
                            ProjectName = name, EnvironmentId = toEnvId,
                            Servers = fromProfile.Servers, Overrides = fromProfile.Overrides,
                            IsPrimary = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                        };
                        SetProjectProfile(profile);
                    }
                }
                if (profile != null) results.Add(profile);
            }
            Log("Environment switch", $"{projectNames.Count} projects → {toEnvId}");
            return results;
        }

        public bool DeleteProfile(string projectName, string environmentId)
        {
            var p = GetProjectProfile(projectName, environmentId);
            if (p == null) return false;
            _profiles.Remove(p);
            Log("Profile deleted", $"{projectName}/{environmentId}");
            return true;
        }

        public async Task SaveAsync(CancellationToken token = default)
        {
            var dir = Path.GetDirectoryName(_persistPath)!;
            Directory.CreateDirectory(dir);
            var wrapper = new PersistWrapper { Tiers = _tiers, Profiles = _profiles };
            var json = JsonSerializer.Serialize(wrapper, JsonOptions);
            await File.WriteAllTextAsync(_persistPath, json, token).ConfigureAwait(false);
        }

        public async Task LoadAsync(CancellationToken token = default)
        {
            if (!File.Exists(_persistPath)) return;
            var json = await File.ReadAllTextAsync(_persistPath, token).ConfigureAwait(false);
            var wrapper = JsonSerializer.Deserialize<PersistWrapper>(json, JsonOptions);
            if (wrapper != null)
            {
                _tiers = wrapper.Tiers ?? CreateStandardTiers();
                _profiles = wrapper.Profiles ?? new();
            }
        }

        // ── Helpers ────────────────────────────────────────────

        private static List<AppEnvironment> CreateStandardTiers() => new()
        {
            new() { Id = "local", Name = "Local", Tier = EnvironmentTier.Local, Order = 0, Color = "#9E9E9E", IsProduction = false },
            new() { Id = "dev", Name = "Development", Tier = EnvironmentTier.Dev, Order = 1, Color = "#2196F3", IsProduction = false },
            new() { Id = "test", Name = "Test", Tier = EnvironmentTier.Test, Order = 2, Color = "#FF9800", IsProduction = false },
            new() { Id = "staging", Name = "Staging", Tier = EnvironmentTier.Staging, Order = 3, Color = "#9C27B0", IsProduction = false },
            new() { Id = "production", Name = "Production", Tier = EnvironmentTier.Production, Order = 4, Color = "#4CAF50", IsProduction = true, RequiresApproval = true },
        };

        private static List<EnvironmentServerConfig> GetDefaultServers(string projectName, EnvironmentTier tier)
        {
            var isLocal = tier == EnvironmentTier.Local;
            var prefix = isLocal ? "https://localhost" : $"https://{tier.ToString().ToLower()}-{projectName.ToLower()}.example.com";
            return new()
            {
                new() { ServerType = ServerType.WebServer, Url = $"{prefix}:5000", HealthCheckEndpoint = "/health", IsRequired = true },
                new() { ServerType = ServerType.ApiServer, Url = $"{prefix}:5001", HealthCheckEndpoint = "/health", IsRequired = true },
                new() { ServerType = ServerType.IdentityServer, Url = $"{prefix}:5002", HealthCheckEndpoint = "/health", IsRequired = true },
                new() { ServerType = ServerType.DbServer, Url = isLocal ? "(localdb)" : $"{tier.ToString().ToLower()}-sql.example.com", IsRequired = true }
            };
        }

        private static string PromoteUrl(string url, EnvironmentTier from, EnvironmentTier to)
        {
            var fromStr = from.ToString().ToLower();
            var toStr = to.ToString().ToLower();
            return url.Replace(fromStr, toStr).Replace("localhost", $"{toStr}-api.example.com");
        }

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private void Log(string message, string? detail = null)
        {
            _editor.AddLogMessage("Environment",
                detail != null ? $"{message}: {detail}" : message,
                DateTime.Now, 0, null, Errors.Ok);
        }

        private class PersistWrapper
        {
            public List<AppEnvironment>? Tiers { get; set; }
            public List<ProjectEnvironmentProfile>? Profiles { get; set; }
        }
    }
}
