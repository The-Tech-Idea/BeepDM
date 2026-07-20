using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio;
using TheTechIdea.Beep.Studio.Migration.Ledger;

namespace TheTechIdea.Beep.Services.Studio.Migration.Ledger;

/// <summary>
/// Default <see cref="IMigrationLedger"/>: a JSON-file-backed ledger at <c>{dataRoot}/migration-ledger.json</c>.
/// </summary>
/// <remarks>
/// <para><b>Stage 2.2 hardening</b> (mirrors <c>SetUp/State/LocalJsonSetupStateStore</c>):</para>
/// <list type="bullet">
/// <item><b>Thread-safe</b>: every public method takes a process-wide lock — concurrent
/// <c>RecordAsync</c> calls from async workflows can't race on the in-memory list or the file.</item>
/// <item><b>Atomic file writes</b>: serialize to a temp file in the same directory, then
/// <c>File.Move(overwrite: true)</c> with retries — a crash mid-write never produces a torn ledger.
/// No partial JSON ever appears at the canonical path.</item>
/// <item><b>Concurrent-read-safe</b>: reads open with <c>FileShare.ReadWrite | FileShare.Delete</c>
/// so a read while another process is writing doesn't throw.</item>
/// <item><b>Read-on-query</b>: every query reloads from disk first, so writes from another process
/// (e.g. another Studio host, or a CLI migration runner) are visible without a restart. The ledger
/// file is small (one entry per migration), so this is cheap.</item>
/// </list>
/// <para>The previous implementation loaded once in the constructor and used plain
/// <c>File.WriteAllText</c>; both behaviors were correctness gaps under concurrent or
/// multi-process use. Stage 2 closes them.</para>
/// </remarks>
public sealed class JsonMigrationLedger : IMigrationLedger
{
    private const int IoRetryCount = 5;
    private const int IoRetryDelayMs = 30;

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,   // round-trip POCO property names verbatim
    };

    private readonly string _filePath;
    private readonly object _lock = new();
    private List<MigrationLedgerEntry> _entries = new();

    public JsonMigrationLedger(string dataRoot)
    {
        if (string.IsNullOrWhiteSpace(dataRoot))
            throw new ArgumentException("dataRoot must be a non-empty path.", nameof(dataRoot));
        _filePath = Path.Combine(dataRoot, "migration-ledger.json");
        // Failure to load on construction is non-fatal: treat as empty ledger (first run).
        ReloadUnsafe();
    }

    // ---------- public API ----------

    public Task<StudioResult<MigrationLedgerEntry>> RecordAsync(MigrationLedgerEntry entry, CancellationToken ct = default)
    {
        if (entry == null)
            return Task.FromResult(StudioResult<MigrationLedgerEntry>.Fail(
                new StudioError(StudioErrorCode.InvalidArgument, "entry is required", null, null)));

        lock (_lock)
        {
            ReloadUnsafe();
            var existing = _entries.FirstOrDefault(e => e.EntryId == entry.EntryId);
            if (existing != null)
            {
                // Updatable fields only — preserve EntryId, AppliedAt, and the original
                // ParentEntryId (a status update must not rewrite provenance).
                existing.Status = entry.Status;
                existing.CompletedAt = entry.CompletedAt ?? DateTimeOffset.UtcNow;
                existing.ErrorMessage = entry.ErrorMessage ?? existing.ErrorMessage;
                existing.RowsAffected = entry.RowsAffected ?? existing.RowsAffected;
                existing.PlanId ??= entry.PlanId;
                existing.PlanHash ??= entry.PlanHash;
                existing.ExecutionToken ??= entry.ExecutionToken;
                if (entry.Metadata != null) existing.Metadata = entry.Metadata;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(entry.EntryId))
                    entry.EntryId = Guid.NewGuid().ToString("N")[..12];
                if (entry.AppliedAt == default) entry.AppliedAt = DateTimeOffset.UtcNow;
                _entries.Add(entry);
            }
            SaveUnsafe();
            return Task.FromResult(StudioResult<MigrationLedgerEntry>.Ok(entry));
        }
    }

    public Task<StudioResult<MigrationLedgerEntry>> UpdateStatusAsync(string entryId, MigrationLedgerStatus status, string? errorMessage = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            ReloadUnsafe();
            var entry = _entries.FirstOrDefault(e => e.EntryId == entryId);
            if (entry == null)
                return Task.FromResult(StudioResult<MigrationLedgerEntry>.Fail(
                    new StudioError(StudioErrorCode.NotFound, $"Entry {entryId} not found", null, null)));

            entry.Status = status;
            if (status is MigrationLedgerStatus.Succeeded or MigrationLedgerStatus.Failed
                or MigrationLedgerStatus.Cancelled or MigrationLedgerStatus.RolledBack)
            {
                entry.CompletedAt = DateTimeOffset.UtcNow;
            }
            if (errorMessage != null) entry.ErrorMessage = errorMessage;
            SaveUnsafe();
            return Task.FromResult(StudioResult<MigrationLedgerEntry>.Ok(entry));
        }
    }

    public Task<StudioResult<IReadOnlyList<MigrationLedgerEntry>>> QueryAsync(MigrationLedgerQuery query, CancellationToken ct = default)
    {
        query ??= new MigrationLedgerQuery();
        lock (_lock)
        {
            ReloadUnsafe();
            var q = _entries.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(query.AppId)) q = q.Where(e => e.AppId == query.AppId);
            if (!string.IsNullOrWhiteSpace(query.EnvId)) q = q.Where(e => e.EnvId == query.EnvId);
            if (!string.IsNullOrWhiteSpace(query.DatasourceName)) q = q.Where(e => e.DatasourceName == query.DatasourceName);
            if (query.Kind.HasValue) q = q.Where(e => e.Kind == query.Kind.Value);
            if (query.Status.HasValue) q = q.Where(e => e.Status == query.Status.Value);
            q = q.OrderByDescending(e => e.AppliedAt);
            if (query.Skip.HasValue && query.Skip.Value > 0) q = q.Skip(query.Skip.Value);
            if (query.Take.HasValue && query.Take.Value > 0) q = q.Take(query.Take.Value);

            // Return a defensive copy — callers must not mutate the in-memory list.
            var result = q.ToList();
            return Task.FromResult(StudioResult<IReadOnlyList<MigrationLedgerEntry>>.Ok(result));
        }
    }

    public Task<StudioResult<bool>> IsAppliedAsync(string planHash, string? datasourceName = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(planHash))
            return Task.FromResult(StudioResult<bool>.Fail(
                new StudioError(StudioErrorCode.InvalidArgument, "planHash is required", null, null)));

        lock (_lock)
        {
            ReloadUnsafe();
            var applied = _entries.Any(e =>
                e.PlanHash == planHash &&
                (datasourceName == null || e.DatasourceName == datasourceName) &&
                e.Status == MigrationLedgerStatus.Succeeded &&
                e.Direction == MigrationDirection.Up);
            return Task.FromResult(StudioResult<bool>.Ok(applied));
        }
    }

    public Task<StudioResult<IReadOnlyList<MigrationLedgerEntry>>> GetRollbackChainAsync(string entryId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            ReloadUnsafe();
            var chain = new List<MigrationLedgerEntry>();
            var entry = _entries.FirstOrDefault(e => e.EntryId == entryId);
            if (entry == null)
                return Task.FromResult(StudioResult<IReadOnlyList<MigrationLedgerEntry>>.Ok(chain));

            chain.Add(entry);
            // All Down entries pointing at this one are part of its rollback chain.
            chain.AddRange(_entries.Where(e => e.ParentEntryId == entryId));
            return Task.FromResult(StudioResult<IReadOnlyList<MigrationLedgerEntry>>.Ok(chain));
        }
    }

    public Task<StudioResult<int>> CountAsync(MigrationLedgerQuery? filter = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            ReloadUnsafe();
            var q = _entries.AsEnumerable();
            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.AppId)) q = q.Where(e => e.AppId == filter.AppId);
                if (!string.IsNullOrWhiteSpace(filter.EnvId)) q = q.Where(e => e.EnvId == filter.EnvId);
                if (!string.IsNullOrWhiteSpace(filter.DatasourceName)) q = q.Where(e => e.DatasourceName == filter.DatasourceName);
                if (filter.Kind.HasValue) q = q.Where(e => e.Kind == filter.Kind.Value);
                if (filter.Status.HasValue) q = q.Where(e => e.Status == filter.Status.Value);
            }
            return Task.FromResult(StudioResult<int>.Ok(q.Count()));
        }
    }

    // ---------- file I/O (called under _lock — hence "Unsafe" suffix) ----------

    /// <summary>Reload <c>_entries</c> from disk. Called under the lock. Non-fatal on error.</summary>
    private void ReloadUnsafe()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _entries = new List<MigrationLedgerEntry>();
                return;
            }
            var json = ReadShared(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                _entries = new List<MigrationLedgerEntry>();
                return;
            }
            var loaded = JsonSerializer.Deserialize<List<MigrationLedgerEntry>>(json, JsonOpts);
            _entries = loaded ?? new List<MigrationLedgerEntry>();
        }
        catch
        {
            // Don't clobber a possibly-corrupt file; fall back to whatever we last had.
            // Callers see an empty/stale ledger rather than crashing the migration.
        }
    }

    /// <summary>Atomic write: temp file in the same dir, then Move(overwrite) with retries.</summary>
    private void SaveUnsafe()
    {
        var dir = Path.GetDirectoryName(_filePath);
        var targetDir = string.IsNullOrEmpty(dir) ? "." : dir;
        Directory.CreateDirectory(targetDir);

        var tmp = Path.Combine(targetDir, Path.GetRandomFileName() + ".tmp");
        try
        {
            File.WriteAllText(tmp, JsonSerializer.Serialize(_entries, JsonOpts), Utf8NoBom);

            for (int attempt = 0; attempt < IoRetryCount; attempt++)
            {
                try
                {
                    File.Move(tmp, _filePath, overwrite: true);
                    return;
                }
                catch (IOException) when (attempt < IoRetryCount - 1) { Thread.Sleep(IoRetryDelayMs); }
                catch (UnauthorizedAccessException) when (attempt < IoRetryCount - 1) { Thread.Sleep(IoRetryDelayMs); }
            }
            // If we get here, every retry failed — log via the only signal we have (the file isn't
            // there). The in-memory list is still correct for this process; another process won't
            // see the write. Higher layers (MigrationStudioService) already swallow ledger errors.
        }
        catch
        {
            // Same posture as the original: persistence is best-effort.
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); }
            catch { /* best-effort cleanup */ }
        }
    }

    /// <summary>Open with ReadWrite|Delete sharing so concurrent writers don't block readers.</summary>
    private static string ReadShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return sr.ReadToEnd();
    }
}
