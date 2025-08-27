using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Base partial class for Connection entity - contains core properties
    /// </summary>
    public partial class Connection
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
        public ConnectionType ConnectionType { get; set; }

        [Required]
        [MaxLength(100)]
        public string ServiceName { get; set; }

        // Authentication properties
        [MaxLength(500)]
        public string ApiKey { get; set; }

        [MaxLength(500)]
        public string ApiSecret { get; set; }

        [MaxLength(500)]
        public string AccessToken { get; set; }

        [MaxLength(500)]
        public string RefreshToken { get; set; }

        [MaxLength(500)]
        public string ClientId { get; set; }

        [MaxLength(500)]
        public string ClientSecret { get; set; }

        [MaxLength(1000)]
        public string BaseUrl { get; set; }

        [MaxLength(100)]
        public string Username { get; set; }

        [MaxLength(500)]
        public string Password { get; set; }

        // OAuth properties
        [MaxLength(1000)]
        public string AuthorizationUrl { get; set; }

        [MaxLength(1000)]
        public string TokenUrl { get; set; }

        [MaxLength(500)]
        public string Scope { get; set; }

        // Database connection properties
        [MaxLength(500)]
        public string ConnectionString { get; set; }

        [MaxLength(100)]
        public string DatabaseName { get; set; }

        [MaxLength(100)]
        public string ServerName { get; set; }

        public int? Port { get; set; }

        // Status and metadata
        public ConnectionStatus Status { get; set; } = ConnectionStatus.Draft;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        public DateTime? LastTestedDate { get; set; }

        public DateTime? TokenExpiryDate { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        [MaxLength(100)]
        public string ModifiedBy { get; set; }

        // Configuration
        [MaxLength(2000)]
        public string ConfigurationJson { get; set; }

        public bool IsActive { get; set; } = true;

        public int TimeoutSeconds { get; set; } = 30;

        public int RetryCount { get; set; } = 3;

        // Rate limiting
        public int RateLimitPerMinute { get; set; } = 60;

        public DateTime? LastRequestDate { get; set; }

        public int RequestCountThisMinute { get; set; }

        // Navigation properties
        public virtual ICollection<ConnectionTestLog> TestLogs { get; set; } = new List<ConnectionTestLog>();
    }

    /// <summary>
    /// Connection type enumeration
    /// </summary>
    public enum ConnectionType
    {
        ApiKey = 1,
        OAuth2 = 2,
        BasicAuth = 3,
        BearerToken = 4,
        Database = 5,
        Webhook = 6,
        Custom = 7
    }

    /// <summary>
    /// Connection status enumeration
    /// </summary>
    public enum ConnectionStatus
    {
        Draft = 0,
        Active = 1,
        Inactive = 2,
        Error = 3,
        Expired = 4
    }
}
