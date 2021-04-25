using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.FileManager
{
    [ClassProperties(Category = DatasourceCategory.FILE, DatasourceType =  DataSourceType.Json)]
    public class JSONSource : IDataSource
    {
        public event EventHandler<PassedArgs> PassEvent;
        public DataSourceType DatasourceType { get ; set ; }
        public DatasourceCategory Category { get ; set ; }
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get ; set ; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get ; set ; }
        public List<object> Records { get ; set ; }
        public ConnectionState ConnectionStatus { get ; set ; }
        public DataSet ds { get ; set ; }
      
        public JSONSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = pDatasourceType;
            Dataconnection = new FileConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,

            };
         
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault();
            OpenConnection();
            Dataconnection.ConnectionStatus = ConnectionStatus;
            GetEntitesList();


        }
        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetDataReader(string querystring)
        {
            throw new NotImplementedException();
        }

        public  List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
              //  List<EntityStructure> entlist = new List<EntityStructure>();
                EntitiesNames = new List<string>();
                EntitiesNames=GetEntities().ToList();
                


                //  DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == DatasourceName).FirstOrDefault().Entities =entlist ;
                Logger.WriteLog("Successfully Retrieve Entites list ");

            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Unsuccessfully Retrieve Entites list {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }

            return EntitiesNames;

        }

        public async Task<object> GetEntityDataAsync(string entityname, string filterstr)
        {

            ErrorObject.Flag = Errors.Ok;
            try
            {
                return await Task.Run(() => ReadList(0, true, 0, 0));



            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
                return null;
            }
           
        }

        public  object GetEntity(string EntityName, List<ReportFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                return   ReadDataTable(EntityName, true, 0, 0);



            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
                return null;
            }
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

       

        public Type GetEntityType(string EntityName)
        {
            string filenamenoext = EntityName;
            DMTypeBuilder.CreateNewObject(EntityName, EntityName, Entities.Where(x => x.EntityName == EntityName).FirstOrDefault().Fields);
            return DMTypeBuilder.myType;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData)
        {
            throw new NotImplementedException();
        }

        public  object RunQuery( string qrystr)
        {
            throw new NotImplementedException();
        }
       
        public EntityStructure GetEntityStructure(string EntityName,bool refresh=false )
        {
            if (ConnectionStatus == ConnectionState.Open)
            {
                if ( Entities.Count() == 0)
                {
                     GetEntityStructures();
                }
            }
            if ( Entities.Count == 1)
            {
                return  Entities.FirstOrDefault();
            }
            else
                return  Entities.Where(x => x.EntityName == EntityName).FirstOrDefault();

        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {


            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }
        public LScript RunScript(LScript dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }
        public List<LScript> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }
        public Task<object> GetEntityAsync(string EntityName, List<ReportFilter> Filter)
        {
            throw new NotImplementedException();
        }
        #region "Json Reading MEthods"
        public ConnectionState OpenConnection()
        {
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == Dataconnection.ConnectionProp.ConnectionName).FirstOrDefault();
            Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(Dataconnection.ConnectionProp.ConnectionName).Entities;
            string filen = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            if (File.Exists(filen))
            {
                ConnectionStatus = ConnectionState.Open;

                Entities = GetEntityStructures(false);


                return ConnectionState.Open;


            }
            else
            {
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionState.Broken;
            }
        }
        public ConnectionState GetFileState()
        {
            string filen = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.ConnectionName);
            if (File.Exists(filen))
            {
                ConnectionStatus = ConnectionState.Open;


                return ConnectionState.Open;


            }
            else
            {
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionState.Broken;
            }
        }
        public IEnumerable<string> GetEntities()
        {
            List<string> entlist = new List<string>();
            if (GetFileState() == ConnectionState.Open)
            {

                entlist = (from DataTable sheet in ds.Tables select sheet.TableName).ToList();

            }
            else
            {
                if (Entities.Count() > 0)
                {

                    foreach (EntityStructure item in Entities)
                    {
                        entlist.Add(item.EntityName);
                    }
                    return entlist;
                }
            }
            return entlist;
        }
        public List<EntityStructure> GetEntityStructures(bool refresh = false)
        {
            List<EntityStructure> retval = new List<EntityStructure>();


            string filen = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            if (File.Exists(filen))
            {

                if ((Entities == null) || (Entities.Count == 0))
                {
                    Entities = new List<EntityStructure>();
                    Getfields();
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = Dataconnection.ConnectionProp.ConnectionName, Entities = Entities });
                    // Dataconnection.ConnectionProp.Entities = Entities;
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                }
                else
                {
                    if (refresh)
                    {
                        Entities = new List<EntityStructure>();
                        Getfields();
                        DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = Dataconnection.ConnectionProp.ConnectionName, Entities = Entities });
                        //  Dataconnection.ConnectionProp.Entities = Entities;
                        DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                    }
                    else
                    {
                        Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(Dataconnection.ConnectionProp.ConnectionName).Entities;

                    }

                }


            }
            else
                retval = Entities;

            return retval;

        }
        private void Getfields()
        {
            ds = new DataSet(); ;
            Entities = new List<EntityStructure>();

            if (File.Exists(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName)) == true)
            {
                try
                {

                    string json = File.ReadAllText(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));

                    ds = DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<DataSet>(json);

                    int i = 0;
                    foreach (DataTable tb in ds.Tables)
                    {
                        EntityStructure entityData = new EntityStructure();

                        string sheetname;
                        sheetname = tb.TableName;
                        entityData.EntityName = sheetname;
                        entityData.DataSourceID = Dataconnection.ConnectionProp.ConnectionName;
                        // entityData.SchemaOrOwnerOrDatabase = Database;
                        List<EntityField> Fields = new List<EntityField>();
                        int y = 0;

                        foreach (DataColumn field in tb.Columns)
                        {

                            Console.WriteLine("        " + field.ColumnName + ": " + field.DataType);

                            EntityField f = new EntityField();


                            //  f.tablename = sheetname;
                            f.fieldname = field.ColumnName;
                            f.fieldtype = field.DataType.ToString();
                            f.ValueRetrievedFromParent = false;
                            f.EntityName = sheetname;
                            f.FieldIndex = y;
                            Fields.Add(f);
                            y += 1;

                        }

                        i += 1;
                        entityData.Fields = new List<EntityField>();
                        entityData.Fields.AddRange(Fields);
                        Entities.Add(entityData);
                    }



                }
                catch (Exception)
                {


                }
            }
            else
            {
                DMEEditor.AddLogMessage("Error", "Json File Not Found " + Dataconnection.ConnectionProp.FileName, DateTime.Now, -1, "", Errors.Failed);

            }


        }
        private List<EntityField> GetSheetColumns(string psheetname)
        {
            return GetEntityDataType(psheetname).Fields.Where(x => x.EntityName == psheetname).ToList();
        }
        private EntityStructure GetEntityDataType(string psheetname)
        {

            return Entities.Where(x => x.EntityName == psheetname).FirstOrDefault();
        }
        private EntityStructure GetEntityDataType(int sheetno)
        {

            return Entities[sheetno];
        }
        public int GetSheetNumber(DataSet ls, string sheetname)
        {
            int retval = 0;
            if (ls.Tables.Count == 1)
            {
                retval = 0;
            }
            else
            {
                if (ls.Tables.Count == 0)
                {
                    retval = -1;
                }
                else
                {
                    if (ls.Tables.Count > 1)
                    {
                        int i = 0;
                        string found = "NotFound";
                        while (found == "Found" || found == "ExitandNotFound")
                        {

                            if (ls.Tables[i].TableName.Equals(sheetname, StringComparison.OrdinalIgnoreCase))
                            {
                                retval = i;

                                found = "Found";
                            }
                            else
                            {
                                if (i == ls.Tables.Count - 1)
                                {
                                    found = "ExitandNotFound";
                                }
                                else
                                {
                                    i += 1;
                                }
                            }
                        }


                    }
                }

            }
            return retval;

        }
    
        public DataTable ReadDataTable(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100)
        {
            if (GetFileState() == ConnectionState.Open)
            {
                DataTable dataRows = new DataTable();

                dataRows = ds.Tables[sheetno];
                toline = dataRows.Rows.Count;
                List<EntityField> flds = GetSheetColumns(ds.Tables[sheetno].TableName);

                return dataRows;
            }
            else
            {
                return null;
            }

        }
        public DataTable ReadDataTable(string sheetname, bool HeaderExist = true, int fromline = 0, int toline = 100)
        {


            return ReadDataTable(GetSheetNumber(ds, sheetname), HeaderExist, fromline, toline); ;
        }
        public void CreateClass(int sheetno = 0)
        {
            if (GetFileState() == ConnectionState.Open)
            {
                DataTable dataRows = new DataTable();

                dataRows = ds.Tables[sheetno];

                List<EntityField> flds = GetSheetColumns(ds.Tables[sheetno].TableName);
                string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();

                DMEEditor.classCreator.CreateClass(ds.Tables[sheetno].TableName, flds, classpath);

            }

        }
        public void CreateClass(string sheetname)
        {
            if (GetFileState() == ConnectionState.Open)
            {
                DataTable dataRows = new DataTable();

                dataRows = ds.Tables[sheetname];

                List<EntityField> flds = GetSheetColumns(ds.Tables[sheetname].TableName);
                string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();

                DMEEditor.classCreator.CreateClass(ds.Tables[sheetname].TableName, flds, classpath);

            }

        }
        public List<Object> ReadList(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100)
        {
            if (GetFileState() == ConnectionState.Open)
            {
                DataTable dataRows = new DataTable();

                dataRows = ds.Tables[sheetno];
                toline = dataRows.Rows.Count;
                List<EntityField> flds = GetSheetColumns(ds.Tables[sheetno].TableName);
                string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();
                CreateClass(sheetno);
                Type a = Type.GetType("TheTechIdea.ProjectClasses." + ds.Tables[sheetno].TableName);
                List<Object> retval = new List<object>();
                EntityStructure enttype = GetEntityDataType(sheetno);
                retval = DMEEditor.Utilfunction.GetListByDataTable(dataRows, a, enttype);
                return retval;
            }
            else
            {
                return null;
            }

        }
        public List<Object> ReadList(string sheetname, bool HeaderExist = true, int fromline = 0, int toline = 100)
        {
            if (GetFileState() == ConnectionState.Open)
            {
                DataTable dataRows = new DataTable();

                dataRows = ds.Tables[sheetname];
                toline = dataRows.Rows.Count;
                List<EntityField> flds = GetSheetColumns(sheetname);
                CreateClass(sheetname);
                Type a = Type.GetType("TheTechIdea.ProjectClasses." + dataRows);
                List<Object> retval = new List<object>();
                EntityStructure enttype = GetEntityDataType(sheetname);
                retval = DMEEditor.Utilfunction.GetListByDataTable(dataRows, a, enttype);
                return retval;
            }
            else
            {
                return null;
            }

        }
        #endregion
    }
}
