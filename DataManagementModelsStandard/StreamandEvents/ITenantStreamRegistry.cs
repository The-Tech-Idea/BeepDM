using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Persistent registry of tenant stream contexts.
    /// Tracks lifecycle state (active, suspended, retired) and emits audit events for
    /// any state transition.  The in-memory implementation is <c>InMemoryTenantRegistry</c>.
    /// </summary>
    public interface ITenantStreamRegistry
    {
        /// <summary>
        /// Registers a new tenant.  Idempotent — calling with an already-registered tenant ID
        /// leaves the existing record unchanged and does not emit a second Registered event.
        /// </summary>
        Task RegisterAsync(TenantStreamContext context, CancellationToken ct = default);

        /// <summary>
        /// Returns the current <see cref="TenantStreamContext"/> for <paramref name="tenantId"/>,
        /// or <c>null</c> if the tenant is unknown.
        /// </summary>
        Task<TenantStreamContext?> GetAsync(string tenantId, CancellationToken ct = default);

        /// <summary>Returns all tenants whose <see cref="TenantStreamContext.IsActive"/> flag is <c>true</c>.</summary>
        Task<IReadOnlyList<TenantStreamContext>> ListActiveAsync(CancellationToken ct = default);

        /// <summary>
        /// Suspends <paramref name="tenantId"/>, preventing further publishing and consuming.
        /// The <paramref name="reason"/> is stored and surfaced via <see cref="TenantStreamEvent"/>.
        /// No-op if the tenant is unknown.
        /// </summary>
        Task SuspendAsync(string tenantId, string reason, CancellationToken ct = default);

        /// <summary>
        /// Re-activates a previously suspended tenant.
        /// No-op if the tenant is unknown or already active.
        /// </summary>
        Task ActivateAsync(string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Permanently removes the tenant from the registry.
        /// Subsequent <see cref="GetAsync"/> calls for this ID return <c>null</c>.
        /// No-op if the tenant is unknown.
        /// </summary>
        Task RetireAsync(string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Returns an async stream of lifecycle events (Registered, Suspended, Activated, Retired).
        /// The stream continues indefinitely until <paramref name="ct"/> is cancelled.
        /// Multiple callers may subscribe simultaneously — each receives all future events.
        /// </summary>
        IAsyncEnumerable<TenantStreamEvent> StreamLifecycleEventsAsync(CancellationToken ct = default);

        /// <summary>
        /// Returns <c>true</c> if the tenant exists and <see cref="TenantStreamContext.IsActive"/> is <c>true</c>.
        /// Never throws for unknown tenant IDs — returns <c>false</c> instead.
        /// </summary>
        Task<bool> IsTenantActiveAsync(string tenantId, CancellationToken ct = default);
    }
}
