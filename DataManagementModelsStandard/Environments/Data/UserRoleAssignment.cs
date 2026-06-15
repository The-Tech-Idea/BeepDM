using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.Environments.Data;

/// <summary>
/// Assigns a role to a user.
/// </summary>
public sealed class UserRoleAssignment
{
    public string UserId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
