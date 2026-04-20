using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Telemetry.Context;

namespace TheTechIdea.Beep.Services.Logging
{
    /// <summary>
    /// Convenience extension methods that let callers open an ambient
    /// <see cref="BeepActivityScope"/> directly off an
    /// <see cref="IBeepLog"/> reference. The log instance itself is not
    /// stored on the scope — the scope is purely an async-local frame —
    /// so disposing the returned handle never affects the log.
    /// </summary>
    /// <remarks>
    /// Idiomatic usage:
    /// <code>
    /// using (log.Scope("Order.Submit", new Dictionary&lt;string, object&gt; { ["orderId"] = id }))
    /// {
    ///     log.Info("Submitting order");
    ///     await ProcessAsync();
    /// }
    /// </code>
    /// Every event produced inside the <c>using</c> block (including those
    /// emitted from inner <c>await</c>-laden helpers) carries the same
    /// trace/correlation ids as long as the configured enrichers include
    /// <see cref="CorrelationEnricher"/>, <see cref="TraceEnricher"/>, or
    /// <see cref="ActivityScopeEnricher"/>.
    /// </remarks>
    public static class BeepLogScopeExtensions
    {
        /// <summary>
        /// Opens a <see cref="BeepActivityScope"/> with the supplied name and
        /// optional tags. Returns a no-op handle when the log is null,
        /// disabled, or the name is empty so callers can guard with
        /// <c>using</c> unconditionally.
        /// </summary>
        public static IDisposable Scope(this IBeepLog log, string name, IDictionary<string, object> tags = null)
        {
            if (log is null || !log.IsEnabled || string.IsNullOrEmpty(name))
            {
                return EmptyScope.Instance;
            }
            return BeepActivityScope.Begin(name, tags);
        }

        private sealed class EmptyScope : IDisposable
        {
            public static readonly EmptyScope Instance = new EmptyScope();
            public void Dispose() { }
        }
    }
}
