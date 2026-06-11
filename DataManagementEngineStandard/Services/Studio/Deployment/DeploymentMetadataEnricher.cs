// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using TheTechIdea.Beep.Services.Telemetry;

namespace TheTechIdea.Beep.Studio.Deployment;

/// <summary>
/// Telemetry enricher that stamps every audit envelope with the current
/// deployment metadata (code revision ref, short SHA, version, build id,
/// built-at). The Studio uses this to make every audit event traceable
/// back to the exact code revision that produced it.
/// </summary>
/// <remarks>
/// <para>
/// Enrichers run on the producer thread and must be cheap and
/// non-blocking. The deployment metadata is resolved once via
/// <see cref="Lazy{T}"/> and cached for the process lifetime — a redeploy
/// produces a new process and a fresh cache.
/// </para>
/// <para>
/// If metadata resolution fails (e.g. no git, no env var, no
/// <c>InformationalVersion</c>), the enricher stamps an explicit
/// <c>"unresolved"</c> marker rather than throwing. Audit must never be
/// lost because of cross-cutting enricher failures.
/// </para>
/// <para>
/// Wiring modes:
/// <list type="bullet">
///   <item>
///     <description>
///     <c>DeploymentMetadataEnricher(IDeploymentMetadataService)</c> —
///     the enricher resolves the metadata the first time
///     <see cref="Enrich"/> runs and caches it. Best for late DI.
///     </description>
///   </item>
///   <item>
///     <description>
///     <c>DeploymentMetadataEnricher(DeploymentMetadata?)</c> — for
///     unit tests and offline wiring with a precomputed value.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
public sealed class DeploymentMetadataEnricher : IEnricher
{
    /// <summary>Stable name used for diagnostics and ordering.</summary>
    public string Name => "deployment-metadata";

    private readonly Lazy<DeploymentMetadata?> _metadata;

    /// <summary>
    /// Default ctor. Resolves the metadata on the first <see cref="Enrich"/>
    /// call. Resolution is bounded by a 2-second timeout and never throws.
    /// </summary>
    public DeploymentMetadataEnricher(IDeploymentMetadataService service)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        _metadata = new Lazy<DeploymentMetadata?>(
            () => ResolveSafe(service),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Constructor used by unit tests with a precomputed value.</summary>
    public DeploymentMetadataEnricher(DeploymentMetadata? preset)
    {
        _metadata = new Lazy<DeploymentMetadata?>(
            () => preset,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private static DeploymentMetadata? ResolveSafe(IDeploymentMetadataService service)
    {
        try
        {
            var task = service.GetCurrentAsync();
            if (!task.Wait(TimeSpan.FromSeconds(2)))
            {
                return null; // timed out — don't block the producer
            }
            var result = task.Result; // unwrap the Task
            return result != null && result.IsSuccess ? result.Value : null;
        }
        catch
        {
            return null; // never throw from a path the host cares about
        }
    }

    /// <summary>The current metadata, or <c>null</c> if resolution failed.</summary>
    public DeploymentMetadata? Current => _metadata.Value;

    /// <summary>
    /// Stamps the envelope's <c>Properties</c> bag with deployment metadata.
    /// Initializes <c>Properties</c> when it is <c>null</c>.
    /// </summary>
    public void Enrich(TelemetryEnvelope envelope)
    {
        if (envelope == null) return;
        envelope.Properties ??= new System.Collections.Generic.Dictionary<string, object>();

        var meta = _metadata.Value;
        if (meta == null)
        {
            envelope.Properties["beep.deployment.resolved"] = false;
            envelope.Properties["beep.deployment.reason"] = "unresolved";
            return;
        }

        envelope.Properties["beep.deployment.resolved"] = true;
        envelope.Properties["beep.deployment.ref"] = meta.CodeRevisionRef ?? string.Empty;
        envelope.Properties["beep.deployment.sha"] = meta.CodeRevisionSha ?? string.Empty;
        envelope.Properties["beep.deployment.sha.short"] = ShortSha(meta.CodeRevisionSha);
        if (!string.IsNullOrEmpty(meta.Version)) envelope.Properties["beep.deployment.version"] = meta.Version;
        if (!string.IsNullOrEmpty(meta.BuildId)) envelope.Properties["beep.deployment.buildId"] = meta.BuildId;
        if (!string.IsNullOrEmpty(meta.BuildUrl)) envelope.Properties["beep.deployment.buildUrl"] = meta.BuildUrl;
        envelope.Properties["beep.deployment.builtAt"] = meta.BuiltAt.ToString("o");
    }

    private static string ShortSha(string? sha) =>
        string.IsNullOrEmpty(sha) || sha.Length < 8 ? sha ?? string.Empty : sha[..8];
}
