using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Base partial class for Scenario entity - contains core properties
    /// </summary>
    public partial class Scenario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        public ScenarioStatus Status { get; set; } = ScenarioStatus.Draft;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        public DateTime? LastRunDate { get; set; }

        public int? CreatedByUserId { get; set; }

        [MaxLength(50)]
        public string CreatedByUserName { get; set; }

        // Navigation properties
        public virtual ICollection<Module> Modules { get; set; } = new List<Module>();

        public virtual ICollection<ScenarioVariable> Variables { get; set; } = new List<ScenarioVariable>();

        public virtual ICollection<ScenarioExecutionLog> ExecutionLogs { get; set; } = new List<ScenarioExecutionLog>();

        // Configuration properties
        public bool IsActive { get; set; } = true;

        public bool IsTemplate { get; set; } = false;

        [MaxLength(100)]
        public string Category { get; set; }

        public int? FolderId { get; set; }

        // Scheduling properties
        public bool IsScheduled { get; set; } = false;

        [MaxLength(100)]
        public string ScheduleExpression { get; set; } // Cron expression

        public DateTime? NextScheduledRun { get; set; }

        // Execution properties
        public int ExecutionTimeoutMinutes { get; set; } = 30;

        public int MaxRetryAttempts { get; set; } = 3;

        public bool ContinueOnError { get; set; } = false;

        // Metadata
        [MaxLength(1000)]
        public string Tags { get; set; } // JSON array of tags

        public int Version { get; set; } = 1;

        [MaxLength(500)]
        public string VersionNotes { get; set; }
    }

    /// <summary>
    /// Scenario status enumeration
    /// </summary>
    public enum ScenarioStatus
    {
        Draft = 0,
        Active = 1,
        Inactive = 2,
        Archived = 3,
        Error = 4
    }
}
