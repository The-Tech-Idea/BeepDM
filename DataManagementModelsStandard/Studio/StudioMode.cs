// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License.

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// Operating mode for Beep Studio. Controls which features are visible
/// and whether approval gates are required for promotions.
/// </summary>
public enum StudioMode
{
    /// <summary>
    /// One person does everything. No role differentiation needed.
    /// All actions available without approval gates.
    /// </summary>
    Solo = 0,

    /// <summary>
    /// Multiple developers + DBA + release manager.
    /// Role-based views. Approval gates for Staging/Live.
    /// </summary>
    Team = 1,

    /// <summary>
    /// Many teams, many apps, many environments.
    /// Full governance, solution control panel, CI/CD endpoints.
    /// </summary>
    Enterprise = 2,
}

/// <summary>
/// User role within Team or Enterprise mode.
/// </summary>
public enum StudioRole
{
    /// <summary>Default for Solo mode. Sees everything.</summary>
    SoloDev = 0,

    /// <summary>Writes code, creates schema changes, pushes to Test.</summary>
    Developer = 1,

    /// <summary>Manages databases, reviews migrations, approves schema changes.</summary>
    DBA = 2,

    /// <summary>Manages environments, promotes to production, governs access.</summary>
    Admin = 3,
}

/// <summary>
/// Configuration that determines which features are available based on
/// the current operating mode and role.
/// </summary>
public sealed class StudioModeConfig
{
    public StudioMode Mode { get; set; } = StudioMode.Solo;
    public StudioRole Role { get; set; } = StudioRole.SoloDev;

    // ── Capability flags ──────────────────────────────────────
    public bool ShowRoleSelector => Mode >= StudioMode.Team;
    public bool RequireApprovalGates => Mode >= StudioMode.Team;
    public bool ShowAppMatrix => Mode >= StudioMode.Team;
    public bool ShowPromotionPipeline => Mode >= StudioMode.Enterprise;
    public bool ShowSolutionControlPanel => Mode >= StudioMode.Enterprise;
    public bool ShowCrossAppGovernance => Mode >= StudioMode.Enterprise;
    public bool ShowAuditTrail => Mode >= StudioMode.Team;
    public bool ShowDriftDetection => Mode >= StudioMode.Team;

    // ── Role-based capabilities ─────────────────────────────────
    public bool CanManageSources => Role is StudioRole.SoloDev or StudioRole.Admin;
    public bool CanManageEnvironments => Role is StudioRole.SoloDev or StudioRole.Admin;
    public bool CanApproveMigrations => Role is StudioRole.SoloDev or StudioRole.DBA or StudioRole.Admin;
    public bool CanApplyToProduction => Role is StudioRole.SoloDev or StudioRole.Admin;
    public bool CanManageUsers => Role is StudioRole.SoloDev or StudioRole.Admin;
    public bool CanViewAllEnvironments => Role is StudioRole.SoloDev or StudioRole.DBA or StudioRole.Admin;

    /// <summary>Default Solo config — everything enabled.</summary>
    public static StudioModeConfig Default => new();

    /// <summary>Team Developer — Dev/Test only, no approvals.</summary>
    public static StudioModeConfig TeamDeveloper => new()
    {
        Mode = StudioMode.Team,
        Role = StudioRole.Developer,
    };

    /// <summary>Team DBA — all envs, can approve.</summary>
    public static StudioModeConfig TeamDBA => new()
    {
        Mode = StudioMode.Team,
        Role = StudioRole.DBA,
    };

    /// <summary>Team Admin — full control.</summary>
    public static StudioModeConfig TeamAdmin => new()
    {
        Mode = StudioMode.Team,
        Role = StudioRole.Admin,
    };

    /// <summary>Enterprise DBA — full governance.</summary>
    public static StudioModeConfig EnterpriseDBA => new()
    {
        Mode = StudioMode.Enterprise,
        Role = StudioRole.DBA,
    };

    /// <summary>Enterprise Admin — everything.</summary>
    public static StudioModeConfig EnterpriseAdmin => new()
    {
        Mode = StudioMode.Enterprise,
        Role = StudioRole.Admin,
    };
}
