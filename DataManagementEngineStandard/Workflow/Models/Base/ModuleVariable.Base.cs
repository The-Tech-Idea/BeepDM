using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Base partial class for ModuleVariable entity - handles module-level variables
    /// </summary>
    public partial class ModuleVariable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ModuleId { get; set; }

        // Navigation property
        [ForeignKey("ModuleId")]
        public virtual Module Module { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [MaxLength(50)]
        public string DataType { get; set; } = "String";

        public object Value { get; set; }

        public object DefaultValue { get; set; }

        public bool IsRequired { get; set; } = false;

        public bool IsOutput { get; set; } = false;

        public bool IsInput { get; set; } = false;

        // Validation
        [MaxLength(500)]
        public string ValidationPattern { get; set; }

        public object MinValue { get; set; }

        public object MaxValue { get; set; }

        [MaxLength(500)]
        public string AllowedValues { get; set; } // JSON array of allowed values

        // UI properties
        [MaxLength(100)]
        public string DisplayName { get; set; }

        [MaxLength(200)]
        public string Category { get; set; }

        public int DisplayOrder { get; set; } = 0;

        // Metadata
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        [MaxLength(100)]
        public string ModifiedBy { get; set; }

        public VariableScope Scope { get; set; } = VariableScope.Module;
    }
}
