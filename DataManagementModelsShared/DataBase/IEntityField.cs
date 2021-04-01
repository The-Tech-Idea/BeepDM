using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataBase
{
    public interface IEntityField
    {
        bool AllowDBNull { get; set; }
       // bool created { get; set; }
        string EntityName { get; set; }
        DbFieldCategory fieldCategory { get; set; }
        int FieldIndex { get; set; }
        string fieldname { get; set; }
        string fieldtype { get; set; }
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
       // string statusdescription { get; set; }
    }
}