// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// Runtime info about the Studio. Returned by <see cref="Contracts.IStudioService.GetInfo"/>.
/// </summary>
public sealed record StudioInfo(
    /// <summary>The Studio's semver version (e.g. <c>0.1.0</c>).</summary>
    string Version,

    /// <summary>The engine's semver version (e.g. <c>3.0.0</c>).</summary>
    string EngineVersion,

    /// <summary>Data-source types the engine can open (e.g. <c>SqlServer</c>, <c>Sqlite</c>, <c>MongoDB</c>).</summary>
    IReadOnlyList<string> SupportedDataSourceTypes,

    /// <summary>Data-source categories the engine can open (e.g. <c>RDBMS</c>, <c>NoSQL</c>, <c>File</c>, <c>WebAPI</c>).</summary>
    IReadOnlyList<string> SupportedDataSourceCategories,

    /// <summary>The rollout tiers the engine recognises.</summary>
    IReadOnlyList<RolloutTier> SupportedTiers,

    /// <summary>True when the audit pipeline is enabled.</summary>
    bool AuditEnabled,

    /// <summary>True when the background services (sync runner etc.) are enabled.</summary>
    bool HostedServicesEnabled,

    /// <summary>True when a <c>DataLifecycleManifest</c> is currently loaded.</summary>
    bool ManifestLoaded,

    /// <summary>The manifest's <c>manifestVersion</c>, or <c>null</c> when no manifest is loaded.</summary>
    string? ManifestVersion,

    /// <summary>Free-form capability map (e.g. <c>{"efCoreAdapter": true}</c>).</summary>
    IReadOnlyDictionary<string, object?> Capabilities);
