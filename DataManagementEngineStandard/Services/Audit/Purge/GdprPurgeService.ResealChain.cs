using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Purge
{
    /// <summary>
    /// Chain re-seal half of <see cref="GdprPurgeService"/>. Reads the
    /// surviving events from the store, hands them off to the signer
    /// for renumbering / re-hashing, then writes them back so the
    /// integrity verifier reports a clean chain.
    /// </summary>
    public sealed partial class GdprPurgeService
    {
        private async Task ResealChainAsync(IAuditPurgeStore store, string chainId, CancellationToken cancellationToken)
        {
            if (_signer is null || string.IsNullOrEmpty(chainId))
            {
                return;
            }

            IReadOnlyList<AuditEvent> survivors =
                await store.ReadChainAsync(chainId, cancellationToken).ConfigureAwait(false);

            if (survivors is null || survivors.Count == 0)
            {
                return;
            }

            IReadOnlyList<AuditEvent> resealed = _signer.Reseal(chainId, survivors);
            await store.UpdateChainAsync(chainId, resealed, cancellationToken).ConfigureAwait(false);
        }

        private async Task EmitPurgeAuditAsync(
            string operation,
            string target,
            int removed,
            ICollection<string> chains,
            CancellationToken cancellationToken)
        {
            if (_audit is null || removed == 0)
            {
                return;
            }

            var ev = new AuditEvent
            {
                EventId = Guid.NewGuid(),
                TimestampUtc = DateTime.UtcNow,
                ChainId = PurgeChainId,
                Category = AuditCategory.Custom,
                Operation = operation,
                Outcome = AuditOutcome.Success,
                UserId = _policy?.OperatorId ?? "purge-operator",
                EntityName = "AuditEvent",
                RecordKey = target,
                Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["purge.removed"] = removed,
                    ["purge.affectedChains"] = string.Join(",", chains)
                }
            };

            try
            {
                await _audit.RecordAsync(ev, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Purge audit emission must never throw — the purge itself
                // already succeeded by the time we get here.
            }
        }
    }
}
