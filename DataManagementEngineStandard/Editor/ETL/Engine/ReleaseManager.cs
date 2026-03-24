using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Observability;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Manages pipeline release lifecycle: builds release manifests with test evidence,
    /// evaluates promotion quality gates, snapshots rollback definitions, and persists
    /// release artifacts for audit and compliance.
    ///
    /// Storage: {BeepDataPath}/Releases/{releaseId}.release.json
    /// </summary>
    public class ReleaseManager
    {
        private readonly PipelineManager _pipelineManager;
        private readonly PipelineQualityGate _gate;
        private readonly string _folder;

        /// <summary>Optional observability store for audit logging.</summary>
        public ObservabilityStore? ObservabilityStore { get; set; }

        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ReleaseManager(PipelineManager pipelineManager)
        {
            _pipelineManager = pipelineManager ?? throw new ArgumentNullException(nameof(pipelineManager));
            _gate            = new PipelineQualityGate();
            _folder          = EnvironmentService.CreateAppfolder("Releases");
        }

        /// <summary>
        /// Creates a release manifest for the specified pipeline, including a rollback
        /// snapshot of the current definition.
        /// </summary>
        public async Task<ReleaseManifest> CreateReleaseAsync(
            string pipelineId,
            string targetEnvironment,
            string? releasedBy = null,
            CancellationToken token = default)
        {
            var def = await _pipelineManager.LoadAsync(pipelineId)
                ?? throw new KeyNotFoundException($"Pipeline '{pipelineId}' not found.");

            var manifest = new ReleaseManifest
            {
                PipelineId        = def.Id,
                PipelineName      = def.Name,
                PipelineVersion   = def.Version,
                TargetEnvironment = targetEnvironment,
                ReleasedBy        = releasedBy,
                Status            = ReleaseStatus.Pending,
                PreviousDefinitionJson = JsonSerializer.Serialize(def, _json)
            };

            await SaveManifestAsync(manifest);

            if (ObservabilityStore != null)
                await ObservabilityStore.AppendAuditAsync(new AuditEntry
                {
                    Action      = "ReleaseCreated",
                    EntityType  = "Release",
                    EntityId    = manifest.ReleaseId,
                    EntityName  = $"{def.Name} v{def.Version} → {targetEnvironment}",
                    PerformedBy = releasedBy
                }).ConfigureAwait(false);

            return manifest;
        }

        /// <summary>
        /// Attaches test suite evidence to a release manifest.
        /// </summary>
        public async Task AttachTestEvidenceAsync(
            string releaseId,
            TestSuiteResult suiteResult,
            CancellationToken token = default)
        {
            var manifest = await LoadManifestAsync(releaseId)
                ?? throw new KeyNotFoundException($"Release '{releaseId}' not found.");

            manifest.TestEvidence.Add(suiteResult);
            await SaveManifestAsync(manifest);
        }

        /// <summary>
        /// Evaluates quality gates for a release using the last run result.
        /// Updates the manifest status to Ready or Failed.
        /// </summary>
        public async Task<(bool Passed, IReadOnlyList<GateEvaluation> Evaluations)> ValidateGatesAsync(
            string releaseId,
            IReadOnlyList<QualityGateRule> gates,
            PipelineRunResult runResult,
            CancellationToken token = default)
        {
            var manifest = await LoadManifestAsync(releaseId)
                ?? throw new KeyNotFoundException($"Release '{releaseId}' not found.");

            manifest.Status = ReleaseStatus.Validating;
            var (evaluations, allPassed) = _gate.EvaluateAll(gates, runResult);

            manifest.GateResults   = evaluations.ToList();
            manifest.AllGatesPassed = allPassed;
            manifest.Status        = allPassed ? ReleaseStatus.Ready : ReleaseStatus.Failed;

            await SaveManifestAsync(manifest);

            if (ObservabilityStore != null)
                await ObservabilityStore.AppendAuditAsync(new AuditEntry
                {
                    Action      = allPassed ? "ReleaseGatesPassed" : "ReleaseGatesFailed",
                    EntityType  = "Release",
                    EntityId    = releaseId,
                    EntityName  = manifest.PipelineName
                }).ConfigureAwait(false);

            return (allPassed, evaluations);
        }

        /// <summary>
        /// Marks verifying rollback was tested. Sets <see cref="ReleaseManifest.RollbackVerified"/>.
        /// </summary>
        public async Task RecordRollbackTestAsync(
            string releaseId,
            string rollbackTestRunId,
            CancellationToken token = default)
        {
            var manifest = await LoadManifestAsync(releaseId)
                ?? throw new KeyNotFoundException($"Release '{releaseId}' not found.");

            manifest.RollbackTestRunId = rollbackTestRunId;
            manifest.RollbackVerified  = true;
            await SaveManifestAsync(manifest);
        }

        /// <summary>
        /// Promotes a release — marks it as approved and deployed.
        /// Requires all gates passed and optionally rollback verified.
        /// </summary>
        public async Task<ReleaseManifest> PromoteAsync(
            string releaseId,
            string approvedBy,
            string? riskNotes = null,
            bool requireRollbackTest = false,
            CancellationToken token = default)
        {
            var manifest = await LoadManifestAsync(releaseId)
                ?? throw new KeyNotFoundException($"Release '{releaseId}' not found.");

            if (!manifest.AllGatesPassed)
                throw new InvalidOperationException("Cannot promote: quality gates have not passed.");

            if (requireRollbackTest && !manifest.RollbackVerified)
                throw new InvalidOperationException("Cannot promote: rollback test has not been verified.");

            manifest.ApprovedBy   = approvedBy;
            manifest.ApprovedAtUtc = DateTime.UtcNow;
            manifest.RiskNotes    = riskNotes;
            manifest.Status       = ReleaseStatus.Promoted;

            await SaveManifestAsync(manifest);

            if (ObservabilityStore != null)
                await ObservabilityStore.AppendAuditAsync(new AuditEntry
                {
                    Action      = "ReleasePromoted",
                    EntityType  = "Release",
                    EntityId    = releaseId,
                    EntityName  = manifest.PipelineName,
                    PerformedBy = approvedBy
                }).ConfigureAwait(false);

            return manifest;
        }

        /// <summary>
        /// Rolls back a promoted release by restoring the previous pipeline definition.
        /// </summary>
        public async Task<ReleaseManifest> RollbackAsync(
            string releaseId,
            string performedBy,
            CancellationToken token = default)
        {
            var manifest = await LoadManifestAsync(releaseId)
                ?? throw new KeyNotFoundException($"Release '{releaseId}' not found.");

            if (manifest.Status != ReleaseStatus.Promoted)
                throw new InvalidOperationException("Can only rollback a promoted release.");

            if (string.IsNullOrWhiteSpace(manifest.PreviousDefinitionJson))
                throw new InvalidOperationException("No previous definition snapshot available for rollback.");

            var previousDef = JsonSerializer.Deserialize<PipelineDefinition>(
                manifest.PreviousDefinitionJson, _json);
            if (previousDef != null)
                await _pipelineManager.SaveAsync(previousDef);

            manifest.Status = ReleaseStatus.RolledBack;
            await SaveManifestAsync(manifest);

            if (ObservabilityStore != null)
                await ObservabilityStore.AppendAuditAsync(new AuditEntry
                {
                    Action      = "ReleaseRolledBack",
                    EntityType  = "Release",
                    EntityId    = releaseId,
                    EntityName  = manifest.PipelineName,
                    PerformedBy = performedBy
                }).ConfigureAwait(false);

            return manifest;
        }

        // ── Query ─────────────────────────────────────────────────────────

        public async Task<ReleaseManifest?> LoadManifestAsync(string releaseId)
        {
            string path = ManifestPath(releaseId);
            if (!File.Exists(path)) return null;
            string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            return JsonSerializer.Deserialize<ReleaseManifest>(json, _json);
        }

        public async Task<IReadOnlyList<ReleaseManifest>> LoadAllAsync()
        {
            var result = new List<ReleaseManifest>();
            foreach (var file in Directory.GetFiles(_folder, "*.release.json"))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                    var m = JsonSerializer.Deserialize<ReleaseManifest>(json, _json);
                    if (m != null) result.Add(m);
                }
                catch { /* skip corrupt files */ }
            }
            return result;
        }

        public async Task<IReadOnlyList<ReleaseManifest>> GetByPipelineAsync(string pipelineId)
        {
            var all = await LoadAllAsync();
            return all.Where(m => m.PipelineId == pipelineId)
                      .OrderByDescending(m => m.CreatedAtUtc)
                      .ToList();
        }

        // ── Persistence ────────────────────────────────────────────────────

        private async Task SaveManifestAsync(ReleaseManifest manifest)
        {
            string path = ManifestPath(manifest.ReleaseId);
            string json = JsonSerializer.Serialize(manifest, _json);
            string tmp  = path + ".tmp";
            await File.WriteAllTextAsync(tmp, json).ConfigureAwait(false);
            File.Move(tmp, path, overwrite: true);
        }

        private string ManifestPath(string releaseId)
            => Path.Combine(_folder, $"{releaseId}.release.json");
    }
}
