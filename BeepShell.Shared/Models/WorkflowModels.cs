using System;
using System.Collections.Generic;
using BeepShell.Shared.Interfaces;

namespace BeepShell.Shared.Models
{
    /// <summary>
    /// Workflow parameter definition
    /// </summary>
    public class WorkflowParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Type ParameterType { get; set; } = typeof(string);
        public bool Required { get; set; }
        public object? DefaultValue { get; set; }
    }

    /// <summary>
    /// Workflow execution result
    /// </summary>
    public class WorkflowResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> OutputData { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Plugin health status
    /// </summary>
    public class PluginHealthStatus
    {
        public bool IsHealthy { get; set; } = true;
        public string Status { get; set; } = "OK";
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    /// <summary>
    /// Plugin load result
    /// </summary>
    public class PluginLoadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public IShellPlugin? Plugin { get; set; }
        public List<string> Errors { get; set; } = new();
        public TimeSpan LoadTime { get; set; }
    }
}
