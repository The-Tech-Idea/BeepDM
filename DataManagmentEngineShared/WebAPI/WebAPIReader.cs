
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Tools;
using TheTechIdea.Util;

namespace DataManagmentEngineShared.WebAPI
{
    public class WebAPIReader
    {
        public IHttpClientFactory ClientFactory { get; set; }
        public HttpClient _client { get; set; } = new HttpClient();
     
        public WebAPIDataConnection cn { get; set; }
        public List<EntityStructure> Entities { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionProperties ConnProp { get; set; }
        public ConnectionState State { get; set; } = ConnectionState.Closed;
        public  DataSet ds { get; set; }
        public WebAPIReader(string datasourcename, IDMEEditor pDMEEditor, IDataConnection pConn, List<EntityField> pfields = null)
        {
            cn =(WebAPIDataConnection) pConn;
            ConnProp = (ConnectionProperties)pConn.ConnectionProp;
            DMEEditor = pDMEEditor;
            ds = new DataSet();



        }
        #region "http Client Code"
        public virtual bool GetData()
        {
            try
            {
                if (GetHttpClient())
                {
                    Task<string> retval = GetResponseAsync();

                   // ds = (DataSet)DMEEditor.ConfigEditor.JsonLoader.DeserializeObjectString<DataSet>(retval.Result.ToString());



                }
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Cannot Connect to Service {ex.Message}", DateTime.Now, -1, cn.ConnectionProp.Url, TheTechIdea.Util.Errors.Failed);

                return false;
            }

        }
        private bool GetHttpClient()
        {
            try
            {
                _client = ClientFactory.CreateClient(ConnProp.ConnectionName);
                _client.BaseAddress = new Uri(cn.ConnectionProp.Url);
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Cannot Connect to  Service  {ex.Message}", DateTime.Now, -1, cn.ConnectionProp.Url, TheTechIdea.Util.Errors.Failed);

                return false;
            }

        }
        private async Task<string> GetResponseAsync()
        {
            try
            {
                string retstring = await _client.GetFromJsonAsync<string>(cn.ConnectionProp.Url);
                return retstring;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Cannot get Data from Service {ex.Message}", DateTime.Now, -1, cn.ConnectionProp.Url, TheTechIdea.Util.Errors.Failed);

                return "Error";
            }
        }
        public ConnectionState OpenConnection()
        {
            ConnProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == ConnProp.ConnectionName).FirstOrDefault();
            Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(ConnProp.ConnectionName).Entities;

            if (Entities.Count() > 0)
            {
                State = ConnectionState.Open;

                Entities = GetEntityStructures(false);

                return ConnectionState.Open;


            }
            else
            {
                if (GetData())
                {
                    Entities = GetEntityStructures(false);

                    return ConnectionState.Open;

                }
                else
                {
                    State = ConnectionState.Broken;
                    return ConnectionState.Broken;
                }

            }
        }
        #endregion

        public virtual IEnumerable<string> GetEntities()
        {
            List<string> entlist = new List<string>();
            if (cn.ConnectionStatus==ConnectionState.Open)
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


           
          

                if ((Entities == null) || (Entities.Count == 0))
                {
                    Entities = new List<EntityStructure>();
                    Getfields();
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = ConnProp.ConnectionName, Entities = Entities });
               //  ConnProp.Entities = Entities;
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                }
                else
                {
                    if (refresh)
                    {
                        Entities = new List<EntityStructure>();
                        Getfields();
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = ConnProp.ConnectionName, Entities = Entities });
                   // ConnProp.Entities = Entities;
                        DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                    }
                    else
                    {

                    Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(ConnProp.ConnectionName).Entities;

                    }

                }


          

            return retval;

        }
        private void Getfields()
        {
            ds = new DataSet(); ;
            Entities = new List<EntityStructure>();
             try
                {

              

                    int i = 0;
                    foreach (DataTable tb in ds.Tables)
                    {
                        EntityStructure entityData = new EntityStructure();

                        string sheetname;
                        sheetname = tb.TableName;
                        entityData.EntityName = sheetname;
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
                catch (Exception ex)
                {

                DMEEditor.AddLogMessage("Error", $"Json File Not Found  {ex.Message} " + ConnProp.FileName, DateTime.Now, -1, "", Errors.Failed);
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

                            if (ls.Tables[i].TableName == sheetname)
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
        private object GetTypeForSheetsFile(string pSheetname)
        {
            List<EntityField> flds = GetSheetColumns(pSheetname);
           return DMTypeBuilder.CreateNewObject(pSheetname, pSheetname, flds);



        }
        public DataTable ReadDataTable(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100)
        {
            if (cn.ConnectionStatus == ConnectionState.Open)
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
            if (cn.ConnectionStatus == ConnectionState.Open)
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
            if (cn.ConnectionStatus == ConnectionState.Open)
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
            if (cn.ConnectionStatus == ConnectionState.Open)
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
            if (cn.ConnectionStatus == ConnectionState.Open)
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
    }
}
