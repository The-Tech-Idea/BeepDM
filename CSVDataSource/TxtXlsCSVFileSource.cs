
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.CSV;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        ConnectionState pConnectionStatus;
        public ConnectionState ConnectionStatus { get { return Dataconnection.ConnectionStatus; }  set { pConnectionStatus = value; }  }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public IDMEEditor DMEEditor { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>(); 
        public bool HeaderExist { get; set; }
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
            Dataconnection = new FileConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject,

            };
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);


            Category = DatasourceCategory.FILE;
            FileName = Dataconnection.ConnectionProp.FileName;
            FilePath = Dataconnection.ConnectionProp.FilePath;
            SetupConfig();
          
          

        }
        public int GetEntityIdx(string entityName)
        {
            if (Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase) || p.DatasourceEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return -1;
            }


        }
        public ConnectionState Openconnection()
        {
            ConnectionStatus = Dataconnection.OpenConnection();

            if (ConnectionStatus == ConnectionState.Open)
            {
                if (DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName) == null)
                {
                    GetEntityStructures(true);
                }
                else
                {
                    Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;
                };
                CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            }

            return ConnectionStatus;
          
        }

        public ConnectionState Closeconnection()
        {
           return ConnectionStatus = ConnectionState.Closed;
        }


        public List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
              
               
                if (GetFileState() == ConnectionState.Open)
                {
                    EntitiesNames = new List<string>();
                    EntitiesNames = getWorksheetNames().ToList();
                 
                   
                    if (Entities.Count > 0)
                    {
                        List<string> ename = Entities.Select(p => p.EntityName.ToUpper()).ToList();
                        List<string> diffnames = ename.Except(EntitiesNames).ToList();
                        if (diffnames.Count > 0)
                        {
                            foreach (string item in diffnames)
                            {
                                Entities.Add(GetEntityStructure(item, false));
                                int idx = Entities.FindIndex(p => p.EntityName.Equals(item, StringComparison.OrdinalIgnoreCase) || p.DatasourceEntityName.Equals(item, StringComparison.OrdinalIgnoreCase));
                                Entities[idx].Created = false;
                                
                            }
                        }
                    }
                }
               
               
                

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
           

            if (GetFileState() == ConnectionState.Open)
            {
                GetEntitesList();
                string filenamenoext = EntityName;
                DMTypeBuilder.CreateNewObject(EntityName, EntityName, Entities.Where(x => x.EntityName == EntityName).FirstOrDefault().Fields);
                return DMTypeBuilder.myType;
            }
            return null;
        }
        public  object GetEntity(string EntityName, List<ReportFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                DataTable dt=null;
                string qrystr="";
             

                if (GetFileState() == ConnectionState.Open)
                {

                    dt = ReadDataTable(EntityName, HeaderExist, 0, 0);
                    SyncFieldTypes(ref dt,EntityName);
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
        public  TypeCode ToConvert( Type dest)
        {
            TypeCode retval = TypeCode.String;
           switch (dest.ToString())
            {
                case "System.String":
                    retval = TypeCode.String;
                    break;
                case "System.Decimal":
                    retval = TypeCode.Decimal;
                    break;
                case "System.DateTime":
                    retval = TypeCode.DateTime;
                    break;
                case "System.Char":
                    retval = TypeCode.Char;
                    break;
                case "System.Boolean":
                    retval = TypeCode.Boolean;
                    break;
                case "System.DBNull":
                    retval = TypeCode.DBNull;
                    break;
                case "System.Byte":
                    retval = TypeCode.Byte;
                    break;
                case "System.Int16":
                    retval = TypeCode.Int16;
                    break;
                case "System.Double":
                    retval = TypeCode.Double;
                    break;
                case "System.Int32":
                    retval = TypeCode.Int32;
                    break;
                case "System.Int64":
                    retval = TypeCode.Int64;
                    break;
                case "System.Single":
                    retval = TypeCode.Single;
                    break;
                case "System.Object":
                    retval = TypeCode.Object;
                    break;
                   

            }
            return retval;
        }
        private void SyncFieldTypes(ref DataTable dt, string EntityName)
        {
            EntityStructure ent = GetEntityStructure(EntityName);
            DataTable newdt = new DataTable(EntityName);
            if (ent != null)
            {
                foreach (var item in ent.Fields)
                {
                    DataColumn cl = new DataColumn(item.fieldname, Type.GetType(item.fieldtype));
                    newdt.Columns.Add(cl);
                    //dt.Columns[item.fieldname].DataType = Type.GetType(item.fieldtype);
                }
                foreach (DataRow dr in dt.Rows)
                {
                    try
                    {
                        DataRow r = newdt.NewRow();
                        foreach (var item in ent.Fields)
                        {
                            if (dr[item.fieldname] != DBNull.Value)
                            {
                                r[item.fieldname] = Convert.ChangeType(dr[item.fieldname], ToConvert(Type.GetType(item.fieldtype)));
                            }
                         
                       
                        }
                        try
                        {
                            newdt.Rows.Add(r);
                        }
                        catch (Exception aa)
                        {

                           
                        }
                      
                    }
                    catch (Exception ex)
                    {

                       // throw;
                    }
            
                }
            }
            dt = newdt;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {

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
            if(GetFileState()== ConnectionState.Open)
            {
                if (Entities != null)
                {
                    if (Entities.Where(x => string.Equals(x.EntityName, EntityName, StringComparison.OrdinalIgnoreCase)).Count() > 0)
                    {
                        retval = true;
                    }
                    else
                        retval = false;

                }

            }

            return retval;
        }
        public EntityStructure GetEntityStructure(string EntityName,bool refresh=false )
        {
           

            if (GetFileState() == ConnectionState.Open)
            {
                if (refresh || Entities.Count() == 0)
                {
                    Entities= DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(DatasourceName).Entities;
                    if (refresh || Entities.Count() == 0)
                    {
                        GetEntityStructures(refresh);
                        DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new ConfigUtil.DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                    }

                }

            }
            EntityStructure retval = null;
            if (Entities != null)
            {
                retval = Entities.Where(x => string.Equals(x.OriginalEntityName, EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            }
            return retval;

        }
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {


            if (GetFileState() == ConnectionState.Open)
            {
                if (refresh || Entities.Count() == 0)
                {
                    Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(DatasourceName).Entities;
                    if (refresh || Entities.Count() == 0)
                    {
                        GetEntityStructures(refresh);
                        DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new ConfigUtil.DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                    }
                }
            }
            EntityStructure retval = null;
            if (Entities != null)
            {
                retval = Entities.Where(x => string.Equals(x.OriginalEntityName, fnd.EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            }
            return retval;
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
            return (Task<object>)GetEntity(EntityName, Filter);
        }
        #region "Excel and CSV Reader"
        
        public ConnectionState GetFileState()
        {
            if (ConnectionStatus == ConnectionState.Open)
            {
                return ConnectionStatus;
            }else
            {
                return Openconnection();
            }

           
        }
        public List<EntityStructure> GetEntityStructures(bool refresh = false)
        {
            List<EntityStructure> retval = new List<EntityStructure>();
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).FirstOrDefault();
           // Openconnection();

            if (GetFileState() == ConnectionState.Open)
            {
                CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
                if (File.Exists(CombineFilePath))
                {

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

                        Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;
                        Delimiter = Dataconnection.ConnectionProp.Delimiter;

                    }


                    retval = Entities;
                }
                else
                    retval = Entities;

            }
          

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
            

            if (GetFileState() == ConnectionState.Open)
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
                        entityData.DatasourceEntityName = tb.TableName;
                        entityData.Caption = tb.TableName;
                        entityData.EntityName = sheetname;
                        entityData.OriginalEntityName = sheetname;
                        
                        List<EntityField> Fields = new List<EntityField>();


                        entityData.Fields = new List<EntityField>();
                        entityData.Fields.AddRange(GetFieldsbyTableScan(tb.TableName, tb.Columns));
                       // entityData.Fields = GetStringSizeFromTable(entityData.Fields, tb);
                        Entities.Add(entityData);
                    }



                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("Fail", $"Error in getting File format {ex.Message}", DateTime.Now, 0, FileName, Errors.Failed);

                }
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

            if (FileData == null)
            {
                FileData = GetExcelDataSet();
            }
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
                if (FileData == null)
                {
                    FileData = GetExcelDataSet();
                }
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
        private List<EntityField> GetFieldTypes(string sheetname,DataColumnCollection datac)
        {
            List<DataRow> dt = getData(sheetname).ToList();
            List<EntityField> flds = new List<EntityField>();
            try
            {
               
                int y = 0;

                //----------------------------------------
                foreach (DataColumn field in datac)
                {
                    EntityField f = new EntityField();


                    //  f.tablename = sheetname;
                    f.fieldname = field.ColumnName;
                    f.fieldtype = field.DataType.ToString();
                    f.ValueRetrievedFromParent = false;
                    f.EntityName = sheetname;
                    f.FieldIndex = y;
                    f.Checked = false;
                    f.AllowDBNull = true;
                    f.IsAutoIncrement = false;
                    f.IsCheck = false;
                    f.IsKey = false;
                    f.IsUnique = false;

                    flds.Add(f);
                    if (f.fieldname.ToLower().Contains("date") || f.fieldname.ToLower().Contains("_dt"))
                    {
                        f.fieldtype = "System.DateTime";
                        f.Checked = true;
                    }
                    y += 1;
                    if (f.Checked == false)
                    {
                        bool foundval = true;
                      
                        int i = 0;
                        while (foundval)
                        {
                            DataRow dr = dt[i];
                            if (dr[f.fieldname] != DBNull.Value)
                            {
                                string valstring = dr[f.fieldname].ToString();
                                decimal dval;
                                double dblval;
                                long longval;
                                bool boolval;
                                int intval;
                                short shortval;
                                float floatval;
                                DateTime dateval=DateTime.Now;


                                if (decimal.TryParse(valstring,out dval))
                                {
                                    f.fieldtype = "System.Decimal";

                                }else
                                if (double.TryParse(valstring, out dblval))
                                {
                                    f.fieldtype = "System.Double";
                                }
                                else
                                if (DateTime.TryParse(valstring, out dateval))
                                {
                                    f.fieldtype = "System.DateTime";

                                }
                                else
                                    if (long.TryParse(valstring, out longval))
                                {
                                    f.fieldtype = "System.Long";

                                }
                                else
                                    if (bool.TryParse(valstring, out boolval))
                                {
                                    f.fieldtype = "System.Bool";

                                }
                                else
                                    if (float.TryParse(valstring, out floatval))
                                {
                                    f.fieldtype = "System.Float";

                                }
                                else
                                if(int.TryParse(valstring, out intval))
                                {
                                    f.fieldtype = "System.Int";

                                }
                                else
                                    if(short.TryParse(valstring, out shortval))
                                {
                                    f.fieldtype = "System.Short";

                                }
                                else                                 
                                    f.fieldtype = "System.String";
                              
                                foundval = false    ;
                            }
                            i += 1;
                            if (i >= dt.Count)
                            {
                                foundval = false;
                            }
                        }
                    }
                   
                    
                }
                return flds;
            }
            catch (Exception ex)
            {

                return null;
            }
        }
        private List<EntityField> GetFieldsbyTableScan(string sheetname, DataColumnCollection datac)
        {
            List<DataRow> tb = getData(sheetname).ToList();
            List<EntityField> flds = new List<EntityField>();
            int y = 0;
            string valstring;
            decimal dval;
            double dblval;
            long longval;
            bool boolval;
            int intval;
            short shortval;
            float floatval;
          
            DateTime dateval = DateTime.Now;
            foreach (DataColumn field in datac)
            {
                EntityField f = new EntityField();


                //  f.tablename = sheetname;
                f.fieldname = field.ColumnName;
                f.fieldtype = field.DataType.ToString();
                f.ValueRetrievedFromParent = false;
                f.EntityName = sheetname;
                f.FieldIndex = y;
                f.Checked = false;
                f.AllowDBNull = true;
                f.IsAutoIncrement = false;
                f.IsCheck = false;
                f.IsKey = false;
                f.IsUnique = false;
                y++;
                flds.Add(f);
              


            }
            foreach (DataRow r in tb)
            {
                try
                {
                    foreach (EntityField f in flds)
                    {
                        try
                        {
                            //if (f.fieldname.Contains("AGE"))
                            //{
                            //    DMEEditor.AddLogMessage("aa");
                            //}
                            if (f.fieldname.ToLower().Contains("date") || f.fieldname.ToLower().Contains("_dt"))
                            {
                                f.fieldtype = "System.DateTime";
                                f.Checked = true;
                            }
                            else
                            if (r[f.fieldname] != DBNull.Value)
                            {
                                valstring = r[f.fieldname].ToString();

                                dateval = DateTime.Now;

                                if (int.TryParse(valstring, out intval))
                                {
                                    f.fieldtype = "System.Int";

                                }
                                else
                                if (decimal.TryParse(valstring, out dval))
                                {
                                    f.fieldtype = "System.Decimal";

                                }
                                else
                                        if (double.TryParse(valstring, out dblval))
                                {
                                    f.fieldtype = "System.Double";
                                }
                                else
                                        if (DateTime.TryParse(valstring, out dateval))
                                {
                                    f.fieldtype = "System.DateTime";

                                }
                                else
                                            if (long.TryParse(valstring, out longval))
                                {
                                    f.fieldtype = "System.Long";

                                }
                                else
                                            if (bool.TryParse(valstring, out boolval))
                                {
                                    f.fieldtype = "System.Bool";

                                }
                                else
                                            if (float.TryParse(valstring, out floatval))
                                {
                                    f.fieldtype = "System.Float";

                                }
                                else
                                            if (short.TryParse(valstring, out shortval))
                                {
                                    f.fieldtype = "System.Short";

                                }
                                else
                                    f.fieldtype = "System.String";
                            }
                        }
                        catch (Exception Fieldex)
                        {

                           
                        }
                     
                    
                        try
                        {
                            if (f.fieldtype.Equals("System.String", StringComparison.OrdinalIgnoreCase))
                            {
                                if (r[f.fieldname] != DBNull.Value)
                                {
                                    if (!string.IsNullOrEmpty(r[f.fieldname].ToString()))
                                    {
                                        if (r[f.fieldname].ToString().Length > f.Size1)
                                        {
                                            f.Size1 = r[f.fieldname].ToString().Length;
                                        }

                                    }
                                }

                            }
                        }
                        catch (Exception stringsizeex)
                        {
                           
                        }
                        try
                        {
                            if (f.fieldtype.Equals("System.Decimal", StringComparison.OrdinalIgnoreCase))
                            {
                                if (r[f.fieldname] != DBNull.Value)
                                {
                                    if (!string.IsNullOrEmpty(r[f.fieldname].ToString()))
                                    {
                                        valstring = r[f.fieldname].ToString();
                                        if (decimal.TryParse(valstring, out dval))
                                        {

                                            f.fieldtype = "System.Decimal";
                                            f.Size1 = GetDecimalPrecision(dval);
                                            f.Size2 = GetDecimalScale(dval);
                                        }
                                    }
                                }

                            }
                        }
                        catch (Exception decimalsizeex)
                        {

                           
                        }
                      
                    }
                }
                catch (Exception rowex)
                {

                    
                }
                
            }
            foreach (EntityField fld in flds)
            {
                if (fld.fieldtype.Equals("System.string", StringComparison.OrdinalIgnoreCase))
                {
                    if (fld.Size1 == 0)
                    {
                        fld.Size1 = 150;
                    }

                }
            }
            return flds;
        }
        private List<EntityField> GetStringSizeFromTable(List<EntityField> entityFields ,DataTable tb)
        {

            foreach (DataRow r in tb.Rows)
            {
                foreach (EntityField fld in entityFields)
                {
                    if (fld.fieldtype.Equals("System.string", StringComparison.OrdinalIgnoreCase))
                    {
                        if (r[fld.fieldname] != DBNull.Value)
                        {
                            if (!string.IsNullOrEmpty(r[fld.fieldname].ToString()))
                            {
                                if (r[fld.fieldname].ToString().Length > fld.Size1)
                                {
                                    fld.Size1 = r[fld.fieldname].ToString().Length;
                                }
                           
                            }
                        }
                        
                    }
                    decimal dval;
               
                    string valstring;
                   
                    if (fld.fieldtype.Equals("System.Decimal", StringComparison.OrdinalIgnoreCase))
                    {
                        if (r[fld.fieldname] != DBNull.Value)
                        {
                            if (!string.IsNullOrEmpty(r[fld.fieldname].ToString()))
                            {
                                valstring = r[fld.fieldname].ToString();
                                if (decimal.TryParse(valstring, out dval))
                                {
                                    
                                    fld.fieldtype = "System.Decimal";
                                    fld.Size1 = GetDecimalPrecision(dval);
                                    fld.Size2= GetDecimalScale(dval);

                                }
                               
                                   
                                

                            }
                        }

                    }
                   
                  
                }
            }
            foreach (EntityField fld in entityFields)
            {
                if (fld.fieldtype.Equals("System.string", StringComparison.OrdinalIgnoreCase))
                {
                  if (fld.Size1==0)
                  {
                        fld.Size1 = 150;
                  }

                }
            }
            return entityFields;
        }
        public static int GetDecimalScale( decimal value)
        {
            if (value == 0)
                return 0;
            int[] bits = decimal.GetBits(value);
            return (int)((bits[3] >> 16) & 0x7F);
        }

        public static int GetDecimalPrecision( decimal value)
        {
            if (value == 0)
                return 0;
            int[] bits = decimal.GetBits(value);
            //We will use false for the sign (false =  positive), because we don't care about it.
            //We will use 0 for the last argument instead of bits[3] to eliminate the fraction point.
            decimal d = new Decimal(bits[0], bits[1], bits[2], false, 0);
            return (int)Math.Floor(Math.Log10((double)d)) + 1;
        }
       
        #endregion

    }
}
