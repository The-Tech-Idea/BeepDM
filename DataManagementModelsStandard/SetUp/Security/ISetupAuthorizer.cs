using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.SetUp.Security
{
    /// <summary>Permissions a setup run and its steps require.</summary>
    public enum SetupPermission
    {
        RunSetup,
        ProvisionDriver,
        ConfigureConnection,
        ApplySchema,
        ApproveMigration,
        Seed,
        Rollback,
        ViewState
    }

    /// <summary>
    /// Decides whether a <see cref="ISetupPrincipal"/> may perform a <see cref="SetupPermission"/>.
    /// </summary>
    /// <remarks>
    /// A denial is a step <em>failure</em> (returns to the wizard as <c>Errors.Failed</c>), never a
    /// thrown exception. The solo default (<c>AllowAllAuthorizer</c>) allows everything, so
    /// zero-config setup is unaffected.
    /// </remarks>
    public interface ISetupAuthorizer
    {
        Task<SetupAuthorizationResult> AuthorizeAsync(
            ISetupPrincipal principal, SetupPermission permission, SetupContext context,
            CancellationToken token = default);
    }

    public sealed class SetupAuthorizationResult
    {
        private SetupAuthorizationResult(bool allowed, string reason)
        {
            Allowed = allowed;
            Reason = reason;
        }

        public bool Allowed { get; }
        public string Reason { get; }

        public static SetupAuthorizationResult Allow() => new(true, null);
        public static SetupAuthorizationResult Deny(string reason) => new(false, reason);
    }
}
