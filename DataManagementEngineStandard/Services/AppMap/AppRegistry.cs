using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;

namespace TheTechIdea.Beep.Services.AppMap;

public sealed class AppRegistry : IAppRegistry
{
    private readonly List<AppDefinition> _apps = new();
    private readonly string _persistPath;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppRegistry(IDMEEditor? dme = null)
    {
        _persistPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BeepDM", "apps.json");
        // Must complete before any caller reads/writes _apps — otherwise the
        // connector (RegisterWithAppRegistry) races the load and creates a
        // duplicate on every startup.
        LoadAsync().GetAwaiter().GetResult();
    }

    public AppDefinition RegisterApp(AppDefinition app)
    {
        // Auto-seed standard environments if none provided
        if (app.Environments.Count == 0)
        {
            app.Environments = new List<AppEnv>
            {
                new() { EnvironmentId = "dev",  Tier = "dev",  Order = 0, IsBaseline = true },
                new() { EnvironmentId = "test", Tier = "test", Order = 1 },
                new() { EnvironmentId = "staging", Tier = "staging", Order = 2, RequiresApproval = true },
                new() { EnvironmentId = "prod", Tier = "production", Order = 3, IsProduction = true, RequiresApproval = true }
            };
        }

        var saved = SaveApp(app);
        return saved ?? app;
    }

    /// <inheritdoc />
    public AppDefinition? SaveApp(AppDefinition app)
    {
        if (app == null) return null;

        // Remove EVERY existing app that matches by id or name — not just the
        // first. This makes the upsert truly idempotent: a connector that
        // registers the same app every startup (with a fresh id but the same
        // name) will collapse to a single record instead of accumulating duplicates.
        var createdAt = _apps
            .Where(a => a.Id == app.Id || a.Name.Equals(app.Name, StringComparison.OrdinalIgnoreCase))
            .Select(a => a.CreatedAt)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Min();
        _apps.RemoveAll(a =>
            a.Id == app.Id || a.Name.Equals(app.Name, StringComparison.OrdinalIgnoreCase));

        var now = DateTime.UtcNow;
        app.CreatedAt = createdAt;
        app.UpdatedAt = now;
        _apps.Add(app);
        _ = SaveAsync();
        return app;
    }

    public AppDefinition? GetApp(string appId) =>
        _apps.FirstOrDefault(a =>
            a.Id.Equals(appId, StringComparison.OrdinalIgnoreCase) ||
            a.Name.Equals(appId, StringComparison.OrdinalIgnoreCase));

    public List<AppDefinition> GetAllApps() => _apps.OrderBy(a => a.Name).ToList();

    public AppDefinition? AddEnvironment(string appId, AppEnv env)
    {
        var app = GetApp(appId);
        if (app == null) return null;
        if (app.Environments.Any(e => e.EnvironmentId == env.EnvironmentId))
            return app;
        app.Environments.Add(env);
        app.UpdatedAt = DateTime.UtcNow;
        _ = SaveAsync();
        return app;
    }

    public AppDefinition? SetDatasource(string appId, string envId, AppDataSource ds)
    {
        var app = GetApp(appId);
        if (app == null) return null;
        var env = app.GetEnvironment(envId);
        if (env == null) return null;
        var existing = env.Datasources.FirstOrDefault(d => d.Name == ds.Name);
        if (existing != null)
            env.Datasources.Remove(existing);
        env.Datasources.Add(ds);
        app.UpdatedAt = DateTime.UtcNow;
        _ = SaveAsync();
        return app;
    }

    public bool RemoveApp(string appId)
    {
        var app = GetApp(appId);
        if (app == null) return false;
        _apps.Remove(app);
        _ = SaveAsync();
        return true;
    }

    public PromotionsNeeded GetPromotionStatus(string appId)
    {
        var app = GetApp(appId);
        var result = new PromotionsNeeded { AppId = appId, AppName = app?.Name ?? appId };
        if (app == null) return result;

        var baseline = app.Environments.FirstOrDefault(e => e.IsBaseline)
                       ?? app.Environments.FirstOrDefault();
        if (baseline == null) return result;

        result.BaselineEnvId = baseline.EnvironmentId;
        foreach (var env in app.Environments.Where(e => !e.IsBaseline))
        {
            if (env.SchemaVersion != baseline.SchemaVersion)
                result.EnvsBehind.Add(env.EnvironmentId);
        }
        return result;
    }

    private async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(_persistPath)) return;
            var json = await File.ReadAllTextAsync(_persistPath);
            var loaded = JsonSerializer.Deserialize<List<AppDefinition>>(json, JsonOpts);
            if (loaded == null) return;

            // De-duplicate on load. Older code paths (and connectors that mint a
            // fresh id every startup) could leave multiple records with the same
            // name. Keep the most recently updated one per name (and per id),
            // persist the cleaned list once, and log the collapse.
            var before = loaded.Count;
            loaded = loaded
                .GroupBy(a => a.Name.Trim().ToLowerInvariant())
                .Select(g => g.OrderByDescending(a => a.UpdatedAt).ThenByDescending(a => a.CreatedAt).First())
                .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Also drop any lingering same-id duplicates the name grouping missed.
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            loaded = loaded.Where(a => seenIds.Add(a.Id)).ToList();

            _apps.AddRange(loaded);
            if (_apps.Count < before)
            {
                // best-effort: persist the cleaned state so it sticks.
                _ = SaveAsync();
            }
        }
        catch { /* first run — no file yet */ }
    }

    public async Task SaveAsync(CancellationToken token = default)
    {
        try
        {
            var dir = Path.GetDirectoryName(_persistPath);
            if (dir != null) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_apps, JsonOpts);
            await File.WriteAllTextAsync(_persistPath, json, token);
        }
        catch { /* best effort */ }
    }
}

public sealed class PromotionsNeeded
{
    public string AppId { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string BaselineEnvId { get; set; } = string.Empty;
    public List<string> EnvsBehind { get; set; } = new();
    public int TotalDriftItems { get; set; }
    public bool AnyBehind => EnvsBehind.Count > 0;
}
