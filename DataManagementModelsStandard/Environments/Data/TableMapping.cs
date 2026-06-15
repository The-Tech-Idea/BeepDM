using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.Environments.Data;

/// <summary>
/// Custom table/column mapping for non-ASP.NET identity systems.
/// </summary>
public sealed class TableMapping
{
    public string UsersTable { get; set; } = string.Empty;
    public string? RolesTable { get; set; }
    public string? UserRolesTable { get; set; }
    public string UserIdColumn { get; set; } = "Id";
    public string? RoleIdColumn { get; set; } = "Id";
    public string UserNameColumn { get; set; } = "UserName";
    public string? EmailColumn { get; set; } = "Email";
    public string? PasswordHashColumn { get; set; } = "PasswordHash";
    public string? PhoneColumn { get; set; } = "PhoneNumber";
}
