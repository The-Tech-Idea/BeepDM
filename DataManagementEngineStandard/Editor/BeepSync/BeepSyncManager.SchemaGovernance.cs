using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor
{
    public partial class BeepSyncManager
    {
        /// <summary>
        /// Promotes <paramref name="schema"/> to <paramref name="targetState"/>, optionally
        /// gated by the <c>sync.schema.promotion-gate</c> rule. Creates a versioned artifact
        /// and persists it.
        /// </summary>
        public async Task<SyncPreflightReport> PromoteSchemaAsync(
            DataSyncSchema schema,
            string targetState,
            string approver = null,
            string changeNotes = null,
            CancellationToken token = default)
        {
            var report = new SyncPreflightReport { PlanId = schema?.Id };

            if (schema == null)
            {
                report.AddError("SCHEMA-NULL", "Schema", "Schema cannot be null.");
                return report;
            }
            if (string.IsNullOrWhiteSpace(targetState))
            {
                report.AddError("TARGET-STATE-EMPTY", "Schema", "Target approval state cannot be empty.");
                return report;
            }

            // Promotion gate via Rule Engine
            var ctx = IntegrationContext;
            if (ctx?.RuleEngine != null && schema.RulePolicy?.Enabled == true
                && ctx.RuleEngine.HasRule("sync.schema.promotion-gate"))
            {
                try
                {
                    var gatePolicy = BuildRulePolicy(schema, 5000);
                    var (_, gateResult) = ctx.RuleEngine.SolveRule(
                        "sync.schema.promotion-gate",
                        new Dictionary<string, object>
                        {
                            ["schema"]                = schema,
                            ["targetState"]           = targetState,
                            ["mappingApprovalState"]  = schema.CurrentSchemaVersion?.ApprovalState ?? "Draft",
                            ["requiredApprovalState"] = schema.MappingPolicy?.RequiredApprovalState ?? "Draft"
                        },
                        gatePolicy);

                    bool gatePass = gateResult is bool b ? b : gateResult?.ToString() != "false";
                    if (!gatePass)
                    {
                        report.AddError("PROMOTION-GATE-REJECTED", "Schema",
                            $"Rule 'sync.schema.promotion-gate' blocked promotion to '{targetState}'.");
                        return report;
                    }
                    report.AddInfo("PROMOTION-GATE-PASSED", "Schema",
                        $"Promotion gate passed — target state '{targetState}'.");
                }
                catch (Exception ex)
                {
                    report.AddError("PROMOTION-GATE-EXCEPTION", "Schema",
                        $"Rule Engine threw during promotion gate: {ex.Message}");
                    return report;
                }
            }

            // Build and stamp the version artifact
            var savedBy     = approver ?? ctx?.RunInitiatedBy ?? Environment.UserName;
            var prevVersion = schema.CurrentSchemaVersion?.Version ?? 0;

            var newVersion = new SyncSchemaVersion
            {
                SchemaId           = schema.Id,
                Version            = prevVersion + 1,
                VersionGuid        = Guid.NewGuid().ToString(),
                SchemaHash         = ComputeSchemaHash(schema),
                SavedAt            = DateTime.UtcNow,
                SavedBy            = savedBy,
                ApprovalState      = targetState,
                RuleCatalogVersion = schema.RulePolicy?.CatalogVersion,
                ChangeNotes        = changeNotes
            };

            schema.CurrentSchemaVersion = newVersion;

            // Co-promote mapping state
            new FieldMappingHelper(_editor).PromoteMappingState(schema, targetState);

            // Persist
            await _persistenceHelper.SaveVersionedSchemaAsync(schema, newVersion);
            await _persistenceHelper.SaveSchemaAsync(schema);

            report.AddInfo("PROMOTION-SUCCESS", "Schema",
                $"Schema '{schema.Id}' promoted to '{targetState}' as version {newVersion.Version} by {savedBy}.");
            return report;
        }

        private static string ComputeSchemaHash(DataSyncSchema schema)
        {
            try
            {
                var fp = $"{schema.SourceDataSourceName}|{schema.DestinationDataSourceName}" +
                         $"|{schema.SourceEntityName}|{schema.DestinationEntityName}" +
                         $"|{schema.SyncDirection}|{schema.SyncType}";

                if (schema.MappedFields != null)
                    fp += "|" + string.Join(",",
                        schema.MappedFields
                              .Select(f => $"{f.SourceField}:{f.DestinationField}")
                              .OrderBy(x => x));

                var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(fp));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }
    }
}
