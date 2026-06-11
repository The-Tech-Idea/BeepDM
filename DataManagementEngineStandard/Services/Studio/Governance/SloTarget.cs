// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Studio.Governance;

/// <summary>A service-level objective (SLO) target. Used by the Studio's governance
/// policies to drive alerts when the engine's telemetry pipeline reports
/// out-of-range values.</summary>
public sealed record SloTarget(
    /// <summary>Human-readable name (e.g. "Migration apply p95 latency").</summary>
    string Name,

    /// <summary>The metric to watch (e.g. <c>migration.apply.latency.p95</c>).</summary>
    string Metric,

    /// <summary>The threshold value.</summary>
    double ThresholdValue,

    /// <summary>The comparison operator: <c>lt</c> | <c>lte</c> | <c>gt</c> | <c>gte</c> | <c>eq</c>.</summary>
    string Comparator,

    /// <summary>The rolling window over which the metric is evaluated.</summary>
    TimeSpan Window,

    /// <summary>The severity when the SLO is breached: <c>Info</c> | <c>Warn</c> | <c>Critical</c>.</summary>
    string Severity);

/// <summary>An alert rule. When the engine's SLO subsystem detects a matching
/// trigger, the Studio raises an alert via the configured actions.</summary>
public sealed record AlertRule(
    /// <summary>Human-readable name.</summary>
    string Name,

    /// <summary>The metric to watch (e.g. <c>migration.apply.latency.p95</c>).</summary>
    string TriggerMetric,

    /// <summary>The comparison operator: <c>lt</c> | <c>lte</c> | <c>gt</c> | <c>gte</c> | <c>eq</c>.</summary>
    string Comparator,

    /// <summary>The threshold value.</summary>
    double ThresholdValue,

    /// <summary>The actions to take: <c>slack</c> | <c>email</c> | <c>snackbar</c>.</summary>
    IReadOnlyList<string> Actions,

    /// <summary>Optional per-action arguments (e.g. Slack webhook URL).</summary>
    IReadOnlyDictionary<string, string>? ActionArgs);
