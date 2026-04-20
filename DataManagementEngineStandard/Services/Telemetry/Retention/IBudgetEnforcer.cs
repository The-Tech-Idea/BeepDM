using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Telemetry.Sinks;

namespace TheTechIdea.Beep.Services.Telemetry.Retention
{
    /// <summary>
    /// Owns the on-disk lifecycle of telemetry files: compresses on
    /// rotate, sweeps by age / count / budget, and surfaces budget
    /// breaches. Implementations must be thread-safe; the hosted service
    /// and ad-hoc callers may invoke them concurrently.
    /// </summary>
    /// <remarks>
    /// One enforcer instance typically governs many directories. Sinks
    /// (e.g. <see cref="FileRollingSink"/>) call <see cref="AttachSink"/>
    /// once at construction so the enforcer can subscribe to their
    /// <see cref="FileRollingSink.Rolled"/> events and compress / enforce
    /// without a polling loop.
    /// </remarks>
    public interface IBudgetEnforcer : IDisposable
    {
        /// <summary>Registers a directory to sweep on the next pass.</summary>
        void RegisterScope(EnforcerScope scope);

        /// <summary>Removes a previously registered scope.</summary>
        bool UnregisterScope(string directory);

        /// <summary>
        /// Subscribes the enforcer to a sink's rotation event so that
        /// gzip compression and an immediate enforcement pass run as
        /// soon as a file rolls.
        /// </summary>
        void AttachSink(FileRollingSink sink, EnforcerScope scope);

        /// <summary>Snapshot of the registered scopes.</summary>
        IReadOnlyCollection<EnforcerScope> Scopes { get; }

        /// <summary>
        /// Sweeps a single scope (age, count, then budget). Idempotent;
        /// safe to call any time.
        /// </summary>
        Task<BudgetSweepResult> EnforceAsync(string directory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sweeps every registered scope. Errors are isolated per scope
        /// and surfaced through <see cref="Swept"/>.
        /// </summary>
        Task<IReadOnlyList<BudgetSweepResult>> EnforceAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Compresses a freshly rolled file when the scope's budget has
        /// <see cref="StorageBudget.CompressOnRotate"/> on. Safe to call
        /// repeatedly for the same file (no-op once compressed).
        /// </summary>
        Task<bool> CompressIfNeededAsync(RolledFile rolled, CancellationToken cancellationToken = default);

        /// <summary>
        /// True when the supplied <paramref name="directory"/> is
        /// currently in <see cref="BudgetBreachAction.BlockNewWrites"/>
        /// state. Audit producers should call this and fail-fast when it
        /// returns <c>true</c>.
        /// </summary>
        bool IsBlocked(string directory);

        /// <summary>Raised after every sweep (success or breach).</summary>
        event Action<BudgetSweepResult> Swept;
    }
}
