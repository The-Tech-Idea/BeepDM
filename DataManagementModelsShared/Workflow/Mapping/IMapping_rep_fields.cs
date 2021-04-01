using System.Collections.Generic;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public interface IMapping_rep_fields
    {
        int FieldIndex1 { get; set; }
        int FieldIndex2 { get; set; }
        string FieldName1 { get; set; }
        string FieldName2 { get; set; }
        string FieldType1 { get; set; }
        string FieldType2 { get; set; }
        string Rules { get; set; }
    }
}