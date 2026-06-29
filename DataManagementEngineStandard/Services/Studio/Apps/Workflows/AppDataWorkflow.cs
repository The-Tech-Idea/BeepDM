using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Studio.Apps.Workflows;

namespace TheTechIdea.Beep.Studio.Apps;

/// <summary>
/// Data movement between an app's environments. Uses the engine's
/// <see cref="TheTechIdea.Beep.IDataSource"/> abstraction directly — reads
/// source entities, inserts rows into the target — so it works without
/// pre-configured sync schemas. Masking rules are read from the app's
/// persisted rule set; the row ratio for subsets is applied per entity.
/// </summary>
internal sealed class AppDataWorkflow : IAppDataWorkflow
{
    private readonly IDMEEditor _editor;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AppDataWorkflow(IDMEEditor editor) => _editor = editor;

    public async Task<StudioResult<DataSyncReport>> CopyAsync(string appId, string fromEnv, string toEnv, CancellationToken ct = default)
        => await RunTransferAsync(appId, fromEnv, toEnv, rowRatio: null, masking: null, label: "Full copy", ct);

    public async Task<StudioResult<DataSyncReport>> SubsetAsync(string appId, string fromEnv, string toEnv, DataSubsetOptions options, CancellationToken ct = default)
    {
        var ratio = options == null ? 1.0 : Math.Clamp(options.RowRatio, 0, 1);
        return await RunTransferAsync(appId, fromEnv, toEnv, rowRatio: ratio, masking: null, label: $"Subset ({ratio:0%})", ct);
    }

    public async Task<StudioResult<DataSyncReport>> MaskedCopyAsync(string appId, string fromEnv, string toEnv, IReadOnlyCollection<MaskingRule> masking, CancellationToken ct = default)
    {
        var rules = masking?.ToList();
        if (rules != null && rules.Count > 0)
            await SetMaskingRulesAsync(appId, rules, ct);
        return await RunTransferAsync(appId, fromEnv, toEnv, rowRatio: null, masking: rules, label: $"Masked copy ({rules?.Count ?? 0} rule(s))", ct);
    }

    public Task<StudioResult<IReadOnlyList<MaskingRule>>> GetMaskingRulesAsync(string appId, CancellationToken ct = default)
    {
        try { return Task.FromResult(StudioResult<IReadOnlyList<MaskingRule>>.Ok(ReadMasking(appId))); }
        catch (Exception ex) { return Task.FromResult(StudioResult<IReadOnlyList<MaskingRule>>.Fail(StudioErrorCode.HostNotSupported, ex.Message)); }
    }

    public async Task<StudioResult<IReadOnlyList<MaskingRule>>> SetMaskingRulesAsync(string appId, IEnumerable<MaskingRule> rules, CancellationToken ct = default)
    {
        try
        {
            var list = rules.ToList();
            var path = MaskingPath(appId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(list, Json), ct);
            return StudioResult<IReadOnlyList<MaskingRule>>.Ok(list);
        }
        catch (Exception ex) { return StudioResult<IReadOnlyList<MaskingRule>>.Fail(StudioErrorCode.HostNotSupported, ex.Message); }
    }

    // ── Core transfer logic ─────────────────────────────────────────────────

