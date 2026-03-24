using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Identity and permission context for the current pipeline caller.
    /// Populated by the host application and propagated through
    /// <see cref="PipelineRunContext"/> to enable pre-run authorization,
    /// audit attribution, and data-classification enforcement.
    /// </summary>
    public class SecurityContext
    {
        /// <summary>Unique user or service-principal identifier.</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>Display name for audit logs.</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>Roles or permission claims held by the caller.</summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>Originating IP address (for audit trail).</summary>
        public string? IpAddress { get; set; }

        /// <summary>Optional session / correlation ID from the host.</summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Checks whether the caller holds the specified permission/role.
        /// </summary>
        public bool HasPermission(string permission)
            => Roles.Contains(permission);

        /// <summary>
        /// Checks whether the caller holds all of the specified permissions.
        /// </summary>
        public bool HasAllPermissions(IEnumerable<string> permissions)
        {
            foreach (var p in permissions)
                if (!Roles.Contains(p))
                    return false;
            return true;
        }
    }
}
