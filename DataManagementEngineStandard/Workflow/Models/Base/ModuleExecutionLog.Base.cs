using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Base partial class for ModuleExecutionLog entity - tracks module execution history
    /// </summary>
    public partial class ModuleExecutionLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ModuleId { get; set; }

        [Required]
        public int ScenarioExecutionId { get; set; }

        // Navigation properties
        [ForeignKey("ModuleId")]
        public virtual Module Module { get; set; }

        [ForeignKey("ScenarioExecutionId")]
        public virtual ScenarioExecutionLog ScenarioExecution { get; set; }

        [Required]
        public ExecutionStatus Status { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public TimeSpan? Duration { get; set; }

        // Execution data
        [MaxLength(4000)]
        public string InputData { get; set; } // JSON

        [MaxLength(4000)]
        public string OutputData { get; set; } // JSON

        [MaxLength(2000)]
        public string ErrorMessage { get; set; }

        [MaxLength(4000)]
        public string ErrorDetails { get; set; } // JSON with stack trace, etc.

        // Retry information
        public int AttemptNumber { get; set; } = 1;

        public int MaxRetries { get; set; }

        // Performance metrics
        public long MemoryUsageBytes { get; set; }

        public double CpuUsagePercent { get; set; }

        // Module specific data
        [MaxLength(500)]
        public string ModuleVersion { get; set; }

        [MaxLength(1000)]
        public string ConfigurationSnapshot { get; set; } // JSON snapshot of module config

        // Metadata
        [MaxLength(100)]
        public string ExecutedBy { get; set; }

        [MaxLength(100)]
        public string MachineName { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Execution status enumeration
    /// </summary>
    public enum ExecutionStatus
    {
        Pending = 0,
        Running = 1,
        Success = 2,
        Failed = 3,
        Skipped = 4,
        Cancelled = 5,
        Timeout = 6
    }
}
