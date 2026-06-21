using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// A saved query template containing named filter sets for a block.
    /// Used by IQueryBuilderManager to persist and reload query presets.
    /// </summary>
    public class QueryTemplateInfo
    {
        /// <summary>Gets or sets the template name</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the block this template targets</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets an optional description</summary>
        public string Description { get; set; }

        /// <summary>Gets or sets when the template was created</summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>Gets or sets the saved filters</summary>
        public List<AppFilter> Filters { get; set; } = new List<AppFilter>();

        /// <summary>Gets or sets the per-field operator map saved with this template</summary>
        public Dictionary<string, QueryOperator> OperatorMap { get; set; } = new Dictionary<string, QueryOperator>(StringComparer.OrdinalIgnoreCase);
    }
}
