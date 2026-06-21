using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Represents a named in-memory record group (RECORD_GROUP in Oracle Forms terminology).
    /// Can be populated via query (RECORDGROUP_FROM_QUERY) or programmatic population.
    /// Used for LOVs, combo boxes, and find dialogs.
    /// </summary>
    public class RecordGroup
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DataSourceName { get; set; }
        public string EntityName { get; set; }
        public List<string> ColumnNames { get; set; } = new();
        public List<object> Records { get; set; } = new();
        public List<AppFilter> Filters { get; set; }
        public int RecordCount => Records.Count;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastPopulatedAt { get; set; }
        public bool IsPopulated { get; set; }

        public RecordGroup() { }

        public RecordGroup(string name, string dataSourceName, string entityName, List<AppFilter> filters = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DataSourceName = dataSourceName;
            EntityName = entityName;
            Filters = filters ?? new List<AppFilter>();
        }
    }
}
