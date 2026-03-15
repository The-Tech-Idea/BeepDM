using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Observability;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// Computes aggregated KPI metrics from stored <see cref="PipelineRunLog"/> records.
    /// Uses <see cref="ObservabilityStore"/> for historical data and an in-memory
    /// dictionary for live (in-flight) metrics.
    /// </summary>
    public class MetricsEngine
    {
        private readonly ObservabilityStore _store;

        /// <summary>Live metric accumulators keyed by RunId.</summary>
        private readonly ConcurrentDictionary<string, LiveAccumulator> _live = new();

        public MetricsEngine(ObservabilityStore store)
        {
            _store = store;
        }

        // ── Historical metrics ─────────────────────────────────────────────────

        public async Task<PipelineMetrics> ComputeAsync(
            string pipelineId, DateTime from, DateTime to)
        {
            var logs = await _store.QueryRunLogsAsync(new RunLogQuery
            {
                PipelineId = pipelineId,
                From       = from,
                To         = to,
                Limit      = int.MaxValue
            }).ConfigureAwait(false);

            return Aggregate(pipelineId, string.Empty, from, to, logs);
        }

        public async Task<IReadOnlyList<PipelineMetrics>> ComputeAllAsync(
            DateTime from, DateTime to)
        {
            var allLogs = await _store.QueryRunLogsAsync(new RunLogQuery
            {
                From  = from,
                To    = to,
                Limit = int.MaxValue
            }).ConfigureAwait(false);

            return allLogs
                .GroupBy(l => l.PipelineId)
                .Select(g => Aggregate(g.Key, g.First().PipelineName, from, to, g.ToList()))
                .ToList();
        }

        // ── Live metrics ──────────────────────────────────────────────────────

        /// <summary>
        /// Called by the engine when a run starts.
        /// Must be matched with <see cref="RecordCompletion"/> to free memory.
        /// </summary>
        public void RecordStart(string runId, string pipelineId, string pipelineName)
        {
            _live[runId] = new LiveAccumulator
            {
                RunId        = runId,
                PipelineId   = pipelineId,
                PipelineName = pipelineName,
                StartedAt    = DateTime.UtcNow
            };
        }

        public void IncrementRead(string runId, long count = 1)
        {
            if (_live.TryGetValue(runId, out var a)) a.RecordsRead    += count;
        }

        public void IncrementWritten(string runId, long count = 1)
        {
            if (_live.TryGetValue(runId, out var a)) a.RecordsWritten += count;
        }

        public void IncrementRejected(string runId, long count = 1)
        {
            if (_live.TryGetValue(runId, out var a)) a.RecordsRejected += count;
        }

        public void UpdateCurrentStep(string runId, string stepName)
        {
            if (_live.TryGetValue(runId, out var a)) a.CurrentStep = stepName;
        }

        public void RecordCompletion(string runId)
        {
            _live.TryRemove(runId, out _);
        }

        /// <summary>Returns a snapshot for a currently running pipeline.</summary>
        public PipelineMetrics? GetLiveMetrics(string runId)
        {
            if (!_live.TryGetValue(runId, out var a)) return null;
            var elapsed = DateTime.UtcNow - a.StartedAt;
            return new PipelineMetrics
            {
                PipelineId            = a.PipelineId,
                PipelineName          = a.PipelineName,
                PeriodStart           = a.StartedAt,
                PeriodEnd             = DateTime.UtcNow,
                TotalRuns             = 1,
                TotalRecordsProcessed = a.RecordsRead,
                AvgRowsPerSecond      = elapsed.TotalSeconds > 0
                    ? a.RecordsRead / elapsed.TotalSeconds : 0,
                AvgDuration           = elapsed
            };
        }

        public IReadOnlyList<LiveAccumulator> GetAllLive()
            => _live.Values.ToList();

        // ── Private aggregation helper ────────────────────────────────────────

        private static PipelineMetrics Aggregate(
            string pipelineId, string pipelineName,
            DateTime from, DateTime to,
            IReadOnlyList<PipelineRunLog> logs)
        {
            if (logs.Count == 0)
                return new PipelineMetrics
                {
                    PipelineId   = pipelineId,
                    PipelineName = pipelineName,
                    PeriodStart  = from,
                    PeriodEnd    = to
                };

            var durations = logs.Select(l => l.Duration).OrderBy(d => d).ToArray();
            int p95Idx    = Math.Max(0, (int)Math.Ceiling(durations.Length * 0.95) - 1);

            var m = new PipelineMetrics
            {
                PipelineId            = pipelineId,
                PipelineName          = pipelineName,
                PeriodStart           = from,
                PeriodEnd             = to,
                TotalRuns             = logs.Count,
                SuccessfulRuns        = logs.Count(l => l.Status == Models.RunStatus.Success),
                FailedRuns            = logs.Count(l => l.Status == Models.RunStatus.Failed),
                CancelledRuns         = logs.Count(l => l.Status == Models.RunStatus.Cancelled),
                TotalRecordsProcessed = logs.Sum(l => l.RecordsRead),
                TotalBytesProcessed   = logs.Sum(l => l.BytesProcessed),
                TotalRejected         = logs.Sum(l => l.RecordsRejected),
                TotalWarned           = logs.Sum(l => l.RecordsWarned),
                AvgDQPassRate         = logs.Average(l => l.DQPassRate),
                MinDuration           = durations.First(),
                MaxDuration           = durations.Last(),
                P95Duration           = durations[p95Idx],
                AvgDuration           = TimeSpan.FromTicks((long)logs.Average(l => l.Duration.Ticks))
            };

            // Rows/second
            double totalSec = logs.Sum(l => l.Duration.TotalSeconds);
            m.AvgRowsPerSecond = totalSec > 0
                ? m.TotalRecordsProcessed / totalSec : 0;

            // Daily time-series (UTC grouped by date)
            m.RunsOverTime = logs
                .GroupBy(l => l.StartedAtUtc.Date)
                .OrderBy(g => g.Key)
                .Select(g => new MetricDataPoint(g.Key, g.Count(), "runs"))
                .ToList();

            m.RowsOverTime = logs
                .GroupBy(l => l.StartedAtUtc.Date)
                .OrderBy(g => g.Key)
                .Select(g => new MetricDataPoint(g.Key, g.Sum(x => x.RecordsRead), "rows"))
                .ToList();

            // Top errors (most frequent non-null error messages)
            m.TopErrors = logs
                .Where(l => !string.IsNullOrEmpty(l.ErrorMessage))
                .GroupBy(l => l.ErrorMessage!)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()}×)")
                .ToList();

            return m;
        }

        // ── Nested accumulator ────────────────────────────────────────────────

        public sealed class LiveAccumulator
        {
            public string   RunId          { get; set; } = string.Empty;
            public string   PipelineId     { get; set; } = string.Empty;
            public string   PipelineName   { get; set; } = string.Empty;
            public DateTime StartedAt      { get; set; }
            public string   CurrentStep    { get; set; } = string.Empty;
            public long     RecordsRead    { get; set; }
            public long     RecordsWritten { get; set; }
            public long     RecordsRejected { get; set; }
        }
    }
}
