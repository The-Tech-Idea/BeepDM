using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
namespace TheTechIdea.Beep.Environments.Data;

/// <summary>
/// Result of scanning a datasource for identity tables.
/// </summary>
public sealed class IdentityDetectionResult
{
    public IdentityDetectionMode Mode { get; set; }
    public List<string> FoundTables { get; set; } = new();
    public List<string> MissingTables { get; set; } = new();
    public bool IsAspNetIdentity => Mode == IdentityDetectionMode.AspNetIdentity;

    /// <summary>Known ASP.NET Identity table names.</summary>
    public static readonly string[] AspNetTableNames =
    {
        "AspNetUsers", "AspNetRoles", "AspNetUserRoles",
        "AspNetUserClaims", "AspNetRoleClaims",
        "AspNetUserLogins", "AspNetUserTokens"
    };

    /// <summary>Core tables required for ASP.NET Identity mode.</summary>
    public static readonly string[] CoreAspNetTables =
    {
        "AspNetUsers", "AspNetRoles", "AspNetUserRoles"
    };
}
