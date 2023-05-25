using System;

namespace TheTechIdea.Beep.DataBase
{
    public interface IInsertTransaction
    {
         int ID { get; set; }
         string GuidID { get; set; } 
        string DestField { get; set; }
        Type FieldType { get; set; }
        string RecordIndex { get; set; }
        string SourceField { get; set; }
        string SourceValue { get; set; }
    }
}