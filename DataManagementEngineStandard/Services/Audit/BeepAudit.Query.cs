using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Integrity;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit
{
    /// <summary>
    /// Query / purge / integrity surface of <see cref="BeepAudit"/>.
    /// Phase 10 wires every method to the late-bound query engine and
    /// purge service supplied by the DI extension. When the audit
    /// feature is disabled the methods short-circuit; when the
    /// dependencies are missing they throw an explanatory
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    public sealed partial class BeepAudit
    {
        private const string MissingQueryEngine =
            "BeepAudit query engine is not registered. Add a query-capable sink (SqliteSink or FileScan) and call AddBeepAudit so the engine is wired.";

        private const string MissingPurgeService =
            "BeepAudit purge service is not registered. Configure BeepAuditOptions.PurgeConfirmationToken (or supply an IPurgePolicy) and a purge-capable sink (SqliteSink).";

        private const string MissingIntegrityVerifier =
            "BeepAudit integrity verifier is not registered. Enable BeepAuditOptions.HashChain and supply at least one query-capable sink.";

        /// <inheritdoc />
        public async Task<IReadOnlyList<AuditEvent>> QueryAsync(
            AuditQuery filter,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                return Array.Empty<AuditEvent>();
            }
            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }
            if (_queryEngine is null)
            {
                throw new InvalidOperationException(MissingQueryEngine);
            }

            await FlushAsync(cancellationToken).ConfigureAwait(false);
            return await _queryEngine.ExecuteAsync(filter, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task PurgeByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || string.IsNullOrEmpty(userId))
            {
                return;
            }
            if (_purgeService is null)
            {
                throw new InvalidOperationException(MissingPurgeService);
            }
            await FlushAsync(cancellationToken).ConfigureAwait(false);
            await _purgeService.PurgeByUserAsync(userId, _options.PurgeConfirmationToken, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task PurgeByEntityAsync(
            string entityName,
            string recordKey,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || string.IsNullOrEmpty(entityName) || string.IsNullOrEmpty(recordKey))
            {
                return;
            }
            if (_purgeService is null)
            {
                throw new InvalidOperationException(MissingPurgeService);
            }
            await FlushAsync(cancellationToken).ConfigureAwait(false);
            await _purgeService.PurgeByEntityAsync(entityName, recordKey, _options.PurgeConfirmationToken, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> VerifyIntegrityAsync(CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                return true;
            }
            if (_integrityVerifier is null || _queryEngine is null)
            {
                throw new InvalidOperationException(MissingIntegrityVerifier);
            }

            await FlushAsync(cancellationToken).ConfigureAwait(false);

            // Verify every chain that has produced at least one event.
            // We discover chains via the query engine because chain-aware
            // sinks (SQLite) keep the chain id in a dedicated column /
            // JSON property and the audit pipeline never enumerates them
            // directly.
            AuditQuery sweep = new AuditQuery().TakeMax(int.MaxValue);
            IReadOnlyList<AuditEvent> all =
                await _queryEngine.ExecuteAsync(sweep, cancellationToken).ConfigureAwait(false);

            var chains = new Dictionary<string, List<AuditEvent>>(StringComparer.Ordinal);
            for (int i = 0; i < all.Count; i++)
            {
                AuditEvent ev = all[i];
                if (ev is null) { continue; }
                string chainId = string.IsNullOrEmpty(ev.ChainId) ? AuditEvent.DefaultChainId : ev.ChainId;
                if (!chains.TryGetValue(chainId, out List<AuditEvent> bucket))
                {
                    bucket = new List<AuditEvent>();
                    chains[chainId] = bucket;
                }
                bucket.Add(ev);
            }

            bool allValid = true;
            foreach (KeyValuePair<string, List<AuditEvent>> entry in chains)
            {
                entry.Value.Sort(static (a, b) => a.Sequence.CompareTo(b.Sequence));
                IntegrityCheckResult result = _integrityVerifier.Verify(entry.Value);
                _pipeline.Metrics?.IncrementChainVerified();
                if (!result.IsValid)
                {
                    _pipeline.Metrics?.IncrementChainDivergence();
                    allValid = false;
                }
            }
            return allValid;
        }
    }
}
