using System.Collections.Generic;
using System.ComponentModel;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Editor
{
    public interface IDataTypesHelper
    {
        IDMEEditor DMEEditor { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        IDMLogger Logger { get; set; }
        List<DatatypeMapping> mapping { get; set; }
        List<string> GetDataClasses();
        //string[] GetMySqlDataTypes();
        string[] GetNetDataTypes();
        string[] GetNetDataTypes2();
        //string[] GetOracleDataTypes();
        //string[] GetSqliteDataTypes();
        //string[] GetSqlServerDataTypes();
        string GetDataType(string DSname, EntityField fld);
      
    }
}