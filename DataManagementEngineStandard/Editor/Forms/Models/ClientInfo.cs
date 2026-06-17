using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Represents client session metadata for datasource-level identity tracking.
    /// Mirrors Oracle Forms DBMS_APPLICATION_INFO (SET_CLIENT_INFO / SET_MODULE / SET_ACTION)
    /// and CLIENT_HOST / CLIENT_INFO built-ins. Datasource-agnostic — each datasource
    /// driver translates these into its native equivalent where supported.
    /// </summary>
    public class ClientInfo
    {
        /// <summary>User-defined client metadata (e.g., "User=Alice; Dept=Sales").</summary>
        public string ClientInfo { get; set; }

        /// <summary>The application module name (e.g., "OrderEntry").</summary>
        public string ModuleName { get; set; }

        /// <summary>The current action within the module (e.g., "EnteringNewOrder").</summary>
        public string Action { get; set; }

        /// <summary>The client machine hostname.</summary>
        public string ClientHost { get; set; }

        /// <summary>The client machine IP address (when determinable).</summary>
        public string ClientIpAddress { get; set; }

        /// <summary>The application user identity driving this session.</summary>
        public string UserName { get; set; }

        /// <summary>Timestamp of the last client-info change.</summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public ClientInfo() { }

        public ClientInfo(string hostName)
        {
            ClientHost = hostName;
            LastModified = DateTime.UtcNow;
        }
    }
}
