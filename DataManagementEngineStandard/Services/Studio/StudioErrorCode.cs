// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// Stable, machine-readable error codes for <see cref="StudioError"/>. The host UI
/// maps each code to a user-friendly message in a single table; never embed UI
/// strings in engine code.
/// </summary>
/// <remarks>
/// The numeric values are stable across releases — do not renumber. New codes
/// are appended at the end. The grouped ranges are:
/// <list type="bullet">
///   <item><c>0-99</c> — generic</item>
///   <item><c>100-199</c> — driver</item>
///   <item><c>200-299</c> — source / connection</item>
///   <item><c>300-399</c> — schema</item>
///   <item><c>400-499</c> — migration</item>
///   <item><c>500-599</c> — sync</item>
///   <item><c>600-699</c> — manifest + deployment</item>
///   <item><c>700-799</c> — audit</item>
///   <item><c>800-899</c> — cancellation</item>
///   <item><c>900-999</c> — host + internal</item>
/// </list>
/// </remarks>
public enum StudioErrorCode
{
    /// <summary>No error. The <see cref="StudioError.None"/> sentinel.</summary>
    None = 0,

    /// <summary>An unspecified error. Inspect the <see cref="StudioError.Message"/> for details.</summary>
    Unknown = 1,

    /// <summary>One of the arguments did not pass validation.</summary>
    InvalidArgument = 2,

    /// <summary>The requested entity does not exist.</summary>
    NotFound = 3,

    /// <summary>The entity being created already exists.</summary>
    AlreadyExists = 4,

    /// <summary>The caller is not permitted to perform the requested action.</summary>
    PermissionDenied = 5,

    /// <summary>No data-source driver is registered for the requested type.</summary>
    DriverMissing = 100,

    /// <summary>A registered driver failed to load (bad DLL, missing dep, etc.).</summary>
    DriverLoadFailed = 101,

    /// <summary>Connection to the data source could not be established.</summary>
    ConnectionFailed = 200,

    /// <summary>Connection was established but a health check failed (e.g. wrong credentials).</summary>
    ConnectionTestFailed = 201,

    /// <summary>The discovered schema is incompatible with what the manifest expects.</summary>
    SchemaIncompatible = 300,

    /// <summary>The migration plan was rejected (e.g. by the policy evaluator).</summary>
    PlanRejected = 400,

    /// <summary>The request violates the manifest's policy (e.g. <c>forbidDestructiveInLive</c>).</summary>
    PolicyViolation = 401,

    /// <summary>The operation requires an approval token that was not supplied.</summary>
    ApprovalRequired = 402,

    /// <summary>The supplied approval token was rejected (wrong signature, expired, or wrong code revision).</summary>
    ApprovalTokenInvalid = 403,

    /// <summary>Applying the migration plan failed (one or more DDL ops errored).</summary>
    ApplyFailed = 404,

    /// <summary>Rollback after a failed apply could not complete cleanly.</summary>
    RollbackFailed = 405,

    /// <summary>The sync run was terminated by an error.</summary>
    SyncRunFailed = 500,

    /// <summary>A sync conflict was detected that no rule resolved.</summary>
    ConflictUnresolved = 501,

    /// <summary>The manifest file could not be parsed or failed validation.</summary>
    ManifestInvalid = 600,

    /// <summary>The manifest's <c>manifestVersion</c> is not supported by this Studio version.</summary>
    ManifestVersionUnsupported = 601,

    /// <summary>The current deployment metadata could not be resolved (no git SHA, no env var, no manifest ref).</summary>
    DeploymentMetadataMissing = 602,

    /// <summary>An audit pipeline error prevented the event from being recorded.</summary>
    AuditFailed = 700,

    /// <summary>The call was cancelled via the <see cref="System.Threading.CancellationToken"/>.</summary>
    Cancelled = 800,

    /// <summary>The host (Blazor / WinForms / etc.) does not support the requested operation.</summary>
    HostNotSupported = 900,

    /// <summary>An unexpected internal error. Inspect the <see cref="StudioError.Exception"/> for the stack trace.</summary>
    InternalError = 999
}
