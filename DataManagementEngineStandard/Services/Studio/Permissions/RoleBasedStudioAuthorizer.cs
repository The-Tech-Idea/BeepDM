// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio;
using TheTechIdea.Beep.Studio.Permissions;

namespace TheTechIdea.Beep.Services.Studio.Permissions;

/// <summary>
/// Stage 4 enterprise-mode <see cref="IStudioAuthorizer"/>: persists <see cref="PermissionGrant"/>s
/// to a JSON file at <c>{dataRoot}/permission-grants.json</c>, evaluates them with the
/// deny-wins/most-specific-wins algorithm in <see cref="PermissionEvaluator"/>.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the Stage 2.2 hardening: process-wide <c>lock</c>, atomic temp-file + <c>File.Move(overwrite:true)</c>
/// with retries, concurrent-read-safe reads (<c>FileShare.ReadWrite | FileShare.Delete</c>), reload-on-read
/// so grants added from another process are visible without a restart. Same idiom as
/// <see cref="TheTechIdea.Beep.Services.Studio.Migration.Ledger.JsonMigrationLedger"/> and
/// <see cref="TheTechIdea.Beep.Services.Studio.Repository.FileStudioRepository"/>.
/// </para>
/// <para>
/// <b>Role assignment</b>: <c>IIdentityStore.AssignRoleAsync</c> expands a role via
/// <see cref="RoleTemplates"/> into one grant per permission at the requested scope. Direct
/// <see cref="GrantAsync"/> calls are the escape hatch for fine-grained overrides (e.g. an explicit
/// <c>Deny</c> on a specific env).
/// </para>
/// </remarks>
public sealed class RoleBasedStudioAuthorizer : IStudioAuthorizer
{
    private const int IoRetryCount = 5;
    private const int IoRetryDelayMs = 30;
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = null };

    private readonly string _filePath;
    private readonly object _lock = new();

    public RoleBasedStudioAuthorizer(string dataRoot)
    {
        if (string.IsNullOrWhiteSpace(dataRoot))
            throw new ArgumentException("dataRoot must be a non-empty path.", nameof(dataRoot));
        _filePath = Path.Combine(dataRoot, "permission-grants.json");
    }

    /// <inheritdoc />
    public Task<PermissionDecision> EvaluateAsync(
        string userId, StudioPermission action,
        string? appId = null, string? envId = null, string? datasourceName = null,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            var grants = LoadGrants();
            var decision = PermissionEvaluator.Evaluate(grants, userId, action, appId, envId, datasourceName);
            return Task.FromResult(decision);
        }
    }

    /// <inheritdoc />
    public Task<TheTechIdea.Beep.SetUp.Security.SetupAuthorizationResult> AuthorizeAsync(
        TheTechIdea.Beep.SetUp.Security.ISetupPrincipal principal,
        TheTechIdea.Beep.SetUp.Security.SetupPermission permission,
        TheTechIdea.Beep.SetUp.SetupContext context,
        CancellationToken token = default)
    {
        // Map the setup permission 1:1 to a StudioPermission (values match by design).
        var studioPermission = (StudioPermission)(int)permission;
        var decision = EvaluateAsync(principal.Id, studioPermission, ct: token).GetAwaiter().GetResult();
        return Task.FromResult(decision.Allowed
            ? TheTechIdea.Beep.SetUp.Security.SetupAuthorizationResult.Allow()
            : TheTechIdea.Beep.SetUp.Security.SetupAuthorizationResult.Deny(decision.Reasons.FirstOrDefault() ?? $"Denied {permission}"));
    }

    public Task GrantAsync(PermissionGrant grant, CancellationToken ct = default)
    {
        if (grant == null) throw new ArgumentNullException(nameof(grant));
        lock (_lock)
        {
            var grants = LoadGrants();
            // Idempotent: replace any existing grant with the same (user, action, scope).
            grants.RemoveAll(g => SameScope(g, grant));
            grants.Add(grant);
            SaveGrants(grants);
            return Task.CompletedTask;
        }
    }

    public Task RevokeAsync(PermissionGrant grant, CancellationToken ct = default)
    {
        if (grant == null) throw new ArgumentNullException(nameof(grant));
        lock (_lock)
        {
            var grants = LoadGrants();
            grants.RemoveAll(g => SameScope(g, grant));
            SaveGrants(grants);
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<PermissionGrant>> ListGrantsAsync(string? userId = null, string? appId = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var q = LoadGrants().AsEnumerable();
            if (!string.IsNullOrEmpty(userId)) q = q.Where(g => string.Equals(g.UserId, userId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(appId)) q = q.Where(g => string.Equals(g.AppId, appId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<IReadOnlyList<PermissionGrant>>(q.ToList());
        }
    }

    public Task<IReadOnlyList<string>> ResolveActorsAsync(StudioPermission action, string? appId, string? envId = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            // Distinct users who have an Allow grant at a scope matching the request.
            var actors = LoadGrants()
                .Where(g => g.Action == action && g.Effect == PermissionEffect.Allow)
                .Where(g => string.IsNullOrEmpty(g.AppId) || string.Equals(g.AppId, appId, StringComparison.OrdinalIgnoreCase))
                .Where(g => string.IsNullOrEmpty(g.EnvId) || string.Equals(g.EnvId, envId, StringComparison.OrdinalIgnoreCase))
                .Select(g => g.UserId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return Task.FromResult<IReadOnlyList<string>>(actors);
        }
    }

    // ─── file I/O (under _lock) ───────────────────────────────────────────────

    private List<PermissionGrant> LoadGrants()
    {
        try
        {
            if (!File.Exists(_filePath)) return new List<PermissionGrant>();
            var json = ReadShared(_filePath);
            return string.IsNullOrWhiteSpace(json)
                ? new List<PermissionGrant>()
                : (JsonSerializer.Deserialize<List<PermissionGrant>>(json, JsonOpts) ?? new List<PermissionGrant>());
        }
        catch { return new List<PermissionGrant>(); }
    }

    private void SaveGrants(List<PermissionGrant> grants)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var tmp = Path.Combine(string.IsNullOrEmpty(dir) ? "." : dir, Path.GetRandomFileName() + ".tmp");
        try
        {
            File.WriteAllText(tmp, JsonSerializer.Serialize(grants, JsonOpts), Utf8NoBom);
            for (int attempt = 0; attempt < IoRetryCount; attempt++)
            {
                try { File.Move(tmp, _filePath, overwrite: true); return; }
                catch (IOException) when (attempt < IoRetryCount - 1) { Thread.Sleep(IoRetryDelayMs); }
                catch (UnauthorizedAccessException) when (attempt < IoRetryCount - 1) { Thread.Sleep(IoRetryDelayMs); }
            }
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    private static string ReadShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return sr.ReadToEnd();
    }

    /// <summary>True when two grants cover the same (user, action, scope) — used for idempotent upsert.</summary>
    private static bool SameScope(PermissionGrant a, PermissionGrant b) =>
        string.Equals(a.UserId, b.UserId, StringComparison.OrdinalIgnoreCase)
        && a.Action == b.Action
        && string.Equals(a.AppId ?? "", b.AppId ?? "", StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.EnvId ?? "", b.EnvId ?? "", StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.DatasourceName ?? "", b.DatasourceName ?? "", StringComparison.OrdinalIgnoreCase);
}
