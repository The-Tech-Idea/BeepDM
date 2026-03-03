using System;

namespace TheTechIdea.Beep.DataView
{
    /// <summary>
    /// Represents a filter restriction for DataView queries.
    /// </summary>
    public class RestrictionValue
    {
        /// <summary>
        /// The name of the field to filter on.
        /// </summary>
        public string Fieldname { get; set; }

        /// <summary>
        /// The operator (e.g., "=", ">", "<", "LIKE").
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// The value to compare against.
        /// </summary>
        public object value { get; set; }
    }
}
