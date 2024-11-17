using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.FileManager
{
    [AddinAttribute(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.CSV | DataSourceType.Xls, FileType = "csv")]
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
            if(Openconnection() == ConnectionState.Open)
            {
                Getfields();
            }
            else
            {
                File.Create(Path.Combine(FilePath, FileName));
                DMEEditor.AddLogMessage("Fail", $"Error Could not find File {datasourcename} , created empty one", DateTime.Now, 0, null, Errors.Failed);
            }

        }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
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
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        public IDMEEditor DMEEditor { get ; set ; }
        ConnectionState pConnectionStatus;
        bool IsFieldsScanned = false;
        #region "Insert or Update or Delete Objects"
        EntityStructure DataStruct = null;
        IDbCommand command = null;
        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;
        #endregion
        public ConnectionState ConnectionStatus { get { return Dataconnection.ConnectionStatus; } set { pConnectionStatus = value; } }

        public event EventHandler<PassedArgs> PassEvent;
        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, false);
             
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }
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
            if (IsFieldsScanned)
            {
                if (Entities.Count > 0)
                {
                    return Entities[0].Fields;
                }

            }
            // Scan all rows in Table for types
            while ((fieldParser.EndOfData == false) || (i<=100))
            {
                try
                {
                    
                    string[] r = fieldParser.ReadFields();
                    if (r == null)
                    {
                        i = 101;
                    }
                    
                    i += 1;
                    int j = 0;
                    if (i <= 100)
                    { // Scan fields in row for Types
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
                                if (f.fieldtype.Equals("System.String", StringComparison.InvariantCultureIgnoreCase))
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
                                if (f.fieldtype.Equals("System.Decimal", StringComparison.InvariantCultureIgnoreCase))
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
                   
                }
                catch (Exception rowex)
                {

                }

            }
            // Check for string size
            foreach (EntityField fld in flds)
            {
                if (fld.fieldtype.Equals("System.string", StringComparison.InvariantCultureIgnoreCase))
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
                if (IsFieldsScanned)
                {
                    if(Entities.Count > 0)
                    {
                        return Entities[0];
                    }
                  
                }
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
                   if(flds != null && flds.Count()>0)
                    {
                        foreach (string field in flds)
                        {
                            EntityField f = new EntityField();
                            string entspace = Regex.Replace(field ,@"[\s-.]+", "_");
                            if (entspace.Equals(sheetname, StringComparison.InvariantCultureIgnoreCase))
                            {
                                entspace = "_" + entspace;
                            }
                           
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

                        entityData.Fields = GetFieldsbyTableScan(sheetname, fl, flds);
                    }
                   
                    Entities = new List<EntityStructure>();
                    EntitiesNames = new List<string>();
                    Entities.Add(entityData);
                    EntitiesNames.Add(sheetname);
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new ConfigUtil.DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                    IsFieldsScanned=true;


                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error : Could not Create Entity For File {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            if (fieldParser != null)
            {
                fieldParser.Close();
            }
            return entityData;
        }
        private DataTable GetDataTable(int nrofrows)
        {
            DataTable dataTable = new DataTable();
            CsvTextFieldParser fieldParser = null;

            try
            {
                // Ensure there's a definition of entity fields
                if (Entities == null || Entities.Count == 0 || Entities[0].Fields.Count == 0)
                {
                    Getfields(); // Attempt to load fields if not loaded
                }

                if (Entities.Count == 0 || Entities[0].Fields.Count == 0)
                {
                    throw new InvalidOperationException("No entity fields defined.");
                }

                // Define DataTable structure based on entity fields
                foreach (EntityField field in Entities[0].Fields)
                {
                    Type fieldType =Type.GetType(field.fieldtype);
                    dataTable.Columns.Add(field.fieldname, fieldType ?? typeof(string)); // Default to string if type is unknown
                }

                // Initialize the CSV reader
                fieldParser = new CsvTextFieldParser(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));
                fieldParser.SetDelimiter(',');
                if(nrofrows == 0)
                {
                    nrofrows = dataTable.Rows.Count ;
                }
               
                // Read CSV data into DataTable
                while (!fieldParser.EndOfData)
                {
                    DataRow newRow = dataTable.NewRow();
                    string[] fields = fieldParser.ReadFields();

                    for (int i = 0; i < Math.Min(fields.Length, dataTable.Columns.Count); i++)
                    {
                        try
                        {
                            string value = fields[i];
                            Type targetType = dataTable.Columns[i].DataType;
                            newRow[i] = Convert.ChangeType(value, targetType);
                        }
                        catch (Exception fieldEx)
                        {
                            newRow[i] = DBNull.Value; // Set as DB Null if conversion fails
                        }
                    }

                    dataTable.Rows.Add(newRow);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in GetData: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
            finally
            {
                fieldParser?.Close();
            }

            return dataTable;
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
                //DMTypeBuilder.CreateNewObject(DMEEditor, "Beep.CSVDataSource", Entities[0].EntityName, Entities[0].Fields);
                 
               
                if (Entities != null)
                {
                   fieldParser = new CsvTextFieldParser(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));
                   fieldParser.SetDelimiter(',');

                  
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
        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }
        public virtual double GetScalar(string query)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                // Assuming you have a database connection and command objects.

                //using (var command = GetDataCommand())
                //{
                //    command.CommandText = query;
                //    var result = command.ExecuteScalar();

                //    // Check if the result is not null and can be converted to a double.
                //    if (result != null && double.TryParse(result.ToString(), out double value))
                //    {
                //        return value;
                //    }
                //}


                // If the query executed successfully but didn't return a valid double, you can handle it here.
                // You might want to log an error or throw an exception as needed.
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }

            // Return a default value or throw an exception if the query failed.
            return 0.0; // You can change this default value as needed.
        }
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
            return DMEEditor.ErrorObject;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            Entities.Clear();
            EntitiesNames.Clear();
            Entities.Add(entity);
            EntitiesNames.Add(entity.EntityName);
            return true;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            return DMEEditor.ErrorObject;
        }

        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return null;
        }

        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
           return null;
        }

        public List<string> GetEntitesList()
        {
            if(Entities.Count == 0)
            {
                EntitiesNames.Clear();
                EntitiesNames.Add("Sheet1");
                Entities.Clear();
                Entities.Add(Getfields());
            }
          
            return EntitiesNames;
        }

        public object GetEntity(string EntityName, List<AppFilter> filter)
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
                var retval= GetDataTable(0);
                enttype = GetEntityType(EntityName);
                Type uowGenericType = typeof(ObservableBindingList<>).MakeGenericType(enttype);
                // Prepare the arguments for the constructor
                object[] constructorArgs = new object[] { retval };

                // Create an instance of UnitOfWork<T> with the specific constructor
                // Dynamically handle the instance since we can't cast to a specific IUnitofWork<T> at compile time
                object uowInstance = Activator.CreateInstance(uowGenericType, constructorArgs);
                return uowInstance;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error in getting File Data({ ex.Message}) ", DateTime.Now, -1, "", Errors.Failed);
               
                return null;
            }
        }
        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                int noofrows = 0;
                if (filter != null)
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

                DMEEditor.AddLogMessage("Fail", $"Error in getting File Data({ex.Message}) ", DateTime.Now, -1, "", Errors.Failed);

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
               // Entities = new List<EntityStructure>();
                Getfields();
            }
            if (refresh)
            {
               // Entities = new List<EntityStructure>();
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
              //  Entities = new List<EntityStructure>();
                Getfields();
            }
            if (refresh)
            {
             //   Entities = new List<EntityStructure>();
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
              //  Entities = new List<EntityStructure>();
                Getfields();
            }

          

            DMTypeBuilder.CreateNewObject(DMEEditor, "Beep.CSVDataSource", Entities.FirstOrDefault().EntityName, Entities.FirstOrDefault().Fields);
            return DMTypeBuilder.myType;
        }

         public  object RunQuery( string qrystr)
        {
            return GetEntity(Entities[0].EntityName,new List<AppFilter>() { });
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData,IProgress<PassedArgs> progress)
        {
            try
            {
                CsvTextFieldParser fieldParser = new CsvTextFieldParser(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));
                fieldParser.SetDelimiter(',');
                fieldParser.WriteEntityStructureToFile(DMEEditor, Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName), UploadData);
                
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep",$"Error in updating File {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }
        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
           var retval =  Task.Run(() => GetEntity(EntityName, Filter)) ;
           return retval;
        }
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in end Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
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
                    Closeconnection();
                    Entities = null;
                    EntitiesNames = null;
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
