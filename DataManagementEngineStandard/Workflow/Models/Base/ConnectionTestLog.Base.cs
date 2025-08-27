using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Base partial class for ConnectionTestLog entity - tracks connection testing history
    /// </summary>
    public partial class ConnectionTestLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ConnectionId { get; set; }

        // Navigation property
        [ForeignKey("ConnectionId")]
        public virtual Connection Connection { get; set; }

        [Required]
        public ConnectionTestStatus Status { get; set; }

        public DateTime TestedAt { get; set; } = DateTime.UtcNow;

        public TimeSpan? Duration { get; set; }

        // Test results
        [MaxLength(2000)]
        public string ErrorMessage { get; set; }

        [MaxLength(4000)]
        public string ErrorDetails { get; set; } // JSON with detailed error information

        [MaxLength(1000)]
        public string ResponseData { get; set; } // JSON response from the service

        // Test configuration
        [MaxLength(500)]
        public string TestParameters { get; set; } // JSON parameters used for testing

        [MaxLength(100)]
        public string TestMethod { get; set; } = "Default"; // Type of test performed

        // Performance metrics
        public long ResponseTimeMs { get; set; }

        public int ResponseSizeBytes { get; set; }

        public int HttpStatusCode { get; set; }

        // Connection snapshot
        [MaxLength(500)]
        public string ConnectionSnapshot { get; set; } // JSON snapshot of connection config

        // Metadata
        [MaxLength(100)]
        public string TestedBy { get; set; }

        [MaxLength(100)]
        public string MachineName { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Connection test status enumeration
    /// </summary>
    public enum ConnectionTestStatus
    {
        Success = 1,
        Failed = 2,
        Timeout = 3,
        AuthError = 4,
        NetworkError = 5,
        ConfigurationError = 6
    }
}
