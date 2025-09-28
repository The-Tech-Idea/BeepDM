using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Performance metric for tracking operation performance
    /// </summary>
    public class PerformanceMetric
    {
        /// <summary>Gets or sets the operation name</summary>
        public string OperationName { get; set; }

        /// <summary>Gets or sets the average duration of the operation</summary>
        public TimeSpan Duration { get; set; }

        /// <summary>Gets or sets the number of times this operation has been executed</summary>
        public long Count { get; set; }
        
        /// <summary>Gets or sets the minimum duration recorded</summary>
        public TimeSpan MinDuration { get; set; } = TimeSpan.MaxValue;
        
        /// <summary>Gets or sets the maximum duration recorded</summary>
        public TimeSpan MaxDuration { get; set; } = TimeSpan.MinValue;
        
        /// <summary>Gets or sets the last execution time</summary>
        public DateTime LastExecuted { get; set; }
        
        /// <summary>Gets or sets the total duration for all executions</summary>
        public TimeSpan TotalDuration { get; set; }
        
        /// <summary>Gets or sets additional metadata for the metric</summary>
        public string Metadata { get; set; }
    }
}