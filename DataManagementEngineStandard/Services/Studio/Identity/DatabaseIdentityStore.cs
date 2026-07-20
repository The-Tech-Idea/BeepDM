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
using TheTechIdea.Beep.Studio.Apps.Workflows;
using TheTechIdea.Beep.Studio.Identity;
using TheTechIdea.Beep.Studio.Permissions;

namespace TheTechIdea.Beep.Services.Studio.Identity;

/// <summary>
/// Stage 4 enterprise-mode <see cref="IIdentityStore"/>: persists users to a JSON file at
/// <c>{dataRoot}/identity-users.json</c>, hashing passwords with PBKDF2 via <see cref="PasswordHasher"/>.
/// </summary>
/// <remarks>
/// <para>
/// Same Stage 2.2 hardening as <see cref="RoleBasedStudioAuthorizer"/>: process-wide lock, atomic
/// temp-file writes, concurrent-read-safe reads, reload-on-read.
/// </para>
/// <para>
/// <b>Name:</b> "Database" in the class name matches the existing <c>DatabaseStudioRepository</c>
/// naming convention even though the v1 persistence is JSON-backed (Stage 3 deferred the real DB
/// backing store). When <c>DatabaseStudioRepository</c> lands, this store switches its
/// persistence calls to the new sub-store without changing the public surface.
/// </para>
/// <para>
/// <b>Constant-time credential check.</b> <see cref="ValidateCredentialsAsync"/> always runs a
/// throwaway PBKDF2 computation on user-not-found so the response time doesn't leak which usernames
/// are present in the directory.
/// </para>
/// </remarks>
public sealed class DatabaseIdentityStore : IIdentityStore
{
    private const int IoRetryCount = 5;
    private const int IoRetryDelayMs = 30;
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = null };

    private readonly string _filePath;
    private readonly IStudioAuthorizer? _authorizer;  // optional — when present, AssignRole expands to grants.
    private readonly object _lock = new();

    public DatabaseIdentityStore(string dataRoot, IStudioAuthorizer? authorizer = null)
    {
        if (string.IsNullOrWhiteSpace(dataRoot))
            throw new ArgumentException("dataRoot must be a non-empty path.", nameof(dataRoot));
        _filePath = Path.Combine(dataRoot, "identity-users.json");
        _authorizer = authorizer;
    }

    public Task<StudioUser?> FindByIdAsync(string userId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var all = LoadUsers();
            return Task.FromResult<StudioUser?>(all.FirstOrDefault(u => string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<StudioUser?> FindByNameAsync(string userName, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var all = LoadUsers();
            return Task.FromResult<StudioUser?>(all.FirstOrDefault(u => string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<IReadOnlyList<StudioUser>> ListUsersAsync(string? appId = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            // Note: appId is ignored at the user-store level. App-scoped membership is captured by
            // permission grants (via _authorizer.AssignRoleAsync). The param is here for interface
            // symmetry with the role services.
            return Task.FromResult<IReadOnlyList<StudioUser>>(LoadUsers().Where(u => u.IsActive).ToList());
        }
    }

    public Task<StudioUser> CreateAsync(StudioUser user, string? password = null, CancellationToken ct = default)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(user.UserName))
            throw new ArgumentException("UserName is required.", nameof(user));

        lock (_lock)
        {
            var all = LoadUsers();
            if (all.Any(u => string.Equals(u.UserName, user.UserName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"A user named '{user.UserName}' already exists.");

            if (string.IsNullOrWhiteSpace(user.Id)) user.Id = Guid.NewGuid().ToString("N")[..12];
            user.CreatedAt = user.CreatedAt == default ? DateTimeOffset.UtcNow : user.CreatedAt;

            // Stash the password hash on a parallel file so the public StudioUser (which can be
            // serialized to JSON for UI lists) never carries it. Keyed by user id.
            if (!string.IsNullOrEmpty(password))
                WritePasswordHash(user.Id, PasswordHasher.Hash(password));

            all.Add(user);
            SaveUsers(all);
            return Task.FromResult(user);
        }
    }

    public Task<StudioUser> UpdateAsync(StudioUser user, CancellationToken ct = default)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        lock (_lock)
        {
            var all = LoadUsers();
            var idx = all.FindIndex(u => string.Equals(u.Id, user.Id, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) throw new InvalidOperationException($"User '{user.Id}' not found.");
            all[idx] = user;
            SaveUsers(all);
            return Task.FromResult(user);
        }
    }

    public Task<bool> DeleteAsync(string userId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var all = LoadUsers();
            var removed = all.RemoveAll(u => string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
            {
                SaveUsers(all);
                // Best-effort password-hash cleanup.
                try { if (File.Exists(PasswordPath(userId))) File.Delete(PasswordPath(userId)); } catch { }
            }
            return Task.FromResult(removed);
        }
    }

    public async Task<StudioUser?> ValidateCredentialsAsync(string userName, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userName) || password == null) return null;

        StudioUser? user;
        string? storedHash;
        lock (_lock)
        {
            user = LoadUsers().FirstOrDefault(u => string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase));
            storedHash = user == null ? null : ReadPasswordHash(user.Id);
        }

        if (user == null || !user.IsActive)
        {
            // Always pay the cost so timing doesn't reveal which usernames exist.
            _ = PasswordHasher.Verify(password, PasswordHasher.Hash("dummy"));
            return null;
        }

        if (string.IsNullOrEmpty(storedHash))
        {
            // User exists but has no password set (e.g. created without one). Deny.
            _ = PasswordHasher.Verify(password, PasswordHasher.Hash("dummy"));
            return null;
        }

        if (!PasswordHasher.Verify(password, storedHash))
            return null;

        // Update LastLoginAt (fire-and-forget — never block login on the bookkeeping write).
        user.LastLoginAt = DateTimeOffset.UtcNow;
        _ = UpdateLoginTimeAsync(user);
        return user;
    }

    public Task<IReadOnlyList<string>> GetRolesAsync(string userId, string? appId = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var user = LoadUsers().FirstOrDefault(u => string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<IReadOnlyList<string>>((IReadOnlyList<string>?)user?.Roles ?? Array.Empty<string>());
        }
    }

    public async Task AssignRoleAsync(string userId, string role, string? appId = null, CancellationToken ct = default)
    {
        if (!Enum.TryParse<AppMemberRole>(role, ignoreCase: true, out var parsed))
            throw new ArgumentException($"Unknown role '{role}'. Valid: {string.Join(", ", Enum.GetNames<AppMemberRole>())}");

        lock (_lock)
        {
            var all = LoadUsers();
            var user = all.FirstOrDefault(u => string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase));
            if (user == null) throw new InvalidOperationException($"User '{userId}' not found.");
            if (!user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
                user.Roles.Add(role);
            SaveUsers(all);
        }

        // Expand the role template to permission grants at the requested scope. The grants live in
        // the authorizer's store; the role on the user record is for display/audit.
        if (_authorizer != null)
        {
            foreach (var perm in RoleTemplates.For(parsed))
            {
                await _authorizer.GrantAsync(new PermissionGrant
                {
                    UserId = userId,
                    AppId = appId,
                    Action = perm,
                    Effect = PermissionEffect.Allow,
                    GrantedBy = "system",
                    GrantedAt = DateTimeOffset.UtcNow,
                }, ct).ConfigureAwait(false);
            }
        }
    }

    public async Task RevokeRoleAsync(string userId, string role, string? appId = null, CancellationToken ct = default)
    {
        if (!Enum.TryParse<AppMemberRole>(role, ignoreCase: true, out var parsed))
            throw new ArgumentException($"Unknown role '{role}'.");

        lock (_lock)
        {
            var all = LoadUsers();
            var user = all.FirstOrDefault(u => string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase));
            if (user == null) return;
            user.Roles.RemoveAll(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
            SaveUsers(all);
        }

        if (_authorizer != null)
        {
            foreach (var perm in RoleTemplates.For(parsed))
            {
                await _authorizer.RevokeAsync(new PermissionGrant
                {
                    UserId = userId,
                    AppId = appId,
                    Action = perm,
                    Effect = PermissionEffect.Allow,
                }, ct).ConfigureAwait(false);
            }
        }
    }

    // ─── file I/O ─────────────────────────────────────────────────────────────

    private List<StudioUser> LoadUsers()
    {
        try
        {
            if (!File.Exists(_filePath)) return new List<StudioUser>();
            var json = ReadShared(_filePath);
            return string.IsNullOrWhiteSpace(json)
                ? new List<StudioUser>()
                : (JsonSerializer.Deserialize<List<StudioUser>>(json, JsonOpts) ?? new List<StudioUser>());
        }
        catch { return new List<StudioUser>(); }
    }

    private void SaveUsers(List<StudioUser> users)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var tmp = Path.Combine(string.IsNullOrEmpty(dir) ? "." : dir, Path.GetRandomFileName() + ".tmp");
        try
        {
            File.WriteAllText(tmp, JsonSerializer.Serialize(users, JsonOpts), Utf8NoBom);
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

    private string PasswordPath(string userId)
    {
        var dir = Path.GetDirectoryName(_filePath);
        return Path.Combine(string.IsNullOrEmpty(dir) ? "." : dir, "identity-passwords", $"{userId}.hash");
    }

    private string? ReadPasswordHash(string userId)
    {
        try
        {
            var p = PasswordPath(userId);
            return File.Exists(p) ? File.ReadAllText(p).Trim() : null;
        }
        catch { return null; }
    }

    private void WritePasswordHash(string userId, string hash)
    {
        var p = PasswordPath(userId);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllText(p, hash, Utf8NoBom);
    }

    private async Task UpdateLoginTimeAsync(StudioUser user)
    {
        // Bookkeeping — best-effort. Wrapped in try/catch at the call site (fire-and-forget).
        try { await UpdateAsync(user).ConfigureAwait(false); } catch { }
    }

    private static string ReadShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return sr.ReadToEnd();
    }
}

/// <summary>
/// Stage 4 enterprise-mode <see cref="IStudioPrincipal"/>: holds the signed-in user, delegates
/// permission checks to <see cref="IStudioAuthorizer"/>.
/// </summary>
public sealed class DatabaseStudioPrincipal : IStudioPrincipal
{
    private readonly IStudioAuthorizer _authorizer;
    public DatabaseStudioPrincipal(IStudioAuthorizer authorizer)
    {
        _authorizer = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
    }

    public StudioUser? User { get; private set; }

    string TheTechIdea.Beep.SetUp.Security.ISetupPrincipal.Id => User?.Id ?? string.Empty;
    string TheTechIdea.Beep.SetUp.Security.ISetupPrincipal.DisplayName => User?.DisplayName ?? string.Empty;
    IReadOnlyCollection<string> TheTechIdea.Beep.SetUp.Security.ISetupPrincipal.Roles => (IReadOnlyCollection<string>?)User?.Roles ?? Array.Empty<string>();
    bool TheTechIdea.Beep.SetUp.Security.ISetupPrincipal.IsAuthenticated => User != null;

    public bool IsAuthenticated => User != null;

    public void SignIn(StudioUser user) => User = user ?? throw new ArgumentNullException(nameof(user));
    public void SignOut() => User = null;

    public async Task<bool> CanAsync(StudioPermission action, string? appId = null, string? envId = null, string? datasourceName = null, CancellationToken ct = default)
    {
        if (User == null) return false;
        var decision = await _authorizer.EvaluateAsync(User.Id, action, appId, envId, datasourceName, ct).ConfigureAwait(false);
        return decision.Allowed;
    }
}
