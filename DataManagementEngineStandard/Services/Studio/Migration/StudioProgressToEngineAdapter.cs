// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Studio.Migration;

/// <summary>
/// Adapts the Studio's platform-agnostic <see cref="IStudioProgress"/> to the
/// engine's existing <see cref="IProgress{T}"/> contract, where
/// <c>T</c> is the engine's <c>PassedArgs</c> envelope. The engine's
/// migration manager and driver provision step already call
/// <c>progress.Report(new PassedArgs { ... })</c>; this adapter lets the
/// Studio receive those reports without the engine knowing about
/// <see cref="IStudioProgress"/>.
/// </summary>
/// <remarks>
/// The adapter is one-way (engine → Studio). The engine never sees
/// <see cref="StudioProgressUpdate"/>; it just sees a normal
/// <see cref="IProgress{T}"/> and calls <c>Report</c> on it. The adapter
/// translates the engine's loosely-typed <c>PassedArgs</c> into a
/// typed <see cref="StudioProgressUpdate"/> using the
/// <c>EventType</c>, <c>Messege</c> (sic — that's the engine's
/// spelling), and <c>Progress</c> fields.
/// </remarks>
public sealed class StudioProgressToEngineAdapter : IProgress<PassedArgs>
{
    private readonly IStudioProgress _inner;
    private readonly string _operationId;
    private readonly string _operationName;

    /// <summary>
    /// Construct the bridge. <paramref name="inner"/> is the Studio
    /// progress reporter (e.g. a Blazor <c>MudBlazorISnackbar</c>-backed
    /// implementation). <paramref name="operationId"/> and
    /// <paramref name="operationName"/> are stamped on every emitted
    /// <see cref="StudioProgressUpdate"/> so the host UI can correlate
    /// updates back to the originating operation.
    /// </summary>
    public StudioProgressToEngineAdapter(
        IStudioProgress inner,
        string operationId,
        string operationName = "")
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _operationId = operationId ?? Guid.NewGuid().ToString("N");
        _operationName = operationName ?? string.Empty;
    }

    /// <summary>
    /// Translates the engine's <paramref name="args"/> into a
    /// <see cref="StudioProgressUpdate"/> and forwards to the inner
    /// <see cref="IStudioProgress"/>. Never throws — the engine's
    /// progress contract is best-effort.
    /// </summary>
    public void Report(PassedArgs args)
    {
        try
        {
            if (args == null) return;

            var stage = MapStage(args.EventType);
            var severity = MapSeverity(args);
            var update = new StudioProgressUpdate(
                OperationId: _operationId,
                OperationName: _operationName,
                Stage: stage,
                CurrentStep: string.IsNullOrEmpty(args.Messege) ? null : args.Messege,
                Percent: ClampPercent(args.Progress),
                Severity: severity,
                Timestamp: DateTimeOffset.UtcNow,
                Payload: null);

            _inner.Report(update);
        }
        catch
        {
            // Progress is best-effort. Never throw from a reporter.
        }
    }

    private static StudioProgressStage MapStage(string? eventType) =>
        (eventType ?? string.Empty).ToLowerInvariant() switch
        {
            "begin" or "start" or "started" => StudioProgressStage.Begin,
            "complete" or "completed" or "success" or "succeeded" => StudioProgressStage.Complete,
            "fail" or "failed" or "error" => StudioProgressStage.Failed,
            _ => StudioProgressStage.Update
        };

    private static StudioProgressSeverity MapSeverity(PassedArgs args)
    {
        if (args.EventType != null &&
            (args.EventType.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
             args.EventType.Contains("error", StringComparison.OrdinalIgnoreCase)))
        {
            return StudioProgressSeverity.Error;
        }
        if (args.ParameterString1?.Contains("warn", StringComparison.OrdinalIgnoreCase) == true)
        {
            return StudioProgressSeverity.Warning;
        }
        return StudioProgressSeverity.Info;
    }

    private static int ClampPercent(int percent)
    {
        if (percent < 0) return 0;
        if (percent > 100) return 100;
        return percent;
    }
}
