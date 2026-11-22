using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        // Version 3 hashes a compact projection of each operation's schema instead of the whole entity structure.
        // Version 2 plans are refused rather than re-hashed: their identity was computed from a different document, so
        // accepting them would mean "the plan changed after approval" for a plan nobody touched.
        private const int CurrentPlanHashVersion = 3;
        private static readonly JsonSerializerSettings SnapshotSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            DateParseHandling = DateParseHandling.None,
            MaxDepth = 256
        };

        private static JsonSerializer SnapshotSerializer() => JsonSerializer.Create(SnapshotSettings);
        private static T ReadSnapshot<T>(string json)
        {
            using var text = new StringReader(json);
            using var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None, MaxDepth = 256 };
            return SnapshotSerializer().Deserialize<T>(reader);
        }

        private static T CopySnapshot<T>(T value) => value == null ? default :
            JToken.FromObject(value, SnapshotSerializer()).ToObject<T>(SnapshotSerializer());

        private static MigrationPlanArtifact CopyExecutionPlan(MigrationPlanArtifact plan)
        {
            var json = JObject.FromObject(plan, SnapshotSerializer());
            // Do not persist a recursive plan -> checkpoint -> plan graph.
            json.Remove(nameof(MigrationPlanArtifact.ExecutionCheckpoint));
            return json.ToObject<MigrationPlanArtifact>(SnapshotSerializer());
        }

        internal static string SerializePlanSnapshot(MigrationPlanArtifact plan) =>
            JToken.FromObject(CopyExecutionPlan(plan), SnapshotSerializer()).ToString(Formatting.None);

        internal static string SerializeCheckpointSnapshot(MigrationExecutionCheckpoint checkpoint) =>
            JToken.FromObject(checkpoint, SnapshotSerializer()).ToString(Formatting.None);

        /// <summary>
        /// The checkpoint record's snapshot, without the approved plan it carries. The plan is already persisted (or
        /// held in memory by the caller) and is hundreds of megabytes for a large module; embedding it in every
        /// per-step checkpoint record made the history quadratic in plan size. Identity and step state remain.
        /// </summary>
        internal static string SerializeCheckpointSnapshotWithoutPlan(MigrationExecutionCheckpoint checkpoint)
        {
            var json = JObject.FromObject(checkpoint, SnapshotSerializer());
            json.Remove(nameof(MigrationExecutionCheckpoint.ApprovedPlan));
            return json.ToString(Formatting.None);
        }

        /// <summary>
        /// The compact snapshot written with each checkpoint record: identity, status and position, without the
        /// approved plan or the full step list.
        /// </summary>
        /// <remarks>
        /// Every execution step writes a checkpoint. Carrying all 2716 steps (and the plan) in each record made a
        /// record about a megabyte and its serialization about two seconds - the schema apply spent its time
        /// re-serializing the same growing state instead of creating tables. <c>LastCompletedStep</c> plus the plan
        /// is what resume needs; the failed/current steps are attached separately to the record.
        /// </remarks>
        internal static string SerializeCheckpointSnapshotCompact(MigrationExecutionCheckpoint checkpoint)
        {
            var json = JObject.FromObject(checkpoint, SnapshotSerializer());

            foreach (var property in json.Properties()
                         .Where(property => string.Equals(property.Name, nameof(MigrationExecutionCheckpoint.ApprovedPlan), StringComparison.OrdinalIgnoreCase)
                                            || string.Equals(property.Name, nameof(MigrationExecutionCheckpoint.Steps), StringComparison.OrdinalIgnoreCase))
                         .ToList())
            {
                property.Remove();
            }

            return json.ToString(Formatting.None);
        }

        /// <summary>Loads a captured plan from this datasource's history. Legacy summary-only records cannot be applied.</summary>
        public MigrationPlanArtifact LoadMigrationPlan(string planId)
        {
            if (string.IsNullOrWhiteSpace(planId)) throw new ArgumentException("Plan identity is required.", nameof(planId));
            var record = _editor.ConfigEditor.LoadMigrationHistory(MigrateDataSource.DatasourceName)?.Migrations?
                .Where(item => item.MigrationId == planId && !string.IsNullOrWhiteSpace(item.PlanArtifactJson))
                .OrderBy(item => item.AppliedOnUtc).LastOrDefault();
            if (record == null) throw new InvalidOperationException("No captured migration plan found. Rebuild and re-approve legacy summary-only plans.");
            var plan = ReadSnapshot<MigrationPlanArtifact>(record.PlanArtifactJson);
            if (plan == null || !ValidatePlanIntent(plan, out _))
                throw new InvalidOperationException("Persisted migration intent is changed, incompatible or targets a different datasource.");
            plan.PlanPersistenceStatus = PersistenceWriteStatus.Saved;
            plan.PlanPersistenceErrorCode = string.Empty;
            return plan;
        }

        private string CaptureTargetFingerprint()
        {
            var connection = MigrateDataSource?.Dataconnection?.ConnectionProp;
            return HashJson(JObject.FromObject(new
            {
                Name = MigrateDataSource?.DatasourceName,
                Type = MigrateDataSource?.DatasourceType,
                Category = MigrateDataSource?.Category,
                Guid = MigrateDataSource?.GuidID,
                ConnectionGuid = connection?.GuidID,
                connection?.Host, connection?.Port, connection?.Database,
                connection?.SchemaName, connection?.Url, connection?.FilePath,
                connection?.FileName, connection?.OracleSIDorService,
                connection?.ConnectionString, connection?.ParameterList
            }, SnapshotSerializer()));
        }

        private static JToken SelectProperties(object source, string names)
        {
            if (source == null) return JValue.CreateNull();
            var json = JObject.FromObject(source, SnapshotSerializer());
            return new JObject(names.Split(' ').Select(name => new JProperty(name,
                json[name]?.DeepClone() ?? JValue.CreateNull())));
        }

        private static JToken SchemaIntent(EntityStructure schema)
        {
            if (schema == null) return JValue.CreateNull();
            var json = (JObject)SelectProperties(schema,
                "EntityName DatasourceEntityName OriginalEntityName SchemaOrOwnerOrDatabase DatabaseType EntityType PrimaryKeyString CustomBuildQuery IsIdentity");
            const string fieldNames = "FieldName Originalfieldname Fieldtype Size Size1 Size2 NumericPrecision NumericScale FieldCategory ValueMin ValueMax IsRequired IsIndexed IsAutoIncrement AllowDBNull IsCheck IsUnique IsKey FieldIndex IsIdentity EntityName OrdinalPosition IsReadOnly IsRowVersion IsLong DefaultValue Expression BaseTableName BaseColumnName MaxLength IsFixedLength ColumnName ColumnTypeName DatabaseGeneratedOptionName IsNotMapped IsUnicode";
            json["Fields"] = new JArray((schema.Fields ?? new List<EntityField>())
                .OrderBy(field => field?.OrdinalPosition ?? 0).ThenBy(field => field?.FieldIndex ?? 0)
                .ThenBy(field => field?.FieldName, StringComparer.Ordinal)
                .Select(field => SelectProperties(field, fieldNames)));
            // Composite key and index column order is semantic, unlike unrelated descriptors.
            json["PrimaryKeys"] = new JArray((schema.PrimaryKeys ?? new List<EntityField>())
                .Select(field => SelectProperties(field, fieldNames)));
            json["Relations"] = new JArray((schema.Relations ?? new List<RelationShipKeys>())
                .Select(relation => SelectProperties(relation,
                    "RalationName RelatedEntityID RelatedEntityColumnID RelatedColumnSequenceID EntityColumnID EntityColumnSequenceID OnDeleteBehavior OnUpdateBehavior"))
                .OrderBy(item => CanonicalJson(item), StringComparer.Ordinal));
            json["Indexes"] = new JArray((schema.Indexes ?? new List<EntityIndex>())
                .Select(index => SelectProperties(index, "Name EntityName Columns IsUnique IsClustered Options"))
                .OrderBy(item => CanonicalJson(item), StringComparer.Ordinal));
            return json;
        }

        private static JToken Canonicalize(JToken token)
        {
            if (token is JObject obj)
                return new JObject(obj.Properties().OrderBy(property => property.Name, StringComparer.Ordinal)
                    .Select(property => new JProperty(property.Name, Canonicalize(property.Value))));
            if (token is JArray array) return new JArray(array.Select(Canonicalize));
            return token.DeepClone();
        }

        private static string CanonicalJson(JToken token) => Canonicalize(token).ToString(Formatting.None);
        private static bool JTokenEquals(object left, object right) =>
            string.Equals(CanonicalJson(JToken.FromObject(left, SnapshotSerializer())),
                CanonicalJson(JToken.FromObject(right, SnapshotSerializer())), StringComparison.Ordinal);

        private static JToken GovernanceIntent(MigrationPolicyOptions options) => SelectProperties(options,
            "EnvironmentTier RequireApprovalForHighRisk RequireApprovalForCriticalRisk BlockDestructiveInProtectedEnvironments AllowDestructiveOverrideInProtectedEnvironments");

        private static bool GovernanceMatches(MigrationPolicyOptions left, MigrationPolicyOptions right) =>
            string.Equals(CanonicalJson(GovernanceIntent(left)), CanonicalJson(GovernanceIntent(right)), StringComparison.Ordinal);

        private static bool ValidateCheckpointIntent(MigrationExecutionCheckpoint checkpoint, MigrationPlanArtifact plan)
        {
            if (checkpoint.ApprovedPlan == null || checkpoint.Steps == null ||
                !string.Equals(checkpoint.PlanHash, plan.PlanHash, StringComparison.Ordinal) ||
                !string.Equals(ComputePlanHash(checkpoint.ApprovedPlan), plan.PlanHash, StringComparison.Ordinal)) return false;
            var expected = BuildExecutionSteps(plan.Operations);
            if (expected.Count != checkpoint.Steps.Count) return false;
            const string names = "Sequence StepId DependsOn EntityName EntityTypeName OperationKind MissingColumns TargetName";
            return expected.Zip(checkpoint.Steps, (left, right) => right != null &&
                string.Equals(CanonicalJson(SelectProperties(left, names)),
                    CanonicalJson(SelectProperties(right, names)), StringComparison.Ordinal)).All(matches => matches);
        }
        private static string HashJson(JToken token)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(CanonicalJson(token))));
        }

        /// <summary>
        /// The part of an entity's shape that belongs to the plan's identity: its name, its key and each field's
        /// name/type/size/nullability/flags. Built by reading properties directly - no serialiser, no whole-object
        /// graph - because hashing a plan used to serialise every operation's full desired and expected structure,
        /// which cost minutes and gigabytes on a large module.
        /// </summary>
        private static JToken SchemaSignature(EntityStructure schema)
        {
            if (schema == null) return JValue.CreateNull();

            var fields = new JArray((schema.Fields ?? new List<EntityField>())
                .Where(field => field != null)
                .OrderBy(field => field.OrdinalPosition)
                .ThenBy(field => field.FieldIndex)
                .ThenBy(field => field.FieldName, StringComparer.Ordinal)
                .Select(field => new JObject(
                    new JProperty("n", field.FieldName ?? string.Empty),
                    new JProperty("t", field.Fieldtype ?? string.Empty),
                    new JProperty("s", field.Size1),
                    new JProperty("k", field.IsKey),
                    new JProperty("nn", !field.AllowDBNull),
                    new JProperty("r", field.IsRequired),
                    new JProperty("i", field.IsAutoIncrement))));

            // Relations decide foreign-key operations; their identity is a handful of names, not object graphs.
            var relations = new JArray((schema.Relations ?? new List<RelationShipKeys>())
                .Where(relation => relation != null)
                .Select(relation =>
                    $"{relation.RalationName}|{relation.RelatedEntityID}|{relation.RelatedEntityColumnID}|" +
                    $"{relation.RelatedColumnSequenceID}|{relation.EntityColumnID}|{relation.EntityColumnSequenceID}|" +
                    $"{relation.OnDeleteBehavior}|{relation.OnUpdateBehavior}")
                .OrderBy(text => text, StringComparer.Ordinal)
                .Select(text => new JValue(text)));

            return new JObject(
                new JProperty("entity", schema.EntityName ?? string.Empty),
                new JProperty("pk", schema.PrimaryKeyString ?? string.Empty),
                new JProperty("fields", fields),
                new JProperty("relations", relations));
        }

        private static JToken OperationIntent(MigrationPlanOperation operation)
        {
            var json = (JObject)SelectProperties(operation,
                "EntityName EntityTypeName Kind RiskLevel IsDestructive IsTypeNarrowing HasNullabilityTightening TargetName MissingColumns");
            var snapshot = operation.SchemaSnapshot;
            json["DesiredSchema"] = SchemaSignature(snapshot?.DesiredSchema);
            json["ExpectedEntityExists"] = snapshot?.ExpectedEntityExists;
            json["ExpectedSchema"] = SchemaSignature(snapshot?.ExpectedSchema);
            return json;
        }

        private static string ComputePlanHash(MigrationPlanArtifact plan)
        {
            var json = (JObject)SelectProperties(plan,
                "PlanHashVersion DataSourceName DataSourceType DataSourceCategory TargetFingerprint ExecutionPolicy");
            // Execution order is intentional and participates in the approved identity.
            json["Operations"] = new JArray(plan.Operations.Select(OperationIntent));
            json["ExecutionPolicy"] = SelectProperties(plan.ExecutionPolicy,
                "MaxTransientRetries RetryDelayMilliseconds TransientErrorMarkers HardFailMarkers RequireOperatorInterventionOnHardFail AbortOnStepFailure");
            json["GovernancePolicy"] = GovernanceIntent(plan.GovernancePolicy);
            json["PerformancePolicy"] = SelectProperties(plan.PerformancePlan?.Policy,
                "BatchSize ThrottleDelayMilliseconds LockTimeoutMilliseconds EnableThrottledMode PreferredWindowMode");
            json["ProviderCapabilities"] = SelectProperties(plan.ProviderCapabilities,
                "DataSourceType DataSourceCategory SupportsAlterColumn SupportsRenameEntity SupportsRenameColumn SupportsTransactionalDdl SupportsForeignKeys SupportsIndexes RequiresOfflineWindowForSchemaChanges");
            json["Readiness"] = new JArray(plan.ReadinessIssues.Select(issue =>
                SelectProperties(issue, "Code Severity EntityName"))
                .OrderBy(item => CanonicalJson(item), StringComparer.Ordinal));
            return HashJson(json);
        }

        private bool ValidatePlanIntent(MigrationPlanArtifact plan, out string message)
        {
            message = string.Empty;
            if (plan.PlanHashVersion != CurrentPlanHashVersion)
                message = "Legacy or unsupported migration plan hash version. Rebuild and re-approve the plan before execution or resume.";
            else if (plan.Operations == null || plan.Operations.Any(operation => operation == null) ||
                     plan.ReadinessIssues == null || plan.ExecutionPolicy == null || plan.GovernancePolicy == null)
                message = "Migration plan intent is incomplete.";
            else if (!string.Equals(plan.PlanHash, ComputePlanHash(plan), StringComparison.Ordinal))
                message = "Migration plan intent changed after planning. Rebuild and re-approve the plan.";
            else if (!string.Equals(plan.TargetFingerprint, CaptureTargetFingerprint(), StringComparison.Ordinal))
                message = "Migration target identity/configuration changed. Rebuild and re-approve the plan.";
            else if (plan.Operations.Any(operation => NeedsSchema(operation.Kind) && operation.SchemaSnapshot?.DesiredSchema == null))
                message = "Migration plan is missing a captured desired schema.";
            return message.Length == 0;
        }

        private static bool NeedsSchema(MigrationPlanOperationKind kind) =>
            kind == MigrationPlanOperationKind.CreateEntity || kind == MigrationPlanOperationKind.AddMissingColumns ||
            kind == MigrationPlanOperationKind.AlterColumn || kind == MigrationPlanOperationKind.AddForeignKey ||
            kind == MigrationPlanOperationKind.CreateIndex;

        /// <summary>Creates a new plan identity for an explicit retry/failure policy revision. Re-approval is required.</summary>
        public MigrationPlanArtifact CreateMigrationPlanRevision(MigrationPlanArtifact plan, MigrationExecutionPolicy policy, MigrationPolicyOptions policyOptions = null)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (!ValidatePlanIntent(plan, out var message)) throw new InvalidOperationException(message);
            var revision = CopyExecutionPlan(plan);
            revision.PlanId = Guid.NewGuid().ToString("N");
            revision.CreatedOnUtc = DateTime.UtcNow;
            revision.LifecycleState = MigrationPlanLifecycleState.Draft;
            revision.ExecutionPolicy = CopySnapshot(policy);
            if (policyOptions != null)
            {
                revision.GovernancePolicy = CopySnapshot(policyOptions);
                revision.GovernancePolicy.Approver = string.Empty;
                revision.GovernancePolicy.OverrideReason = string.Empty;
                revision.GovernancePolicy.ApprovedPlanHash = string.Empty;
            }
            revision.PlanHash = ComputePlanHash(revision);
            revision.PolicyEvaluation = EvaluateMigrationPlanPolicy(revision);
            revision.DryRunReport = GenerateDryRunReport(revision);
            revision.ImpactReport = BuildImpactReport(revision);
            revision.CompensationPlan = BuildCompensationPlan(revision);
            revision.PerformancePlan = BuildPerformancePlan(revision, CopySnapshot(revision.PerformancePlan.Policy));
            revision.PreflightReport = new MigrationPreflightReport { PlanId = revision.PlanId, PlanHash = revision.PlanHash };
            revision.AuditTrail.Clear();
            revision.Diagnostics.Clear();
            revision.CiValidationReport = ValidatePlanForCi(revision);
            revision.RolloutGovernanceReport = EvaluateRolloutGovernance(revision);
            revision.ExecutionCheckpoint = CreatePlanningCheckpoint(revision);
            TryTrackMigrationPlan(revision, nameof(CreateMigrationPlanRevision));
            return revision;
        }
    }
}
