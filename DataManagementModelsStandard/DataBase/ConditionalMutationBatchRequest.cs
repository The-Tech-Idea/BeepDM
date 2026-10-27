using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Immutable transaction data; the app owns authorization and observation.</summary>
    public sealed class ConditionalMutationBatchRequest
    {
        /// <summary>Transaction correlation identifier.</summary>
        public Guid RequestId { get; } = Guid.NewGuid();
        /// <summary>Ordered operations; every operation must change exactly one row.</summary>
        public IReadOnlyList<ConditionalMutationRequest> Mutations { get; }
        /// <summary>Required observed foreign rows, evaluated before changes.</summary>
        public IReadOnlyList<ConditionalGuardGroup> Guards { get; }
        /// <summary>Required cohort cardinalities, evaluated before changes.</summary>
        public IReadOnlyList<ConditionalRowSetGuard> Sets { get; }
        /// <summary>Stable authority/time guards rechecked immediately before commit.</summary>
        public IReadOnlyList<ConditionalGuardGroup> CommitGuards { get; }
        /// <summary>Selected explicit request budget.</summary>
        public ConditionalMutationBatchBudget Budget { get; }
        /// <summary>Copies a bounded, nonempty transaction: at most 64 operations, 128 row groups, 64 cohorts, 16 commit groups and 8192 aggregate scalar observations. Complete source-bound line revisions can retain per-line identity absence checks without splitting the physical transaction.</summary>
        public ConditionalMutationBatchRequest(IEnumerable<ConditionalMutationRequest> mutations, IEnumerable<ConditionalGuardGroup> guards, IEnumerable<ConditionalRowSetGuard> sets, IEnumerable<ConditionalGuardGroup> commitGuards = null)
            : this(mutations, guards, sets, commitGuards, ConditionalMutationBatchBudget.Standard) { }
        /// <summary>Copies one complete explicitly bounded document without changing the existing standard constructor.</summary>
        public ConditionalMutationBatchRequest(IEnumerable<ConditionalMutationRequest> mutations, IEnumerable<ConditionalGuardGroup> guards, IEnumerable<ConditionalRowSetGuard> sets, IEnumerable<ConditionalGuardGroup> commitGuards, ConditionalMutationBatchBudget budget)
        {
            if (budget != ConditionalMutationBatchBudget.Standard && budget != ConditionalMutationBatchBudget.Document) throw new ArgumentException("A declared transaction budget is required.");
            Budget = budget;
            var document = budget == ConditionalMutationBatchBudget.Document;
            var maximumOperations = document ? 1024 : 64;
            var maximumCohorts = document ? 512 : 64;
            var maximumScalars = document ? 65536 : 8192;
            var ops = mutations?.ToArray() ?? throw new ArgumentNullException(nameof(mutations));
            var rows = guards?.ToArray() ?? throw new ArgumentNullException(nameof(guards));
            var cohorts = sets?.ToArray() ?? throw new ArgumentNullException(nameof(sets));
            var final = commitGuards?.ToArray() ?? Array.Empty<ConditionalGuardGroup>();
            if (ops.Length < 1 || ops.Length > maximumOperations || ops.Any(item => item == null) || rows.Length is < 1 or > 128 || rows.Any(item => item == null) ||
                cohorts.Length > maximumCohorts || cohorts.Any(item => item == null) || final.Length > 16 || final.Any(item => item == null) || ops.Select(item => item.RequestId).Distinct().Count() != ops.Length)
                throw new ArgumentException("A bounded transaction with unique operations and required guards is required.");
            var scalarCount = ops.Sum(item => item.Values.Count + item.Match.Count + item.Guards.Sum(group => group.Alternatives.Sum(guard => guard.Match.Count)))
                + rows.Sum(group => group.Alternatives.Sum(guard => guard.Match.Count)) + cohorts.Sum(item => item.Match.Count + 1 + item.DateRanges.Count * 5 + item.TextEqualities.Sum(text => text.ScalarCost)) + final.Sum(group => group.Alternatives.Sum(guard => guard.Match.Count));
            if (scalarCount > maximumScalars) throw new ArgumentException("The transaction exceeds its bounded scalar budget.");
            if (document)
            {
                using var encoded = new BoundedMutationEncodingStream(16L * 1024 * 1024);
                JsonSerializer.Serialize(encoded, new { RequestId, Budget, Mutations = ops, Guards = rows, Sets = cohorts, CommitGuards = final });
            }
            Mutations = Array.AsReadOnly(ops); Guards = Array.AsReadOnly(rows); Sets = Array.AsReadOnly(cohorts); CommitGuards = Array.AsReadOnly(final);
        }
    }
}
