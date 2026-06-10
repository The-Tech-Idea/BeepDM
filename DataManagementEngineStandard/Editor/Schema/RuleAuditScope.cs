using System;
using System.Threading;
using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Editor.Schema
{
    /// <summary>
    /// Disposable scope that increments a counter every time the bound rule engine
    /// fires <see cref="IRuleEngine.RuleEvaluated"/>. Used by callers that need to
    /// report "how many rules fired during this run" without leaking the
    /// subscribe / unsubscribe pair across multiple early-exit paths.
    ///
    /// <para>
    /// Usage:
    /// </para>
    /// <code>
    /// int ruleAuditCount = 0;
    /// using (RuleAuditScope.Count(ruleEngine, () => Interlocked.Increment(ref ruleAuditCount)))
    /// {
    ///     // ... invoke rules ...
    /// }
    /// // ruleAuditCount now reflects the number of evaluations that happened in the scope.
    /// </code>
    /// </summary>
    public struct RuleAuditScope : IDisposable
    {
        // The dispose state lives on the heap so we can use Interlocked.Exchange
        // (you can't Interlocked.Exchange a field on a `readonly` struct).
        private readonly RuleAuditScopeState? _state;

        private RuleAuditScope(RuleAuditScopeState state) => _state = state;

        /// <summary>
        /// Subscribe <paramref name="onEvaluated"/> to the engine's
        /// <see cref="IRuleEngine.RuleEvaluated"/> event. The returned
        /// <see cref="RuleAuditScope"/> detaches on dispose. Pass <c>null</c> for
        /// <paramref name="onEvaluated"/> to count evaluations without doing anything
        /// per-evaluation.
        /// </summary>
        public static RuleAuditScope Count(IRuleEngine? engine, Action? onEvaluated = null)
        {
            if (engine == null) return default;

            var state = new RuleAuditScopeState(engine);
            EventHandler<RuleAuditEventArgs> handler = (s, e) =>
            {
                if (onEvaluated != null) onEvaluated();
                state.Increment();
            };
            engine.RuleEvaluated += handler;
            state.Handler = handler;
            return new RuleAuditScope(state);
        }

        /// <summary>Detach the handler. Safe to call multiple times.</summary>
        public void Dispose()
        {
            var state = _state;
            if (state == null) return;
            if (state.TryDispose()) return;
            // Already disposed
        }

        // Internal heap-allocated state so Interlocked can manage the dispose flag
        // and the increment counter together without requiring a `readonly` struct.
        private sealed class RuleAuditScopeState
        {
            private readonly IRuleEngine _engine;
            private int _disposed;
            private int _count;
            public EventHandler<RuleAuditEventArgs>? Handler { get; set; }

            public RuleAuditScopeState(IRuleEngine engine) => _engine = engine;

            public void Increment() => Interlocked.Increment(ref _count);
            public int Count => _count;

            /// <summary>Returns true on first dispose (caller should detach), false on subsequent disposes.</summary>
            public bool TryDispose() => Interlocked.Exchange(ref _disposed, 1) == 0;
        }
    }
}
