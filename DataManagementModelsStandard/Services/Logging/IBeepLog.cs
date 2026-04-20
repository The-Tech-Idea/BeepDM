using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Logging
{
    /// <summary>
    /// Unified, opt-in, structured logging contract for the Beep stack.
    /// Implementations route entries through the shared telemetry pipeline
    /// (queue, enrichers, redactors, sampler, sinks).
    /// </summary>
    /// <remarks>
    /// When the logging feature is disabled, <see cref="IsEnabled"/> is <c>false</c>
    /// and every method is a fast no-op (see the null implementation supplied by
    /// the engine project).
    /// </remarks>
    public interface IBeepLog
    {
        /// <summary>
        /// Returns <c>true</c> when the logging feature has been activated.
        /// Callers may short-circuit expensive property bag construction when
        /// this is <c>false</c>.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// The minimum severity that will be processed. Entries below this
        /// level are dropped before enrichment.
        /// </summary>
        BeepLogLevel MinLevel { get; }

        /// <summary>
        /// Records a structured log entry.
        /// </summary>
        /// <param name="level">Severity classification.</param>
        /// <param name="category">Logical category (component or feature).</param>
        /// <param name="message">Human-readable message; may be a template.</param>
        /// <param name="properties">Optional structured property bag.</param>
        /// <param name="exception">Optional exception to attach.</param>
        void Log(
            BeepLogLevel level,
            string category,
            string message,
            IReadOnlyDictionary<string, object> properties = null,
            Exception exception = null);

        /// <summary>Convenience wrapper for <see cref="BeepLogLevel.Trace"/>.</summary>
        void Trace(string message, object properties = null);

        /// <summary>Convenience wrapper for <see cref="BeepLogLevel.Debug"/>.</summary>
        void Debug(string message, object properties = null);

        /// <summary>Convenience wrapper for <see cref="BeepLogLevel.Information"/>.</summary>
        void Info(string message, object properties = null);

        /// <summary>Convenience wrapper for <see cref="BeepLogLevel.Warning"/>.</summary>
        void Warn(string message, object properties = null);

        /// <summary>Convenience wrapper for <see cref="BeepLogLevel.Error"/>.</summary>
        void Error(string message, Exception ex = null, object properties = null);

        /// <summary>Convenience wrapper for <see cref="BeepLogLevel.Critical"/>.</summary>
        void Critical(string message, Exception ex = null, object properties = null);

        /// <summary>
        /// Drains the in-memory queue and awaits each sink's flush.
        /// Used during clean shutdown.
        /// </summary>
        Task FlushAsync(CancellationToken cancellationToken = default);
    }
}
