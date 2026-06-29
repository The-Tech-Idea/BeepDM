// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Lifecycle;

/// <summary>
/// Persists and retrieves the Studio operating mode from user-local settings.
/// Modes control feature visibility and approval gate requirements.
/// </summary>
public interface IStudioModeService
{
    StudioModeConfig Current { get; }
    Task<StudioModeConfig> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(StudioModeConfig config, CancellationToken ct = default);
    void SetMode(StudioMode mode, StudioRole? role = null);
}

public sealed class StudioModeService : IStudioModeService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BeepDM", "studio-settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private StudioModeConfig _current = StudioModeConfig.Default;

    public StudioModeConfig Current => _current;

    public async Task<StudioModeConfig> LoadAsync(CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = await File.ReadAllTextAsync(SettingsPath, ct);
                _current = JsonSerializer.Deserialize<StudioModeConfig>(json, JsonOpts)
                           ?? StudioModeConfig.Default;
            }
        }
        catch { _current = StudioModeConfig.Default; }
        return _current;
    }

    public async Task SaveAsync(StudioModeConfig config, CancellationToken ct = default)
    {
        _current = config ?? throw new ArgumentNullException(nameof(config));
        var dir = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(SettingsPath,
            JsonSerializer.Serialize(_current, JsonOpts), ct);
    }

    public void SetMode(StudioMode mode, StudioRole? role = null)
    {
        _current = mode switch
        {
            StudioMode.Solo => StudioModeConfig.Default,
            StudioMode.Team => role switch
            {
                StudioRole.Developer => StudioModeConfig.TeamDeveloper,
                StudioRole.DBA => StudioModeConfig.TeamDBA,
                StudioRole.Admin => StudioModeConfig.TeamAdmin,
                _ => StudioModeConfig.TeamDeveloper,
            },
            StudioMode.Enterprise => role switch
            {
                StudioRole.DBA => StudioModeConfig.EnterpriseDBA,
                StudioRole.Admin => StudioModeConfig.EnterpriseAdmin,
                _ => StudioModeConfig.EnterpriseDBA,
            },
            _ => StudioModeConfig.Default,
        };
    }
}
