using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Base partial class for ScenarioExecutionLog entity - tracks scenario execution history
    /// </summary>
    public partial class ScenarioExecutionLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ScenarioId { get; set; }

        // Navigation property
        [ForeignKey("ScenarioId")]
        public virtual Scenario Scenario { get; set; }

        [Required]
        public ExecutionStatus Status { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public TimeSpan? Duration { get; set; }

        // Execution data
        [MaxLength(2000)]
        public string TriggerData { get; set; } // JSON data that triggered the scenario

        [MaxLength(2000)]
        public string ErrorMessage { get; set; }

        [MaxLength(4000)]
        public string ErrorDetails { get; set; } // JSON with detailed error information

        // Execution statistics
        public int TotalModules { get; set; }

        public int SuccessfulModules { get; set; }

        public int FailedModules { get; set; }

        public int SkippedModules { get; set; }

        // Performance metrics
        public long TotalMemoryUsageBytes { get; set; }

        public double AverageCpuUsagePercent { get; set; }

        public TimeSpan? AverageModuleDuration { get; set; }

        // Execution context
        [MaxLength(100)]
        public string ExecutionMode { get; set; } = "Manual"; // Manual, Scheduled, API, Webhook

        [MaxLength(500)]
        public string TriggerSource { get; set; } // Which trigger started this execution

        [MaxLength(1000)]
        public string ExecutionParameters { get; set; } // JSON parameters passed to execution

        // Retry information
        public int AttemptNumber { get; set; } = 1;

        public int MaxRetries { get; set; }

        // Scenario snapshot
        [MaxLength(500)]
        public string ScenarioVersion { get; set; }

        [MaxLength(2000)]
        public string ScenarioSnapshot { get; set; } // JSON snapshot of scenario configuration

        // Module execution logs
        public virtual ICollection<ModuleExecutionLog> ModuleLogs { get; set; } = new List<ModuleExecutionLog>();

        // Metadata
        [MaxLength(100)]
        public string ExecutedBy { get; set; }

        [MaxLength(100)]
        public string MachineName { get; set; }

        [MaxLength(45)]
        public string ClientIp { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
