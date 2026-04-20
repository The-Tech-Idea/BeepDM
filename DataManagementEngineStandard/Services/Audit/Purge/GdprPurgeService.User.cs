using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Audit.Models;

namespace TheTechIdea.Beep.Services.Audit.Purge
{
    /// <summary>
    /// User-scoped purge half of <see cref="GdprPurgeService"/>.
    /// </summary>
    public sealed partial class GdprPurgeService
    {
        /// <summary>
        /// Removes every audit event whose <see cref="AuditEvent.UserId"/>
        /// matches <paramref name="userId"/> and re-seals every affected
        /// chain. Writes a synthetic <c>Purge</c> event so the operation
        /// is itself auditable.
        /// </summary>
        public async Task<int> PurgeByUserAsync(
            string userId,
            string confirmationToken,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthorized(confirmationToken);

            if (string.IsNullOrEmpty(userId))
            {
                return 0;
            }

            int totalRemoved = 0;
            var allChains = new HashSet<string>();
            var perStore = new List<(IAuditPurgeStore Store, PurgeImpact Impact)>();

            foreach (IAuditPurgeStore store in _stores)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                PurgeImpact impact = await store.DeleteByUserAsync(userId, cancellationToken).ConfigureAwait(false);
                if (impact is null || !impact.HasChanges)
                {
                    continue;
                }
                perStore.Add((store, impact));
                totalRemoved += impact.RemovedCount;
                foreach (string chain in impact.AffectedChains)
                {
                    allChains.Add(chain);
                }
            }

            foreach ((IAuditPurgeStore Store, PurgeImpact Impact) entry in perStore)
            {
                foreach (string chainId in entry.Impact.AffectedChains)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    await ResealChainAsync(entry.Store, chainId, cancellationToken).ConfigureAwait(false);
                }
            }

            await EmitPurgeAuditAsync(
                operation: "PurgeByUser",
                target: userId,
                removed: totalRemoved,
                chains: allChains,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return totalRemoved;
        }
    }
}
