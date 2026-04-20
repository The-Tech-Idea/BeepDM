using System;
using TheTechIdea.Beep.Distributed.Observability;
using TheTechIdea.Beep.Distributed.Performance;
using TheTechIdea.Beep.Distributed.Routing;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — Phase 14 capacity
    /// engineering surface: global + per-shard concurrency gates,
    /// per-shard token-bucket rate limiter, adaptive timeouts, and
    /// hot-shard read shedding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The gate, limiter, and mitigator are lazy-built on first use so
    /// tests that opt out of Phase 14 allocate nothing. The mitigator
    /// attaches itself to the metrics aggregator exactly once per
    /// <see cref="DistributedDataSource"/> lifetime.
    /// </para>
    /// <para>
    /// All helpers return safely when
    /// <see cref="PerformanceOptions.EnableCapacityGates"/> is
    /// <c>false</c> so the executors can unconditionally call into
    /// this partial without branching every line.
    /// </para>
    /// </remarks>
    public partial class DistributedDataSource
    {
        private DistributedConcurrencyGate _concurrencyGate;
        private IDistributedRateLimiter    _rateLimiter;
        private AdaptiveTimeoutCalculator  _adaptiveTimeouts;
        private HotShardMitigator          _hotShardMitigator;
        private readonly object            _performanceLock = new object();

        /// <summary>Performance option block for the active instance.</summary>
        public PerformanceOptions PerformanceOptions => _options.Performance ?? (_options.Performance = new PerformanceOptions());

        /// <summary>Global + per-shard concurrency gate.</summary>
        public DistributedConcurrencyGate ConcurrencyGate
        {
            get { EnsurePerformanceWired(); return _concurrencyGate; }
        }

        /// <summary>Per-shard rate limiter.</summary>
        public IDistributedRateLimiter RateLimiter
        {
            get { EnsurePerformanceWired(); return _rateLimiter; }
        }

        /// <summary>Per-shard adaptive timeout calculator.</summary>
        public AdaptiveTimeoutCalculator AdaptiveTimeouts
        {
            get { EnsurePerformanceWired(); return _adaptiveTimeouts; }
        }

        /// <summary>Hot-shard read-shedding mitigator.</summary>
        public HotShardMitigator HotShardMitigator
        {
            get { EnsurePerformanceWired(); return _hotShardMitigator; }
        }

        /// <summary>
        /// Raised when a Phase 14 capacity gate rejects a call.
        /// Subscribers run synchronously — keep handlers cheap.
        /// </summary>
        public event EventHandler<BackpressureEventArgs> OnBackpressure;

        internal void RaiseBackpressure(
            string                gateName,
            string                shardId,
            BackpressureException exception)
        {
            try
            {
                OnBackpressure?.Invoke(this, new BackpressureEventArgs(
                    gateName:   gateName,
                    shardId:    shardId,
                    entityName: null,
                    operation:  null,
                    retryAfter: exception?.RetryAfter ?? TimeSpan.Zero,
                    exception:  exception));
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("OnBackpressure handler failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Replaces the concurrency gate at runtime. Hands in-flight
        /// callers back to the old gate; future callers see the new
        /// caps immediately. Pass <c>null</c> to restore defaults.
        /// </summary>
        public void ConfigureConcurrencyGate(DistributedConcurrencyGate gate)
        {
            lock (_performanceLock)
            {
                var old = _concurrencyGate;
                _concurrencyGate = gate ?? BuildDefaultConcurrencyGate();
                try { old?.Dispose(); } catch { /* best-effort */ }
            }
        }

        /// <summary>Replaces the rate limiter at runtime.</summary>
        public void ConfigureRateLimiter(IDistributedRateLimiter limiter)
        {
            lock (_performanceLock)
            {
                _rateLimiter = limiter ?? BuildDefaultRateLimiter();
            }
        }

        /// <summary>
        /// Returns the adaptive deadline for <paramref name="shardId"/>,
        /// honouring the caller's explicit budget (never shortens
        /// below <paramref name="fallbackMs"/>).
        /// </summary>
        internal int ComputeShardDeadlineMs(string shardId, int fallbackMs)
        {
            EnsurePerformanceWired();
            if (_adaptiveTimeouts == null) return fallbackMs;
            return _adaptiveTimeouts.ComputeDeadlineMs(shardId, fallbackMs);
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="shardId"/> is
        /// currently flagged hot and the placement supports read
        /// shedding (<see cref="RoutingDecision.Mode"/> is
        /// <see cref="DistributionMode.Replicated"/> or
        /// <see cref="DistributionMode.Broadcast"/>).
        /// </summary>
        internal bool ShouldShedReadForDecision(RoutingDecision decision, string shardId)
        {
            if (decision == null) return false;
            if (!PerformanceOptions.EnableHotShardReadShedding) return false;
            if (string.IsNullOrWhiteSpace(shardId)) return false;

            bool canShed = decision.Mode == DistributionMode.Replicated
                        || decision.Mode == DistributionMode.Broadcast;
            if (!canShed) return false;

            EnsurePerformanceWired();
            return _hotShardMitigator != null && _hotShardMitigator.ShouldShedRead(shardId, canShed: true);
        }

        /// <summary>
        /// Acquires the global distributed permit, honouring
        /// <see cref="PerformanceOptions.DistributedPermitWait"/>.
        /// Returns a no-op permit when gates are disabled.
        /// </summary>
        internal IDisposable AcquireDistributedCallPermit(System.Threading.CancellationToken cancellationToken = default)
        {
            if (!PerformanceOptions.EnableCapacityGates) return NullPermit.Instance;
            EnsurePerformanceWired();
            try
            {
                return _concurrencyGate.AcquireDistributed(PerformanceOptions.DistributedPermitWait, cancellationToken);
            }
            catch (BackpressureException bp)
            {
                RaiseBackpressure(bp.GateName, shardId: null, bp);
                throw;
            }
        }

        /// <summary>
        /// Acquires a per-shard permit and also consumes one token
        /// from the per-shard rate limiter. Throws
        /// <see cref="BackpressureException"/> when either control
        /// rejects the call.
        /// </summary>
        internal IDisposable AcquireShardCallPermit(string shardId, System.Threading.CancellationToken cancellationToken = default)
        {
            if (!PerformanceOptions.EnableCapacityGates || string.IsNullOrWhiteSpace(shardId)) return NullPermit.Instance;
            EnsurePerformanceWired();

            try
            {
                // Rate limiter first — cheaper to reject upstream than
                // hold a semaphore while waiting for a token.
                _rateLimiter?.AcquireAsync(shardId, PerformanceOptions.ShardPermitWait, cancellationToken).GetAwaiter().GetResult();
                return _concurrencyGate.AcquireShard(shardId, PerformanceOptions.ShardPermitWait, cancellationToken);
            }
            catch (BackpressureException bp)
            {
                RaiseBackpressure(bp.GateName, shardId, bp);
                throw;
            }
        }

        /// <summary>Async variant of <see cref="AcquireShardCallPermit"/>.</summary>
        internal async System.Threading.Tasks.Task<IDisposable> AcquireShardCallPermitAsync(
            string                                shardId,
            System.Threading.CancellationToken    cancellationToken = default)
        {
            if (!PerformanceOptions.EnableCapacityGates || string.IsNullOrWhiteSpace(shardId))
                return NullPermit.Instance;

            EnsurePerformanceWired();
            try
            {
                if (_rateLimiter != null)
                {
                    await _rateLimiter.AcquireAsync(shardId, PerformanceOptions.ShardPermitWait, cancellationToken).ConfigureAwait(false);
                }
                return await _concurrencyGate.AcquireShardAsync(shardId, PerformanceOptions.ShardPermitWait, cancellationToken).ConfigureAwait(false);
            }
            catch (BackpressureException bp)
            {
                RaiseBackpressure(bp.GateName, shardId, bp);
                throw;
            }
        }

        // ── Build / wiring ────────────────────────────────────────────────

        private void EnsurePerformanceWired()
        {
            if (_concurrencyGate != null && _rateLimiter != null && _adaptiveTimeouts != null && _hotShardMitigator != null)
                return;

            lock (_performanceLock)
            {
                if (_concurrencyGate == null)
                {
                    _concurrencyGate = BuildDefaultConcurrencyGate();
                }
                if (_rateLimiter == null)
                {
                    _rateLimiter = _options.RateLimiter ?? BuildDefaultRateLimiter();
                }
                if (_adaptiveTimeouts == null)
                {
                    _adaptiveTimeouts = new AdaptiveTimeoutCalculator(
                        aggregatorAccessor: () => _metricsAggregator,
                        options:            PerformanceOptions);
                }
                if (_hotShardMitigator == null)
                {
                    _hotShardMitigator = new HotShardMitigator(
                        enabled:  PerformanceOptions.EnableHotShardReadShedding,
                        cooldown: TimeSpan.FromSeconds(30));

                    // Attach to the live aggregator (lazy-build it if
                    // metrics are enabled; otherwise the mitigator
                    // just stays idle).
                    if (_options.EnableDistributedMetrics)
                    {
                        EnsureMetricsAggregator();
                        _hotShardMitigator.Attach(_metricsAggregator);
                    }
                }
            }
        }

        private DistributedConcurrencyGate BuildDefaultConcurrencyGate()
        {
            var perf = PerformanceOptions;
            return new DistributedConcurrencyGate(
                maxConcurrentDistributedCalls: perf.MaxConcurrentDistributedCalls,
                maxConcurrentCallsPerShard:    perf.MaxConcurrentCallsPerShard);
        }

        private IDistributedRateLimiter BuildDefaultRateLimiter()
        {
            var perf = PerformanceOptions;
            return new TokenBucketRateLimiter(
                ratePerSecond: perf.ShardRateLimitPerSecond,
                burst:         perf.ShardRateLimitBurst);
        }

        private sealed class NullPermit : IDisposable
        {
            internal static readonly NullPermit Instance = new NullPermit();
            public void Dispose() { }
        }
    }
}