    private async Task<StudioResult<DataSyncReport>> RunTransferAsync(
        string appId, string fromEnv, string toEnv, double? rowRatio, IReadOnlyCollection<MaskingRule>? masking, string label, CancellationToken ct)
    {
        var app = _editor.AppRegistry?.GetApp(appId);
        if (app == null) return StudioResult<DataSyncReport>.Fail(StudioErrorCode.NotFound, $"App '{appId}' not found.");
        var srcEnv = app.GetEnvironment(fromEnv);
        var tgtEnv = app.GetEnvironment(toEnv);
        if (srcEnv == null || tgtEnv == null) return StudioResult<DataSyncReport>.Fail(StudioErrorCode.InvalidArgument, "Both environments must exist.");
        if (srcEnv.Datasources.Count == 0 || tgtEnv.Datasources.Count == 0)
            return StudioResult<DataSyncReport>.Fail(StudioErrorCode.InvalidArgument, "Both environments need at least one datasource.");

        var report = new DataSyncReport { AppId = appId, FromEnv = fromEnv, ToEnv = toEnv, ColumnsMasked = masking?.Sum(m => 1) ?? 0 };
        var rules = masking?.ToList();

        foreach (var srcDs in srcEnv.Datasources)
        {
            ct.ThrowIfCancellationRequested();
            var tgtDs = tgtEnv.Datasources.FirstOrDefault(d =>
                !string.IsNullOrWhiteSpace(d.ProjectName) && string.Equals(d.ProjectName, srcDs.ProjectName, StringComparison.OrdinalIgnoreCase))
                ?? tgtEnv.Datasources.FirstOrDefault();
            if (tgtDs == null) continue;

            try
            {
                var source = _editor.GetDataSource(srcDs.Name);
                var target = _editor.GetDataSource(tgtDs.Name);
                if (source == null || target == null) continue;

                if (source.ConnectionStatus != System.Data.ConnectionState.Open)
                    source.Openconnection();
                if (target.ConnectionStatus != System.Data.ConnectionState.Open)
                    target.Openconnection();

                foreach (var entity in source.Entities)
                {
                    ct.ThrowIfCancellationRequested();
                    var entityName = entity.EntityName;
                    try
                    {
                        var rows = source.GetEntity(entityName, null);
                        if (rows == null) continue;
                        var allRows = rows.ToList();
                        var toCopy = rowRatio.HasValue && rowRatio.Value < 1.0
                            ? allRows.Take((int)Math.Ceiling(allRows.Count * rowRatio.Value)).ToList()
                            : allRows;

                        foreach (var row in toCopy)
                        {
                            var rowToInsert = row;
                            if (rules != null && rules.Any(r => r.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase)))
                                rowToInsert = ApplyMasking(row, entityName, rules);
                            target.InsertEntity(entityName, rowToInsert);
                        }
                        report.EntitiesCopied++;
                        report.RowsCopied += toCopy.Count;
                    }
                    catch (Exception ex)
                    {
                        report.Message += $"[{entityName}] {ex.Message}; ";
                    }
                }
                source.Closeconnection();
                target.Closeconnection();
            }
            catch (Exception ex) { report.Message += $"[{srcDs.Name}→{tgtDs.Name}] {ex.Message}; "; }
        }

        report.Succeeded = report.EntitiesCopied > 0;
        if (string.IsNullOrWhiteSpace(report.Message)) report.Message = label;
        return StudioResult<DataSyncReport>.Ok(report);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static object ApplyMasking(object row, string entityName, List<MaskingRule> rules)
    {
        foreach (var rule in rules.Where(r => r.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var prop = row.GetType().GetProperty(rule.ColumnName);
                if (prop != null && prop.CanWrite)
                {
                    var current = prop.GetValue(row);
                    var masked = rule.Strategy switch
                    {
                        MaskingStrategy.Constant => rule.ConstantValue,
                        MaskingStrategy.Nullify => null,
                        MaskingStrategy.Hash => current?.GetHashCode().ToString(),
                        MaskingStrategy.Fake => FakeValue(current, rule.ColumnName),
                        MaskingStrategy.Partial => current is string s && s.Length > 4
                            ? s[..Math.Min(2, s.Length)] + "***" + s[Math.Max(s.Length - 2, 0)..]
                            : current,
                        _ => current
                    };
                    prop.SetValue(row, masked);
                }
            }
            catch { /* best-effort per column */ }
        }
        return row;
    }

    private static object? FakeValue(object? current, string columnName)
    {
        var lower = columnName.ToLowerInvariant();
        if (lower.Contains("email")) return "user@example.com";
        if (lower.Contains("name")) return "***";
        if (lower.Contains("phone") || lower.Contains("mobile")) return "000-000-0000";
        if (lower.Contains("address")) return "123 Main St";
        if (current is int) return 0;
        if (current is long) return 0L;
        if (current is decimal or double or float) return 0.0;
        if (current is bool) return false;
        if (current is DateTime) return DateTime.MinValue;
        return "***";
    }

    private string DataRoot() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeepDM", "Studio");

    private string MaskingPath(string appId) => Path.Combine(DataRoot(), "masking", $"{appId}.json");
    private List<MaskingRule> ReadMasking(string appId)
    {
        var path = MaskingPath(appId);
        return File.Exists(path) ? (JsonSerializer.Deserialize<List<MaskingRule>>(File.ReadAllText(path), Json) ?? new()) : new();
    }
}
