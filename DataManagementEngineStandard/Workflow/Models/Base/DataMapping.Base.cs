using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Base partial class for DataMapping entity - handles field mapping between modules
    /// </summary>
    public partial class DataMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SourceModuleId { get; set; }

        [Required]
        public int TargetModuleId { get; set; }

        // Navigation properties
        [ForeignKey("SourceModuleId")]
        public virtual Module SourceModule { get; set; }

        [ForeignKey("TargetModuleId")]
        public virtual Module TargetModule { get; set; }

        [Required]
        [MaxLength(200)]
        public string SourceField { get; set; }

        [Required]
        [MaxLength(200)]
        public string TargetField { get; set; }

        [MaxLength(50)]
        public string SourceFieldType { get; set; }

        [MaxLength(50)]
        public string TargetFieldType { get; set; }

        // Mapping configuration
        public MappingType MappingType { get; set; } = MappingType.Direct;

        [MaxLength(1000)]
        public string TransformationExpression { get; set; }

        public bool IsRequired { get; set; } = false;

        public object DefaultValue { get; set; }

        // Validation
        [MaxLength(500)]
        public string ValidationPattern { get; set; }

        public bool EnableValidation { get; set; } = true;

        // Metadata
        public int MappingOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        [MaxLength(100)]
        public string ModifiedBy { get; set; }
    }

    /// <summary>
    /// Mapping type enumeration
    /// </summary>
    public enum MappingType
    {
        Direct = 1,
        Transform = 2,
        Lookup = 3,
        Conditional = 4,
        Custom = 5
    }
}
