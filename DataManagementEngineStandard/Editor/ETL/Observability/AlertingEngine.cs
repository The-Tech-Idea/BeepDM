using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Observability;

namespace TheTechIdea.Beep.Pipelines.Observability
{
    /// <summary>
    /// Evaluates alert rules against completed pipeline run logs,
    /// fires matching <see cref="IPipelineNotifier"/> plugins,
    /// and persists <see cref="AlertEvent"/> records via <see cref="ObservabilityStore"/>.
    /// </summary>
    public class AlertingEngine
    {
        private readonly ObservabilityStore _store;
        private readonly Func<string, IPipelineNotifier?> _notifierFactory;

        /// <summary>In-memory rule set. Reload from storage when needed.</summary>
        private readonly List<AlertRule> _rules = new();
        private readonly object _rulesLock = new();

        /// <summary>Tracks last-fired time per (ruleId, pipelineId) to enforce silence windows.</summary>
        private readonly ConcurrentDictionary<string, DateTime> _lastFired = new();

        /// <summary>Raised synchronously after each alert event is persisted.</summary>
        public event EventHandler<AlertEvent>? AlertFired;

        public AlertingEngine(
            ObservabilityStore store,
            Func<string, IPipelineNotifier?> notifierFactory)
        {
            _store           = store;
            _notifierFactory = notifierFactory;
        }

        // ── Rule management ───────────────────────────────────────────────────

        public void AddRule(AlertRule rule)
        {
            lock (_rulesLock) _rules.Add(rule);
        }

        public void RemoveRule(string ruleId)
        {
            lock (_rulesLock) _rules.RemoveAll(r => r.Id == ruleId);
        }

        public IReadOnlyList<AlertRule> GetRules()
        {
            lock (_rulesLock) return _rules.ToList();
        }

