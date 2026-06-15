using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.Environments.Data;

/// <summary>
/// A user record — can represent an ASP.NET Identity user or a generic user.
/// </summary>
public sealed class UserRecord
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? NormalizedUserName { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool IsLocked { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }

    /// <summary>Roles assigned to this user (populated on read).</summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>Custom fields for non-ASP.NET identity systems.</summary>
    public Dictionary<string, string?> CustomFields { get; set; } = new();
}
