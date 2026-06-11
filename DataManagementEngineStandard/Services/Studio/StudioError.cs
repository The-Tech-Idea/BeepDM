// Copyright (c) The Tech Idea. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Studio;

/// <summary>
/// Error envelope for a <see cref="StudioResult{T}"/>. Mirrors RFC 7807 ProblemDetails
/// in spirit but is engine-agnostic. The <see cref="Code"/> is a stable, machine-readable
/// identifier the host UI maps to a user-friendly message.
/// </summary>
public readonly record struct StudioError(
    StudioErrorCode Code,
    string Message,
    Exception? Exception,
    IReadOnlyDictionary<string, object?>? Details)
{
    /// <summary>The default "no error" sentinel used by <see cref="StudioResult{T}.Ok"/>.</summary>
    public static StudioError None { get; } = new(StudioErrorCode.None, string.Empty, null, null);

    /// <summary>True when this is the <see cref="None"/> sentinel.</summary>
    public bool IsNone => Code == StudioErrorCode.None;
}
