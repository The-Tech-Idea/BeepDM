namespace TheTechIdea.Beep.Services.Audit.Models
{
    /// <summary>
    /// High-level classification used to route, filter, and retain audit
    /// events. Producers must pick the most specific category; the
    /// <see cref="Custom"/> bucket is reserved for operator-defined flows
    /// outside the built-in subsystems.
    /// </summary>
    public enum AuditCategory
    {
        /// <summary>Default value when no category is supplied.</summary>
        Unspecified = 0,

        /// <summary>CRUD via FormsManager / UnitofWork / IDataSource.</summary>
        DataAccess = 1,

        /// <summary>Sign-in, sign-out, role grants/revokes.</summary>
        Auth = 2,

        /// <summary>Configuration changes via ConfigEditor or settings UI.</summary>
        Config = 3,

        /// <summary>DDL via MigrationManager or distributed schema service.</summary>
        Schema = 4,

        /// <summary>Resharding, plan changes, cross-shard transactions.</summary>
        Distributed = 5,

        /// <summary>Operator-defined event outside the built-in subsystems.</summary>
        Custom = 100
    }
}
