// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// Platform-agnostic progress reporter. The Studio reports lifecycle events
/// (Begin / Update / Complete / Failed) with a structured payload so any UI
/// shell (Blazor, WinForms, WPF, Maui, Console) can render it without
/// knowing engine internals.
/// </summary>
/// <remarks>
/// This is the cross-cutting contract every Studio host implements. The default
/// implementation is <see cref="NullStudioProgress"/>, which silently drops
/// every update (useful for tests and CLI runs).
/// </remarks>
public interface IStudioProgress
{
    /// <summary>Emit a progress update. Implementations must be thread-safe.</summary>
    void Report(StudioProgressUpdate update);
}

/// <summary>
/// A single progress update from the engine. Carries enough context for the
/// host UI to render a step progress strip, a per-step sub-label, a percentage,
/// a severity (info / warning / error), and an arbitrary payload for
/// operation-specific data (e.g. the current entity being migrated).
/// </summary>
/// <param name="OperationId">A stable id that correlates all updates for one operation.</param>
/// <param name="OperationName">A human-readable name (e.g. "Provisioning SQLite driver").</param>
/// <param name="Stage">The lifecycle stage: <c>Begin</c> at start, repeated <c>Update</c> calls, then <c>Complete</c> or <c>Failed</c>.</param>
/// <param name="CurrentStep">A free-form description of the current sub-step (e.g. "Downloading package...").</param>
/// <param name="Percent">0..100. The host may clamp to a max for display.</param>
/// <param name="Severity">Info / Warning / Error. Failed operations should set this to <c>Error</c>.</param>
/// <param name="Timestamp">When the engine emitted the update.</param>
/// <param name="Payload">Operation-specific data (current entity, current row count, etc.).</param>
public sealed record StudioProgressUpdate(
    string OperationId,
    string OperationName,
    StudioProgressStage Stage,
    string? CurrentStep,
    int Percent,
    StudioProgressSeverity Severity,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object?>? Payload);

/// <summary>The four lifecycle stages the engine emits.</summary>
public enum StudioProgressStage
{
    /// <summary>The operation has started. The first update for an <see cref="StudioProgressUpdate.OperationId"/>.</summary>
    Begin = 0,

    /// <summary>An intermediate progress update. The engine may emit zero or more of these.</summary>
    Update = 1,

    /// <summary>The operation completed successfully. The final update for the <see cref="StudioProgressUpdate.OperationId"/>.</summary>
    Complete = 2,

    /// <summary>The operation failed. The final update for the <see cref="StudioProgressUpdate.OperationId"/>.</summary>
    Failed = 3
}

/// <summary>The severity of a <see cref="StudioProgressUpdate"/>.</summary>
public enum StudioProgressSeverity
{
    /// <summary>Informational only. The default.</summary>
    Info = 0,

    /// <summary>A non-fatal warning. The host UI may show a yellow indicator.</summary>
    Warning = 1,

    /// <summary>An error. Usually paired with <see cref="StudioProgressStage.Failed"/>.</summary>
    Error = 2
}

/// <summary>
/// No-op progress reporter. Every <see cref="Report"/> call is silently dropped.
/// Use in tests and in CLI runs where no UI consumes the updates.
/// </summary>
public sealed class NullStudioProgress : IStudioProgress
{
    /// <summary>Singleton instance — safe to share across all threads and circuits.</summary>
    public static NullStudioProgress Instance { get; } = new();

    private NullStudioProgress() { }

    /// <inheritdoc />
    public void Report(StudioProgressUpdate update) { /* no-op */ }
}
