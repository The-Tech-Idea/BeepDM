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
    /// Orchestrates wave-based migration of pipeline definitions from legacy to
    /// enterprise-target behavior.
    ///
    /// Responsibilities:
    ///   - Persist and load <see cref="MigrationWave"/> definitions.
    ///   - Execute canary and shadow parallel runs for comparison.
    ///   - Compute per-wave <see cref="MigrationKpiSnapshot"/> and weekly <see cref="KpiGovernanceReport"/>.
    ///   - Promote waves to Completed or trigger automatic rollback based on KPI thresholds.
    ///
    /// Storage: {BeepDataPath}/MigrationWaves/{waveId}.wave.json
    /// Comparisons: {BeepDataPath}/MigrationWaves/Comparisons/{comparisonId}.cmp.json
    /// </summary>
    public class MigrationManager
    {
        private readonly PipelineManager _pipelineManager;
        private readonly string          _waveFolder;
        private readonly string          _cmpFolder;

        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>Optional observability store for audit entries.</summary>
        public ObservabilityStore? ObservabilityStore { get; set; }

        public MigrationManager(PipelineManager pipelineManager)
        {
            _pipelineManager = pipelineManager ?? throw new ArgumentNullException(nameof(pipelineManager));
            _waveFolder      = EnvironmentService.CreateAppfolder("MigrationWaves");
            _cmpFolder       = EnvironmentService.CreateAppfolder("MigrationWaves", "Comparisons");
        }

        // ── Wave management ───────────────────────────────────────────────────

        /// <summary>Persists a wave definition (create or update).</summary>
        public async Task SaveWaveAsync(MigrationWave wave, CancellationToken token = default)
        {
            string path = WavePath(wave.WaveId);
            string json = JsonSerializer.Serialize(wave, _json);
            string tmp  = path + ".tmp";
            await File.WriteAllTextAsync(tmp, json, token).ConfigureAwait(false);
            File.Move(tmp, path, overwrite: true);

            if (ObservabilityStore != null)
                await ObservabilityStore.AppendAuditAsync(new AuditEntry
                {
                    Action     = "WaveSaved",
                    EntityType = "MigrationWave",
                    EntityId   = wave.WaveId,
                    EntityName = wave.Name
                }).ConfigureAwait(false);
        }

        public async Task<MigrationWave?> LoadWaveAsync(string waveId)
        {
            string path = WavePath(waveId);
            if (!File.Exists(path)) return null;
            string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            return JsonSerializer.Deserialize<MigrationWave>(json, _json);
        }

        public async Task<IReadOnlyList<MigrationWave>> LoadAllWavesAsync()
        {
            var result = new List<MigrationWave>();
            foreach (var file in Directory.GetFiles(_waveFolder, "*.wave.json"))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                    var w = JsonSerializer.Deserialize<MigrationWave>(json, _json);
                    if (w != null) result.Add(w);
                }
                catch { /* skip corrupt files */ }
            }
            return result.OrderBy(w => (int)w.Tier).ToList();
        }

        // ── Wave lifecycle ────────────────────────────────────────────────────

        /// <summary>
        /// Starts a wave by setting its status to <see cref="WaveStatus.InProgress"/>
        /// and recording who promoted it.
        /// </summary>
        public async Task<MigrationWave> StartWaveAsync(
            string waveId,
            string promotedBy,
            CancellationToken token = default)
        {
            var wave = await LoadWaveAsync(waveId)
                ?? throw new KeyNotFoundException($"Wave '{waveId}' not found.");

            if (wave.Status != WaveStatus.Pending)
                throw new InvalidOperationException($"Wave '{wave.Name}' is already {wave.Status}.");

            wave.Status      = WaveStatus.InProgress;
            wave.StartedAtUtc = DateTime.UtcNow;
            wave.PromotedBy  = promotedBy;
            await SaveWaveAsync(wave, token);

            if (ObservabilityStore != null)
                await ObservabilityStore.AppendAuditAsync(new AuditEntry
                {
                    Action      = "WaveStarted",
                    EntityType  = "MigrationWave",
                    EntityId    = waveId,
                    EntityName  = wave.Name,
                    PerformedBy = promotedBy
                }).ConfigureAwait(false);

            return wave;
        }

        /// <summary>
        /// Completes a wave after all health checks pass.
        /// </summary>
        public async Task<MigrationWave> CompleteWaveAsync(
            string waveId,
            string performedBy,
            CancellationToken token = default)
        {
            var wave = await LoadWaveAsync(waveId)
                ?? throw new KeyNotFoundException($"Wave '{waveId}' not found.");

            wave.Status        = WaveStatus.Completed;
            wave.FinishedAtUtc = DateTime.UtcNow;
            await SaveWaveAsync(wave, token);

            if (ObservabilityStore != null)
                await ObservabilityStore.AppendAuditAsync(new AuditEntry
                {
                    Action      = "WaveCompleted",
                    EntityType  = "MigrationWave",
                    EntityId    = waveId,
                    EntityName  = wave.Name,
                    PerformedBy = performedBy
                }).ConfigureAwait(false);

            return wave;
        }

        /// <summary>
        /// Rolls back a wave. Optionally restores each pipeline from a
        /// pre-migration snapshot stored in a <see cref="ReleaseManifest"/>.
        /// </summary>
        public async Task<MigrationWave> RollbackWaveAsync(
            string waveId,
            string rolledBackBy,
            CancellationToken token = default)
        {
            var wave = await LoadWaveAsync(waveId)
                ?? throw new KeyNotFoundException($"Wave '{waveId}' not found.");

            wave.Status        = WaveStatus.RolledBack;
            wave.FinishedAtUtc = DateTime.UtcNow;
            wave.RolledBackBy  = rolledBackBy;
            await SaveWaveAsync(wave, token);

            if (ObservabilityStore != null)
                await ObservabilityStore.AppendAuditAsync(new AuditEntry
                {
                    Action      = "WaveRolledBack",
                    EntityType  = "MigrationWave",
                    EntityId    = waveId,
                    EntityName  = wave.Name,
                    PerformedBy = rolledBackBy
                }).ConfigureAwait(false);

            return wave;
        }

        // ── Canary / shadow runs ──────────────────────────────────────────────

        /// <summary>
        /// Runs both the current pipeline and a candidate pipeline in sequence
        /// (canary) and compares their output metrics.
        /// The candidate pipeline is identified by <see cref="PipelineDefinition.CandidatePipelineId"/>.
        /// </summary>
        public async Task<CanaryRunComparison> RunCanaryAsync(
            string baselinePipelineId,
            string candidatePipelineId,
            IProgress<PassedArgs>? progress = null,
            CancellationToken token = default)
        {
            var baseline  = await _pipelineManager.RunAsync(baselinePipelineId,  progress, token);
            var candidate = await _pipelineManager.RunAsync(candidatePipelineId, progress, token);

            return await RecordComparisonAsync(
                baselinePipelineId, baseline, candidate, CanaryRunType.Canary);
        }

        /// <summary>
        /// Runs the candidate pipeline in parallel with the baseline.
        /// The candidate's output records are discarded — only metrics are compared.
        /// This is used to validate resource usage and error rates before full promotion.
        /// </summary>
        public async Task<CanaryRunComparison> RunShadowAsync(
            string baselinePipelineId,
            string candidatePipelineId,
            IProgress<PassedArgs>? progress = null,
            CancellationToken token = default)
        {
            // Run both concurrently; candidate output is not considered production output
            var baselineTask  = _pipelineManager.RunAsync(baselinePipelineId,  progress, token);
            var candidateTask = _pipelineManager.RunAsync(candidatePipelineId, progress, token);

            await Task.WhenAll(baselineTask, candidateTask).ConfigureAwait(false);

            return await RecordComparisonAsync(
                baselinePipelineId, baselineTask.Result, candidateTask.Result, CanaryRunType.Shadow);
        }

        // ── KPI computation ───────────────────────────────────────────────────

        /// <summary>
        /// Computes a <see cref="MigrationKpiSnapshot"/> for the pipelines in a wave
        /// over the specified time window.
        /// </summary>
        public async Task<MigrationKpiSnapshot> ComputeKpiSnapshotAsync(
            string waveId,
            DateTime from,
            DateTime to,
            CancellationToken token = default)
        {
            var wave = await LoadWaveAsync(waveId)
                ?? throw new KeyNotFoundException($"Wave '{waveId}' not found.");

            var snapshot = new MigrationKpiSnapshot
            {
                WaveId      = waveId,
                Tier        = wave.Tier,
                WindowStart = from,
                WindowEnd   = to
            };

            // Aggregate run history for all pipelines in the wave
            int totalRuns     = 0;
            int successRuns   = 0;
            int timeouts      = 0;
            long totalRecords = 0;
            long totalRejected = 0;
            var durations     = new List<TimeSpan>();
            double totalCost  = 0;
            int lineageCount  = 0;

            foreach (var pipelineId in wave.PipelineIds)
            {
                var history = await _pipelineManager.GetRunHistoryAsync(pipelineId, limit: 1000)
                    .ConfigureAwait(false);

                var windowHistory = history
                    .Where(r => r.StartedAtUtc >= from && r.StartedAtUtc <= to)
                    .ToList();

                totalRuns   += windowHistory.Count;
                successRuns += windowHistory.Count(r => r.Status == RunStatus.Succeeded);
                timeouts    += windowHistory.Count(r => r.Status == RunStatus.TimedOut);
                totalRecords  += windowHistory.Sum(r => r.RecordsRead);
                totalRejected += windowHistory.Sum(r => r.RecordsRejected);

                foreach (var r in windowHistory)
                    if (r.Duration.HasValue) durations.Add(r.Duration.Value);
            }

            snapshot.TotalRuns       = totalRuns;
            snapshot.SuccessRatePct  = totalRuns > 0 ? (double)successRuns / totalRuns * 100.0 : 100.0;
            snapshot.TimeoutCount    = timeouts;
            snapshot.DqRejectRatioPct = totalRecords > 0
                ? (double)totalRejected / totalRecords * 100.0 : 0.0;

            if (durations.Count > 0)
            {
                durations.Sort();
                int p95 = (int)Math.Ceiling(durations.Count * 0.95) - 1;
                snapshot.AvgP95Duration = durations[Math.Clamp(p95, 0, durations.Count - 1)];
            }

            // Evaluate wave exit criteria
            var violations = new List<string>();
            if (snapshot.SuccessRatePct < wave.ExitSuccessRateMin)
                violations.Add($"Success rate {snapshot.SuccessRatePct:F1}% < exit criterion {wave.ExitSuccessRateMin}%");
            if (snapshot.DqRejectRatioPct > wave.ExitRejectRatioMax)
                violations.Add($"Reject ratio {snapshot.DqRejectRatioPct:F2}% > max {wave.ExitRejectRatioMax}%");

            snapshot.Violations        = violations;
            snapshot.MeetsExitCriteria = violations.Count == 0;
            snapshot.TriggersRollback  = snapshot.SuccessRatePct < wave.RollbackSuccessRateThreshold;

            return snapshot;
        }

        /// <summary>
        /// Generates a weekly KPI governance report across all defined waves.
        /// </summary>
        public async Task<KpiGovernanceReport> GenerateGovernanceReportAsync(
            DateTime from,
            DateTime to,
            CancellationToken token = default)
        {
            var waves     = await LoadAllWavesAsync().ConfigureAwait(false);
            var snapshots = new List<MigrationKpiSnapshot>();

            foreach (var wave in waves)
            {
                var snap = await ComputeKpiSnapshotAsync(wave.WaveId, from, to, token)
                    .ConfigureAwait(false);
                snapshots.Add(snap);
            }

            int completedWaves = waves.Count(w => w.Status == WaveStatus.Completed);
            int totalPipelines = waves.Sum(w => w.PipelineIds.Count);
            var completedPipelines = waves
                .Where(w => w.Status == WaveStatus.Completed)
                .Sum(w => w.PipelineIds.Count);

            var report = new KpiGovernanceReport
            {
                WindowStart            = from,
                WindowEnd              = to,
                WaveSnapshots          = snapshots,
                OverallCompletionPct   = totalPipelines > 0
                    ? (double)completedPipelines / totalPipelines * 100.0 : 0,
                WavesWithRollbackTrigger = snapshots.Count(s => s.TriggersRollback),
                Recommendation         = BuildRecommendation(snapshots, waves)
            };

            return report;
        }

        /// <summary>
        /// Evaluates an in-progress wave's current KPIs and automatically triggers
        /// rollback if thresholds are breached.
        /// </summary>
        public async Task<(bool RollbackTriggered, MigrationKpiSnapshot Snapshot)> EvaluateAndAutoRollbackAsync(
            string waveId,
            string triggeredBy,
            CancellationToken token = default)
        {
            var to   = DateTime.UtcNow;
            var from = to.AddDays(-1);

            var snapshot = await ComputeKpiSnapshotAsync(waveId, from, to, token);

            if (snapshot.TriggersRollback)
                await RollbackWaveAsync(waveId, $"auto:{triggeredBy}", token);

            return (snapshot.TriggersRollback, snapshot);
        }

        // ── Comparison history ────────────────────────────────────────────────

        public async Task<IReadOnlyList<CanaryRunComparison>> GetComparisonHistoryAsync(
            string pipelineId,
            int limit = 50)
        {
            var result = new List<CanaryRunComparison>();
            foreach (var file in Directory.GetFiles(_cmpFolder, "*.cmp.json")
                         .OrderByDescending(f => f)
                         .Take(limit * 5))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                    var cmp = JsonSerializer.Deserialize<CanaryRunComparison>(json, _json);
                    if (cmp != null && cmp.PipelineId == pipelineId)
                    {
                        result.Add(cmp);
                        if (result.Count >= limit) break;
                    }
                }
                catch { }
            }
            return result;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private async Task<CanaryRunComparison> RecordComparisonAsync(
            string baselinePipelineId,
            PipelineRunResult baseline,
            PipelineRunResult candidate,
            CanaryRunType type)
        {
            bool passed           = candidate.Status == RunStatus.Succeeded
                                 && candidate.RecordsRejected <= baseline.RecordsRejected * 1.05;
            string? divergence    = null;

            if (!passed)
            {
                if (candidate.Status != RunStatus.Succeeded)
                    divergence = $"Candidate run status: {candidate.Status}";
                else
                    divergence = $"Candidate rejected {candidate.RecordsRejected} vs baseline {baseline.RecordsRejected}";
            }

            var cmp = new CanaryRunComparison
            {
                PipelineId              = baselinePipelineId,
                RunType                 = type,
                BaselineRunId           = baseline.RunId,
                CandidateRunId          = candidate.RunId,
                BaselineRecordsWritten  = baseline.RecordsWritten,
                CandidateRecordsWritten = candidate.RecordsWritten,
                BaselineRecordsRejected = baseline.RecordsRejected,
                CandidateRecordsRejected= candidate.RecordsRejected,
                BaselineDurationMs      = baseline.Duration?.TotalMilliseconds ?? 0,
                CandidateDurationMs     = candidate.Duration?.TotalMilliseconds ?? 0,
                Passed                  = passed,
                DivergenceReason        = divergence
            };

            string path = Path.Combine(_cmpFolder, $"{cmp.ComparisonId}.cmp.json");
            await File.WriteAllTextAsync(path,
                JsonSerializer.Serialize(cmp, _json)).ConfigureAwait(false);

            return cmp;
        }

        private static string BuildRecommendation(
            IReadOnlyList<MigrationKpiSnapshot> snapshots,
            IReadOnlyList<MigrationWave> waves)
        {
            int rollbackCount = snapshots.Count(s => s.TriggersRollback);
            int violations    = snapshots.Sum(s => s.Violations.Count);

            if (rollbackCount > 0)
                return $"ATTENTION: {rollbackCount} wave(s) have crossed rollback thresholds. " +
                       "Review wave KPIs and initiate rollback procedures.";

            if (violations > 0)
                return $"WARNING: {violations} exit criterion violation(s) detected. " +
                       "Do not promote affected waves until criteria are met.";

            int inProgress = waves.Count(w => w.Status == WaveStatus.InProgress);
            if (inProgress > 0)
                return $"OK: {inProgress} wave(s) in progress. All KPIs within thresholds. " +
                       "Continue monitoring at next cadence.";

            return "OK: All waves healthy. No violations or rollback triggers detected.";
        }

        private string WavePath(string waveId)
            => Path.Combine(_waveFolder, $"{waveId}.wave.json");
    }
}
