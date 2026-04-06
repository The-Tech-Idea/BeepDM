using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOW.Models
{
    /// <summary>Records a single query execution for audit/debug purposes.</summary>
    public class QueryHistoryEntry
    {
        public DateTime ExecutedAt        { get; set; }
        public List<AppFilter> Filters    { get; set; }
        public int RowCount               { get; set; }
        public TimeSpan Duration          { get; set; }
        public bool Succeeded             { get; set; }
        public string EntityName          { get; set; }
    }
}
