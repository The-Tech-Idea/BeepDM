using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using TheTechIdea.Beep.Services.Telemetry.Sinks;

namespace TheTechIdea.Beep.Services.Telemetry.Retention
{
    /// <summary>
    /// Default <see cref="IBudgetEnforcer"/> implementation. Manages a
    /// dictionary of <see cref="EnforcerScope"/> instances keyed by
    /// directory and orchestrates compress / sweep work for each scope.
    /// </summary>
    /// <remarks>
    /// Split into three partial files:
    /// <list type="bullet">
    ///   <item><c>.Core</c> — fields, ctor, scope registry, dispose.</item>
    ///   <item><c>.Sweep</c> — age / count / budget enforcement loop.</item>
    ///   <item><c>.Compress</c> — gzip-on-rotate handler hooked from sinks.</item>
    /// </list>
    /// All disk IO runs on the calling task; the
    /// <see cref="RetentionSweeperHostedService"/> drives a periodic
    /// invocation of <see cref="EnforceAllAsync"/>. Sinks may also
    /// trigger work synchronously through <see cref="AttachSink"/>.
    /// </remarks>
    public sealed partial class DefaultBudgetEnforcer : IBudgetEnforcer
    {
        private readonly ConcurrentDictionary<string, EnforcerScope> _scopes = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, bool> _blocked = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<FileRollingSink, EnforcerScope> _attachedSinks = new();
        private int _disposed;

        /// <inheritdoc />
        public IReadOnlyCollection<EnforcerScope> Scopes => (IReadOnlyCollection<EnforcerScope>)_scopes.Values;

        /// <inheritdoc />
        public event Action<BudgetSweepResult> Swept;

        /// <inheritdoc />
        public void RegisterScope(EnforcerScope scope)
        {
            if (scope is null)
            {
                throw new ArgumentNullException(nameof(scope));
            }
            if (string.IsNullOrWhiteSpace(scope.Directory))
            {
                throw new ArgumentException("Scope.Directory must be supplied.", nameof(scope));
            }

            string key = NormalizeKey(scope.Directory);
            _scopes[key] = scope;
        }

        /// <inheritdoc />
        public bool UnregisterScope(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }
            string key = NormalizeKey(directory);
            _blocked.TryRemove(key, out _);
            return _scopes.TryRemove(key, out _);
        }

        /// <inheritdoc />
        public void AttachSink(FileRollingSink sink, EnforcerScope scope)
        {
            if (sink is null)
            {
                throw new ArgumentNullException(nameof(sink));
            }
            if (scope is null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            RegisterScope(scope);

            if (_attachedSinks.TryAdd(sink, scope))
            {
                sink.Rolled += OnSinkRolled;
            }
        }

        /// <inheritdoc />
        public bool IsBlocked(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }
            return _blocked.TryGetValue(NormalizeKey(directory), out bool blocked) && blocked;
        }

        /// <summary>Resolves the scope registered for a given directory, or <c>null</c>.</summary>
        public EnforcerScope FindScope(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }
            return _scopes.TryGetValue(NormalizeKey(directory), out EnforcerScope scope) ? scope : null;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            foreach (KeyValuePair<FileRollingSink, EnforcerScope> kv in _attachedSinks)
            {
                try
                {
                    kv.Key.Rolled -= OnSinkRolled;
                }
                catch
                {
                    // best-effort detach
                }
            }
            _attachedSinks.Clear();
            _scopes.Clear();
            _blocked.Clear();
        }

        private static string NormalizeKey(string directory)
        {
            try
            {
                return Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);
            }
            catch
            {
                return directory;
            }
        }

        private void RaiseSwept(BudgetSweepResult result)
        {
            Action<BudgetSweepResult> handler = Swept;
            if (handler is null || result is null)
            {
                return;
            }
            try
            {
                handler(result);
            }
            catch
            {
                // Subscribers are best-effort; never let observers break the sweeper.
            }
        }
    }
}
