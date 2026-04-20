using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Terminal outcome of an <see cref="IReshardingService"/>
    /// operation. Carries the governing reshard id, the kind of
    /// operation performed, per-entity copy results, and the final
    /// plan applied to the datasource.
    /// </summary>
    public sealed class ReshardOutcome
    {
        /// <summary>Initialises a new outcome.</summary>
        public ReshardOutcome(
            string                  reshardId,
            string                  operation,
            bool                    success,
            bool                    cancelled,
            Exception               error,
            IReadOnlyList<CopyResult> copyResults,
            int                     planVersion)
        {
            ReshardId    = reshardId  ?? string.Empty;
            Operation    = operation  ?? string.Empty;
            Success      = success;
            Cancelled    = cancelled;
            Error        = error;
            CopyResults  = copyResults ?? Array.Empty<CopyResult>();
            PlanVersion  = planVersion;
            CompletedUtc = DateTime.UtcNow;
        }

        /// <summary>Governing reshard id.</summary>
        public string ReshardId { get; }

        /// <summary>Operation name (AddShard / RemoveShard / MoveEntity / Repartition / ApplyPlan).</summary>
        public string Operation { get; }

        /// <summary><c>true</c> when the operation completed and the target plan is live.</summary>
        public bool Success { get; }

        /// <summary><c>true</c> when the caller cancelled the operation.</summary>
        public bool Cancelled { get; }

        /// <summary>Failure detail when <see cref="Success"/> is <c>false</c>.</summary>
        public Exception Error { get; }

        /// <summary>Per-leg copy results.</summary>
        public IReadOnlyList<CopyResult> CopyResults { get; }

        /// <summary>Plan version in effect after the operation.</summary>
        public int PlanVersion { get; }

        /// <summary>UTC timestamp the operation completed.</summary>
        public DateTime CompletedUtc { get; }

        /// <summary>Total rows copied across every leg.</summary>
        public long RowsCopied => CopyResults.Sum(r => r.RowsCopied);

        /// <inheritdoc/>
        public override string ToString()
            => $"ReshardOutcome(id={ReshardId}, op={Operation}, success={Success}, cancelled={Cancelled}, " +
               $"rows={RowsCopied}, version={PlanVersion}, error={Error?.GetType().Name ?? "-"})";
    }
}
