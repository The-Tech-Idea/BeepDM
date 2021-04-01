
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.DataView
{
   

    public class DataViewReader : IDataViewReader
    {

        public IDMEEditor DMEEditor { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public ConnectionState State { get; set; } = ConnectionState.Closed;
        string DataViewFile;
        string FileName;
        public IDMDataView DataView { get; set; }
        public bool FileLoaded { get; set; } = false;
        int EntityIndex { get; set; } = 0;
        public DataViewReader()
        {

        }
        public DataViewReader(string datasourcename, IDMEEditor pDMEEditor, string pFilePath, string pFileName)
        {
            DataViewFile = Path.Combine(pFilePath, pFileName);
            FileName = pFileName;
            DMEEditor = pDMEEditor;
            Dataconnection = new DataViewConnection();
            Dataconnection.ErrorObject = DMEEditor.ErrorObject;
            Dataconnection.Logger = DMEEditor.Logger;

            if (DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).Any())
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).FirstOrDefault();
            }
            else
            {

                Dataconnection.ConnectionProp = new ConnectionProperties();
                Dataconnection.ConnectionProp.FileName = pFileName;
                Dataconnection.ConnectionProp.FilePath = pFilePath;
                Dataconnection.ConnectionProp.Category = DatasourceCategory.VIEWS;
                Dataconnection.ConnectionProp.DatabaseType = DataSourceType.Json;
                Dataconnection.ConnectionProp.DriverVersion = "1";
                Dataconnection.ConnectionProp.DriverName = "DataViewReader";
                DMEEditor.ConfigEditor.DataConnections.Add((ConnectionProperties)Dataconnection.ConnectionProp);
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
              
            }
            if (GetFileState() == ConnectionState.Open)
            {
                LoadView();

            }else
            {
                DataView = GenerateView(Path.GetFileNameWithoutExtension(pFileName), pFileName);
                WriteDataViewFile(DataViewFile);

            }
           
        


        }
        public ConnectionState OpenConnection()
        {

            try

            {

                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).FirstOrDefault();
                DataView.Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;

                if (File.Exists(DataViewFile))
                {
                    State = ConnectionState.Open;
                    LoadView();

                }
                else
                {
                    State = ConnectionState.Broken;

                }


            }
            catch (Exception ex)
            {
                State = ConnectionState.Broken;
                string errmsg = "Error Opening or Loading View File ";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return State;
        }
        public ConnectionState GetFileState()
        {
            if (File.Exists(DataViewFile))
            {
               // Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).FirstOrDefault();

                // State = ConnectionState.Open;
                //if ( Dataconnection.ConnectionProp.Entities.Count == 0)
                //{
                //    if (FileLoaded == false)
                //    {
                //        LoadView();
                //    }
                //    DataView.Entities = GetEntitiesStructures(false);
                //    Dataconnection.ConnectionProp.Entities = DataView.Entities;
                //    DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                //}
                //else
                //{
                //    DataView.Entities = Dataconnection.ConnectionProp.Entities;
                //}

                return ConnectionState.Open;


            }
            else
            {
                State = ConnectionState.Broken;
                return ConnectionState.Broken;
            }
        }
        public List<EntityStructure> GetEntitiesStructures(bool refresh = false)
        {
            List<EntityStructure> retval = new List<EntityStructure>();
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).FirstOrDefault();
            IDataSource dvds = DMEEditor.GetDataSource(FileName);

            if (File.Exists(DataViewFile))
            {
               

                
                if ((dvds.Entities == null) || (dvds.Entities.Count == 0))
                {
                    dvds.Entities = new List<EntityStructure>();
                    LoadView();
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                }
                else
                {
                    if (refresh)
                    {
                        dvds.Entities = new List<EntityStructure>();
                        LoadView();
                        DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                    }


                }


            }
            else
                retval = dvds.Entities;

            return retval;

        }

        #region "View Generating Methods"
        public IErrorsInfo RemoveEntity( int EntityID)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
               
                foreach (EntityStructure item in DataView.Entities.Where(x => x.ParentId == EntityID).ToList())
                {
                    RemoveEntity( item.Id);
                }
                DataView.Entities.Remove(DataView.Entities.Where(m => m.Id == EntityID).FirstOrDefault());
            }
            catch (Exception ex)
            {


                DMEEditor.AddLogMessage("Fail",ex.Message, DateTime.Now, -1, "", Errors.Failed);

            }


            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo RemoveChildEntities( int EntityID)
        {

            try
            {
                
                // CurrentEntity = CurrentView.Entity[EntityListIndex(ViewID, EntityID)];
                var ls = DataView.Entities.Where(x => x.ParentId == EntityID);
                foreach (EntityStructure item in ls.ToList())
                {
                    if (DataView.Entities.Where(y => y.ParentId == item.Id).Any())
                    {
                        RemoveChildEntities( item.Id);
                    }
                    DataView.Entities.Remove(item);
                }


                DMEEditor.AddLogMessage("Success", "Removed Child entities", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Remove Child entities";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
     
        public int GenerateViewFromTable(string viewname, IDataSource SourceConnection, string tablename, string SchemaName, string Filterparamters)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            int retval = 0;
            try
            {

                DataView.EntityDataSourceID = SourceConnection.DatasourceName;

                DataView.VID = Guid.NewGuid().ToString();
                retval = GenerateDataView(SourceConnection, tablename, SchemaName, Filterparamters);


            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Faile", $"Error in Loading View from file ({ex.Message}) ", DateTime.Now, 0, FileName, Errors.Failed);

            }
            return retval;

        }
        public int GenerateDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters)
        {
            //int maxcnt;

            DMEEditor.ErrorObject.Flag = Errors.Ok;
            List<ChildRelation> ds = conn.GetChildTablesList(tablename, SchemaName, Filterparamters);
            if (DataView.Entities.Count == 0)
            {


                EntityStructure viewheader = new EntityStructure() { Id = NextHearId(), EntityName = DataView.ViewName };
                viewheader.DataSourceID = conn.DatasourceName;
                viewheader.EntityName = DataView.ViewName.ToUpper();
                viewheader.ParentId = 0;
                viewheader.ViewID = DataView.ViewID;

                DataView.Entities.Add(viewheader);
            }


            //  maxcnt = DataView.Entities.Max(m => m.Id);
            EntityStructure maintab = new EntityStructure() { Id = NextHearId(), EntityName = tablename };
            maintab.DataSourceID = conn.DatasourceName;
            maintab.EntityName = tablename.ToUpper();
            maintab.ViewID = DataView.ViewID;
            maintab.ParentId = DataView.Entities[0].Id;

            DataView.Entities.Add(maintab);

            if (ds != null && ds.Count > 0)
            {

               // var tb = ds.Tables[0];
                //-------------------------------
                // Create Parent Record First
                //-------------------------------
                if (ds.Count > 0)
                {
                    foreach (ChildRelation r in ds)
                    {
                        EntityStructure a;
                        a = GetEntityStructure(DataView, DataView.Entities,r.child_table, tablename, r.child_column, r.parent_column, maintab.Id);



                    }

                }

            }

            return maintab.Id;

        }
        public IDMDataView GenerateView(string ViewName, string ConnectionName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            IDMDataView retval = null;

            try
            {
                retval = new DMDataView();
                retval.ViewID = 0;
                retval.EntityDataSourceID = ConnectionName;
            //    string schemaname = "";
                retval.ViewName = ViewName.ToUpper();
                retval.DataViewDataSourceID = ConnectionName;
                retval.Viewtype = ViewType.Table;
                retval.VID = Guid.NewGuid().ToString();
                EntityStructure viewheader = new EntityStructure() { Id = 1, EntityName = ViewName };

                viewheader.EntityName = ViewName.ToUpper();
                viewheader.ViewID = retval.ViewID;
                viewheader.ParentId = 0;
                retval.Entities.Add(viewheader);

            }
            catch (Exception ex)
            {


                DMEEditor.AddLogMessage("Fail", $"Error in creating View ({ex.Message}) ", DateTime.Now, 0, ViewName, Errors.Failed);

            }
            return retval;
        }
        public IErrorsInfo GenerateDataViewForChildNode(IDataSource conn, int pid, string tablename, string SchemaName, string Filterparamters)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try

            {


                List<ChildRelation> ds = conn.GetChildTablesList(tablename, conn.Dataconnection.ConnectionProp.SchemaName, Filterparamters);
                if (ds != null)
                {
                    EntityStructure pd = DataView.Entities.Where(c => c.Id == pid).FirstOrDefault();
                    if (ds != null && ds.Count > 0)
                    {

                       // var tb = ds.Tables[0];
                        //-------------------------------
                        // Create Parent Record First
                        //-------------------------------
                        if (ds.Count > 0)
                        {
                            foreach (ChildRelation r in ds)
                            {
                                EntityStructure a;
                                a = GetEntityStructure(DataView, DataView.Entities, r.child_table, tablename, r.child_column, r.parent_column, pd.Id);

                            }
                        }
                    }
                }
                
                DMEEditor.AddLogMessage("Success", $"Getting Child from DataSource", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error Getting Child from DataSource";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
           
          
            return DMEEditor.ErrorObject;
        }
        public int AddEntityAsChild(IDataSource conn, string tablename, string SchemaName, string Filterparamters, int viewindex, int ParentTableIndex)
        {

            DMEEditor.ErrorObject.Flag = Errors.Ok;

            List<ChildRelation> ds = conn.GetChildTablesList(tablename, SchemaName, Filterparamters);


            EntityStructure maintab = new EntityStructure() { Id = NextHearId(), EntityName = tablename };
            maintab.DataSourceID = conn.DatasourceName;
            maintab.EntityName = tablename;
            maintab.ViewID = DataView.ViewID;
            maintab.ParentId = DataView.Entities[0].Id;

            DataView.Entities.Add(maintab);


            if (ds != null && ds.Count > 0)
            {

                //var tb = ds.Tables[0];
                //-------------------------------
                // Create Parent Record First
                //-------------------------------
                if (ds.Count > 0)
                {
                    foreach (ChildRelation r in ds)
                    {
                        EntityStructure a;
                        a = GetEntityStructure(DataView, DataView.Entities, r.child_table, tablename, r.child_column, r.parent_column, maintab.Id);

                    }

                }

            }

            return maintab.Id;

        }
        public int AddEntitytoDataView(IDataSource conn, string tablename, string SchemaName, string Filterparamters)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            EntityStructure maintab;
            try
            {

                List<ChildRelation> ds = conn.GetChildTablesList(tablename, SchemaName, Filterparamters);
                maintab = new EntityStructure() { Id = NextHearId(), EntityName = tablename };
                maintab.DataSourceID = conn.DatasourceName;
                maintab.EntityName = tablename.ToUpper();
                maintab.ViewID = 0;
                maintab.ParentId = DataView.Entities[0].Id;
                maintab.DatabaseType = conn.DatasourceType;
                switch (maintab.DatabaseType)
                {
                    case DataSourceType.Oracle:

                    case DataSourceType.SqlServer:

                    case DataSourceType.Mysql:

                    case DataSourceType.SqlCompact:
                    case DataSourceType.Postgre:

                    case DataSourceType.Firebase:

                    case DataSourceType.FireBird:

                    case DataSourceType.Couchbase:

                    case DataSourceType.RavenDB:

                    case DataSourceType.MongoDB:

                    case DataSourceType.CouchDB:

                    case DataSourceType.VistaDB:

                    case DataSourceType.DB2:

                    case DataSourceType.SqlLite:
                        maintab.Viewtype = ViewType.Table;
                        break;
                    case DataSourceType.Text:

                    case DataSourceType.CSV:

                    case DataSourceType.Xls:
                    case DataSourceType.Json:

                    case DataSourceType.xml:
                        maintab.Viewtype = ViewType.File;
                        break;
                    case DataSourceType.WebService:

                    case DataSourceType.OPC:
                        maintab.Viewtype = ViewType.Url;
                        break;
                    default:
                        break;
                }

                DataView.Entities.Add(maintab);
                if (ds != null && ds.Count > 0)
                {
                   // var tb = ds.Tables[0];
                    //-------------------------------
                    // Create Parent Record First
                    //-------------------------------
                    if (ds.Count > 0)
                    {
                        foreach (ChildRelation r in ds)
                        {
                            EntityStructure a;
                            a = GetEntityStructure(DataView, DataView.Entities, r.child_table, tablename, r.child_column, r.parent_column, maintab.Id);



                        }

                    }

                }

                return maintab.Id;

            }
            catch (Exception )
            {


                return -1;
            }




        }
        #endregion  "View Generating Methods"
        #region "Misc and Util Methods"
        private EntityStructure GetEntityStructure(IDMDataView v, List<EntityStructure> Rootnamespacelist, string childtable, string parenttable, string childcolumn, string parentcolumn, int pid)
        {

            EntityStructure a;
            int pkid = NextHearId();
            IDataSource ds = (IDataSource)DMEEditor.GetDataSource(v.Entities.Where(x => x.Id == pid).FirstOrDefault().DataSourceID);
            string schemaname = "";
            if (ds.Category == DatasourceCategory.RDBMS)
            {
                IRDBSource rdb = (IRDBSource)ds;
                schemaname = rdb.GetSchemaName();
            }

            if (Rootnamespacelist.Where(f => f.ParentId == pid && f.EntityName.ToUpper() == childtable.ToUpper()).Count() == 0)//f => f.Id == childtable &&
            {
                a = new EntityStructure() { Id = pkid, ParentId = pid, EntityName = childtable.ToUpper(), ViewID = v.ViewID };
                a.DataSourceID = v.Entities.Where(x => x.Id == pid).FirstOrDefault().DataSourceID;

                a.Relations = ds.GetEntityforeignkeys(childtable.ToUpper(), schemaname);

                Rootnamespacelist.Add(a);


            }
            else
            {
                a = Rootnamespacelist.Where(f => f.ParentId == pid).FirstOrDefault(); //f.Id == childtable &&
                a.DataSourceID = v.EntityDataSourceID;
                a.Relations.Add(new RelationShipKeys { EntityColumnID = childcolumn.ToUpper(), ParentEntityColumnID = parentcolumn.ToUpper(), ParentEntityID = parenttable.ToUpper() });

            }
            return a;
        }

        public EntityStructure GetEntity(string entityname)
        {
            EntityStructure retval = null;


            try
            {
                retval = DataView.Entities[EntityListIndex(entityname)];
            }
            catch (Exception)
            {

                retval = null;
            }


            return retval;
        }
        public int NextHearId()
        {

            if (DataView.Entities != null)
            {
                if (DataView.Entities.Max(p => p.Id) > EntityIndex)
                {
                    EntityIndex = DataView.Entities.Max(p => p.Id);
                }
            }

            return EntityIndex+= 1;
        }
        public int EntityListIndex(string entityname)
        {

            return DataView.Entities.FindIndex(a => a.EntityName == entityname);
        }
        public int EntityListIndex(int entityid)
        {

            return DataView.Entities.FindIndex(a => a.Id == entityid);
        }
        #endregion "Misc and Util Methods"
        #region "Dataset and entity Structure Methods"
        public List<DataSet> GetDataSetForView(string viewname)
        {
            List<DataSet> retval = new List<DataSet>();
            EntityStructure entity = new EntityStructure();
            try
            {

                IDataSource ds = DMEEditor.GetDataSource(DataView.DataViewDataSourceID);
                EntityStructure maine = DataView.Entities[0];
                int mainid = maine.Id;


                ///--------------- Create Main Tables
                foreach (EntityStructure item in DataView.Entities.Where(x => x.ParentId == mainid && mainid != x.Id))
                {
                    DataSet mainds = new DataSet(viewname + "." + item.EntityName);
                    retval.Add(mainds);
                    //  entity = item;
                    if (item.Fields.Count == 0 || item.Relations.Count == 0)
                    {

                        EntityStructure entity1 = ds.GetEntityStructure(item.EntityName, false);

                        item.Relations = entity1.Relations;
                        item.Fields = entity1.Fields;

                    }

                    // Add main Table
                    mainds.Tables.Add(CreateTableFromEntityStructure(item));
                    // Adding Child Tables
                    GetChildTablesForDataset(ds, ref mainds, item, DataView.Entities.Where(f => f.Id != item.Id).ToList(), item.Id);
                }




            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Convert View to Object" + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            DMEEditor.ConfigEditor.SaveDataconnectionsValues();
            return retval;
        }
        private DataTable CreateTableFromEntityStructure(EntityStructure e)
        {
            DataTable dt = new DataTable(e.EntityName);
            try
            {
                foreach (EntityField item in e.Fields)
                {
                    DataColumn co = new DataColumn(item.fieldname, Type.GetType(item.fieldtype));
                    co.AllowDBNull = item.AllowDBNull;
                    co.AutoIncrement = item.IsAutoIncrement;
                    co.Unique = item.IsUnique;
                    dt.Columns.Add(co);
                }
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Table object from Entity structure " + mes, DateTime.Now, -1, mes, Errors.Failed);
                dt = null;
            };
            return dt;
        }
        private IErrorsInfo GetChildTablesForDataset(IDataSource ds, ref DataSet dataset, EntityStructure parenttb, List<EntityStructure> ls, int parentid)
        {

            foreach (EntityStructure tab in ls.Where(x => x.ParentId == parentid && x.Id != parentid && x.EntityName != parenttb.EntityName))
            {
                try
                {
                    DataTable tb = new DataTable();
                    EntityStructure entity = new EntityStructure();
                    entity = tab;
                    if (dataset.Tables.Contains(tab.EntityName) == false)
                    {

                        if (tab.Fields.Count == 0 || tab.Relations.Count == 0)
                        {

                            EntityStructure entity1 = ds.GetEntityStructure(tab.EntityName, false);
                            tab.Fields = entity1.Fields;
                            tab.Relations = entity1.Relations;

                        }




                        tb = CreateTableFromEntityStructure(tab);
                        dataset.Tables.Add(tb);
                    }
                    else
                    {
                        tb = dataset.Tables[tab.EntityName];
                    }




                    //---------------- Adding Relations 
                    List<RelationShipKeys> rl = entity.Relations.Where(c => c.ParentEntityID == parenttb.EntityName).ToList();

                    DataTable Parenttb = dataset.Tables[parenttb.EntityName];
                    foreach (string relationname in rl.Select(x => x.RalationName).Distinct())
                    {
                        int k = 0;
                        int cnt = rl.Where(u => u.RalationName == relationname).Count();
                        DataColumn[] ParentColumn = new DataColumn[cnt];
                        DataColumn[] ChildColumn = new DataColumn[cnt];
                        foreach (RelationShipKeys keys in rl.Where(u => u.RalationName == relationname))
                        {
                            ParentColumn[k] = Parenttb.Columns[keys.ParentEntityColumnID];
                            ChildColumn[k] = tb.Columns[keys.EntityColumnID];


                            k += 1;
                        }
                        tb.ParentRelations.Add(Parenttb.TableName + "." + tb.TableName, ParentColumn, ChildColumn);
                    }

                    if (ls.Where(x => x.ParentId == entity.Id).Count() > 0)
                    {
                        GetChildTablesForDataset(ds, ref dataset, entity, ls.Where(f => f.Id != entity.Id).ToList(), entity.Id);
                    }
                }
                catch (Exception ex)
                {
                    string mes = ex.Message;
                    DMEEditor.AddLogMessage(ex.Message, "Could not add child tables in Dataset " + mes, DateTime.Now, -1, mes, Errors.Failed);
                };

            }
            return DMEEditor.ErrorObject;
        }

        #endregion "Dataset and entity Structure Methods"
        #region Read/write Views to file
        public void WriteDataViewFile(string filename)
        {
            string path = Path.Combine(DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.DataView).FirstOrDefault().FolderPath, $"{filename}");
            DMEEditor.ConfigEditor.JsonLoader.Serialize(path,DataView);
            
        }

        public void WriteDataViewFile(string path, string filename)
        {
            string name = Path.Combine(path, $"{filename}");
            DMEEditor.ConfigEditor.JsonLoader.Serialize(name, DataView);
           
        }
        public IDMDataView ReadDataViewFile(string pathandfilename)
        {
           // String JSONtxt = File.ReadAllText(pathandfilename);
            return  DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<DMDataView>(pathandfilename);
           

        }
        public IDMDataView ReadDataViewFile(string path, string filename)
        {

            string name = Path.Combine(path, filename);
           // String JSONtxt = File.ReadAllText(name);
            return ReadDataViewFile(name);


        }
        public IErrorsInfo LoadView()
        {
            try
            {
                State = Dataconnection.OpenConnection();
                if (State == ConnectionState.Open)
                {
                    DataView =ReadDataViewFile(DataViewFile);

                    if (DataView.VID == null)
                    {
                        DataView.VID = Guid.NewGuid().ToString();
                    }

                }

                DMEEditor.AddLogMessage("Success", "Loaded File", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Load File";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }

        #endregion
    }
}
