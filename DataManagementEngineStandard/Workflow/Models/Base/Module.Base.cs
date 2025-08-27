using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Base partial class for Module entity - contains core properties
    /// </summary>
    public partial class Module
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
        public ModuleType ModuleType { get; set; }

        [Required]
        [MaxLength(100)]
        public string ModuleDefinitionName { get; set; }

        [Required]
        public int ScenarioId { get; set; }

        // Navigation property
        [ForeignKey("ScenarioId")]
        public virtual Scenario Scenario { get; set; }

        // Module connections
        public List<int> InputModules { get; set; } = new List<int>();

        public List<int> OutputModules { get; set; } = new List<int>();

        // Position in workflow designer
        public int PositionX { get; set; }

        public int PositionY { get; set; }

        // Configuration
        [MaxLength(4000)]
        public string ConfigurationJson { get; set; }

        public bool IsEnabled { get; set; } = true;

        public int ExecutionOrder { get; set; }

        // Execution properties
        public TimeSpan? EstimatedExecutionTime { get; set; }

        public int RetryCount { get; set; } = 0;

        public int MaxRetryCount { get; set; } = 3;

        public bool ContinueOnError { get; set; } = false;

        // Metadata
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        [MaxLength(100)]
        public string ModifiedBy { get; set; }

        // Data mapping
        public virtual ICollection<DataMapping> InputMappings { get; set; } = new List<DataMapping>();

        public virtual ICollection<DataMapping> OutputMappings { get; set; } = new List<DataMapping>();

        // Variables
        public virtual ICollection<ModuleVariable> Variables { get; set; } = new List<ModuleVariable>();

        // Execution logs
        public virtual ICollection<ModuleExecutionLog> ExecutionLogs { get; set; } = new List<ModuleExecutionLog>();
    }

    /// <summary>
    /// Module type enumeration
    /// </summary>
    public enum ModuleType
    {
        Trigger = 1,
        Action = 2,
        Router = 3
    }
}
