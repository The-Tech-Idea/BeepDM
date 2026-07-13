using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Studio.Migration.Ledger;

public enum MigrationKind { Schema, Data }
public enum MigrationDirection { Up, Down }
public enum MigrationLedgerStatus { Pending, Running, Succeeded, Failed, Skipped, RolledBack, Cancelled }

public sealed class MigrationLedgerEntry
{
    public string EntryId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string? ParentEntryId { get; set; }
    public MigrationKind Kind { get; set; }
    public MigrationDirection Direction { get; set; }
    public MigrationLedgerStatus Status { get; set; } = MigrationLedgerStatus.Pending;

    public string? AppId { get; set; }
    public string? EnvId { get; set; }
    public string DatasourceName { get; set; } = string.Empty;
    public string? SourceEnv { get; set; }
    public string? TargetEnv { get; set; }
    public string? PlanId { get; set; }
    public string? PlanHash { get; set; }
    public string? ExecutionToken { get; set; }
    public int? RowsAffected { get; set; }
    public int StepCount { get; set; }
    public string? Checksum { get; set; }
    public string AppliedBy { get; set; } = "system";
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}

public sealed class MigrationLedgerQuery
{
    public string? AppId { get; set; }
    public string? EnvId { get; set; }
    public string? DatasourceName { get; set; }
    public MigrationKind? Kind { get; set; }
    public MigrationLedgerStatus? Status { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }
}
