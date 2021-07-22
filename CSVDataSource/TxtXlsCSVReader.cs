using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Logger;
using TheTechIdea.Tools;
using TheTechIdea.Util;


using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.FileManager
{
    public class TxtXlsCSVReader : ITxtXlsCSVReader
    {
        public char Delimiter { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }

        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public List<ColumnDef> ColumnValuesDef { get; set; } = new List<ColumnDef>();
        public IDMEEditor DMEEditor { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public DataSet FileData { get; set; }
        public IConnectionProperties ConnProp { get; set; }
      //  private IClassCreator  csCr;
        public ConnectionState State { get; set; } = ConnectionState.Closed;
        string filen;
        ExcelReaderConfiguration ReaderConfig;
        ExcelDataSetConfiguration ExcelDataSetConfig;

        IExcelDataReader reader;

        public TxtXlsCSVReader(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, FileTypes pFileType, IErrorsInfo per, string pFilePath, List<EntityField> pfields = null)
        {
            Logger = logger;
            ErrorObject = per;
            FilePath = pFilePath;
            FileName = datasourcename;
            ErrorObject.Flag = Errors.Ok;
            DMEEditor = pDMEEditor;
            SetupConfig();
           

        }
        public ConnectionState OpenConnection()
        {
            ConnProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).FirstOrDefault();
             if (DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName) == null)
            {
                GetEntityStructures(true);
            }else
            {
                Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;
            };
            
            filen = Path.Combine(ConnProp.FilePath, ConnProp.FileName);
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
        public ConnectionState GetFileState()
        {
            if (File.Exists(filen))
            {
                ConnProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).FirstOrDefault();
                Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;
                State = ConnectionState.Open;
                if (Entities.Count == 0)
                {
                    FileData = GetExcelDataSet();
                    Entities = GetEntityStructures(false);
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = FileName, Entities = Entities });
                   // ConnProp.Entities = Entities;
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();


                }

                return ConnectionState.Open;


            }
            else
            {
                State = ConnectionState.Broken;
                return ConnectionState.Broken;
            }
        }
        public List<EntityStructure> GetEntityStructures(bool refresh = false)
        {
            List<EntityStructure> retval = new List<EntityStructure>();
            ConnProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == FileName).FirstOrDefault();

            filen = Path.Combine(ConnProp.FilePath, ConnProp.FileName);
            if (File.Exists(filen))
            {

                if ((Entities == null) || (Entities.Count == 0)||(refresh))
                {
                    Entities = new List<EntityStructure>();
                    Getfields();
                    ConnProp.Delimiter = Delimiter;
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = FileName, Entities = Entities });
                  //  ConnProp.Entities = Entities;
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                }
                else
                {
                     
                        Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;
                    Delimiter = ConnProp.Delimiter;
               
                }

                FileData = GetExcelDataSet();
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
                switch (ConnProp.Ext.ToLower())
                {
                    case ".csv":
                        reader = ExcelReaderFactory.CreateCsvReader(stream, ReaderConfig);
                        break;
                    case ".xls":
                        reader = ExcelReaderFactory.CreateBinaryReader(stream, ReaderConfig);
                        break;
                    case ".xlsx":
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
        private EntityStructure GetEntityDataType(string psheetname)
        {

            return Entities.Where(x => x.EntityName == psheetname).FirstOrDefault();
        }
        private EntityStructure GetEntityDataType(int sheetno)
        {

            return Entities[sheetno];
        }
        //public List<Object> ReadList(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100)
        //{
        //    if (GetFileState() == ConnectionState.Open)
        //    {
        //        DataTable dataRows = new DataTable();
        //        FileData = GetExcelDataSet();
        //        dataRows = FileData.Tables[sheetno];
        //        toline = dataRows.Rows.Count;
        //        List<EntityField> flds = GetSheetColumns(FileData.Tables[sheetno].TableName);
        //        string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();
        //        DMEEditor.classCreator.CreateClass(FileData.Tables[sheetno].TableName, flds, classpath);
        //        //GetTypeForSheetsFile(dataRows.TableName);
        //        Type a = Type.GetType("TheTechIdea.ProjectClasses." + FileData.Tables[sheetno].TableName);
        //        List<Object> retval = new List<object>();
        //        EntityStructure enttype = GetEntityDataType(sheetno);
        //        retval = DMEEditor.Utilfunction.GetListByDataTable(dataRows, a, enttype);
        //        return retval;
        //    }
        //    else
        //    {
        //        return null;
        //    }

        //}
        //public List<Object> ReadList(string sheetname, bool HeaderExist = true, int fromline = 0, int toline = 100)
        //{
        //    if (GetFileState() == ConnectionState.Open)
        //    {
        //        DataTable dataRows = new DataTable();
        //        FileData = GetExcelDataSet();
        //        dataRows = FileData.Tables[sheetname];
        //        toline = dataRows.Rows.Count;
        //        List<EntityField> flds = GetSheetColumns(sheetname);
        //        string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();
        //        DMEEditor.classCreator.CreateClass(FileData.Tables[sheetname].TableName, flds, classpath);
        //        //GetTypeForSheetsFile(dataRows.TableName);
        //        Type a = Type.GetType("TheTechIdea.ProjectClasses." + FileData.Tables[sheetname].TableName);
        //        List<Object> retval = new List<object>();
        //        EntityStructure enttype = GetEntityDataType(sheetname);
        //        retval = DMEEditor.Utilfunction.GetListByDataTable(dataRows, a, enttype);
        //        return retval;
        //    }
        //    else
        //    {
        //        return null;
        //    }

        //}
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
              
                if (Entities.Count== 0)
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
    }
}
