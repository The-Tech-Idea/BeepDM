using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Aggregate execution statistics for a block's trigger set.
    /// Cross-platform replacement for WinForms TriggerStatistics.
    /// </summary>
    public class TriggerStatisticsInfo
    {
        /// <summary>Gets or sets the block name represented by the statistics.</summary>
        public string BlockName { get; set; } = string.Empty;

        /// <summary>Gets or sets the total trigger definitions for the block.</summary>
        public int TotalTriggers { get; set; }

        /// <summary>Gets or sets the number of enabled triggers.</summary>
        public int EnabledTriggers { get; set; }

        /// <summary>Gets or sets the number of disabled triggers.</summary>
        public int DisabledTriggers { get; set; }

        /// <summary>Gets or sets the total number of trigger executions.</summary>
        public long TotalExecutions { get; set; }

        /// <summary>Gets or sets the total number of successful executions.</summary>
        public long TotalSuccesses { get; set; }

        /// <summary>Gets or sets the total number of failed executions.</summary>
        public long TotalFailures { get; set; }

        /// <summary>Gets or sets the average execution time in milliseconds.</summary>
        public double AverageExecutionMs { get; set; }

        /// <summary>Breakdown by trigger type name</summary>
        public Dictionary<string, int> TriggersByType { get; set; } = new();

        /// <summary>Name of the most-executed trigger (null if none)</summary>
        public string MostExecutedTriggerName { get; set; }

        /// <summary>Returns a compact textual summary of the trigger statistics.</summary>
        public override string ToString() =>
            $"Block={BlockName} Triggers={TotalTriggers}(On={EnabledTriggers},Off={DisabledTriggers}) " +
            $"Exec={TotalExecutions} Success={TotalSuccesses} Fail={TotalFailures} AvgMs={AverageExecutionMs:F2}";
    }
}
