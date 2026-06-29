using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Studio.Apps.Workflows;

/// <summary>
/// Data movement between an app's environments: copy, subset, and PII-masked copy.
/// Enterprise/cloud must-have — moving prod data to non-prod requires masking.
/// </summary>
public interface IAppDataWorkflow
{
    /// <summary>Copy all data from one env's datasource to another (same schema).</summary>
    Task<StudioResult<DataSyncReport>> CopyAsync(string appId, string fromEnv, string toEnv, CancellationToken ct = default);

    /// <summary>Copy a subset of rows (by ratio/filter) from one env to another — e.g. a slice of prod to dev.</summary>
    Task<StudioResult<DataSyncReport>> SubsetAsync(string appId, string fromEnv, string toEnv, DataSubsetOptions options, CancellationToken ct = default);

    /// <summary>Copy with PII columns masked/anonymized on the target (prod → non-prod safe copy).</summary>
    Task<StudioResult<DataSyncReport>> MaskedCopyAsync(string appId, string fromEnv, string toEnv, IReadOnlyCollection<MaskingRule> masking, CancellationToken ct = default);

    /// <summary>Get/refresh the masking rules registered for an app (persisted with the app).</summary>
    Task<StudioResult<IReadOnlyList<MaskingRule>>> GetMaskingRulesAsync(string appId, CancellationToken ct = default);

    /// <summary>Set the masking rules for an app (used by <see cref="MaskedCopyAsync"/> when no inline rules are supplied).</summary>
    Task<StudioResult<IReadOnlyList<MaskingRule>>> SetMaskingRulesAsync(string appId, IEnumerable<MaskingRule> rules, CancellationToken ct = default);
}

public sealed class DataSubsetOptions
{
    /// <summary>Row ratio per entity, 0.0–1.0. Default 0.1 (10%).</summary>
    public double RowRatio { get; set; } = 0.1;
    /// <summary>Restrict to these entity (table) names. Empty = all.</summary>
    public List<string> EntityNames { get; set; } = new();
    /// <summary>Optional WHERE fragment appended per entity (caller is responsible for validity).</summary>
    public string? FilterClause { get; set; }
}

/// <summary>How a single column should be masked when copying prod → non-prod.</summary>
public sealed record MaskingRule(
    string EntityName,
    string ColumnName,
    MaskingStrategy Strategy,
    string? ConstantValue = null);

public enum MaskingStrategy
{
    /// <summary>Replace with a fixed value (<see cref="MaskingRule.ConstantValue"/>).</summary>
    Constant = 0,
    /// <summary>Replace with NULL.</summary>
    Nullify = 1,
    /// <summary>One-way hash (deterministic, same input → same hash).</summary>
    Hash = 2,
    /// <summary>Randomized fake value (e.g. fake email/name) — best effort by type.</summary>
    Fake = 3,
    /// <summary>Keep only the domain of an email / last 4 of a number.</summary>
    Partial = 4
}

public sealed class DataSyncReport
{
    public required string AppId { get; set; }
    public required string FromEnv { get; set; }
    public required string ToEnv { get; set; }
    public bool Succeeded { get; set; }
    public int EntitiesCopied { get; set; }
    public long RowsCopied { get; set; }
    public int ColumnsMasked { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
}
