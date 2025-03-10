﻿
using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Editor
{
    public interface IDataTypesHelper:IDisposable
    {
        IDMEEditor DMEEditor { get; set; }
        int DefaultStringSize { get; set; }
        List<DatatypeMapping> mapping { get; set; }
        List<string> GetDataClasses();
        string[] GetNetDataTypes();
        string[] GetNetDataTypes2();
        string GetDataType(string DSname, EntityField fld);
        string GetFieldTypeWoConversion(string DSname, EntityField fld);
    }
}