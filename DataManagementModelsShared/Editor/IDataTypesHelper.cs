using System.Collections.Generic;
using System.ComponentModel;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Editor
{
    public interface IDataTypesHelper
    {
        IDMEEditor DMEEditor { get; set; }
     
        List<DatatypeMapping> mapping { get; set; }
        List<string> GetDataClasses();
        //string[] GetMySqlDataTypes();
        string[] GetNetDataTypes();
        string[] GetNetDataTypes2();
        //string[] GetOracleDataTypes();
        //string[] GetSqliteDataTypes();
        //string[] GetSqlServerDataTypes();
        string GetDataType(string DSname, EntityField fld);
        string GetFieldTypeWoConversion(string DSname, EntityField fld);
    }
}