using System;

namespace TheTechIdea.Beep.DataBase
{
    public interface IColumnLookupList
    {
         int ID { get; set; }
         string GuidID { get; set; }
        string Display { get; set; }
        object Value { get; set; }
    }
}