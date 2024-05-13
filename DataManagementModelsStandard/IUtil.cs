using DataManagementModels.DriversConfigurations;
using DataManagementModels.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep
{
    public interface IUtil
    {
        EntityStructure GetEntityStructureFromType<T>();
        
        EntityStructure GetEntityStructureFromList<T>(List<T> list);
       
        ObservableCollection<T> ConvertToObservableCollection<T>(List<T> list);
        List<string> Classlist { get; set; }
      //  Dictionary<Type, DbType> typeMap { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        IDMEEditor DME { get; set; }
        IDMLogger Logger { get; set; }
        IConfigEditor ConfigEditor { get; set; }
        List<string> Namespacelist { get; set; }
        List<ParentChildObject> FunctionHierarchy { get; set; }
        List<T> ConvertDataTable<T>(DataTable dt);
        ObservableBindingList<T> ConvertDataTableToObservableBindingList<T>(DataTable dt) where T : Entity;
        IBindingList ConvertDataTableToObservableList(DataTable dataTable, Type type);
       DataTable CreateDataTableVer1(object[] array);
        DataTable CreateDataTableVer2(object[] arr);
        DataTable CreateDataTableFromFile(string strFilePath);
        string GetRelativePath(string fromPath, string toPath);
        //  object GetInstance(string strFullyQualifiedName);

        // Type GetType(string strFullyQualifiedName);
        TypeCode GetTypeCode(Type dest);
        T CreateInstance<T>(params object[] paramArray);
        bool IsObjectNumeric( object o);
        List<object> ConvertTableToList(DataTable dt, EntityStructure ent, Type enttype);
        List<object> GetListByDataTable(DataTable dt, Type type, EntityStructure enttype);
        List<object> GetListByDataTable(DataTable dt, string Namespace, string EntityName);
        object GetBindingListByDataTable(DataTable dt, Type type, EntityStructure enttype);
        object GetBindingListFromIList(IList inputList, Type itemType, EntityStructure entType);
        //   List<ExpandoObject> GetExpandoObject(DataTable dt, Type type, EntityStructure enttype);
        ConnectionDriversConfig LinkConnection2Drivers(IConnectionProperties cn);
       // dynamic GetTypeFromString(string strFullyQualifiedName);
        EntityStructure GetEntityStructure(DataTable tb);
        bool Download(string url, string downloadFileName, string downloadFilePath);
        Type GetTypeFromStringValue(string str);
        Type MakeGenericType(string typestring);
        Type MakeGenericListofType(string typestring);
        Type MakeGenericType(string typestring, Type[] parameters);
        DataRow ConvertItemClassToDataRow(EntityStructure ent);
        List<EntityField> GetFieldFromGeneratedObject(object dt, Type tp = null);
        bool ToCSVFile(DataTable list, string filepath);
        bool ToCSVFile(IList list, string filepath);
        DataTable JsonToDataTable(string jsonString);
        DataTable ToDataTable(IList list,Type tp);
        DataTable ToDataTable(Type tp);
        DataTable ToDataTable(IEntityStructure entity);
        Type GetEntityType(IDMEEditor DMEEditor, string EntityName, List<EntityField> Fields);
        object GetEntityObject(IDMEEditor DMEEditor, string EntityName, List<EntityField> Fields);
        Type GetListType(object someList);
        DataTable CreateDataTableFromListofStrings(List<string> strings);
        EntityStructure GetEntityStructureFromListorTable( dynamic retval);
        DataRow GetDataRowFromobject(string EntityName, Type enttype, object UploadDataRow, EntityStructure DataStruct);
        Type GetCollectionElementType(Type type);
        object MapObjectToAnother(IDMEEditor DMEEditor, string DestEntityname, EntityDataMap_DTL SelectedMapping, object sourceobj);
        object GetFieldValueFromObject(string fieldname, object sourceobj);
        IErrorsInfo SetFieldValueFromObject(string fieldname, object sourceobj,object value);
        List<T> GetTypedList<T>(List<object> ls);

        List<ConnectionProperties> LoadFiles(string[] filenames);

        bool IsFileValid(string filename);

        string CreateFileExtensionString();

        string CreateFileExtensionString(string extens);

        List<ConnectionProperties> LoadFiles(string directoryname, string extens);

        List<ConnectionProperties> CreateFileConnections(string[] filenames);

        ConnectionProperties CreateFileDataConnection(string file);

        ConnectionDriversConfig GetConnectionDrivers(string ext);


        IDataSource CreateDataSource(string filepath);

        Tuple<IErrorsInfo, RootFolder> CreateProject(string folderpath, ProjectFolderType folderType = ProjectFolderType.Files);


         Folder CreateFolderStructure(Folder folder, string path);

        Folder CreateFolderStructure(string path);
        
        RootFolder CreateFolderStructure(string path, ProjectFolderType folderType = ProjectFolderType.Files);
       
    }
}
