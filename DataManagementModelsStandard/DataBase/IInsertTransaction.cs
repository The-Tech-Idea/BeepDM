using System;

namespace TheTechIdea.Beep.DataBase
{
    public interface IInsertTransaction
    {
        string DestField { get; set; }
        Type FieldType { get; set; }
        string RecordIndex { get; set; }
        string SourceField { get; set; }
        string SourceValue { get; set; }
    }
}