
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Tools;
using TheTechIdea.Util;


namespace TheTechIdea.DataManagment_Engine.FileManager
{
    public class JSONReader : IJSONReader
    {
        public List<EntityStructure> Entities { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionProperties ConnProp { get; set; }
        public ConnectionState State { get; set; } = ConnectionState.Closed;
        private DataSet ds;
        public JSONReader(string datasourcename, IDMEEditor pDMEEditor, IConnectionProperties pConnProp, List<EntityField> pfields = null)
        {

            ConnProp = (ConnectionProperties)pConnProp;

            DMEEditor = pDMEEditor;



        }
        public ConnectionState OpenConnection()
        {
            ConnProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == ConnProp.ConnectionName).FirstOrDefault();
            Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(ConnProp.ConnectionName).Entities;
            string filen = Path.Combine(ConnProp.FilePath, ConnProp.FileName);
            if (File.Exists(filen))
            {
                State = ConnectionState.Open;

                Entities = GetEntityStructures(false);


                return ConnectionState.Open;


            }
            else
            {
                State = ConnectionState.Broken;
                return ConnectionState.Broken;
            }
        }
        public ConnectionState GetFileState()
        {
            string filen = Path.Combine(ConnProp.FilePath, ConnProp.ConnectionName);
            if (File.Exists(filen))
            {
                State = ConnectionState.Open;


                return ConnectionState.Open;


            }
            else
            {
                State = ConnectionState.Broken;
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


            string filen = Path.Combine(ConnProp.FilePath, ConnProp.FileName);
            if (File.Exists(filen))
            {

                if ((Entities == null) || (Entities.Count == 0))
                {
                    Entities = new List<EntityStructure>();
                    Getfields();
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = ConnProp.ConnectionName, Entities = Entities });
                   // ConnProp.Entities = Entities;
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                }
                else
                {
                    if (refresh)
                    {
                        Entities = new List<EntityStructure>();
                        Getfields();
                        DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = ConnProp.ConnectionName, Entities = Entities });
                      //  ConnProp.Entities = Entities;
                        DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                    }
                    else
                    {
                        Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(ConnProp.ConnectionName).Entities;

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

            if (File.Exists(Path.Combine(ConnProp.FilePath, ConnProp.FileName)) == true)
            {
                try
                {

                    string json = File.ReadAllText(Path.Combine(ConnProp.FilePath, ConnProp.FileName));

                    ds = DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<DataSet>(json);

                    int i = 0;
                    foreach (DataTable tb in ds.Tables)
                    {
                        EntityStructure entityData = new EntityStructure();

                        string sheetname;
                        sheetname = tb.TableName;
                        entityData.EntityName = sheetname;
                        entityData.DataSourceID = ConnProp.ConnectionName;
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
                catch (Exception )
                {


                }
            }
            else
            {
                DMEEditor.AddLogMessage("Error", "Json File Not Found " + ConnProp.FileName, DateTime.Now, -1, "", Errors.Failed);

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
        private void GetTypeForSheetsFile(string pSheetname)
        {
            List<EntityField> flds = GetSheetColumns(pSheetname);
            DMTypeBuilder.CreateNewObject(pSheetname, pSheetname, flds);



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
    }
}
