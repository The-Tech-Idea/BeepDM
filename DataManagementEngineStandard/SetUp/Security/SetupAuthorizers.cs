using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.SetUp.Security;

namespace TheTechIdea.Beep.SetUp.Security
{
    /// <summary>
    /// Solo default: allows everything. Keeps zero-config setup working — registering it changes
    /// nothing about behaviour, which is the point of the no-op default.
    /// </summary>
    public sealed class AllowAllAuthorizer : ISetupAuthorizer
    {
        public Task<SetupAuthorizationResult> AuthorizeAsync(
            ISetupPrincipal principal, SetupPermission permission, SetupContext context,
            CancellationToken token = default)
            => Task.FromResult(SetupAuthorizationResult.Allow());
    }

    /// <summary>
    /// Enterprise authorizer: allows a permission only when the principal holds one of the roles
    /// mapped to it. Unmapped permissions are denied by default (fail closed).
    /// </summary>
    public sealed class RoleBasedSetupAuthorizer : ISetupAuthorizer
    {
        private readonly IReadOnlyDictionary<SetupPermission, string[]> _rolesByPermission;

        public RoleBasedSetupAuthorizer(IReadOnlyDictionary<SetupPermission, string[]> rolesByPermission)
            => _rolesByPermission = rolesByPermission ?? throw new ArgumentNullException(nameof(rolesByPermission));

        public Task<SetupAuthorizationResult> AuthorizeAsync(
            ISetupPrincipal principal, SetupPermission permission, SetupContext context,
            CancellationToken token = default)
        {
            if (principal == null || !principal.IsAuthenticated)
                return Task.FromResult(SetupAuthorizationResult.Deny(
                    $"Not authenticated; '{permission}' requires an authenticated principal."));

            if (!_rolesByPermission.TryGetValue(permission, out var allowedRoles) || allowedRoles.Length == 0)
                return Task.FromResult(SetupAuthorizationResult.Deny(
                    $"No role grants '{permission}'."));

            var roles = principal.Roles ?? Array.Empty<string>();
            bool granted = roles.Any(r => allowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase));

            return Task.FromResult(granted
                ? SetupAuthorizationResult.Allow()
                : SetupAuthorizationResult.Deny(
                    $"Principal '{principal.Id}' lacks a role for '{permission}' " +
                    $"(needs one of: {string.Join(", ", allowedRoles)})."));
        }
    }
}
