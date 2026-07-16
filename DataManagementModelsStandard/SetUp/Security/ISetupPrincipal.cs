using System.Collections.Generic;

namespace TheTechIdea.Beep.SetUp.Security
{
    /// <summary>
    /// Who is running a setup. Bound onto <see cref="SetupState"/> / <see cref="SetupReport"/> so an
    /// audit trail (Phase 6) can say who did what.
    /// </summary>
    public interface ISetupPrincipal
    {
        string Id { get; }
        string DisplayName { get; }
        IReadOnlyCollection<string> Roles { get; }

        /// <summary>False for the solo/anonymous default — never let an audit imply it was true.</summary>
        bool IsAuthenticated { get; }
    }

    /// <summary>
    /// Solo default: identifies the local OS user without authenticating them.
    /// </summary>
    public sealed class AnonymousSetupPrincipal : ISetupPrincipal
    {
        // System.Environment (not SetupOptions.Environment — the names collide across this codebase).
        public string Id => System.Environment.UserName;
        public string DisplayName => System.Environment.UserName;
        public IReadOnlyCollection<string> Roles => System.Array.Empty<string>();
        public bool IsAuthenticated => false;
    }
}
