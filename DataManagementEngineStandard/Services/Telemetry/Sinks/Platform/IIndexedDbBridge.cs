using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks.Platform
{
    /// <summary>
    /// Host-supplied JS-interop seam for the
    /// <see cref="BlazorIndexedDbSink"/>. The core library never
    /// references <c>Microsoft.JSInterop</c> directly so the Telemetry
    /// pipeline keeps building on every TFM (net8 / net9 / net10) and
    /// non-Blazor hosts pay nothing.
    /// </summary>
    /// <remarks>
    /// Implementations live in the Blazor host project and wrap calls
    /// like <c>IJSRuntime.InvokeAsync("beepTelemetry.put", …)</c> to
    /// the bundled <c>wwwroot/beep-telemetry/beep-indexeddb.js</c>
    /// helper. Every method must be safe to call concurrently from
    /// the batch-writer drain thread.
    /// </remarks>
    public interface IIndexedDbBridge
    {
        /// <summary>
        /// Persists <paramref name="lines"/> (one envelope per line,
        /// already serialized by the sink) into the underlying object
        /// store. Implementations should batch into a single
        /// transaction when the bridge supports it.
        /// </summary>
        Task PutBatchAsync(IReadOnlyList<string> lines, CancellationToken cancellationToken);

        /// <summary>
        /// Returns an estimate (bytes) of the storage currently used
        /// by Beep telemetry inside IndexedDB. Used by the sink to
        /// decide when to call <see cref="PruneOldestAsync"/>. May
        /// return 0 if the bridge cannot estimate cheaply.
        /// </summary>
        Task<long> EstimateUsedBytesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Deletes envelopes from the oldest end of the store until
        /// at least <paramref name="bytesToFree"/> are released.
        /// Implementations may free more than requested.
        /// </summary>
        Task PruneOldestAsync(long bytesToFree, CancellationToken cancellationToken);

        /// <summary>
        /// Forces any pending writes through to IDB. Called during
        /// the pipeline shutdown drain.
        /// </summary>
        Task FlushAsync(CancellationToken cancellationToken);
    }
}
