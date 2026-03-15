using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Observability;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// Query facade that feeds the Phase 7 dashboard UI and any external tooling.
    /// All reads go through <see cref="ObservabilityStore"/> and <see cref="MetricsEngine"/>.
    /// </summary>
    public class PipelineDashboardApi
    {
        private readonly ObservabilityStore _store;
        private readonly MetricsEngine      _metrics;
        private readonly AlertingEngine     _alerting;

        public PipelineDashboardApi(
            ObservabilityStore store,
            MetricsEngine      metrics,
            AlertingEngine     alerting)
        {
            _store    = store;
            _metrics  = metrics;
            _alerting = alerting;
        }

        // ── Overview ──────────────────────────────────────────────────────────

        /// <summary>Returns top-level summary for the main dashboard.</summary>
        public async Task<DashboardSummary> GetSummaryAsync(DateTime from, DateTime to)
        {
            var today     = DateTime.UtcNow.Date;
            var allToday  = await _store.QueryRunLogsAsync(new RunLogQuery
            {
                From  = today,
                Limit = int.MaxValue
            }).ConfigureAwait(false);

            var allMetrics = await _metrics.ComputeAllAsync(from, to).ConfigureAwait(false);
            var alerts     = await _alerting.GetRecentAlertsAsync(10).ConfigureAwait(false);
            var recent     = await _store.QueryRunLogsAsync(new RunLogQuery { Limit = 10 })
                                         .ConfigureAwait(false);

            return new DashboardSummary
            {
                TotalPipelines     = allMetrics.Count,
                ActiveRuns         = _metrics.GetAllLive().Count,
                RunsToday          = allToday.Count,
                FailuresToday      = allToday.Count(r => r.Status == RunStatus.Failed),
                RowsProcessedToday = allToday.Sum(r => r.RecordsRead),
                AvgSuccessRate     = allMetrics.Count > 0
                    ? allMetrics.Average(m => m.SuccessRate) : 0.0,
                RecentAlerts       = alerts.ToList(),
                RecentRuns         = recent.ToList()
            };
        }

        // ── Per-pipeline ──────────────────────────────────────────────────────

        public Task<PipelineMetrics> GetPipelineMetricsAsync(
            string pipelineId, DateTime from, DateTime to)
            => _metrics.ComputeAsync(pipelineId, from, to);

        public Task<IReadOnlyList<PipelineRunLog>> GetRecentRunsAsync(
            string? pipelineId, int limit = 50)
            => _store.QueryRunLogsAsync(new RunLogQuery
            {
                PipelineId = pipelineId,
                Limit      = limit
            });

        // ── Live runs ─────────────────────────────────────────────────────────

        public IReadOnlyList<LiveRunStatus> GetActiveRuns()
        {
            return _metrics.GetAllLive()
                .Select(a => new LiveRunStatus
                {
                    RunId            = a.RunId,
                    PipelineId       = a.PipelineId,
                    PipelineName     = a.PipelineName,
                    StartedAtUtc     = a.StartedAt,
                    CurrentStep      = a.CurrentStep,
                    RecordsRead      = a.RecordsRead,
                    RecordsWritten   = a.RecordsWritten,
                    RecordsRejected  = a.RecordsRejected
                }).ToList();
        }

        // ── Alerts ────────────────────────────────────────────────────────────

        public Task<IReadOnlyList<AlertEvent>> GetRecentAlertsAsync(int limit = 100)
            => _alerting.GetRecentAlertsAsync(limit);

        // ── Lineage ───────────────────────────────────────────────────────────

        public Task<LineageGraph> GetLineageGraphAsync(
            string srcDataSource, string destDataSource)
            => ((ILineageStore)_store).GetGraphAsync(srcDataSource, destDataSource);

        // ── Audit ─────────────────────────────────────────────────────────────

        public Task<IReadOnlyList<AuditEntry>> GetAuditTrailAsync(
            string entityType, string entityId)
            => _store.GetAuditTrailAsync(new AuditQuery
            {
                EntityType = entityType,
                EntityId   = entityId,
                Limit      = 500
            });
    }
}
