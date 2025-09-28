using System.Collections.Generic;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers
{
    /// <summary>
    /// Specification for JOIN operations
    /// </summary>
    public class JoinSpecification
    {
        public string MainTable { get; set; }
        public List<string> SelectColumns { get; set; } = new List<string>();
        public List<JoinClause> Joins { get; set; } = new List<JoinClause>();
        public string WhereClause { get; set; }
        public string OrderBy { get; set; }
    }

    /// <summary>
    /// Individual JOIN clause specification
    /// </summary>
    public class JoinClause
    {
        public string JoinType { get; set; } = "INNER"; // INNER, LEFT, RIGHT, FULL OUTER
        public string TableName { get; set; }
        public string OnCondition { get; set; }
    }

    /// <summary>
    /// Specification for window functions
    /// </summary>
    public class WindowFunctionSpec
    {
        public List<string> SelectColumns { get; set; } = new List<string>();
        public string WindowFunction { get; set; } // ROW_NUMBER(), RANK(), etc.
        public List<string> PartitionBy { get; set; } = new List<string>();
        public List<string> OrderBy { get; set; } = new List<string>();
        public string WindowFrame { get; set; } // ROWS BETWEEN ... AND ...
        public string Alias { get; set; }
        public string WhereClause { get; set; }
    }
}