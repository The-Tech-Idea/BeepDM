using DataManagementModels.DriversConfigurations;
using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor
{
    public interface IDataTypesHelper:IDisposable
    {
        IDMEEditor DMEEditor { get; set; }
     
        List<DatatypeMapping> mapping { get; set; }
        List<string> GetDataClasses();
        string[] GetNetDataTypes();
        string[] GetNetDataTypes2();
        string GetDataType(string DSname, EntityField fld);
        string GetFieldTypeWoConversion(string DSname, EntityField fld);
    }
}