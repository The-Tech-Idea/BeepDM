using System;
using System.Collections.Generic;
using System.Linq;
namespace TheTechIdea.Beep.Environments;

/// <summary>
/// A key-value configuration override for a specific environment.
/// </summary>
public sealed class EnvironmentConfigOverride
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
