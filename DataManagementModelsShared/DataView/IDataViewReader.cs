using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataView
{
    public interface IDataViewReader
    {
        IDataConnection Dataconnection { get; set; }
        IDMDataView DataView { get; set; }
        IDMEEditor DMEEditor { get; set; }
        bool FileLoaded { get; set; }
        ConnectionState State { get; set; }

        int AddEntityAsChild(IDataSource conn, string tablename, string SchemaName, string Filterparamters, int viewindex, int ParentTableIndex);
        int AddEntitytoDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters);
        int AddEntitytoDataView( EntityStructure maintab);
        int EntityListIndex(int entityid);
        int EntityListIndex(string entityname);
        IErrorsInfo RemoveChildEntities(int EntityID);
        IErrorsInfo RemoveEntity(int EntityID);

        int GenerateDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters);
        IErrorsInfo GenerateDataViewForChildNode(IDataSource conn, int pid, string tablename, string SchemaName, string Filterparamters);
        IDMDataView GenerateView(string ViewName, string ConnectionName);
        int GenerateViewFromTable(string viewname, IDataSource SourceConnection, string tablename, string SchemaName, string Filterparamters);
        List<DataSet> GetDataSetForView(string viewname);
        List<EntityStructure> GetEntitiesStructures(bool refresh = false);
        EntityStructure GetEntity(string entityname);
        ConnectionState GetFileState();
        IErrorsInfo LoadView();
        int NextHearId();
        ConnectionState OpenConnection();
        IDMDataView ReadDataViewFile(string pathandfilename);
        IDMDataView ReadDataViewFile(string path, string filename);
        void WriteDataViewFile(string filename);
        void WriteDataViewFile(string path, string filename);
    }
}
