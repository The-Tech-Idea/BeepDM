using System.Collections.Generic;

namespace TheTechIdea.Beep.Workflow
{
    public interface IMapping_rep_fields
    {
        string ToEntityName { get; set; }
        int ToFieldIndex { get; set; }
        string ToFieldName { get; set; }
        string ToFieldType { get; set; }

        string FromEntityName { get; set; }
        int FromFieldIndex { get; set; }
        string FromFieldName { get; set; }
        string FromFieldType { get; set; }
        string Rules { get; set; }
    }
}