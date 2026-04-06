using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Aggregate execution statistics for a block's trigger set.
    /// Cross-platform replacement for WinForms TriggerStatistics.
    /// </summary>
    public class TriggerStatisticsInfo
    {
        public string BlockName { get; set; } = string.Empty;
        public int TotalTriggers { get; set; }
        public int EnabledTriggers { get; set; }
        public int DisabledTriggers { get; set; }
        public long TotalExecutions { get; set; }
        public long TotalSuccesses { get; set; }
        public long TotalFailures { get; set; }
        public double AverageExecutionMs { get; set; }

        /// <summary>Breakdown by trigger type name</summary>
        public Dictionary<string, int> TriggersByType { get; set; } = new();

        /// <summary>Name of the most-executed trigger (null if none)</summary>
        public string MostExecutedTriggerName { get; set; }

        public override string ToString() =>
            $"Block={BlockName} Triggers={TotalTriggers}(On={EnabledTriggers},Off={DisabledTriggers}) " +
            $"Exec={TotalExecutions} Success={TotalSuccesses} Fail={TotalFailures} AvgMs={AverageExecutionMs:F2}";
    }
}
