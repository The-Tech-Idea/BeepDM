// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio.Contracts;

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// Real implementation of <see cref="IEnvironmentProfileService"/>.
/// Persists <see cref="EnvironmentProfile"/> records to <c>env-profiles.json</c>
/// and seeds default profiles that map to the engine's
/// <see cref="TheTechIdea.Beep.Environments.EnvironmentType"/> tiers.
/// </summary>
public sealed class EnvironmentProfileService : IEnvironmentProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _persistPath;
    private readonly object _sync = new();
    private List<EnvironmentProfile> _profiles;

    public EnvironmentProfileService(string? dataRoot = null)
    {
        _persistPath = Path.Combine(
            dataRoot ?? EnvironmentService.CreateAppfolder("Studio"),
            "env-profiles.json");
        _profiles = LoadFromDisk();
    }

    public Task<StudioResult<IReadOnlyList<EnvironmentProfile>>> ListAsync(CancellationToken ct = default)
    {
        lock (_sync)
        {
            var ordered = _profiles.OrderBy(p => p.Order).ToList();
            return Task.FromResult(StudioResult<IReadOnlyList<EnvironmentProfile>>.Ok(ordered));
        }
    }

    public Task<StudioResult<EnvironmentProfile>> GetAsync(string environmentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(environmentId))
        {
            return Task.FromResult(StudioResult<EnvironmentProfile>.Fail(
                StudioErrorCode.InvalidArgument, "Environment id is required."));
        }

        lock (_sync)
        {
            var profile = _profiles.FirstOrDefault(p =>
                string.Equals(p.Id, environmentId, StringComparison.OrdinalIgnoreCase));
            if (profile == null)
            {
                return Task.FromResult(StudioResult<EnvironmentProfile>.Fail(
                    StudioErrorCode.NotFound, $"Environment profile '{environmentId}' not found."));
            }

            return Task.FromResult(StudioResult<EnvironmentProfile>.Ok(profile));
        }
    }

    public Task<StudioResult<EnvironmentProfile>> SaveAsync(EnvironmentProfile profile, CancellationToken ct = default)
    {
        if (profile == null || string.IsNullOrWhiteSpace(profile.Id))
        {
            return Task.FromResult(StudioResult<EnvironmentProfile>.Fail(
                StudioErrorCode.InvalidArgument, "Profile with a valid Id is required."));
        }

        lock (_sync)
        {
            var existing = _profiles.FirstOrDefault(p =>
                string.Equals(p.Id, profile.Id, StringComparison.OrdinalIgnoreCase));

            var now = DateTimeOffset.UtcNow;
            var createdAt = existing?.CreatedAt ?? now;
            var updated = profile with { UpdatedAt = now, CreatedAt = createdAt };

            if (existing != null)
            {
                _profiles.Remove(existing);
            }

            _profiles.Add(updated);
            SaveToDisk();

            return Task.FromResult(StudioResult<EnvironmentProfile>.Ok(updated));
        }
    }

    public Task<StudioResult<bool>> DeleteAsync(string environmentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(environmentId))
        {
            return Task.FromResult(StudioResult<bool>.Fail(
                StudioErrorCode.InvalidArgument, "Environment id is required."));
        }

        lock (_sync)
        {
            var existing = _profiles.FirstOrDefault(p =>
                string.Equals(p.Id, environmentId, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                return Task.FromResult(StudioResult<bool>.Fail(
                    StudioErrorCode.NotFound, $"Environment profile '{environmentId}' not found."));
            }

            _profiles.Remove(existing);
            SaveToDisk();
            return Task.FromResult(StudioResult<bool>.Ok(true));
        }
    }

    public Task<StudioResult<EnvironmentProfile>> GetDefaultAsync(CancellationToken ct = default)
    {
        lock (_sync)
        {
            var dev = _profiles.FirstOrDefault(p => p.Tier == RolloutTier.Dev)
                      ?? _profiles.OrderBy(p => p.Order).FirstOrDefault();

            if (dev == null)
            {
                return Task.FromResult(StudioResult<EnvironmentProfile>.Fail(
                    StudioErrorCode.NotFound, "No default environment profile found."));
            }

            return Task.FromResult(StudioResult<EnvironmentProfile>.Ok(dev));
        }
    }

    // ── Persistence ──────────────────────────────────────────

    private List<EnvironmentProfile> LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_persistPath))
            {
                return CreateDefaultProfiles();
            }

            var json = File.ReadAllText(_persistPath);
            var loaded = JsonSerializer.Deserialize<List<EnvironmentProfile>>(json, JsonOptions);
            if (loaded == null || loaded.Count == 0)
            {
                return CreateDefaultProfiles();
            }

            return loaded;
        }
        catch
        {
            return CreateDefaultProfiles();
        }
    }

    private void SaveToDisk()
    {
        try
        {
            var dir = Path.GetDirectoryName(_persistPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(_profiles, JsonOptions);
            File.WriteAllText(_persistPath, json);
        }
        catch
        {
            // Persistence failures are non-fatal; profiles remain in-memory.
        }
    }

    /// <summary>
    /// Creates the standard set of environment profiles that map to the
    /// engine's <see cref="TheTechIdea.Beep.Environments.EnvironmentType"/> values.
    /// </summary>
    private static List<EnvironmentProfile> CreateDefaultProfiles()
    {
        var now = DateTimeOffset.UtcNow;
        return new List<EnvironmentProfile>
        {
            new("dev", "Development", RolloutTier.Dev, 0, "#2196F3",
                RequiresApproval: false, RequiredApproverCount: 0, IsProduction: false,
                Tags: Array.Empty<string>(), CreatedAt: now, UpdatedAt: now),
            new("test", "Test", RolloutTier.Test, 1, "#FF9800",
                RequiresApproval: false, RequiredApproverCount: 0, IsProduction: false,
                Tags: Array.Empty<string>(), CreatedAt: now, UpdatedAt: now),
            new("staging", "Staging", RolloutTier.Staging, 2, "#9C27B0",
                RequiresApproval: true, RequiredApproverCount: 1, IsProduction: false,
                Tags: Array.Empty<string>(), CreatedAt: now, UpdatedAt: now),
            new("live", "Live (Production)", RolloutTier.Live, 3, "#4CAF50",
                RequiresApproval: true, RequiredApproverCount: 2, IsProduction: true,
                Tags: new[] { "production" }, CreatedAt: now, UpdatedAt: now),
        };
    }
}
