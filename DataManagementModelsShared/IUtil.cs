using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.Reflection;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine
{
    public interface IUtil
    {
        List<string> Classlist { get; set; }
      //  Dictionary<Type, DbType> typeMap { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        IDMEEditor DME { get; set; }
        IDMLogger Logger { get; set; }
        List<string> Namespacelist { get; set; }
        List<ParentChildObject> FunctionHierarchy { get; set; }
        List<T> ConvertDataTable<T>(DataTable dt);
        DataTable CreateDataTableVer1(object[] array);
        DataTable CreateDataTableVer2(object[] arr);
        string GetRelativePath(string fromPath, string toPath);
        object GetInstance(string strFullyQualifiedName);
       
        Type GetType(string strFullyQualifiedName);
        List<object> GetListByDataTable(DataTable dt, Type type, EntityStructure enttype);
     //   List<ExpandoObject> GetExpandoObject(DataTable dt, Type type, EntityStructure enttype);
        ConnectionDriversConfig LinkConnection2Drivers(IConnectionProperties cn);
        dynamic GetTypeFromString(string strFullyQualifiedName);
        EntityStructure GetEntityStructure(DataTable tb);
        bool Download(string url, string downloadFileName, string downloadFilePath);
        Type GetTypeFromStringValue(string str);
        Type MakeGenericType(string typestring);
        Type MakeGenericListofType(string typestring);

    }
}