using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio;
using TheTechIdea.Beep.Studio.Migration.Ledger;

namespace TheTechIdea.Beep.Services.Studio.Migration.Ledger;

public sealed class JsonMigrationLedger : IMigrationLedger
{
    private readonly string _filePath;
    private readonly List<MigrationLedgerEntry> _entries = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public JsonMigrationLedger(string dataRoot)
    {
        _filePath = Path.Combine(dataRoot, "migration-ledger.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<List<MigrationLedgerEntry>>(json, JsonOpts);
                if (loaded != null) _entries.AddRange(loaded);
            }
        }
        catch { /* first run — empty ledger */ }
    }

    private void Save()
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_entries, JsonOpts));
    }

    public Task<StudioResult<MigrationLedgerEntry>> RecordAsync(MigrationLedgerEntry entry, CancellationToken ct = default)
    {
        var existing = _entries.FirstOrDefault(e => e.EntryId == entry.EntryId);
        if (existing != null)
        {
            existing.Status = entry.Status;
            existing.CompletedAt = entry.CompletedAt ?? DateTimeOffset.UtcNow;
            existing.ErrorMessage = entry.ErrorMessage;
            existing.RowsAffected = entry.RowsAffected;
        }
        else
        {
            _entries.Add(entry);
        }
        Save();
        return Task.FromResult(StudioResult<MigrationLedgerEntry>.Ok(entry));
    }

    public Task<StudioResult<MigrationLedgerEntry>> UpdateStatusAsync(string entryId, MigrationLedgerStatus status, string? errorMessage = null, CancellationToken ct = default)
    {
        var entry = _entries.FirstOrDefault(e => e.EntryId == entryId);
        if (entry == null)
            return Task.FromResult(StudioResult<MigrationLedgerEntry>.Fail(
                new StudioError(StudioErrorCode.NotFound, $"Entry {entryId} not found", null, null)));

        entry.Status = status;
        if (status is MigrationLedgerStatus.Succeeded or MigrationLedgerStatus.Failed or MigrationLedgerStatus.Cancelled)
            entry.CompletedAt = DateTimeOffset.UtcNow;
        if (errorMessage != null) entry.ErrorMessage = errorMessage;
        Save();
        return Task.FromResult(StudioResult<MigrationLedgerEntry>.Ok(entry));
    }

    public Task<StudioResult<IReadOnlyList<MigrationLedgerEntry>>> QueryAsync(MigrationLedgerQuery query, CancellationToken ct = default)
    {
        var q = _entries.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query.AppId)) q = q.Where(e => e.AppId == query.AppId);
        if (!string.IsNullOrWhiteSpace(query.EnvId)) q = q.Where(e => e.EnvId == query.EnvId);
        if (!string.IsNullOrWhiteSpace(query.DatasourceName)) q = q.Where(e => e.DatasourceName == query.DatasourceName);
        if (query.Kind.HasValue) q = q.Where(e => e.Kind == query.Kind.Value);
        if (query.Status.HasValue) q = q.Where(e => e.Status == query.Status.Value);
        q = q.OrderByDescending(e => e.AppliedAt);
        if (query.Skip.HasValue) q = q.Skip(query.Skip.Value);
        if (query.Take.HasValue) q = q.Take(query.Take.Value);

        var result = q.ToList() as IReadOnlyList<MigrationLedgerEntry>;
        return Task.FromResult(StudioResult<IReadOnlyList<MigrationLedgerEntry>>.Ok(result));
    }

    public Task<StudioResult<bool>> IsAppliedAsync(string planHash, string? datasourceName = null, CancellationToken ct = default)
    {
        var applied = _entries.Any(e =>
            e.PlanHash == planHash &&
            (datasourceName == null || e.DatasourceName == datasourceName) &&
            e.Status == MigrationLedgerStatus.Succeeded &&
            e.Direction == MigrationDirection.Up);
        return Task.FromResult(StudioResult<bool>.Ok(applied));
    }

    public Task<StudioResult<IReadOnlyList<MigrationLedgerEntry>>> GetRollbackChainAsync(string entryId, CancellationToken ct = default)
    {
        var chain = new List<MigrationLedgerEntry>();
        var entry = _entries.FirstOrDefault(e => e.EntryId == entryId);
        if (entry == null)
            return Task.FromResult(StudioResult<IReadOnlyList<MigrationLedgerEntry>>.Ok(chain));

        chain.Add(entry);
        var children = _entries.Where(e => e.ParentEntryId == entryId).ToList();
        chain.AddRange(children);
        return Task.FromResult(StudioResult<IReadOnlyList<MigrationLedgerEntry>>.Ok((IReadOnlyList<MigrationLedgerEntry>)chain));
    }

    public Task<StudioResult<int>> CountAsync(MigrationLedgerQuery? filter = null, CancellationToken ct = default)
    {
        var q = _entries.AsEnumerable();
        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.AppId)) q = q.Where(e => e.AppId == filter.AppId);
            if (!string.IsNullOrWhiteSpace(filter.EnvId)) q = q.Where(e => e.EnvId == filter.EnvId);
            if (filter.Kind.HasValue) q = q.Where(e => e.Kind == filter.Kind.Value);
            if (filter.Status.HasValue) q = q.Where(e => e.Status == filter.Status.Value);
        }
        return Task.FromResult(StudioResult<int>.Ok(q.Count()));
    }
}
