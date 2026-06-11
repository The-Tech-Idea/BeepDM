// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio.Manifest;

namespace TheTechIdea.Beep.Studio.Manifest;

/// <summary>
/// Default implementation of <see cref="IDataLifecycleManifestService"/>.
/// Reads / writes / validates a <see cref="DataLifecycleManifest"/> from a JSON
/// file in the project repo (default: <c>beep/data-lifecycle-manifest.json</c>).
/// </summary>
public sealed class DataLifecycleManifestService : IDataLifecycleManifestService
{
    private readonly string _defaultPath;
    private DataLifecycleManifest? _current;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly string _studioDataRoot;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public DataLifecycleManifestService(string? dataRoot = null)
    {
        _studioDataRoot = dataRoot ?? TheTechIdea.Beep.Services.EnvironmentService.CreateAppfolder("BeepDMS");
        _defaultPath = ResolveManifestPath(Environment.CurrentDirectory)
            ?? Path.Combine(_studioDataRoot, "data-lifecycle-manifest.json");
    }

    public DataLifecycleManifest? Current => _current;

    public string? ResolveManifestPath(string? startDirectory = null)
    {
        var overridePath = Environment.GetEnvironmentVariable(StudioConstants.ManifestPathEnvVar);
        if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
            return overridePath;

        var dir = startDirectory ?? Environment.CurrentDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var candidate = Path.Combine(dir, StudioConstants.DefaultManifestSubFolder, StudioConstants.DefaultManifestFileName);
            if (File.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return null;
    }

    public async Task<StudioResult<DataLifecycleManifest>> LoadAsync(string? overridePath = null, CancellationToken ct = default)
    {
        var path = !string.IsNullOrWhiteSpace(overridePath) ? overridePath : _defaultPath;
        if (!File.Exists(path))
            return StudioResult<DataLifecycleManifest>.Fail(StudioErrorCode.NotFound, $"Manifest not found at '{path}'.");

        await _loadLock.WaitAsync(ct);
        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            var manifest = JsonSerializer.Deserialize<DataLifecycleManifest>(json, JsonOpts);
            if (manifest == null)
                return StudioResult<DataLifecycleManifest>.Fail(StudioErrorCode.ManifestInvalid, "Manifest deserialized to null.");

            if (manifest.ManifestVersion != StudioConstants.CurrentManifestVersion)
                return StudioResult<DataLifecycleManifest>.Fail(
                    StudioErrorCode.ManifestVersionUnsupported,
                    $"Manifest version {manifest.ManifestVersion} is not supported (current: {StudioConstants.CurrentManifestVersion}).");

            _current = manifest;
            return StudioResult<DataLifecycleManifest>.Ok(manifest);
        }
        catch (JsonException jx)
        {
            return StudioResult<DataLifecycleManifest>.Fail(StudioErrorCode.ManifestInvalid, jx.Message, jx);
        }
        catch (Exception ex)
        {
            return StudioResult<DataLifecycleManifest>.Fail(StudioErrorCode.InternalError, ex.Message, ex);
        }
        finally { _loadLock.Release(); }
    }

