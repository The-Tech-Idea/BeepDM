using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.SetUp.Security
{
    /// <summary>
    /// Obtains approval for a schema change, bound to the plan hash so an approval can't be replayed
    /// against a different plan.
    /// </summary>
    /// <remarks>
    /// Replaces the self-granted approval (label <c>"SetupWizard"</c>, note "Auto-approved by setup
    /// wizard"). The solo default approves and records <see cref="SetupApproval.IsSelfApproved"/> =
    /// true — honest, not laundered. An enterprise provider rejects self-approval.
    /// </remarks>
    public interface ISetupApprovalProvider
    {
        Task<SetupApproval> RequestApprovalAsync(
            SetupContext context, ISetupPrincipal principal, string planHash,
            CancellationToken token = default);
    }

    public sealed class SetupApproval
    {
        public string ApproverId { get; set; }
        public string ApproverLabel { get; set; }
        public DateTimeOffset ApprovedAt { get; set; }
        public string PlanHash { get; set; }

        /// <summary>The approver is the same principal that requested it (solo) — recorded, not hidden.</summary>
        public bool IsSelfApproved { get; set; }

        public bool Granted { get; set; }
        public string Note { get; set; }
    }
}
