
using TheTechIdea.Logger;
using System.Data;
using TheTechIdea.Util;

using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using ExcelDataReader;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.FileManager
{
    [ClassProperties(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.CSV|DataSourceType.Xls,FileType = "csv,xls,xlsx") ]
    public class TxtXlsCSVFileSource : IDataSource
    {
        public event EventHandler<PassedArgs> PassEvent;
        public string Id { get; set; }
        public string DatasourceName { get; set; }
    
        public FileTypes FileType { get; set; }

        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        ConnectionState pConnectionStatus;
        public ConnectionState ConnectionStatus { get { return Dataconnection.ConnectionStatus; }  set { pConnectionStatus = value; }  }
        public List<string> EntitiesNames { get; set; } = new List<string>();

        public IDMEEditor DMEEditor { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public List<object> Records { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>(); 

      
        public bool HeaderExist { get; set; }
     
       // public TxtXlsCSVReader Reader { get; set; }
        public IDataConnection Dataconnection { get; set ; }
        string FileName;
        string FilePath;
        string CombineFilePath;
        char Delimiter;
        ExcelReaderConfiguration ReaderConfig;
        ExcelDataSetConfiguration ExcelDataSetConfig;
        public DataSet FileData { get; set; }
        IExcelDataReader reader;
        
        public TxtXlsCSVFileSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType ,  IErrorsInfo per)
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
         
          
           
            Category = DatasourceCategory.FILE;
            FileName = Dataconnection.ConnectionProp.FileName;
            FilePath = Dataconnection.ConnectionProp.FilePath;
            SetupConfig();
            //Reader = new TxtXlsCSVReader(, Logger, DMEEditor, FileType, per, Dataconnection.ConnectionProp.FilePath, null);
            //ConnectionStatus = OpenConnection();
            //Dataconnection.ConnectionStatus = ConnectionStatus;
            //if (ConnectionStatus == ConnectionState.Open)
            //{
            //   // Entities = Reader.Entities;
            //    EntitiesNames = Entities.Select(o => o.EntityName).ToList();
            //}
           // GetEntitesList();
            
          
        }
        public List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                ConnectionStatus = OpenConnection();
                Dataconnection.ConnectionStatus = ConnectionStatus;
                if (ConnectionStatus == ConnectionState.Open)
                {
                    EntitiesNames = new List<string>();
                    EntitiesNames = getWorksheetNames().ToList();
                }
                // List<EntityStructure> entlist = new List<EntityStructure>();
               
                

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
        public EntityStructure GetEntityDataType(string EntityName)
        {
            GetEntitesList();

            return Entities.Where(x => x.EntityName == EntityName).FirstOrDefault();
        }
        public Type GetEntityType(string EntityName)
        {
            ConnectionStatus = OpenConnection();
            Dataconnection.ConnectionStatus = ConnectionStatus;
            if (ConnectionStatus == ConnectionState.Open)
            {
                GetEntitesList();
                string filenamenoext = EntityName;
                DMTypeBuilder.CreateNewObject(EntityName, EntityName, Entities.Where(x => x.EntityName == EntityName).FirstOrDefault().Fields);
                return DMTypeBuilder.myType;
            }
            return null;
        }
        //public async Task<object> GetEntityDataAsync(string entityname, string filterstr)
        //{

        //    ErrorObject.Flag = Errors.Ok;
        //    try
        //    {
        //        return await Task.Run(() => Reader.ReadList(0, HeaderExist, 0, 0));
              
              

        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorObject.Flag = Errors.Failed;
        //        ErrorObject.Ex = ex;
        //        Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
        //        return null;
        //    }
           
        //}
        public  object GetEntity(string EntityName, List<ReportFilter> filter)
        {

            ErrorObject.Flag = Errors.Ok;
            try
            {
                DataTable dt=null;
                string qrystr="";
                ConnectionStatus = OpenConnection();
                
                Dataconnection.ConnectionStatus = ConnectionStatus;
                if (ConnectionStatus == ConnectionState.Open)
                {
                    dt = ReadDataTable(EntityName, HeaderExist, 0, 0);

                    if (filter != null)
                    {
                        if (filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                        {

                            foreach (ReportFilter item in filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)))
                            {
                                if (!string.IsNullOrEmpty(item.FilterValue) && !string.IsNullOrWhiteSpace(item.FilterValue))
                                {
                                    //  EntityField f = ent.Fields.Where(i => i.fieldname == item.FieldName).FirstOrDefault();
                                    if (item.Operator.ToLower() == "between")
                                    {
                                        if (item.valueType == "System.DateTime")
                                        {
                                            qrystr += "[" + item.FieldName + "] " + item.Operator + " '" + DateTime.Parse(item.FilterValue) + "' and  '" + DateTime.Parse(item.FilterValue1) + "'" + Environment.NewLine;
                                        }
                                        else
                                        {
                                            qrystr += "[" + item.FieldName + "] " + item.Operator + " " + item.FilterValue + " and  " + item.FilterValue1 + " " + Environment.NewLine;
                                        }

                                    }
                                    else
                                    {
                                        if (item.valueType == "System.String")
                                        {
                                            qrystr += "[" + item.FieldName + "] " + item.Operator + " '" + item.FilterValue + "' " + Environment.NewLine;
                                        }
                                        else
                                        {
                                            qrystr += "[" + item.FieldName + "] " + item.Operator + " " + item.FilterValue + " " + Environment.NewLine;
                                        }

                                    }

                                }



                            }
                        }

                        if (!string.IsNullOrEmpty(qrystr))
                        {
                            dt = dt.Select(qrystr).CopyToDataTable();
                        }

                    }
                }
              
                
                return dt;

            }
            catch (Exception ex)
            {
                
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
                return null;
            }
           // return Records;
        }
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {



                //Entities = new List<string>();
                //Entities.Add(Fileconnection.FileName);



                Logger.WriteLog("Successfully Retrieve Entites list ");

            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Unsuccessfully Retrieve Entites list {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }

            return ErrorObject;
        }
     
        //public async Task<List<object>> GetSampleData(bool HeaderExist)
        //{
        //    ErrorObject.Flag = Errors.Ok;
        //    try
        //    {


        //        SampleLines = await Task.Run(() => Reader.ReadList(0, HeaderExist, 0,100));
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorObject.Flag = Errors.Failed;
        //        ErrorObject.Ex = ex;
        //        Logger.WriteLog($"Error in getting Sample Data  ({ex.Message}) ");
        //    }
        //    return SampleLines;
        //}
        //public DataTable GetSampleDataTable(bool HeaderExist)
        //{
        //    ErrorObject.Flag = Errors.Ok;
        //    try
        //    {


        //        SourceEntityData = Reader.ReadDataTable(0,HeaderExist, 0, 100);
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorObject.Flag = Errors.Failed;
        //        ErrorObject.Ex = ex;
        //        Logger.WriteLog($"Error in getting Sample Data  ({ex.Message}) ");
        //    }
        //    return SourceEntityData;
        //}
        //public DataTable GetEntityDataTable(string EntityName, string filterstr)
        //{
        //    ErrorObject.Flag = Errors.Ok;
        //    try
        //    {

        //        SourceEntityData = Reader.ReadDataTable(0, HeaderExist, fromline, toline);

        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorObject.Flag = Errors.Failed;
        //        ErrorObject.Ex = ex;
        //        Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
        //    }
        //    return SourceEntityData;

        //}

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return null;
        }

        public DataSet GetChildTablesListFromCustomQuery(string tablename, string customquery)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public bool CheckEntityExist(string EntityName)
        {
            bool retval=false;
            if (Entities != null)
            {
                if (Entities.Where(x => string.Equals(x.EntityName, EntityName,StringComparison.OrdinalIgnoreCase)).Count() > 0)
                {
                    retval = true;
                }
                else
                    retval = false;

            }

            return retval;
        }

        public EntityStructure GetEntityStructure(string EntityName,bool refresh=false )
        {
            if (ConnectionStatus == ConnectionState.Open)
            {
                if (refresh || Entities.Count() == 0)
                {
                    GetEntityStructures( refresh);
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new ConfigUtil.DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                }

            }
       
           return Entities.Where(x => string.Equals(x.EntityName, EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        }


        public  object RunQuery( string qrystr)
        {
            throw new NotImplementedException();
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
            ConnectionStatus = OpenConnection();
           // Dataconnection.ConnectionStatus = ConnectionStatus;
            if (ConnectionStatus == ConnectionState.Open)
            {
                GetEntitesList();
                if (ConnectionStatus == ConnectionState.Open)
                {
                    if (refresh || Entities.Count() == 0)
                    {
                        GetEntityStructures(refresh);
                    }
                }
               
            }
            return Entities.Where(x => x.EntityName == fnd.EntityName).FirstOrDefault();
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
        #region "Excel and CSV Reader"
        public ConnectionState OpenConnection()
        {
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).FirstOrDefault();
            if (DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName) == null)
            {
                GetEntityStructures(true);
            }
            else
            {
                Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;
            };

            CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            if (File.Exists(CombineFilePath))
            {
                ConnectionStatus = ConnectionState.Open;

                Dataconnection.ConnectionStatus = ConnectionStatus;

                return ConnectionState.Open;


            }
            else
            {
                Dataconnection.ConnectionStatus = ConnectionStatus;
                ConnectionStatus = ConnectionState.Broken;
                return ConnectionState.Broken;
            }
        }
        public ConnectionState GetFileState()
        {
            if (File.Exists(CombineFilePath))
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).FirstOrDefault();
                Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;
                ConnectionStatus = ConnectionState.Open;
                Dataconnection.ConnectionStatus = ConnectionStatus;
                if (Entities.Count == 0)
                {
                    FileData = GetExcelDataSet();
                    Entities = GetEntityStructures(false);
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = FileName, Entities = Entities });
                    // ConnProp.Entities = Entities;
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();


                }
                Dataconnection.ConnectionStatus = ConnectionStatus;
                return ConnectionState.Open;


            }
            else
            {
               
                ConnectionStatus = ConnectionState.Broken;
                Dataconnection.ConnectionStatus = ConnectionStatus;
                return ConnectionState.Broken;
            }
        }
        public List<EntityStructure> GetEntityStructures(bool refresh = false)
        {
            List<EntityStructure> retval = new List<EntityStructure>();
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).FirstOrDefault();

            CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            if (File.Exists(CombineFilePath))
            {
                ConnectionStatus = ConnectionState.Open;
                Dataconnection.ConnectionStatus = ConnectionStatus;
                if ((Entities == null) || (Entities.Count == 0) || (refresh))
                {
                    Entities = new List<EntityStructure>();
                    Getfields();
                    Dataconnection.ConnectionProp.Delimiter = Delimiter;
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = FileName, Entities = Entities });
                    //  ConnProp.Entities = Entities;
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                }
                else
                {
                    ConnectionStatus = ConnectionState.Broken;
                    Dataconnection.ConnectionStatus = ConnectionStatus;
                    Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;
                    Delimiter = Dataconnection.ConnectionProp.Delimiter;

                }

                FileData = GetExcelDataSet();
                retval = Entities;
            }
            else
                retval = Entities;

            return retval;

        }
        private void SetupConfig()
        {
            ReaderConfig = new ExcelReaderConfiguration()
            {
                // Gets or sets the encoding to use when the input XLS lacks a CodePage
                // record, or when the input CSV lacks a BOM and does not parse as UTF8. 
                // Default: cp1252 (XLS BIFF2-5 and CSV only)
                FallbackEncoding = Encoding.GetEncoding(1252),

                //// Gets or sets the password used to open password protected workbooks.
                //Password = "password",

                // Gets or sets an array of CSV separator candidates. The reader 
                // autodetects which best fits the input data. Default: , ; TAB | # 
                // (CSV only)
                AutodetectSeparators = new char[] { ',', ';', '\t', '|', '#' },

                // Gets or sets a value indicating whether to leave the stream open after
                // the IExcelDataReader object is disposed. Default: false
                LeaveOpen = false,

                // Gets or sets a value indicating the number of rows to analyze for
                // encoding, separator and field count in a CSV. When set, this option
                // causes the IExcelDataReader.RowCount property to throw an exception.
                // Default: 0 - analyzes the entire file (CSV only, has no effect on other
                // formats)
                AnalyzeInitialCsvRows = 0,
            };
            ExcelDataSetConfig = new ExcelDataSetConfiguration()
            {
                // Gets or sets a value indicating whether to set the DataColumn.DataType 
                // property in a second pass.
                UseColumnDataType = true,

                // Gets or sets a callback to determine whether to include the current sheet
                // in the DataSet. Called once per sheet before ConfigureDataTable.
                FilterSheet = (tableReader, sheetIndex) => true,

                // Gets or sets a callback to obtain configuration options for a DataTable. 
                ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                {
                    // Gets or sets a value indicating the prefix of generated column names.
                    //EmptyColumnNamePrefix = "Column",

                    // Gets or sets a value indicating whether to use a row from the 
                    // data as column names.
                    UseHeaderRow = true,


                    // Gets or sets a callback to determine which row is the header row. 
                    // Only called when UseHeaderRow = true.
                    ReadHeaderRow = (rowReader) =>
                    {
                        // F.ex skip the first row and use the 2nd row as column headers:
                        // rowReader.Read();

                    },

                    // Gets or sets a callback to determine whether to include the 
                    // current row in the DataTable.
                    FilterRow = (rowReader) =>
                    {
                        //return true;
                        var hasData = false;
                        for (var u = 0; u < rowReader.FieldCount; u++)
                        {
                            if (rowReader[u] == null || string.IsNullOrEmpty(rowReader[u].ToString()))
                            {
                                continue;
                            }
                            else
                            {
                                hasData = true;
                                break;
                            }


                        }

                        return hasData;
                    },

                    // Gets or sets a callback to determine whether to include the specific
                    // column in the DataTable. Called once per column after reading the 
                    // headers.
                    FilterColumn = (rowReader, columnIndex) =>
                    {
                        return true;
                    }
                }
            };

        }

        private DataSet GetExcelDataSet()
        {
            DataSet ds = new DataSet();
            using (var stream = File.Open(Path.Combine(FilePath, FileName), FileMode.Open, FileAccess.Read))
            {
                switch (Dataconnection.ConnectionProp.Ext.Replace(".","").ToLower())
                {
                    case "csv":
                        reader = ExcelReaderFactory.CreateCsvReader(stream, ReaderConfig);
                        break;
                    case "xls":
                        reader = ExcelReaderFactory.CreateBinaryReader(stream, ReaderConfig);
                        break;
                    case "xlsx":
                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream, ReaderConfig);
                        break;
                    default:
                        throw new Exception("ExcelDataReaderFactory() - unknown/unsupported file extension");
                        // break;
                }


                // 2. Use the AsDataSet extension method
                ds = reader.AsDataSet(ExcelDataSetConfig);
                // The result of each spreadsheet is in result.Tables

                stream.Close();
            }
            return ds;
        }

        private void Getfields()
        {
            DataSet ds;
            Entities = new List<EntityStructure>();

            if (File.Exists(Path.Combine(FilePath, FileName)) == true)
            {
                try
                {


                    ds = GetExcelDataSet();

                    int i = 0;
                    foreach (DataTable tb in ds.Tables)
                    {
                        EntityStructure entityData = new EntityStructure();

                        string sheetname;
                        sheetname = tb.TableName;
                        entityData.Viewtype = ViewType.File;
                        entityData.DatabaseType = DataSourceType.Text;
                        entityData.DataSourceID = FileName;
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
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Ex = ex;
                    Logger.WriteLog($"Error in getting File format ({ex.Message}) ");
                }
            }
            else
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = null;
                Logger.WriteLog($"File is not Found ");
            }


        }
        private List<EntityField> GetSheetColumns(string psheetname)
        {
            return GetEntityDataType(psheetname).Fields.Where(x => x.EntityName == psheetname).ToList();
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
                FileData = GetExcelDataSet();
                dataRows = FileData.Tables[sheetno];
                toline = dataRows.Rows.Count;
                List<EntityField> flds = GetSheetColumns(FileData.Tables[sheetno].TableName);
                string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();
                // DMEEditor.classCreator.CreateClass(FileData.Tables[sheetno].TableName, flds, classpath);
                //GetTypeForSheetsFile(dataRows.TableName);
                return dataRows;
            }
            else
            {
                return null;
            }

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
        public DataTable ReadDataTable(string sheetname, bool HeaderExist = true, int fromline = 0, int toline = 100)
        {

            FileData = GetExcelDataSet();
            return ReadDataTable(GetSheetNumber(FileData, sheetname), HeaderExist, fromline, toline); ;
        }
       
        private EntityStructure GetEntityDataType(int sheetno)
        {

            return Entities[sheetno];
        }
        public List<Object> ReadList(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100)
        {
            if (GetFileState() == ConnectionState.Open)
            {
                DataTable dataRows = new DataTable();
                FileData = GetExcelDataSet();
                dataRows = FileData.Tables[sheetno];
                toline = dataRows.Rows.Count;
                List<EntityField> flds = GetSheetColumns(FileData.Tables[sheetno].TableName);
                string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();
                DMEEditor.classCreator.CreateClass(FileData.Tables[sheetno].TableName, flds, classpath);
                //GetTypeForSheetsFile(dataRows.TableName);
                Type a = Type.GetType("TheTechIdea.ProjectClasses." + FileData.Tables[sheetno].TableName);
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
                FileData = GetExcelDataSet();
                dataRows = FileData.Tables[sheetname];
                toline = dataRows.Rows.Count;
                List<EntityField> flds = GetSheetColumns(sheetname);
                string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();
                DMEEditor.classCreator.CreateClass(FileData.Tables[sheetname].TableName, flds, classpath);
                //GetTypeForSheetsFile(dataRows.TableName);
                Type a = Type.GetType("TheTechIdea.ProjectClasses." + FileData.Tables[sheetname].TableName);
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
        // Private Excel Data Reader Methods
        //-------------------------------------
        public IExcelDataReader getExcelReader()
        {
            return ExcelReaderFactory.CreateReader(System.IO.File.OpenRead(FilePath), ReaderConfig);
        }

        public IEnumerable<string> getWorksheetNames()
        {
            List<string> entlist = new List<string>();
            if (GetFileState() == ConnectionState.Open)
            {

                if (Entities.Count == 0)
                {
                    Entities = GetEntityStructures(true);

                }
                foreach (EntityStructure item in Entities)
                {

                    entlist.Add(item.EntityName);
                }

                //return null;
                //var workbook = FileData;
                //var sheets = from DataTable sheet in workbook.Tables.Cast<DataTable>() select sheet.TableName;
                //return sheets;

            }
            return entlist;

        }

        public IEnumerable<DataRow> getData(string sheet, bool firstRowIsColumnNames = false)
        {
            //var reader = this.getExcelReader();
            //reader.AsDataSet(ExcelDataSetConfig);
            if (GetFileState() == ConnectionState.Open)
            {
                var workSheet = FileData.Tables[sheet];
                var rows = from DataRow a in workSheet.Rows select a;
                return rows;
            }
            else
            {
                return null;
            }

        }

        public IEnumerable<DataRow> GetFirstSheetData(bool firstRowIsColumnNames = false)
        {
            //var reader = this.getExcelReader();
            //reader.AsDataSet(ExcelDataSetConfig);
            if (GetFileState() == ConnectionState.Open)
            {
                return getData(getWorksheetNames().First());
            }
            else
            {
                return null;
            }

        }
        public DataTable GetFirstSheetData(bool firstRowIsColumnNames = false, int sheetno = 0)
        {
            //var reader = this.getExcelReader();
            //reader.AsDataSet(ExcelDataSetConfig);
            //var workSheet = reader.AsDataSet().Tables[sheet];
            //var rows = from DataRow a in workSheet.Rows select a;
            return FileData.Tables[sheetno];
        }
        #endregion

    }
}
