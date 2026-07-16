using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.SetUp.Security;

namespace TheTechIdea.Beep.SetUp.Security
{
    /// <summary>
    /// Solo default: approves, and records the approval as self-granted rather than dressing it up.
    /// </summary>
    public sealed class AutoApprovalProvider : ISetupApprovalProvider
    {
        public Task<SetupApproval> RequestApprovalAsync(
            SetupContext context, ISetupPrincipal principal, string planHash,
            CancellationToken token = default)
        {
            var id = principal?.Id ?? "anonymous";
            return Task.FromResult(new SetupApproval
            {
                Granted = true,
                ApproverId = id,
                ApproverLabel = principal?.DisplayName ?? id,
                ApprovedAt = DateTimeOffset.UtcNow,
                PlanHash = planHash,
                IsSelfApproved = true,
                Note = "Auto-approved (solo mode, no approver configured)."
            });
        }
    }

    /// <summary>
    /// Enterprise approval: the requester and the approver must differ. A principal cannot approve
    /// its own plan — that's the entire point of an approval gate.
    /// </summary>
    /// <remarks>
    /// Delegates the "did an approver actually sign off" decision to <paramref name="approverLookup"/>
    /// (returns the approver's id, or null if none). Binds the approval to <c>planHash</c> so it
    /// can't be replayed against a different plan.
    /// </remarks>
    public sealed class SeparationOfDutyApprovalProvider : ISetupApprovalProvider
    {
        private readonly Func<SetupContext, string, CancellationToken, Task<string>> _approverLookup;

        public SeparationOfDutyApprovalProvider(
            Func<SetupContext, string, CancellationToken, Task<string>> approverLookup)
            => _approverLookup = approverLookup ?? throw new ArgumentNullException(nameof(approverLookup));

        public async Task<SetupApproval> RequestApprovalAsync(
            SetupContext context, ISetupPrincipal principal, string planHash,
            CancellationToken token = default)
        {
            var approverId = await _approverLookup(context, planHash, token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(approverId))
                return new SetupApproval
                {
                    Granted = false, PlanHash = planHash,
                    Note = "No approver signed off on this plan."
                };

            if (string.Equals(approverId, principal?.Id, StringComparison.OrdinalIgnoreCase))
                return new SetupApproval
                {
                    Granted = false, PlanHash = planHash, ApproverId = approverId, IsSelfApproved = true,
                    Note = "Self-approval rejected: the approver must differ from the requester."
                };

            return new SetupApproval
            {
                Granted = true, ApproverId = approverId, ApproverLabel = approverId,
                ApprovedAt = DateTimeOffset.UtcNow, PlanHash = planHash, IsSelfApproved = false,
                Note = "Approved."
            };
        }
    }
}
