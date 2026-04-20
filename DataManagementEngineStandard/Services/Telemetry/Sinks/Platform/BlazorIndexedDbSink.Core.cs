using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Telemetry.Diagnostics;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks.Platform
{
    /// <summary>
    /// Telemetry sink that persists envelopes inside the browser's
    /// IndexedDB store via a host-supplied <see cref="IIndexedDbBridge"/>.
    /// The sink itself is portable C# so it builds on every TFM; the JS
    /// interop is delegated to the host package
    /// <c>wwwroot/beep-telemetry/beep-indexeddb.js</c>.
    /// </summary>
    /// <remarks>
    /// Split into three partial files:
    /// <list type="bullet">
    ///   <item><c>.Core</c> — fields, ctor, public state, dispose.</item>
    ///   <item><c>.Write</c> — batch serialization + IDB put + budget pruning.</item>
    ///   <item><c>.Health</c> — <see cref="ISinkHealthProbe"/> surface.</item>
    /// </list>
    /// </remarks>
    public sealed partial class BlazorIndexedDbSink : ITelemetrySink, IAsyncDisposable
    {
        /// <summary>Default soft-cap percentage that triggers proactive pruning.</summary>
        public const int DefaultPruneThresholdPercent = 80;

        private readonly IIndexedDbBridge _bridge;
        private readonly long _storageBudgetBytes;
        private readonly int _pruneThresholdPercent;
        private readonly SemaphoreSlim _writeGate = new SemaphoreSlim(1, 1);

        private long _writtenCount;
        private long _prunedCount;
        private bool _healthy = true;
        private string _lastError;
        private int _disposed;

        /// <summary>
        /// Creates a new sink. <paramref name="storageBudgetBytes"/>
        /// is the soft cap; the sink prunes oldest envelopes whenever
        /// the bridge reports usage above
        /// <c>storageBudgetBytes * pruneThresholdPercent / 100</c>.
        /// </summary>
        public BlazorIndexedDbSink(
            IIndexedDbBridge bridge,
            long storageBudgetBytes = PlatformDefaults.BlazorBudgetBytes,
            int pruneThresholdPercent = DefaultPruneThresholdPercent,
            string name = "blazor-idb")
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            if (storageBudgetBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(storageBudgetBytes));
            }
            if (pruneThresholdPercent <= 0 || pruneThresholdPercent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(pruneThresholdPercent));
            }
            Name = string.IsNullOrWhiteSpace(name) ? "blazor-idb" : name;
            _storageBudgetBytes = storageBudgetBytes;
            _pruneThresholdPercent = pruneThresholdPercent;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>Configured soft cap (bytes).</summary>
        public long StorageBudgetBytes => _storageBudgetBytes;

        /// <summary>Soft-cap percentage that triggers proactive pruning.</summary>
        public int PruneThresholdPercent => _pruneThresholdPercent;

        /// <inheritdoc />
        public bool IsHealthy => Volatile.Read(ref _healthy);

        /// <summary>Total envelopes successfully written into IDB.</summary>
        public long WrittenCount => Interlocked.Read(ref _writtenCount);

        /// <summary>Total envelopes proactively pruned by the budget guard.</summary>
        public long PrunedCount => Interlocked.Read(ref _prunedCount);

        /// <summary>Most recent IDB error message, or <c>null</c>.</summary>
        public string LastError => Volatile.Read(ref _lastError);

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            try
            {
                await _bridge.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // best-effort during dispose
            }
            _writeGate.Dispose();
        }

        private void MarkUnhealthy(Exception ex)
        {
            Volatile.Write(ref _healthy, false);
            Volatile.Write(ref _lastError, ex.Message);
            RecordError();
        }

        private void MarkHealthy()
        {
            Volatile.Write(ref _healthy, true);
            RecordSuccess();
        }
    }

    /// <summary>
    /// Static link between <see cref="BlazorIndexedDbSink"/>'s default
    /// budget and the centralized <c>PlatformBudgets</c> table. Kept as
    /// a tiny shim so the sink ctor can default a const without taking
    /// a hard dependency on the Presets namespace.
    /// </summary>
    internal static class PlatformDefaults
    {
        internal const long BlazorBudgetBytes = 5L * 1024 * 1024;
    }
}
