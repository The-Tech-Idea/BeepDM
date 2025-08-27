using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Workflow.Models.Base;

namespace TheTechIdea.Beep.Workflow.Models
{
    /// <summary>
    /// Connection health status enumeration - unified version
    /// </summary>
    public enum ConnectionHealthStatus
    {
        Unknown = 0,
        Healthy = 1,
        Stale = 2,
        NeedsReauthorization = 3,
        Expired = 4,
        Error = 5,
        Inactive = 6,
        Degraded = 7,
        Unhealthy = 8
    }

    /// <summary>
    /// Variable scope enumeration
    /// </summary>
    public enum VariableScope
    {
        Scenario = 1,
        Module = 2,
        Global = 3
    }

    /// <summary>
    /// Variable statistics - unified version for both scenario and module variables
    /// </summary>
    public class VariableStatistics
    {
        /// <summary>
        /// Total number of variables
        /// </summary>
        public int TotalVariables { get; set; }

        /// <summary>
        /// Number of required variables
        /// </summary>
        public int RequiredVariables { get; set; }

        /// <summary>
        /// Number of global variables (scenario variables)
        /// </summary>
        public int GlobalVariables { get; set; }

        /// <summary>
        /// Number of input variables (module variables)
        /// </summary>
        public int InputVariables { get; set; }

        /// <summary>
        /// Number of output variables (module variables)
        /// </summary>
        public int OutputVariables { get; set; }

        /// <summary>
        /// Number of variables with values set
        /// </summary>
        public int VariablesWithValues { get; set; }

        /// <summary>
        /// Number of variables with validation errors
        /// </summary>
        public int VariablesWithErrors { get; set; }

        /// <summary>
        /// Count of variables by data type
        /// </summary>
        public Dictionary<string, int> DataTypeCounts { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Count of variables by scope
        /// </summary>
        public Dictionary<VariableScope, int> ScopeCounts { get; set; } = new Dictionary<VariableScope, int>();
    }

    /// <summary>
    /// Execution time trend data point
    /// </summary>
    public class ExecutionTimeTrend
    {
        public DateTime Date { get; set; }
        public double AverageExecutionTime { get; set; }
        public int ExecutionCount { get; set; }
    }
}
