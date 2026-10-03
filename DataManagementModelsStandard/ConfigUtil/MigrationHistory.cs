using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil
{
    public class MigrationHistory
    {
        /// <summary>Zero identifies a legacy history; coordinated snapshots use version one.</summary>
        public int StorageFormatVersion { get; set; }
        public string DataSourceName { get; set; }
        public DataSourceType DataSourceType { get; set; } = DataSourceType.Unknown;
        public List<MigrationRecord> Migrations { get; set; } = new List<MigrationRecord>();
    }

    public class MigrationRecord
    {
        /// <summary>Versioned governed plan payload, when this record captures a plan artifact.</summary>
        public string PlanArtifactJson { get; set; }
        public string MigrationId { get; set; }
        public string Name { get; set; }
        public DateTime AppliedOnUtc { get; set; } = DateTime.UtcNow;
        public bool Success { get; set; }
        public string Notes { get; set; }
        public List<MigrationStep> Steps { get; set; } = new List<MigrationStep>();
    }

    public class MigrationStep
    {
        public string Operation { get; set; }
        public string EntityName { get; set; }
        public string ColumnName { get; set; }
        public string Sql { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
