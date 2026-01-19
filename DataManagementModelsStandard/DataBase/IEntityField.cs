using System;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    public interface IEntityField
    {
      
         string GuidID { get; set; }
        bool AllowDBNull { get; set; }
       // bool created { get; set; }
        string EntityName { get; set; }
        DbFieldCategory FieldCategory { get; set; }
        int FieldIndex { get; set; }
        string FieldName { get; set; }
        string Fieldtype { get; set; }
       
        // bool FoundValue { get; set; }
        int id { get; set; }
        bool IsAutoIncrement { get; set; }
        bool IsCheck { get; set; }
        bool IsKey { get; set; }
        bool IsUnique { get; set; }
        short NumericPrecision { get; set; }
        short NumericScale { get; set; }
        int Size1 { get; set; }
        int Size2 { get; set; }
        bool Checked { get; set; }
        bool IsDisplayField { get; set; }
        string Caption { get; set; }
        bool IsIdentity { get; set; }   
        string Description { get; set; }
        // New fields
        int OrdinalPosition { get; set; }
        bool IsReadOnly { get; set; }
        bool IsRowVersion { get; set; }
        bool IsLong { get; set; }
        string DefaultValue { get; set; }
        string Expression { get; set; }
        string BaseTableName { get; set; }
        string BaseColumnName { get; set; }
        int MaxLength { get; set; }
        bool IsFixedLength { get; set; }
        bool IsHidden { get; set; }

    }
}