    public async Task<StudioResult<bool>> SaveAsync(DataLifecycleManifest manifest, string path, CancellationToken ct = default)
    {
        if (manifest == null) return StudioResult<bool>.Fail(StudioErrorCode.InvalidArgument, "manifest is required.");
        if (string.IsNullOrWhiteSpace(path)) return StudioResult<bool>.Fail(StudioErrorCode.InvalidArgument, "path is required.");
        try
        {
            var json = JsonSerializer.Serialize(manifest, JsonOpts);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, json, ct);
            _current = manifest;
            return StudioResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return StudioResult<bool>.Fail(StudioErrorCode.InternalError, ex.Message, ex);
        }
    }

    public Task<StudioResult<ManifestValidationReport>> ValidateAsync(DataLifecycleManifest manifest, CancellationToken ct = default)
    {
        if (manifest == null)
            return Task.FromResult(StudioResult<ManifestValidationReport>.Fail(StudioErrorCode.InvalidArgument, "manifest is required."));

        var issues = new List<ManifestValidationIssue>();

        if (manifest.ManifestVersion != StudioConstants.CurrentManifestVersion)
            issues.Add(new ManifestValidationIssue("MNF001", "$.manifestVersion",
                $"Manifest version {manifest.ManifestVersion} is not supported (current: {StudioConstants.CurrentManifestVersion}).",
                "Error"));

        if (string.IsNullOrWhiteSpace(manifest.Project?.Name))
            issues.Add(new ManifestValidationIssue("MNF002", "$.project.name", "Project name is required.", "Error"));

        if (manifest.DataLifecycle?.Environments == null || manifest.DataLifecycle.Environments.Count == 0)
            issues.Add(new ManifestValidationIssue("MNF003", "$.dataLifecycle.environments", "At least one environment is required.", "Error"));

        // MNF004 — every environment id is unique
        if (manifest.DataLifecycle?.Environments != null)
        {
            var dupes = manifest.DataLifecycle.Environments
                .GroupBy(e => e.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            foreach (var d in dupes)
                issues.Add(new ManifestValidationIssue("MNF004", "$.dataLifecycle.environments[?(@.id==" + d + ")]",
                    $"Duplicate environment id: '{d}'.", "Error"));
        }

        // MNF005 — every environment.DataSourceAliases references a known expectedSources.alias
        if (manifest.DataLifecycle?.Environments != null && manifest.DataLifecycle.ExpectedSources != null)
        {
            var knownAliases = new HashSet<string>(
                manifest.DataLifecycle.ExpectedSources.Select(s => s.Alias),
                StringComparer.OrdinalIgnoreCase);
            foreach (var env in manifest.DataLifecycle.Environments)
            {
                foreach (var alias in env.DataSourceAliases ?? new List<string>())
                {
                    if (!knownAliases.Contains(alias))
                        issues.Add(new ManifestValidationIssue("MNF005", $"$.dataLifecycle.environments[?(@.id=='{env.Id}')].dataSourceAliases",
                            $"Environment '{env.Id}' references unknown source alias '{alias}'.", "Error"));
                }
            }
        }

        // MNF006 — every expectedSources.driver is a non-empty string
        if (manifest.DataLifecycle?.ExpectedSources != null)
        {
            foreach (var src in manifest.DataLifecycle.ExpectedSources)
            {
                if (string.IsNullOrWhiteSpace(src.Driver))
                    issues.Add(new ManifestValidationIssue("MNF006", $"$.dataLifecycle.expectedSources[?(@.alias=='{src.Alias}')].driver",
                        "Driver is required.", "Error"));
            }
        }

        // MNF007 — syncPolicies.maxRowsPerRun > 0
        if (manifest.DataLifecycle?.SyncPolicies != null && manifest.DataLifecycle.SyncPolicies.MaxRowsPerRun <= 0)
            issues.Add(new ManifestValidationIssue("MNF007", "$.dataLifecycle.syncPolicies.maxRowsPerRun",
                "maxRowsPerRun must be positive.", "Error"));

        // MNF008 — auditPolicies.retentionDays >= 0
        if (manifest.DataLifecycle?.AuditPolicies != null && manifest.DataLifecycle.AuditPolicies.RetentionDays < 0)
            issues.Add(new ManifestValidationIssue("MNF008", "$.dataLifecycle.auditPolicies.retentionDays",
                "retentionDays must be >= 0.", "Error"));

        var hasErrors = issues.Any(i => i.Severity == "Error");
        var report = new ManifestValidationReport(
            IsValid: !hasErrors,
            Issues: issues,
            ValidatedAt: DateTimeOffset.UtcNow,
            ManifestSha256: ComputeSha256(manifest));

        return Task.FromResult(StudioResult<ManifestValidationReport>.Ok(report));
    }

    private static string? ComputeSha256(DataLifecycleManifest manifest)
    {
        try
        {
            var json = JsonSerializer.Serialize(manifest, JsonOpts);
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
            return Convert.ToHexString(hash);
        }
        catch { return null; }
    }
}
