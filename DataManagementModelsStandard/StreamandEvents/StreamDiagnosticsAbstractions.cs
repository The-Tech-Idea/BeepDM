using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Testable Clock ────────────────────────────────────────────────────────────

    /// <summary>
    /// Abstracts wall-clock access for streaming components.
    /// Inject a test double to advance time in unit tests without real delays.
    /// </summary>
    public interface IStreamingRuntimeClock
    {
        DateTimeOffset UtcNow { get; }
        Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken = default);
    }

    /// <summary>Production clock backed by <see cref="DateTimeOffset.UtcNow"/> and <see cref="Task.Delay"/>.</summary>
    public sealed class SystemStreamingClock : IStreamingRuntimeClock
    {
        public static readonly SystemStreamingClock Instance = new();

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken = default) =>
            Task.Delay(duration, cancellationToken);
    }

    // ── Diagnostics Writer ────────────────────────────────────────────────────────

    /// <summary>
    /// Broker-neutral diagnostics abstraction.
    /// Implementations can forward to OTEL <c>Activity</c>, <c>Meter</c>, or any structured log sink.
    /// The engine calls these hooks at natural observation points; implementations are
    /// fully optional and default to no-op via <see cref="NullStreamingDiagnostics"/>.
    /// </summary>
    public interface IStreamingDiagnosticsWriter
    {
        /// <summary>Records a structured event (log) at the given severity.</summary>
        void WriteEvent(DiagnosticsEventLevel level, string eventName, string topic, string? detail = null);

        /// <summary>Increments a named counter for the given topic and optional consumer group.</summary>
        void IncrementCounter(string metricName, string topic, string? consumerGroup = null, long delta = 1);

        /// <summary>Records a duration sample (histogram) in milliseconds.</summary>
        void RecordDuration(string metricName, string topic, double milliseconds, string? consumerGroup = null);

        /// <summary>Starts a trace span for a produce or consume operation. Dispose the handle to end the span.</summary>
        IDisposable StartSpan(string operationName, string topic, string? eventId = null);
    }

    public enum DiagnosticsEventLevel { Trace, Debug, Info, Warning, Error, Critical }

    /// <summary>No-op diagnostics writer. Default when no real sink is registered.</summary>
    public sealed class NullStreamingDiagnostics : IStreamingDiagnosticsWriter
    {
        public static readonly NullStreamingDiagnostics Instance = new();

        public void WriteEvent(DiagnosticsEventLevel level, string eventName, string topic, string? detail = null) { }
        public void IncrementCounter(string metricName, string topic, string? consumerGroup = null, long delta = 1) { }
        public void RecordDuration(string metricName, string topic, double milliseconds, string? consumerGroup = null) { }
        public IDisposable StartSpan(string operationName, string topic, string? eventId = null) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }

    // ── Lint Results (used by CI contract checks) ─────────────────────────────────

    public enum LintSeverity { Info, Warning, Error }

    /// <summary>
    /// A single lint finding from a schema or topic contract check.
    /// <see cref="LintSeverity.Error"/> findings must block CI merges.
    /// </summary>
    public sealed record StreamLintResult
    {
        public required LintSeverity   Severity  { get; init; }
        public required string         Code      { get; init; }   // e.g. "TOPIC_NAME_INVALID"
        public required string         Message   { get; init; }
        public string?                 Target    { get; init; }   // topic name, schema id, field name, etc.
    }
}
