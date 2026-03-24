using System.Collections.Generic;
using TheTechIdea.Beep.Core;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        private MigrationProviderCapabilityProfile BuildProviderCapabilityProfile(DataSourceType type, DatasourceCategory category)
        {
            var capabilities = GetCapabilities(type, category);
            var helper = ResolveHelper(type);

            var alterProbe = ProbeAlterColumnSupport(helper);
            var renameEntityProbe = ProbeRenameEntitySupport(helper);
            var renameColumnProbe = ProbeRenameColumnSupport(helper);
            var transactionProbe = ProbeTransactionalDdlSupport(helper);

            var profile = new MigrationProviderCapabilityProfile
            {
                DataSourceType = type,
                DataSourceCategory = category,
                SupportsAlterColumn = alterProbe.IsSupported,
                SupportsRenameEntity = renameEntityProbe.IsSupported,
                SupportsRenameColumn = renameColumnProbe.IsSupported,
                SupportsTransactionalDdl = transactionProbe.IsSupported,
                RequiresOfflineWindowForSchemaChanges = category == DatasourceCategory.RDBMS &&
                                                        (type == DataSourceType.Oracle || type == DataSourceType.SqlLite || !transactionProbe.IsSupported),
                PortabilityWarning = BuildPortabilityWarning(type, category),
                Constraints = BuildCapabilityConstraints(type, category, capabilities)
            };

            if (!alterProbe.IsSupported && !string.IsNullOrWhiteSpace(alterProbe.Reason))
                profile.Constraints.Add($"ALTER support probe: {alterProbe.Reason}");

            if (!renameEntityProbe.IsSupported && !string.IsNullOrWhiteSpace(renameEntityProbe.Reason))
                profile.Constraints.Add($"RENAME TABLE support probe: {renameEntityProbe.Reason}");

            if (!renameColumnProbe.IsSupported && !string.IsNullOrWhiteSpace(renameColumnProbe.Reason))
                profile.Constraints.Add($"RENAME COLUMN support probe: {renameColumnProbe.Reason}");

            if (!transactionProbe.IsSupported && !string.IsNullOrWhiteSpace(transactionProbe.Reason))
                profile.Constraints.Add($"Transactional DDL probe: {transactionProbe.Reason}");

            return profile;
        }

        private static string BuildPortabilityWarning(DataSourceType type, DatasourceCategory category)
        {
            if (category == DatasourceCategory.FILE)
                return "File-based providers often emulate schema changes; migration behavior may differ across CSV/JSON/XML engines.";

            if (category == DatasourceCategory.NOSQL)
                return "NoSQL schema evolution semantics vary by provider and may not map 1:1 with relational migration assumptions.";

            if (type == DataSourceType.Oracle)
                return "Oracle portability: identifier length and staged DDL rollout constraints can differ from SQL Server/PostgreSQL baselines.";

            if (type == DataSourceType.SqlLite || type == DataSourceType.DuckDB)
                return "Embedded engines may require table-rebuild fallback for some alter/rename operations.";

            return "Re-run migration planning per provider; do not assume helper output is fully portable across datasource types.";
        }

        private static List<string> BuildCapabilityConstraints(DataSourceType type, DatasourceCategory category, DataSourceCapabilities capabilities)
        {
            var constraints = new List<string>();

            if (!capabilities.SupportsSchemaEvolution)
                constraints.Add("Schema evolution support is limited; prefer create-new-resource or additive-only patterns.");

            if (category != DatasourceCategory.RDBMS)
            {
                constraints.Add("Non-RDBMS provider: ALTER/RENAME DDL operations may be unavailable or emulated.");
            }

            if (type == DataSourceType.Oracle)
            {
                constraints.Add("Oracle estates often require controlled maintenance windows for larger schema changes.");
            }

            if (type == DataSourceType.SqlLite || type == DataSourceType.DuckDB)
            {
                constraints.Add("Column alter/rename may require copy-table fallback depending on engine/runtime.");
            }

            if (!capabilities.SupportsTransactions)
            {
                constraints.Add("Transactional safety is limited; rollback/compensation must be explicit.");
            }

            if (constraints.Count == 0)
            {
                constraints.Add("Provider supports standard additive migration patterns with helper validation.");
            }

            return constraints;
        }

        private IDataSourceHelper ResolveHelper(DataSourceType type)
        {
            if (_editor == null)
                return null;

            // Always route through the Core universal helper entrypoint so
            // migration planning uses the same helper behavior as runtime.
            return new GeneralDataSourceHelper(type, _editor);
        }

        private static (bool IsSupported, string Reason) ProbeAlterColumnSupport(IDataSourceHelper helper)
        {
            if (helper == null)
                return (false, "No helper available.");

            var probeColumn = new EntityField { FieldName = "__probe_col__", Fieldtype = typeof(string).FullName };
            var result = helper.GenerateAlterColumnSql("__probe_table__", "__probe_col__", probeColumn);
            return NormalizeProbeResult(result.Success, result.ErrorMessage);
        }

        private static (bool IsSupported, string Reason) ProbeRenameEntitySupport(IDataSourceHelper helper)
        {
            if (helper == null)
                return (false, "No helper available.");

            var result = helper.GenerateRenameTableSql("__probe_old__", "__probe_new__");
            return NormalizeProbeResult(result.Success, result.ErrorMessage);
        }

        private static (bool IsSupported, string Reason) ProbeRenameColumnSupport(IDataSourceHelper helper)
        {
            if (helper == null)
                return (false, "No helper available.");

            var result = helper.GenerateRenameColumnSql("__probe_table__", "__probe_old__", "__probe_new__");
            return NormalizeProbeResult(result.Success, result.ErrorMessage);
        }

        private static (bool IsSupported, string Reason) ProbeTransactionalDdlSupport(IDataSourceHelper helper)
        {
            if (helper == null)
                return (false, "No helper available.");

            var begin = helper.GenerateBeginTransactionSql();
            var commit = helper.GenerateCommitSql();
            if (begin.Success && commit.Success)
                return (true, string.Empty);

            var reason = $"{begin.ErrorMessage} {commit.ErrorMessage}".Trim();
            if (string.IsNullOrWhiteSpace(reason))
                reason = "Transaction SQL generation is not supported.";
            return (false, reason);
        }

        private static (bool IsSupported, string Reason) NormalizeProbeResult(bool success, string errorMessage)
        {
            if (success)
                return (true, string.Empty);

            if (string.IsNullOrWhiteSpace(errorMessage))
                return (false, "Operation probe did not report support.");

            return (false, errorMessage);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Phase 5 – Recommendation Profiles
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a versioned recommendation profile for the resolved datasource type/category.
        /// Uses the current <see cref="MigrateDataSource"/> when parameters are null.
        /// </summary>
        public RecommendationProfile GetRecommendationProfile(
            DataSourceType? dataSourceType = null,
            DatasourceCategory? dataSourceCategory = null)
        {
            var type     = dataSourceType     ?? MigrateDataSource?.DatasourceType     ?? DataSourceType.Unknown;
            var category = dataSourceCategory ?? MigrateDataSource?.Category           ?? DatasourceCategory.NONE;
            return BuildRecommendationProfile(type, category);
        }

        private static RecommendationProfile BuildRecommendationProfile(DataSourceType type, DatasourceCategory category)
        {
            var capabilities = DataSourceCapabilityMatrix.GetCapabilities(type, category) ?? new DataSourceCapabilities();

            var profile = new RecommendationProfile
            {
                ProfileId           = $"PROFILE-{category}-{type}",
                ProfileVersion      = "1.0",
                DataSourceType      = type,
                DataSourceCategory  = category
            };

            // Core additive-migration recommendation — applies to all providers
            profile.Recommendations.Add(new RecommendationEntry
            {
                Id                    = "REC-CORE-001",
                Severity              = RecommendationSeverity.Info,
                Text                  = "Prefer additive migrations (create-if-missing, add-columns). Avoid destructive alters in production without an explicit rollback plan.",
                Rationale             = "Additive migrations are safe on all providers; destructive changes risk data loss and are often emulated or unsupported.",
                CapabilityDependencies = new List<string> { "SupportsSchemaEvolution" },
                CapabilitySource      = "MigrationManager"
            });

            // Helper registration recommendation
            profile.Recommendations.Add(new RecommendationEntry
            {
                Id                    = "REC-HELPER-001",
                Severity              = RecommendationSeverity.Warning,
                Text                  = "Register a specialized IDataSourceHelper for this provider to enable helper-driven DDL validation.",
                Rationale             = "Without a helper, DDL is emulated or skipped, which can lead to silent no-ops or incomplete migrations.",
                CapabilityDependencies = new List<string> { "HelperAvailable" },
                CapabilitySource      = "MigrationManager"
            });

            // Schema evolution limited
            if (!capabilities.SupportsSchemaEvolution)
            {
                profile.Recommendations.Add(new RecommendationEntry
                {
                    Id                    = "REC-SCHEMA-001",
                    Severity              = RecommendationSeverity.Warning,
                    Text                  = "This provider has limited schema-evolution capability. Prefer create-new-resource or additive-only migration patterns.",
                    Rationale             = "Providers without schema evolution support may not allow ALTER or RENAME operations reliably.",
                    CapabilityDependencies = new List<string> { "SupportsSchemaEvolution" },
                    CapabilitySource      = "UniversalDataSourceHelpers"
                });
            }

            // Transaction safety
            if (!capabilities.SupportsTransactions)
            {
                profile.Recommendations.Add(new RecommendationEntry
                {
                    Id                    = "REC-TXN-001",
                    Severity              = RecommendationSeverity.Warning,
                    Text                  = "This provider does not support transactional DDL. Plan explicit compensation or rollback strategies.",
                    Rationale             = "Without transactional DDL, partial failures leave the schema in an intermediate state.",
                    CapabilityDependencies = new List<string> { "SupportsTransactions" },
                    CapabilitySource      = "UniversalDataSourceHelpers"
                });
            }

            // Discovery best practice
            profile.Recommendations.Add(new RecommendationEntry
            {
                Id                    = "REC-DISCOVERY-001",
                Severity              = RecommendationSeverity.Info,
                Text                  = "Prefer explicit-type migration over assembly discovery for application-owned schemas to avoid scanning wide assembly graphs.",
                Rationale             = "Explicit types are deterministic; discovery may find unintended entity classes from framework or plugin assemblies.",
                CapabilityDependencies = new List<string>(),
                CapabilitySource      = "MigrationManager"
            });

            // Category-specific recommendations
            switch (category)
            {
                case DatasourceCategory.RDBMS:
                    profile.Recommendations.Add(new RecommendationEntry
                    {
                        Id                    = "REC-RDBMS-001",
                        Severity              = RecommendationSeverity.Info,
                        Text                  = "Review lock impact, index rebuild implications, and nullability transitions before applying column-level changes on large tables.",
                        Rationale             = "RDBMS DDL operations can hold locks that degrade concurrent workload performance.",
                        CapabilityDependencies = new List<string> { "SupportsSchemaEvolution" },
                        CapabilitySource      = "RDBMSHelpers"
                    });
                    break;

                case DatasourceCategory.FILE:
                    profile.Recommendations.Add(new RecommendationEntry
                    {
                        Id                    = "REC-FILE-001",
                        Severity              = RecommendationSeverity.Warning,
                        Text                  = "File-based providers emulate schema changes via file mutation. Validate backups exist before structural changes.",
                        Rationale             = "File mutation is non-atomic; a crash mid-write can corrupt the data file.",
                        CapabilityDependencies = new List<string>(),
                        CapabilitySource      = "MigrationManager"
                    });
                    break;

                case DatasourceCategory.NOSQL:
                    profile.Recommendations.Add(new RecommendationEntry
                    {
                        Id                    = "REC-NOSQL-001",
                        Severity              = RecommendationSeverity.Info,
                        Text                  = "NoSQL providers treat schema as flexible. Use migration to ensure index and collection consistency rather than enforcing strict column presence.",
                        Rationale             = "Schemaless providers may silently ignore column-level DDL; validate through the provider's own tooling.",
                        CapabilityDependencies = new List<string> { "IsSchemaEnforced" },
                        CapabilitySource      = "UniversalDataSourceHelpers"
                    });
                    break;
            }

            return profile;
        }
    }
}
