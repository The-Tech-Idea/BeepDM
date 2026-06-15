using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Environments.Data;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Services.AppMap
{
    /// <summary>
    /// Auto-detects ASP.NET Identity tables and provides user/role CRUD.
    /// Falls back to generic table-mapped CRUD for non-ASP.NET systems.
    /// </summary>
    public interface IIdentityManagementService
    {
        /// <summary>Detect whether a datasource uses ASP.NET Identity tables.</summary>
        Task<IdentityDetectionResult> DetectAsync(string datasourceName, CancellationToken token = default);

        /// <summary>Get all users from the datasource.</summary>
        Task<List<UserRecord>> GetUsersAsync(CancellationToken token = default);

        /// <summary>Create a new user.</summary>
        Task CreateUserAsync(UserRecord user, CancellationToken token = default);

        /// <summary>Update an existing user.</summary>
        Task UpdateUserAsync(UserRecord user, CancellationToken token = default);

        /// <summary>Delete a user.</summary>
        Task DeleteUserAsync(string userId, CancellationToken token = default);

        /// <summary>Get all roles.</summary>
        Task<List<RoleRecord>> GetRolesAsync(CancellationToken token = default);

        /// <summary>Create a new role.</summary>
        Task CreateRoleAsync(RoleRecord role, CancellationToken token = default);

        /// <summary>Delete a role.</summary>
        Task DeleteRoleAsync(string roleId, CancellationToken token = default);

        /// <summary>Assign a role to a user.</summary>
        Task AssignRoleAsync(string userId, string roleId, CancellationToken token = default);

        /// <summary>Remove a role from a user.</summary>
        Task RemoveRoleAsync(string userId, string roleId, CancellationToken token = default);

        /// <summary>Get roles assigned to a user.</summary>
        Task<List<UserRoleAssignment>> GetUserRolesAsync(string userId, CancellationToken token = default);

        /// <summary>Set custom table mapping for non-ASP.NET identity.</summary>
        void SetTableMapping(TableMapping mapping);

        /// <summary>Get the current table mapping (null for ASP.NET identity).</summary>
        TableMapping? GetTableMapping();
    }
}
