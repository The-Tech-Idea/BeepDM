// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Studio;
using TheTechIdea.Beep.Studio.Apps.Workflows;
using TheTechIdea.Beep.Studio.Governance;
using TheTechIdea.Beep.Studio.Migration.Ledger;
using TheTechIdea.Beep.Studio.Repository;
using TheTechIdea.Beep.Services.Studio.Migration.Ledger; // JsonMigrationLedger lives here (inconsistent with the rest of the Studio.Services namespace)

namespace TheTechIdea.Beep.Services.Studio.Repository;

/// <summary>
/// Stage 3 default <see cref="IStudioRepository"/>: file-backed, mirroring the Stage 2.2 hardening
/// of <c>JsonMigrationLedger</c> (atomic temp-file + <c>File.Move(overwrite:true)</c> with retries,
/// concurrent-read-safe reads, process-wide lock, reload-on-read so cross-process writes are visible).
/// </summary>
/// <remarks>
/// <para>
/// This is the SOLO default — same observable behavior as the pre-Stage-3 services (<c>AppRegistry</c>,
/// <c>GovernanceService</c>, <c>EnvironmentProfileService</c>, masking-rules) but with the hardening
/// they each lacked: atomic writes, thread safety, and OCC.
/// </para>
/// <para>
/// <see cref="IStudioRepository.MigrationLedger"/> is satisfied by reusing the existing
/// <see cref="JsonMigrationLedger"/> (Stage 2.2). Apps/EnvProfiles/Governance/Masking get fresh
/// sub-stores that consolidate the previously-scattered file code under one pattern.
/// </para>
/// <para>
/// <b>RowVersion</b> = the SHA-256 of the canonical JSON (lowercased hex). The file impl validates
/// it on save when <c>expectedRowVersion</c> is non-null — this is the optimistic-concurrency gate
/// the DB impl will satisfy with a real binary token. The OCC refusal signal is always
/// <see cref="StudioRepositoryConflictException"/>.
/// </para>
/// </remarks>
public sealed class FileStudioRepository : IStudioRepository
{
    private const int IoRetryCount = 5;
    private const int IoRetryDelayMs = 30;
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        // Records serialize cleanly with the defaults; preserve original property names.
        PropertyNamingPolicy = null,
    };

    private readonly string _root;
    private readonly object _lock = new();

    public FileStudioRepository(string dataRoot, IMigrationLedger? migrationLedger = null)
    {
        if (string.IsNullOrWhiteSpace(dataRoot))
            throw new ArgumentException("dataRoot must be a non-empty path.", nameof(dataRoot));
        _root = dataRoot;
        Directory.CreateDirectory(_root);
        Apps = new AppStore(this);
        EnvironmentProfiles = new EnvironmentProfileStore(this);
        Governance = new GovernanceStore(this);
        MaskingRules = new MaskingRuleStore(this);
        MigrationLedger = migrationLedger ?? new JsonMigrationLedger(_root);
    }

    public IAppRepositoryStore Apps { get; }
    public IEnvironmentProfileRepositoryStore EnvironmentProfiles { get; }
    public IGovernanceRepositoryStore Governance { get; }
    public IMaskingRuleRepositoryStore MaskingRules { get; }
    public IMigrationLedger MigrationLedger { get; }

    public Task<IStudioRepositoryLease?> TryAcquireLeaseAsync(string resourceKey, TimeSpan ttl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
            throw new ArgumentException("resourceKey is required.", nameof(resourceKey));
        lock (_lock)
        {
            var leasePath = LeasePath(resourceKey);
            Directory.CreateDirectory(Path.GetDirectoryName(leasePath)!);

            // If a lock file exists and is not expired, refuse.
            if (File.Exists(leasePath))
            {
                var existing = TryReadJson<LeaseRecord>(leasePath);
                if (existing != null && existing.ExpiresAt > DateTimeOffset.UtcNow)
                    return Task.FromResult<IStudioRepositoryLease?>(null);
                // Expired — fall through and overwrite.
            }

            var leaseId = Guid.NewGuid().ToString("N");
            var record = new LeaseRecord(leaseId, DateTimeOffset.UtcNow.Add(ttl));
            AtomicWrite(leasePath, JsonSerializer.Serialize(record, JsonOpts));
            return Task.FromResult<IStudioRepositoryLease?>(new FileLease(this, resourceKey, leaseId, record.ExpiresAt));
        }
    }

    // ─── shared file primitives (all called under _lock) ──────────────────────

    internal string PathFor(string fileName) => Path.Combine(_root, fileName);

    private string LeasePath(string resourceKey) =>
        Path.Combine(_root, "locks", $"{Sanitize(resourceKey)}.lock");

    private static string Sanitize(string key) =>
        string.Concat(key.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_'));

    /// <summary>Read with shared FileShare so concurrent writers don't block readers.</summary>
    internal static string ReadShared(string path)
    {
        if (!File.Exists(path)) return string.Empty;
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return sr.ReadToEnd();
    }

    /// <summary>Atomic write: temp file in the same dir, then Move(overwrite) with retries.</summary>
    internal void AtomicWrite(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        var targetDir = string.IsNullOrEmpty(dir) ? "." : dir;
        Directory.CreateDirectory(targetDir);
        var tmp = Path.Combine(targetDir, Path.GetRandomFileName() + ".tmp");
        try
        {
            File.WriteAllText(tmp, content, Utf8NoBom);
            for (int attempt = 0; attempt < IoRetryCount; attempt++)
            {
                try { File.Move(tmp, path, overwrite: true); return; }
                catch (IOException) when (attempt < IoRetryCount - 1) { Thread.Sleep(IoRetryDelayMs); }
                catch (UnauthorizedAccessException) when (attempt < IoRetryCount - 1) { Thread.Sleep(IoRetryDelayMs); }
            }
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); }
            catch { /* best-effort cleanup */ }
        }
    }

    /// <summary>Compute the RowVersion = SHA-256 of canonical JSON content.</summary>
    private static string RowVersionOf(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    private static void VerifyRowVersion(string? expected, string current, string resourceKey)
    {
        if (expected != null && !string.Equals(expected, current, StringComparison.Ordinal))
        {
            throw new StudioRepositoryConflictException(
                $"Stale write refused for '{resourceKey}'. Caller expected row version {expected}, " +
                $"store currently has {current}. Reload and retry.")
            {
                ResourceKey = resourceKey,
                ExpectedVersion = expected,
                CurrentVersion = current,
            };
        }
    }

    private T? TryReadJson<T>(string path) where T : class
    {
        var json = ReadShared(path);
        return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    // ─── sub-store implementations ───────────────────────────────────────────

    private sealed class AppStore : IAppRepositoryStore
    {
        private readonly FileStudioRepository _repo;
        public AppStore(FileStudioRepository repo) => _repo = repo;

        public Task<IReadOnlyList<AppDefinition>> LoadAllAsync(CancellationToken ct = default)
        {
            lock (_repo._lock)
            {
                var all = _repo.TryReadJson<List<AppDefinition>>(_repo.PathFor("apps.json")) ?? new List<AppDefinition>();
                return Task.FromResult<IReadOnlyList<AppDefinition>>(all.ToList());
            }
        }

        public Task<(AppDefinition? App, string? RowVersion)> LoadAsync(string appId, CancellationToken ct = default)
        {
            lock (_repo._lock)
            {
                var all = _repo.TryReadJson<List<AppDefinition>>(_repo.PathFor("apps.json")) ?? new List<AppDefinition>();
                var found = all.FirstOrDefault(a => string.Equals(a.Id, appId, StringComparison.OrdinalIgnoreCase));
                if (found == null) return Task.FromResult<(AppDefinition?, string?)>((null, null));
                var version = RowVersionOf(ReadShared(_repo.PathFor("apps.json")));
                return Task.FromResult<(AppDefinition?, string?)>((found, version));
            }
        }

        public Task<string> SaveAsync(AppDefinition app, string? expectedRowVersion = null, CancellationToken ct = default)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            lock (_repo._lock)
            {
                var path = _repo.PathFor("apps.json");
                var currentJson = ReadShared(path);
                VerifyRowVersion(expectedRowVersion, RowVersionOf(currentJson), "apps");

                var all = string.IsNullOrWhiteSpace(currentJson)
                    ? new List<AppDefinition>()
                    : (JsonSerializer.Deserialize<List<AppDefinition>>(currentJson, JsonOpts) ?? new List<AppDefinition>());

                // Match AppRegistry semantics: upsert by Id OR Name; preserve CreatedAt.
                var existing = all.FirstOrDefault(a =>
                    string.Equals(a.Id, app.Id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a.Name, app.Name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    app.CreatedAt = existing.CreatedAt;
                    var idx = all.IndexOf(existing);
                    all[idx] = app;
                }
                else
                {
                    app.CreatedAt = app.CreatedAt == default ? DateTime.UtcNow : app.CreatedAt;
                    all.Add(app);
                }
                app.UpdatedAt = DateTime.UtcNow;

                var newJson = JsonSerializer.Serialize(all, JsonOpts);
                _repo.AtomicWrite(path, newJson);
                return Task.FromResult(RowVersionOf(newJson));
            }
        }

        public Task<bool> DeleteAsync(string appId, string? expectedRowVersion = null, CancellationToken ct = default)
        {
            lock (_repo._lock)
            {
                var path = _repo.PathFor("apps.json");
                var currentJson = ReadShared(path);
                VerifyRowVersion(expectedRowVersion, RowVersionOf(currentJson), "apps");

                var all = string.IsNullOrWhiteSpace(currentJson)
                    ? new List<AppDefinition>()
                    : (JsonSerializer.Deserialize<List<AppDefinition>>(currentJson, JsonOpts) ?? new List<AppDefinition>());
                var count = all.RemoveAll(a => string.Equals(a.Id, appId, StringComparison.OrdinalIgnoreCase));
                if (count > 0) _repo.AtomicWrite(path, JsonSerializer.Serialize(all, JsonOpts));
                return Task.FromResult(count > 0);
            }
        }
    }

    private sealed class EnvironmentProfileStore : IEnvironmentProfileRepositoryStore
    {
        private readonly FileStudioRepository _repo;
        public EnvironmentProfileStore(FileStudioRepository repo) => _repo = repo;

        public Task<IReadOnlyList<EnvironmentProfile>> LoadAllAsync(CancellationToken ct = default)
        {
            lock (_repo._lock)
            {
                var all = _repo.TryReadJson<List<EnvironmentProfile>>(_repo.PathFor("env-profiles.json")) ?? new List<EnvironmentProfile>();
                return Task.FromResult<IReadOnlyList<EnvironmentProfile>>(all.ToList());
            }
        }

        public Task<string> SaveAsync(EnvironmentProfile profile, string? expectedRowVersion = null, CancellationToken ct = default)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            lock (_repo._lock)
            {
                var path = _repo.PathFor("env-profiles.json");
                var currentJson = ReadShared(path);
                VerifyRowVersion(expectedRowVersion, RowVersionOf(currentJson), "env-profiles");

                var all = string.IsNullOrWhiteSpace(currentJson)
                    ? new List<EnvironmentProfile>()
                    : (JsonSerializer.Deserialize<List<EnvironmentProfile>>(currentJson, JsonOpts) ?? new List<EnvironmentProfile>());

                var existing = all.FirstOrDefault(p => string.Equals(p.Id, profile.Id, StringComparison.OrdinalIgnoreCase));
                if (existing != null) all[all.IndexOf(existing)] = profile;
                else all.Add(profile);

                var newJson = JsonSerializer.Serialize(all, JsonOpts);
                _repo.AtomicWrite(path, newJson);
                return Task.FromResult(RowVersionOf(newJson));
            }
        }

        public Task<bool> DeleteAsync(string envId, string? expectedRowVersion = null, CancellationToken ct = default)
        {
            lock (_repo._lock)
            {
                var path = _repo.PathFor("env-profiles.json");
                var currentJson = ReadShared(path);
                VerifyRowVersion(expectedRowVersion, RowVersionOf(currentJson), "env-profiles");

                var all = string.IsNullOrWhiteSpace(currentJson)
                    ? new List<EnvironmentProfile>()
                    : (JsonSerializer.Deserialize<List<EnvironmentProfile>>(currentJson, JsonOpts) ?? new List<EnvironmentProfile>());
                var count = all.RemoveAll(p => string.Equals(p.Id, envId, StringComparison.OrdinalIgnoreCase));
                if (count > 0) _repo.AtomicWrite(path, JsonSerializer.Serialize(all, JsonOpts));
                return Task.FromResult(count > 0);
            }
        }
    }

    private sealed class GovernanceStore : IGovernanceRepositoryStore
    {
        private readonly FileStudioRepository _repo;
        public GovernanceStore(FileStudioRepository repo) => _repo = repo;

        public Task<IReadOnlyList<GovernancePolicy>> LoadPoliciesAsync(CancellationToken ct = default)
        {
            lock (_repo._lock)
            {
                var all = _repo.TryReadJson<List<GovernancePolicy>>(_repo.PathFor("governance-policies.json")) ?? new List<GovernancePolicy>();
                return Task.FromResult<IReadOnlyList<GovernancePolicy>>(all.ToList());
            }
        }

        public Task<string> SavePolicyAsync(GovernancePolicy policy, string? expectedRowVersion = null, CancellationToken ct = default)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            lock (_repo._lock)
            {
                var path = _repo.PathFor("governance-policies.json");
                var currentJson = ReadShared(path);
                VerifyRowVersion(expectedRowVersion, RowVersionOf(currentJson), "governance-policies");

                var all = string.IsNullOrWhiteSpace(currentJson)
                    ? new List<GovernancePolicy>()
                    : (JsonSerializer.Deserialize<List<GovernancePolicy>>(currentJson, JsonOpts) ?? new List<GovernancePolicy>());

                var existing = all.FirstOrDefault(p => string.Equals(p.PolicyId, policy.PolicyId, StringComparison.OrdinalIgnoreCase));
                if (existing != null) all[all.IndexOf(existing)] = policy;
                else all.Add(policy);

                var newJson = JsonSerializer.Serialize(all, JsonOpts);
                _repo.AtomicWrite(path, newJson);
                return Task.FromResult(RowVersionOf(newJson));
            }
        }

        public Task<bool> DeletePolicyAsync(string policyId, string? expectedRowVersion = null, CancellationToken ct = default)
        {
            lock (_repo._lock)
            {
                var path = _repo.PathFor("governance-policies.json");
                var currentJson = ReadShared(path);
                VerifyRowVersion(expectedRowVersion, RowVersionOf(currentJson), "governance-policies");

                var all = string.IsNullOrWhiteSpace(currentJson)
                    ? new List<GovernancePolicy>()
                    : (JsonSerializer.Deserialize<List<GovernancePolicy>>(currentJson, JsonOpts) ?? new List<GovernancePolicy>());
                // Caller passes the PolicyId; the explore doc's GovernanceService uses PolicyId as the key.
                var count = all.RemoveAll(p => string.Equals(p.PolicyId, policyId, StringComparison.OrdinalIgnoreCase));
                if (count > 0) _repo.AtomicWrite(path, JsonSerializer.Serialize(all, JsonOpts));
                return Task.FromResult(count > 0);
            }
        }

        public Task<IReadOnlyList<ApprovalRequest>> LoadApprovalsAsync(CancellationToken ct = default)
        {
            lock (_repo._lock)
            {
                var all = _repo.TryReadJson<List<ApprovalRequest>>(_repo.PathFor("governance-approvals.json")) ?? new List<ApprovalRequest>();
                return Task.FromResult<IReadOnlyList<ApprovalRequest>>(all.ToList());
            }
        }

        public Task<string> SaveApprovalAsync(ApprovalRequest approval, string? expectedRowVersion = null, CancellationToken ct = default)
        {
            if (approval == null) throw new ArgumentNullException(nameof(approval));
            lock (_repo._lock)
            {
                var path = _repo.PathFor("governance-approvals.json");
                var currentJson = ReadShared(path);
                VerifyRowVersion(expectedRowVersion, RowVersionOf(currentJson), "governance-approvals");

                var all = string.IsNullOrWhiteSpace(currentJson)
                    ? new List<ApprovalRequest>()
                    : (JsonSerializer.Deserialize<List<ApprovalRequest>>(currentJson, JsonOpts) ?? new List<ApprovalRequest>());

                var existing = all.FirstOrDefault(a => string.Equals(a.ApprovalId, approval.ApprovalId, StringComparison.OrdinalIgnoreCase));
                if (existing != null) all[all.IndexOf(existing)] = approval;
                else all.Add(approval);

                var newJson = JsonSerializer.Serialize(all, JsonOpts);
                _repo.AtomicWrite(path, newJson);
                return Task.FromResult(RowVersionOf(newJson));
            }
        }
    }

    private sealed class MaskingRuleStore : IMaskingRuleRepositoryStore
    {
        private readonly FileStudioRepository _repo;
        public MaskingRuleStore(FileStudioRepository repo) => _repo = repo;

        public Task<IReadOnlyList<MaskingRule>> LoadAsync(string appId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(appId)) throw new ArgumentException("appId is required.", nameof(appId));
            lock (_repo._lock)
            {
                var path = _repo.PathFor(Path.Combine("masking", $"{Sanitize(appId)}.json"));
                var all = _repo.TryReadJson<List<MaskingRule>>(path) ?? new List<MaskingRule>();
                return Task.FromResult<IReadOnlyList<MaskingRule>>(all.ToList());
            }
        }

        public Task SaveAsync(string appId, IReadOnlyCollection<MaskingRule> rules, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(appId)) throw new ArgumentException("appId is required.", nameof(appId));
            lock (_repo._lock)
            {
                var path = _repo.PathFor(Path.Combine("masking", $"{Sanitize(appId)}.json"));
                _repo.AtomicWrite(path, JsonSerializer.Serialize(rules.ToList(), JsonOpts));
                return Task.CompletedTask;
            }
        }
    }

    // ─── lease ────────────────────────────────────────────────────────────────

    private sealed record LeaseRecord(string LeaseId, DateTimeOffset ExpiresAt);

    private sealed class FileLease : IStudioRepositoryLease
    {
        private readonly FileStudioRepository _repo;
        private bool _disposed;

        public FileLease(FileStudioRepository repo, string resourceKey, string leaseId, DateTimeOffset expiresAt)
        {
            _repo = repo;
            ResourceKey = resourceKey;
            LeaseId = leaseId;
            ExpiresAt = expiresAt;
        }

        public string ResourceKey { get; }
        public string LeaseId { get; }
        public DateTimeOffset ExpiresAt { get; private set; }

        public Task<bool> RenewAsync(CancellationToken ct = default)
        {
            lock (_repo._lock)
            {
                if (_disposed) return Task.FromResult(false);
                var path = _repo.LeasePath(ResourceKey);
                var existing = _repo.TryReadJson<LeaseRecord>(path);
                if (existing == null || existing.LeaseId != LeaseId)
                    return Task.FromResult(false); // lost
                ExpiresAt = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(30));
                _repo.AtomicWrite(path, JsonSerializer.Serialize(new LeaseRecord(LeaseId, ExpiresAt), JsonOpts));
                return Task.FromResult(true);
            }
        }

        public ValueTask DisposeAsync()
        {
            lock (_repo._lock)
            {
                if (_disposed) return ValueTask.CompletedTask;
                _disposed = true;
                var path = _repo.LeasePath(ResourceKey);
                try
                {
                    var existing = _repo.TryReadJson<LeaseRecord>(path);
                    // Only delete if our lease still owns it — never stomp a reclaimed lease.
                    if (existing != null && existing.LeaseId == LeaseId && File.Exists(path))
                        File.Delete(path);
                }
                catch { /* best-effort release */ }
            }
            return ValueTask.CompletedTask;
        }
    }
}