        // ── Evaluation ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the engine after every run completes.
        /// Evaluates all enabled rules whose PipelineIds match.
        /// </summary>
        public async Task EvaluateAsync(PipelineRunLog runLog, CancellationToken token)
        {
            List<AlertRule> candidates;
            lock (_rulesLock)
                candidates = _rules.Where(r =>
                    r.IsEnabled &&
                    (r.PipelineIds == null || r.PipelineIds.Contains(runLog.PipelineId))).ToList();

            foreach (var rule in candidates)
            {
                token.ThrowIfCancellationRequested();

                if (!RuleMatches(rule, runLog)) continue;
                if (InSilenceWindow(rule, runLog.PipelineId)) continue;

                var evt = new AlertEvent
                {
                    RuleId       = rule.Id,
                    RuleName     = rule.Name,
                    PipelineId   = runLog.PipelineId,
                    PipelineName = runLog.PipelineName,
                    RunId        = runLog.RunId,
                    Severity     = rule.Severity,
                    Message      = BuildMessage(rule, runLog),
                    FiredAtUtc   = DateTime.UtcNow
                };

                await _store.AppendAlertEventAsync(evt).ConfigureAwait(false);
                MarkFired(rule, runLog.PipelineId);
                AlertFired?.Invoke(this, evt);

                await FireNotifiersAsync(rule, evt, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Checks "OnNoRunWithin" liveness rules — call on a regular schedule (e.g. hourly).
        /// </summary>
        public async Task EvaluateLivenessAsync(CancellationToken token)
        {
            List<AlertRule> rules;
            lock (_rulesLock)
                rules = _rules.Where(r =>
                    r.IsEnabled && r.Trigger == AlertTrigger.OnNoRunWithin).ToList();

            foreach (var rule in rules)
            {
                token.ThrowIfCancellationRequested();
                if (rule.PipelineIds == null) continue;

                foreach (var pid in rule.PipelineIds)
                {
                    if (InSilenceWindow(rule, pid)) continue;

                    // Parse hours threshold from Condition, e.g. "HoursWithoutRun > 2"
                    double hoursThreshold = TryParseHoursThreshold(rule.Condition);
                    if (hoursThreshold <= 0) continue;

                    var recent = await _store.QueryRunLogsAsync(new RunLogQuery
                    {
                        PipelineId = pid,
                        From       = DateTime.UtcNow.AddHours(-hoursThreshold),
                        Limit      = 1
                    }).ConfigureAwait(false);

                    if (recent.Count > 0) continue;  // there was a recent run — ok

                    var evt = new AlertEvent
                    {
                        RuleId       = rule.Id,
                        RuleName     = rule.Name,
                        PipelineId   = pid,
                        Severity     = rule.Severity,
                        Message      = $"No run detected for pipeline '{pid}' in the last {hoursThreshold}h.",
                        FiredAtUtc   = DateTime.UtcNow
                    };

                    await _store.AppendAlertEventAsync(evt).ConfigureAwait(false);
                    MarkFired(rule, pid);
                    AlertFired?.Invoke(this, evt);
                    await FireNotifiersAsync(rule, evt, token).ConfigureAwait(false);
                }
            }
        }

        // ── Acknowledgement ───────────────────────────────────────────────────

        public async Task AcknowledgeAsync(string eventId, string acknowledgedBy)
        {
            await _store.UpdateAlertAcknowledgementAsync(
                eventId, acknowledgedBy, DateTime.UtcNow).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<AlertEvent>> GetRecentAlertsAsync(int limit = 100)
            => await _store.GetAlertEventsAsync(new AlertEventQuery { Limit = limit })
                .ConfigureAwait(false);

        public async Task<IReadOnlyList<AlertEvent>> GetUnacknowledgedAsync()
            => await _store.GetAlertEventsAsync(new AlertEventQuery
            {
                Acknowledged = false,
                Limit        = 1000
            }).ConfigureAwait(false);

        // ── Private helpers ───────────────────────────────────────────────────

        private static bool RuleMatches(AlertRule rule, PipelineRunLog log)
        {
            return rule.Trigger switch
            {
                AlertTrigger.OnFailure             => log.Status == RunStatus.Failed,
                AlertTrigger.OnSuccess             => log.Status == RunStatus.Success,
                AlertTrigger.OnCompletion          => true,
                AlertTrigger.OnDQThreshold         => EvalNumericCondition(rule.Condition, log.DQPassRate),
                AlertTrigger.OnRejectedThreshold   => EvalNumericCondition(rule.Condition, log.RecordsRejected),
                AlertTrigger.OnDurationThreshold   => EvalNumericCondition(rule.Condition, log.Duration.TotalSeconds),
                AlertTrigger.OnCustomExpression    => EvalCustom(rule.Condition, log),
                _ => false
            };
        }

        private static bool EvalNumericCondition(string? condition, double value)
        {
            if (string.IsNullOrWhiteSpace(condition)) return false;
            // Accepts patterns like "> 1000", "< 0.95", ">= 100"
            var parts = condition.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !double.TryParse(parts[1], out double threshold)) return false;
            return parts[0] switch
            {
                ">"  => value >  threshold,
                ">=" => value >= threshold,
                "<"  => value <  threshold,
                "<=" => value <= threshold,
                "==" => Math.Abs(value - threshold) < 1e-9,
                _    => false
            };
        }

        private static bool EvalCustom(string? condition, PipelineRunLog log)
        {
            if (string.IsNullOrWhiteSpace(condition)) return false;
            // Best-effort simple evaluation on the most common patterns
            try
            {
                if (condition.Contains("RecordsRejected"))
                    return EvalNumericCondition(
                        ExtractOperatorAndValue(condition, "RecordsRejected"),
                        log.RecordsRejected);
                if (condition.Contains("DQPassRate"))
                    return EvalNumericCondition(
                        ExtractOperatorAndValue(condition, "DQPassRate"),
                        log.DQPassRate);
                if (condition.Contains("Duration"))
                    return EvalNumericCondition(
                        ExtractOperatorAndValue(condition, "Duration"),
                        log.Duration.TotalSeconds);
            }
            catch { /* ignore eval errors */ }
            return false;
        }

        private static string ExtractOperatorAndValue(string expr, string fieldName)
        {
            // e.g. "RecordsRejected > 1000" → "> 1000"
            int idx = expr.IndexOf(fieldName, StringComparison.Ordinal);
            if (idx < 0) return "";
            return expr.Substring(idx + fieldName.Length).Trim();
        }

        private bool InSilenceWindow(AlertRule rule, string pipelineId)
        {
            string key = $"{rule.Id}:{pipelineId}";
            if (!_lastFired.TryGetValue(key, out var lastFiredAt)) return false;
            return (DateTime.UtcNow - lastFiredAt).TotalMinutes < rule.SilenceWindowMinutes;
        }

        private void MarkFired(AlertRule rule, string pipelineId)
        {
            _lastFired[$"{rule.Id}:{pipelineId}"] = DateTime.UtcNow;
        }

        private static string BuildMessage(AlertRule rule, PipelineRunLog log)
        {
            return rule.Trigger switch
            {
                AlertTrigger.OnFailure => $"Pipeline '{log.PipelineName}' failed: {log.ErrorMessage}",
                AlertTrigger.OnDQThreshold => $"DQ pass rate {log.DQPassRate:P1} breached threshold (Run {log.RunId})",
                AlertTrigger.OnRejectedThreshold => $"{log.RecordsRejected:N0} rows rejected (Run {log.RunId})",
                AlertTrigger.OnDurationThreshold => $"Run {log.RunId} took {log.Duration:g}",
                _ => $"Alert '{rule.Name}' triggered for pipeline '{log.PipelineName}' (Run {log.RunId})"
            };
        }

        private static double TryParseHoursThreshold(string? condition)
        {
            // Condition like "HoursWithoutRun > 2"
            if (string.IsNullOrWhiteSpace(condition)) return 0;
            var raw = ExtractOperatorAndValue(condition, "HoursWithoutRun").TrimStart('>', '<', '=', ' ');
            return double.TryParse(raw.Trim(), out double v) ? v : 0;
        }

        private async Task FireNotifiersAsync(
            AlertRule rule, AlertEvent evt, CancellationToken token)
        {
            foreach (var pluginId in rule.NotifierPluginIds)
            {
                var notifier = _notifierFactory(pluginId);
                if (notifier == null) continue;

                if (rule.NotifierConfig.Count > 0)
                    notifier.Configure(rule.NotifierConfig);

                try
                {
                    await notifier.NotifyAsync(evt, token).ConfigureAwait(false);
                }
                catch { /* notifier errors are swallowed — already logged internally */ }
            }
        }
    }
}
