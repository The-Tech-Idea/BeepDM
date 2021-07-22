using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.FileManager
{
    [ClassProperties(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.CSV | DataSourceType.Xls, FileType = "csv")]
    public class CSVDataSource : IDataSource
    {
        private CsvTextFieldParser fieldParser = null;
        string FileName;
        string FilePath;
        string CombineFilePath;
        char Delimiter;
        public CSVDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
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
           // System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            Category = DatasourceCategory.FILE;
            FileName = Dataconnection.ConnectionProp.FileName;
            FilePath = Dataconnection.ConnectionProp.FilePath;

        }
      

      
        public DataSourceType DatasourceType { get; set; } = DataSourceType.CSV;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public List<object> Records { get; set; } = new List<object>();
        public IDMEEditor DMEEditor { get ; set ; }
        ConnectionState pConnectionStatus;
        public ConnectionState ConnectionStatus { get { return Dataconnection.ConnectionStatus; } set { pConnectionStatus = value; } }

        public event EventHandler<PassedArgs> PassEvent;
        #region "Get Fields and Data"
        public ConnectionState GetFileState()
        {
            if (ConnectionStatus == ConnectionState.Open)
            {
                return ConnectionStatus;
            }
            else
            {
                return Openconnection();
            }

        }
        public static int GetDecimalScale(decimal value)
        {
            if (value == 0)
                return 0;
            int[] bits = decimal.GetBits(value);
            return (int)((bits[3] >> 16) & 0x7F);
        }
        public static int GetDecimalPrecision(decimal value)
        {
            if (value == 0)
                return 0;
            int[] bits = decimal.GetBits(value);
            //We will use false for the sign (false =  positive), because we don't care about it.
            //We will use 0 for the last argument instead of bits[3] to eliminate the fraction point.
            decimal d = new Decimal(bits[0], bits[1], bits[2], false, 0);
            return (int)Math.Floor(Math.Log10((double)d)) + 1;
        }
        private List<EntityField> GetFieldsbyTableScan(string sheetname, List<EntityField> flds, string[] datac)
        {
            
            
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
            // setup Fields for Entity
           
            int i = 1;
            fieldParser.TrimWhiteSpace = true;
            // Scan all rows in Table for types
            while ((fieldParser.EndOfData == false))
            {
                try
                {
                    string[] r = fieldParser.ReadFields();
                    i += 1;
                    int j = 0;
                    // Scan fields in row for Types
                    foreach (EntityField f in flds)
                    {
                        try
                        {
                            if (f.fieldname.ToLower().Contains("date") || f.fieldname.ToLower().Contains("_dt"))
                            {
                                f.fieldtype = "System.DateTime";
                                f.Checked = true;
                            }
                            else
                            {
                                valstring = r[j].ToString();
                                dateval = DateTime.Now;
                                if (!f.Checked)
                                {
                                    if (!string.IsNullOrEmpty(valstring) && !string.IsNullOrWhiteSpace(valstring))
                                    {
                                        if (decimal.TryParse(valstring, out dval))
                                        {
                                            f.fieldtype = "System.Decimal";
                                            //    f.Checked = true;
                                        }
                                        else
                                         if (double.TryParse(valstring, out dblval))
                                        {
                                            f.fieldtype = "System.Double";
                                            //  f.Checked = true;
                                        }
                                        else
                                        if (long.TryParse(valstring, out longval))
                                        {
                                            f.fieldtype = "System.Long";
                                            // f.Checked = true;
                                        }
                                        else
                                         if (float.TryParse(valstring, out floatval))
                                        {
                                            f.fieldtype = "System.Float";
                                            //  f.Checked = true;
                                        }
                                        else
                                         if (int.TryParse(valstring, out intval))
                                        {
                                            f.fieldtype = "System.Int32";
                                            //   f.Checked = true;
                                        }
                                        else
                                         if (DateTime.TryParse(valstring, out dateval))
                                        {
                                            f.fieldtype = "System.DateTime";
                                            // f.Checked = true;
                                        }
                                        else
                                         if (bool.TryParse(valstring, out boolval))
                                        {
                                            f.fieldtype = "System.Bool";
                                            //  f.Checked = true;
                                        }
                                        else
                                         if (short.TryParse(valstring, out shortval))
                                        {
                                            f.fieldtype = "System.Short";
                                            //f.Checked = true;
                                        }
                                        else
                                        {
                                            f.fieldtype = "System.String";
                                            f.Checked = true;
                                        }

                                    }
                                }
                              
                            }
                        }
                        catch (Exception Fieldex)
                        {

                        }
                        try
                        {
                            if (f.fieldtype.Equals("System.String", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrEmpty(r[j]) && !string.IsNullOrWhiteSpace(r[j]))
                                {
                                    if (!string.IsNullOrEmpty(r[j].ToString()))
                                    {
                                        if (r[j].ToString().Length > f.Size1)
                                        {
                                            f.Size1 = r[j].Length;
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
                                if (!string.IsNullOrEmpty(r[j]) && !string.IsNullOrWhiteSpace(r[j]))
                                {
                                    if (!string.IsNullOrEmpty(r[j].ToString()))
                                    {
                                        valstring = r[j].ToString();
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
                        j++;
                    }
                }
                catch (Exception rowex)
                {

                }

            }
            // Check for string size
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
       
        private EntityStructure Getfields()
        {
            EntityStructure entityData = new EntityStructure();
            try
            {
                string[] flds = null;
                if(GetFileState()== ConnectionState.Open)
                {

                    fieldParser = new CsvTextFieldParser(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));
                    fieldParser.SetDelimiter(',');
                    flds = fieldParser.ReadFields();
                    int y = 0;
                    List<EntityField> fl = new List<EntityField>();
                    string sheetname=Path.GetFileNameWithoutExtension(DatasourceName);
                   
                    entityData.Viewtype = ViewType.File;
                    entityData.DatabaseType = DataSourceType.CSV;
                    entityData.DataSourceID = FileName;
                    entityData.DatasourceEntityName = sheetname;
                    entityData.Caption = sheetname;
                    entityData.EntityName = sheetname;
                    entityData.OriginalEntityName = sheetname;
                    entityData.Id =0;
                   
                    foreach (string field in flds)
                    {
                        EntityField f = new EntityField();
                        string entspace = Regex.Replace(field, @"\s+", "_");
                        f.fieldname = entspace;
                        f.Originalfieldname = field;
                        f.fieldtype = "System.String";
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
                        fl.Add(f);
                    }
                   
                    entityData.Fields = GetFieldsbyTableScan(DatasourceName,fl, flds);
                    Entities = new List<EntityStructure>();
                    EntitiesNames = new List<string>();
                    Entities.Add(entityData);
                    EntitiesNames.Add(DatasourceName);
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new ConfigUtil.DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });


                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error : Could not Create Entity For File {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            fieldParser.Close();
            return entityData;
        }
        private List<object> GetData(int nrofrows)
        {
            try
            {
                if (Entities != null)
                {
                    if (Entities.Count == 0)
                    {
                        if (Entities[0].Fields.Count == 0)
                        {
                            Getfields();

                        }
                    }
                }
                else
                {
                    Entities = new List<EntityStructure>();
                    Getfields();
                }
                Records = new List<object>();
                if (Entities != null)
                {
                   fieldParser = new CsvTextFieldParser(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));
                   fieldParser.SetDelimiter(',');

                    DMTypeBuilder.CreateNewObject(DatasourceName, DatasourceName, Entities[0].Fields);
                    Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();
                    foreach (EntityField item in Entities[0].Fields)
                    {
                        properties.Add(item.fieldname, DMTypeBuilder.myType.GetProperty(item.fieldname));
                        //  properties[item.ColumnName].SetValue(x, row[item.ColumnName], null);
                    }

                    string[] r = fieldParser.ReadFields();
                    while ((fieldParser.EndOfData == false) )
                        {
                            dynamic x = Activator.CreateInstance(DMTypeBuilder.myType);

                        r = fieldParser.ReadFields();

                        for (int i = 0; i < Entities[0].Fields.Count; i++)
                        {
                         try
                            {
                                string st = r[i].ToString();
                                if (!string.IsNullOrEmpty(st) && !string.IsNullOrWhiteSpace(st))
                                {
                                    properties[Entities[0].Fields[i].fieldname].SetValue(x, Convert.ChangeType(st, DMEEditor.Utilfunction.GetTypeCode(Type.GetType(Entities[0].Fields[i].fieldtype))), null);
                                }
                            }
                            catch (Exception fieldex)
                            {

                               
                            }
                                  
                                    

                        }
                        Records.Add(x);
                    }
                    
                }

                fieldParser.Close();
                return Records;

            }
            catch (Exception ex)
            {
                fieldParser.Close();
                DMEEditor.AddLogMessage("Fail", $"Error : Could not Get Data from File {DatasourceName}-  {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }
     
        #endregion
        public int GetEntityIdx(string entityName)
        {
            return 0;

        }
        public ConnectionState Openconnection()
        {
            ConnectionStatus = Dataconnection.OpenConnection();

            if (ConnectionStatus == ConnectionState.Open)
            {
                if (DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName) == null)
                {
                    //GetEntityStructures(true);
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
        public bool CheckEntityExist(string EntityName)
        {
            if (Dataconnection.OpenConnection() == ConnectionState.Open)
            {
                return true;
            }
            else
                return false;
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
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

        public List<SyncEntity> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetEntitesList()
        {
            
            EntitiesNames.Clear();
            EntitiesNames.Add(DatasourceName);
            return EntitiesNames;
        }

        public object GetEntity(string EntityName, List<ReportFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                int noofrows = 0;
                if (filter!=null)
                {
                    
                    
                    if (filter.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(filter[0].FilterValue) && !string.IsNullOrWhiteSpace(filter[0].FilterValue))
                        {
                            noofrows = Convert.ToInt32(filter[0].FilterValue);
                        }
                    }
                  
                }
                return GetData(noofrows);
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error in getting File Data({ ex.Message}) ", DateTime.Now, -1, "", Errors.Failed);
               
                return null;
            }
        }

       
        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            if (Entities != null)
            {
                if (Entities.Count == 0)
                {
                    if (Entities[0].Fields.Count == 0)
                    {
                        Getfields();

                    }
                }
            }
            else
            {
                Entities = new List<EntityStructure>();
                Getfields();
            }
            if (refresh)
            {
                Entities = new List<EntityStructure>();
                Getfields();
            }
           

            if (Entities.Count == 1)
            {
                return Entities[0];
            }
            else
                return null;


        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            if (Entities != null)
            {
                if (Entities.Count == 0)
                {
                    if (Entities[0].Fields.Count == 0)
                    {
                        Getfields();

                    }
                }
            }
            else
            {
                Entities = new List<EntityStructure>();
                Getfields();
            }
            if (refresh)
            {
                Entities = new List<EntityStructure>();
                Getfields();
            }
            if (Entities.Count == 1)
            {
                return Entities[0];
            }
            else
                return null;

        }

        public Type GetEntityType(string EntityName)
        {
            if (Entities != null)
            {
                if (Entities.Count == 0)
                {
                    if (Entities[0].Fields.Count == 0)
                    {
                        Getfields();

                    }
                }
            }
            else
            {
                Entities = new List<EntityStructure>();
                Getfields();
            }

          

            DMTypeBuilder.CreateNewObject(Entities.FirstOrDefault().EntityName, Entities.FirstOrDefault().EntityName, Entities.FirstOrDefault().Fields);
            return DMTypeBuilder.myType;
        }

         public  object RunQuery( string qrystr)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RunScript(SyncEntity dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData,IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
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
        #region "dispose"
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RDBSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
