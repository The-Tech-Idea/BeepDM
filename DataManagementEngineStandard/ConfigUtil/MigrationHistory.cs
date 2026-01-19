using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil
{
    public class MigrationHistory
    {
        public string DataSourceName { get; set; }
        public DataSourceType DataSourceType { get; set; } = DataSourceType.Unknown;
        public List<MigrationRecord> Migrations { get; set; } = new List<MigrationRecord>();
    }

    public class MigrationRecord
    {
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
