using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// <see cref="ReshardingService"/> partial — <c>AddShardAsync</c>.
    /// Adding a shard is a no-copy operation: the new shard is
    /// registered in the catalog but no placements point at it until
    /// a later <see cref="IReshardingService.ApplyPlanAsync"/> or
    /// repartition call brings it into service.
    /// </summary>
    public sealed partial class ReshardingService
    {
        /// <inheritdoc/>
        public Task<ReshardOutcome> AddShardAsync(
            ShardSpec         spec,
            CancellationToken cancellationToken = default)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            var reshardId = NewReshardId("AddShard");
            var current   = _getCurrentPlan();
            _raiseStarted(reshardId, $"AddShard '{spec.ShardId}'", 0);

            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _raiseCompleted(reshardId, "AddShard cancelled before registration", 0, null);
                    return Task.FromResult(CancelledOutcome(reshardId, "AddShard", current?.Version ?? 0, Array.Empty<CopyResult>()));
                }

                _registerShard(spec.ShardId, spec.Cluster);
                _raiseCompleted(reshardId, $"AddShard '{spec.ShardId}' completed", 0, null);
                return Task.FromResult(SuccessOutcome(reshardId, "AddShard", current?.Version ?? 0, Array.Empty<CopyResult>()));
            }
            catch (OperationCanceledException)
            {
                _raiseCompleted(reshardId, "AddShard cancelled", 0, null);
                return Task.FromResult(CancelledOutcome(reshardId, "AddShard", current?.Version ?? 0, Array.Empty<CopyResult>()));
            }
            catch (Exception ex)
            {
                _raiseCompleted(reshardId, $"AddShard failed: {ex.Message}", 0, ex);
                return Task.FromResult(FailureOutcome(reshardId, "AddShard", current?.Version ?? 0, Array.Empty<CopyResult>(), ex));
            }
        }
    }
}
