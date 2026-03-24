using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor.Mapping
{
    public enum MappingApprovalState
    {
        Draft,
        Review,
        Approved,
        Deprecated
    }

    public sealed class MappingVersionSnapshotField
    {
        public string ToFieldName { get; set; } = string.Empty;
        public string ToFieldType { get; set; } = string.Empty;
        public string FromFieldName { get; set; } = string.Empty;
        public string FromFieldType { get; set; } = string.Empty;
        public string Rules { get; set; } = string.Empty;
    }

    public sealed class MappingVersionRecord
    {
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = "system";
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string ChangeReason { get; set; } = "mapping update";
        public MappingApprovalState ApprovalState { get; set; } = MappingApprovalState.Draft;
        public string MappingSignature { get; set; } = string.Empty;
        public string DiffSummary { get; set; } = string.Empty;
        public List<MappingVersionSnapshotField> SnapshotFields { get; set; } = new List<MappingVersionSnapshotField>();
    }

    public sealed class MappingAuditEntry
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string EventType { get; set; } = string.Empty;
        public string Actor { get; set; } = "system";
        public string Version { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    public sealed class MappingGovernanceRecord
    {
        public string EntityName { get; set; } = string.Empty;
        public string DataSource { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = "1.0.0";
        public MappingApprovalState CurrentApprovalState { get; set; } = MappingApprovalState.Draft;
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
        public List<MappingVersionRecord> Versions { get; set; } = new List<MappingVersionRecord>();
        public List<MappingAuditEntry> AuditTrail { get; set; } = new List<MappingAuditEntry>();
    }

    public static partial class MappingManager
    {
        private static readonly JsonSerializerOptions GovernanceJsonOptions = new()
        {
            WriteIndented = true
        };

        private static readonly AsyncLocal<MappingGovernanceContext> GovernanceContext = new();

        public static IDisposable BeginGovernanceScope(
            string author,
            string changeReason,
            MappingApprovalState targetState = MappingApprovalState.Draft)
        {
            var previous = GovernanceContext.Value;
            GovernanceContext.Value = new MappingGovernanceContext
            {
                Author = string.IsNullOrWhiteSpace(author) ? "system" : author.Trim(),
                ChangeReason = string.IsNullOrWhiteSpace(changeReason) ? "mapping update" : changeReason.Trim(),
                TargetState = targetState
            };

            return new GovernanceScope(() => GovernanceContext.Value = previous);
        }

        public static MappingGovernanceRecord GetMappingGovernance(
            IDMEEditor editor,
            string entityName,
            string dataSource)
        {
            return LoadGovernanceRecord(editor, entityName, dataSource)
                   ?? CreateGovernanceRecord(entityName, dataSource);
        }

        public static IReadOnlyList<MappingVersionRecord> GetMappingVersionHistory(
            IDMEEditor editor,
            string entityName,
            string dataSource)
        {
            var governance = GetMappingGovernance(editor, entityName, dataSource);
            return governance.Versions
                .OrderBy(item => item.TimestampUtc)
                .ToList();
        }

        public static bool UpdateMappingApprovalState(
            IDMEEditor editor,
            string entityName,
            string dataSource,
            MappingApprovalState state,
            string actor,
            string reason)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (string.IsNullOrWhiteSpace(entityName)) throw new ArgumentException("Entity name is required.", nameof(entityName));
            if (string.IsNullOrWhiteSpace(dataSource)) throw new ArgumentException("Data source is required.", nameof(dataSource));

            var governance = LoadGovernanceRecord(editor, entityName, dataSource) ?? CreateGovernanceRecord(entityName, dataSource);
            governance.CurrentApprovalState = state;
            governance.LastUpdatedUtc = DateTime.UtcNow;

            var currentVersion = governance.Versions
                .FirstOrDefault(item => string.Equals(item.Version, governance.CurrentVersion, StringComparison.OrdinalIgnoreCase));
            if (currentVersion != null)
                currentVersion.ApprovalState = state;

            AddGovernanceAuditEntry(
                governance,
                "approval-state-updated",
                actor,
                governance.CurrentVersion,
                reason,
                $"State changed to {state}");

            SaveGovernanceRecord(editor, entityName, dataSource, governance);
            return true;
        }

        public static string BuildMappingVersionDiffText(
            IDMEEditor editor,
            string entityName,
            string dataSource,
            string baselineVersion,
            string currentVersion)
        {
            var governance = GetMappingGovernance(editor, entityName, dataSource);
            var baseline = governance.Versions.FirstOrDefault(item => string.Equals(item.Version, baselineVersion, StringComparison.OrdinalIgnoreCase));
            var current = governance.Versions.FirstOrDefault(item => string.Equals(item.Version, currentVersion, StringComparison.OrdinalIgnoreCase));

            if (baseline == null || current == null)
                return $"No version diff available between '{baselineVersion}' and '{currentVersion}'.";

            var baselineIndex = baseline.SnapshotFields
                .ToDictionary(item => item.ToFieldName ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            var currentIndex = current.SnapshotFields
                .ToDictionary(item => item.ToFieldName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            var added = currentIndex.Keys.Where(key => !baselineIndex.ContainsKey(key)).OrderBy(item => item).ToList();
            var removed = baselineIndex.Keys.Where(key => !currentIndex.ContainsKey(key)).OrderBy(item => item).ToList();
            var changed = currentIndex.Keys
                .Where(baselineIndex.ContainsKey)
                .Where(key =>
                {
                    var left = baselineIndex[key];
                    var right = currentIndex[key];
                    return !string.Equals(left.FromFieldName, right.FromFieldName, StringComparison.OrdinalIgnoreCase) ||
                           !string.Equals(left.FromFieldType, right.FromFieldType, StringComparison.OrdinalIgnoreCase) ||
                           !string.Equals(left.ToFieldType, right.ToFieldType, StringComparison.OrdinalIgnoreCase) ||
                           !string.Equals(left.Rules, right.Rules, StringComparison.Ordinal);
                })
                .OrderBy(item => item)
                .ToList();

            return $"Diff {baselineVersion} -> {currentVersion}: added={added.Count}, removed={removed.Count}, changed={changed.Count}";
        }

        private static void ApplyGovernanceOnSave(
            IDMEEditor editor,
            string entityName,
            string dataSource,
            EntityDataMap previousMapping,
            EntityDataMap currentMapping)
        {
            if (editor == null || currentMapping == null)
                return;

            var governance = LoadGovernanceRecord(editor, entityName, dataSource) ?? CreateGovernanceRecord(entityName, dataSource);
            var signature = ComputeMappingSignatureFromEntityMap(currentMapping);
            var latestVersion = governance.Versions.OrderByDescending(item => item.TimestampUtc).FirstOrDefault();

            var actor = GovernanceContext.Value?.Author;
            if (string.IsNullOrWhiteSpace(actor))
                actor = Environment.UserName;
            if (string.IsNullOrWhiteSpace(actor))
                actor = "system";

            var reason = GovernanceContext.Value?.ChangeReason;
            if (string.IsNullOrWhiteSpace(reason))
                reason = "mapping update";

            if (latestVersion != null && string.Equals(latestVersion.MappingSignature, signature, StringComparison.Ordinal))
            {
                AddGovernanceAuditEntry(
                    governance,
                    "mapping-saved-no-change",
                    actor,
                    governance.CurrentVersion,
                    reason,
                    "Mapping persisted without structural changes.");
                SaveGovernanceRecord(editor, entityName, dataSource, governance);
                return;
            }

            var nextVersion = ComputeNextVersion(governance.CurrentVersion);
            var diffSummary = BuildDiffSummary(previousMapping, currentMapping);
            var targetState = GovernanceContext.Value?.TargetState ?? MappingApprovalState.Draft;

            var versionRecord = new MappingVersionRecord
            {
                Version = nextVersion,
                Author = actor,
                TimestampUtc = DateTime.UtcNow,
                ChangeReason = reason,
                ApprovalState = targetState,
                MappingSignature = signature,
                DiffSummary = diffSummary,
                SnapshotFields = FlattenFields(currentMapping)
                    .Select(field => new MappingVersionSnapshotField
                    {
                        ToFieldName = field.ToFieldName ?? string.Empty,
                        ToFieldType = field.ToFieldType ?? string.Empty,
                        FromFieldName = field.FromFieldName ?? string.Empty,
                        FromFieldType = field.FromFieldType ?? string.Empty,
                        Rules = field.Rules ?? string.Empty
                    })
                    .ToList()
            };

            governance.Versions.Add(versionRecord);
            governance.CurrentVersion = versionRecord.Version;
            governance.CurrentApprovalState = versionRecord.ApprovalState;
            governance.LastUpdatedUtc = DateTime.UtcNow;

            AddGovernanceAuditEntry(
                governance,
                "mapping-version-created",
                actor,
                versionRecord.Version,
                reason,
                versionRecord.DiffSummary);

            SaveGovernanceRecord(editor, entityName, dataSource, governance);
        }

        private static EntityDataMap TryLoadMappingSnapshot(IDMEEditor editor, string entityName, string dataSource)
        {
            try
            {
                if (editor?.ConfigEditor == null)
                    return null;

                var loaded = editor.ConfigEditor.LoadMappingValues(entityName, dataSource);
                if (loaded == null)
                    return null;

                var snapshot = new EntityDataMap
                {
                    GuidID = loaded.GuidID,
                    id = loaded.id,
                    MappingName = loaded.MappingName,
                    Description = loaded.Description,
                    EntityName = loaded.EntityName,
                    EntityDataSource = loaded.EntityDataSource,
                    EntityFields = loaded.EntityFields?.ToList() ?? new List<TheTechIdea.Beep.DataBase.EntityField>(),
                    MappedEntities = loaded.MappedEntities?
                        .Select(detail => new EntityDataMap_DTL
                        {
                            GuidID = detail.GuidID,
                            EntityDataSource = detail.EntityDataSource,
                            EntityName = detail.EntityName,
                            Filter = detail.Filter?.ToList() ?? new List<TheTechIdea.Beep.Report.AppFilter>(),
                            EntityFields = detail.EntityFields?.ToList() ?? new List<TheTechIdea.Beep.DataBase.EntityField>(),
                            SelectedDestFields = detail.SelectedDestFields?.ToList() ?? new List<TheTechIdea.Beep.DataBase.EntityField>(),
                            FieldMapping = detail.FieldMapping?
                                .Select(field => new TheTechIdea.Beep.Workflow.Mapping_rep_fields
                                {
                                    ToFieldName = field.ToFieldName,
                                    ToFieldType = field.ToFieldType,
                                    FromFieldName = field.FromFieldName,
                                    FromFieldType = field.FromFieldType,
                                    Rules = field.Rules
                                })
                                .ToList() ?? new List<TheTechIdea.Beep.Workflow.Mapping_rep_fields>()
                        })
                        .ToList() ?? new List<EntityDataMap_DTL>()
                };

                return snapshot;
            }
            catch
            {
                return null;
            }
        }

        private static string BuildDiffSummary(EntityDataMap previousMapping, EntityDataMap currentMapping)
        {
            if (currentMapping == null)
                return "No mapping snapshot available.";

            if (previousMapping == null)
                return $"Initial mapping version. fields={FlattenFields(currentMapping).Count()}";

            var diff = DiffMapping(previousMapping, currentMapping);
            return $"added={diff.Added.Count}, removed={diff.Removed.Count}, changed={diff.Changed.Count}";
        }

        private static string ComputeMappingSignatureFromEntityMap(EntityDataMap map)
        {
            var fields = FlattenFields(map).ToList();
            return string.Join("|", fields.Select(item =>
                $"{item?.FromFieldName}>{item?.ToFieldName}:{item?.FromFieldType}>{item?.ToFieldType}:{item?.Rules}"));
        }

        private static string ComputeNextVersion(string currentVersion)
        {
            if (Version.TryParse(currentVersion, out var parsed))
                return $"{parsed.Major}.{parsed.Minor}.{parsed.Build + 1}";

            return "1.0.0";
        }

        private static void AddGovernanceAuditEntry(
            MappingGovernanceRecord governance,
            string eventType,
            string actor,
            string version,
            string reason,
            string details)
        {
            governance.AuditTrail.Add(new MappingAuditEntry
            {
                TimestampUtc = DateTime.UtcNow,
                EventType = eventType ?? string.Empty,
                Actor = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim(),
                Version = version ?? string.Empty,
                Reason = reason ?? string.Empty,
                Details = details ?? string.Empty
            });
        }

        private static MappingGovernanceRecord LoadGovernanceRecord(
            IDMEEditor editor,
            string entityName,
            string dataSource)
        {
            try
            {
                var path = GetGovernanceFilePath(editor, entityName, dataSource);
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return null;

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<MappingGovernanceRecord>(json, GovernanceJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static void SaveGovernanceRecord(
            IDMEEditor editor,
            string entityName,
            string dataSource,
            MappingGovernanceRecord governance)
        {
            var path = GetGovernanceFilePath(editor, entityName, dataSource);
            if (string.IsNullOrWhiteSpace(path))
                return;

            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(folder))
                Directory.CreateDirectory(folder);

            var json = JsonSerializer.Serialize(governance, GovernanceJsonOptions);
            File.WriteAllText(path, json);
        }

        private static string GetGovernanceFilePath(IDMEEditor editor, string entityName, string dataSource)
        {
            var mappingRoot = editor?.ConfigEditor?.Config?.MappingPath;
            if (string.IsNullOrWhiteSpace(mappingRoot) ||
                string.IsNullOrWhiteSpace(entityName) ||
                string.IsNullOrWhiteSpace(dataSource))
                return string.Empty;

            return Path.Combine(mappingRoot, dataSource, $"{entityName}_Mapping.governance.json");
        }

        private static MappingGovernanceRecord CreateGovernanceRecord(string entityName, string dataSource)
        {
            return new MappingGovernanceRecord
            {
                EntityName = entityName ?? string.Empty,
                DataSource = dataSource ?? string.Empty,
                CurrentVersion = "1.0.0",
                CurrentApprovalState = MappingApprovalState.Draft,
                LastUpdatedUtc = DateTime.UtcNow
            };
        }

        private sealed class MappingGovernanceContext
        {
            public string Author { get; set; } = string.Empty;
            public string ChangeReason { get; set; } = string.Empty;
            public MappingApprovalState TargetState { get; set; } = MappingApprovalState.Draft;
        }

        private sealed class GovernanceScope : IDisposable
        {
            private readonly Action _onDispose;
            private bool _disposed;

            public GovernanceScope(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _onDispose?.Invoke();
            }
        }
    }
}
