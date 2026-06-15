using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Environments;

namespace TheTechIdea.Beep.Services.AppMap.ControlPanel;

/// <summary>
/// Result of a health check against a server or database.
/// </summary>
public sealed class HealthCheckResult
{
    public HealthStatus Status { get; set; }
    public int? StatusCode { get; set; }
    public long LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    public bool IsHealthy => Status == HealthStatus.Healthy;
}
