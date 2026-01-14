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
using System.Text;
using System.ComponentModel;

namespace TheTechIdea.Beep.FileManager
{
    [AddinAttribute(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.CSV | DataSourceType.Xls, FileType = "csv")]
    public class CSVDataSource : IDataSource
    {
        #region "Properties" 
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType DatasourceType { get; set; } = DataSourceType.CSV;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public List<object> Records { get; set; } = new List<object>();
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        public IDMEEditor DMEEditor { get; set; }
        ConnectionState pConnectionStatus;
        bool IsFieldsScanned = false;
        #endregion "Properties"
        private CsvTextFieldParser fieldParser = null;
        string FileName;
        string FilePath;
        string CombineFilePath;
        char Delimiter;
        // CSVDataSource constructor with delimiter detection
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
            Category = DatasourceCategory.FILE;
            FileName = Dataconnection.ConnectionProp.FileName;
            FilePath = Dataconnection.ConnectionProp.FilePath;
            string fullPath = Path.Combine(FilePath, FileName);
            if (File.Exists(fullPath))
            {
                Delimiter = DetectDelimiter(fullPath);
            }
            else
            {
                Delimiter = ','; // Default to comma
            }
            Dataconnection.ConnectionProp.Delimiter = Delimiter;
            // Save the updated delimiter back to the connection properties
            DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault().Delimiter = Delimiter;
            DMEEditor.ConfigEditor.SaveDataconnectionsValues();
            // Check for custom delimiter in connection properties
           

            if (Openconnection() == ConnectionState.Open)
            {
                Getfields();
            }
            else
            {
                File.Create(Path.Combine(FilePath, FileName));
                DMEEditor.AddLogMessage("Fail", $"Error Could not find File {datasourcename} , created empty one", DateTime.Now, 0, null, Errors.Failed);
            }
        }

     
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
                        for (int findex = 0; i < flds.Count() - 1; i++)
                        {
                            EntityField f = flds[findex];
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
                    fieldParser.SetDelimiter(Delimiter);
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
                        for (int i = 0; i < flds.Count(); i++)
                        {
                            string field = flds[i];
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
                fieldParser.SetDelimiter(Delimiter);
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
                   fieldParser.SetDelimiter(Delimiter);

                  
                    Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();
                    foreach (EntityField item in Entities[0].Fields)
                    {
                        properties.Add(item.fieldname, DMTypeBuilder.MyType.GetProperty(item.fieldname));
                        //  properties[item.ColumnName].SetValue(x, row[item.ColumnName], null);
                    }

                    string[] r = fieldParser.ReadFields();
                    while ((fieldParser.EndOfData == false) )
                        {
                            dynamic x = Activator.CreateInstance(DMTypeBuilder.MyType);

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
        // Add better validation for CSV files during opening
        public ConnectionState Openconnection()
        {
            try
            {
                ConnectionStatus = Dataconnection.OpenConnection();
                if (ConnectionStatus == ConnectionState.Open)
                {
                    CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);

                    // Validate the CSV file format
                    if (!File.Exists(CombineFilePath))
                    {
                        DMEEditor.AddLogMessage("Warning", $"CSV file does not exist: {CombineFilePath}", DateTime.Now, 0, null, Errors.Failed);
                        return ConnectionState.Closed;
                    }

                    // Try reading the first line to ensure it's valid
                    try
                    {
                        using (var reader = new StreamReader(CombineFilePath))
                        {
                            if (reader.ReadLine() == null)
                            {
                                DMEEditor.AddLogMessage("Warning", "CSV file is empty or cannot be read", DateTime.Now, 0, null, Errors.Failed);
                                return ConnectionState.Closed;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.AddLogMessage("Error", $"Cannot read CSV file: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                        return ConnectionState.Closed;
                    }

                    // Load entity structure
                    if (DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName) == null)
                    {
                        if (File.Exists(CombineFilePath))
                        {
                            Delimiter = DetectDelimiter(CombineFilePath);
                        }
                        else
                        {
                            Delimiter = ','; // Default to comma
                        }
                        Getfields();
                    }
                    else
                    {
                        Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;
                    }
                }
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Failed to open connection: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return ConnectionState.Closed;
            }
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
            ErrorObject.Flag = Errors.Ok;

            try
            {
                foreach (var entity in entities)
                {
                    // Build the file path for the entity (CSV file)
                    string entityFilePath = Path.Combine(FilePath, $"{entity.EntityName}.csv");

                    if (!File.Exists(entityFilePath))
                    {
                        // Create a new CSV file and write the headers (field names)
                        using (var writer = new StreamWriter(entityFilePath, false))
                        {
                            var headerLine = string.Join(Delimiter.ToString(), entity.Fields.Select(f => f.fieldname));
                            writer.WriteLine(headerLine);
                        }

                        Logger.WriteLog($"Entity '{entity.EntityName}' created successfully at {entityFilePath}.");

                        // Add the entity to the internal Entities list
                        if (!EntitiesNames.Contains(entity.EntityName))
                        {
                            EntitiesNames.Add(entity.EntityName);
                            Entities.Add(entity);
                        }
                    }
                    else
                    {
                        Logger.WriteLog($"Entity '{entity.EntityName}' already exists at {entityFilePath}.");
                    }
                }

                // Save the entities configuration for persistence
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities
                {
                    datasourcename = DatasourceName,
                    Entities = Entities
                });
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in CreateEntities: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }

            return ErrorObject;
        }
        /// <summary>
        /// Creates a new entity or updates an existing one based on the provided structure.
        /// If the entity already exists, its fields will be updated (added, removed) to match the new structure.
        /// </summary>
        /// <param name="entity">The structure of the entity to create or update</param>
        /// <returns>True if creation or update is successful, false otherwise</returns>
        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                // Check if a file already exists for this entity
                string entityFilePath = Path.Combine(FilePath, $"{entity.EntityName}.csv");
                bool entityExists = File.Exists(entityFilePath);

                if (entityExists)
                {
                    // Entity exists - update it by modifying its structure
                    DMEEditor.AddLogMessage("Info", $"Entity {entity.EntityName} already exists - updating structure", DateTime.Now, 0, null, Errors.Ok);

                    // Get the existing entity structure from file
                    EntityStructure existingEntity = null;

                    // Try to find the entity in our cached list first
                    if (Entities != null && EntitiesNames != null && EntitiesNames.Contains(entity.EntityName))
                    {
                        existingEntity = Entities.FirstOrDefault(e => e.EntityName == entity.EntityName);
                    }

                    // If not found in cache, try to load it from the file
                    if (existingEntity == null)
                    {
                        // Read the existing structure from file
                        using (var reader = new StreamReader(entityFilePath))
                        {
                            // Get header line
                            string headerLine = reader.ReadLine();
                            if (!string.IsNullOrEmpty(headerLine))
                            {
                                // Parse headers to get field names
                                string[] existingFields = headerLine.Split(Delimiter);

                                // Create a temporary entity structure
                                existingEntity = new EntityStructure
                                {
                                    EntityName = entity.EntityName,
                                    Fields = new List<EntityField>()
                                };

                                // Create fields from the header
                                for (int i = 0; i < existingFields.Length; i++)
                                {
                                    existingEntity.Fields.Add(new EntityField
                                    {
                                        fieldname = existingFields[i],
                                        EntityName = entity.EntityName,
                                        fieldtype = "System.String", // Default type
                                        FieldIndex = i
                                    });
                                }
                            }
                        }
                    }

                    if (existingEntity == null)
                    {
                        // Couldn't determine the existing entity structure
                        DMEEditor.AddLogMessage("Error", $"Could not load structure for existing entity {entity.EntityName}", DateTime.Now, 0, null, Errors.Failed);
                        return false;
                    }

                    // Read all data from CSV except header, to preserve it during structure update
                    List<string[]> existingData = new List<string[]>();
                    using (var parser = new CsvTextFieldParser(entityFilePath))
                    {
                        parser.SetDelimiter(Delimiter);

                        // Skip header
                        parser.ReadFields();

                        // Read all data rows
                        while (!parser.EndOfData)
                        {
                            existingData.Add(parser.ReadFields());
                        }
                    }

                    // Create field mapping between old and new structure
                    Dictionary<string, string> fieldNameMapping = new Dictionary<string, string>();
                    Dictionary<string, int> oldFieldIndexes = new Dictionary<string, int>();

                    // Map existing fields by name
                    for (int i = 0; i < existingEntity.Fields.Count; i++)
                    {
                        oldFieldIndexes[existingEntity.Fields[i].fieldname] = i;
                    }

                    // Create a backup of the file before modifying it
                    string backupPath = Path.Combine(FilePath, $"{entity.EntityName}_backup_{DateTime.Now:yyyyMMddHHmmss}.csv");
                    File.Copy(entityFilePath, backupPath);
                    DMEEditor.AddLogMessage("Info", $"Created backup of entity at {backupPath}", DateTime.Now, 0, null, Errors.Ok);

                    // Write the updated CSV with new structure
                    using (var writer = new StreamWriter(entityFilePath, false))
                    {
                        // Write the new header
                        writer.WriteLine(string.Join(Delimiter.ToString(), entity.Fields.Select(f => f.fieldname)));

                        // Write existing data with field mapping
                        foreach (var row in existingData)
                        {
                            string[] newRow = new string[entity.Fields.Count];

                            for (int i = 0; i < entity.Fields.Count; i++)
                            {
                                string fieldName = entity.Fields[i].fieldname;
                                if (oldFieldIndexes.TryGetValue(fieldName, out int oldIndex) && oldIndex < row.Length)
                                {
                                    // Field exists in old structure, copy the value
                                    newRow[i] = row[oldIndex];
                                }
                                else
                                {
                                    // New field, set empty value
                                    newRow[i] = string.Empty;
                                }
                            }

                            writer.WriteLine(string.Join(Delimiter.ToString(), newRow));
                        }
                    }

                    // Update our cached lists
                    Entities.RemoveAll(e => e.EntityName == entity.EntityName);
                    EntitiesNames.RemoveAll(n => n == entity.EntityName);
                }
                else
                {
                    // Entity doesn't exist - create it
                    DMEEditor.AddLogMessage("Info", $"Creating new entity {entity.EntityName}", DateTime.Now, 0, null, Errors.Ok);

                    // Create a new CSV file with just the headers
                    using (var writer = new StreamWriter(entityFilePath, false))
                    {
                        var headerLine = string.Join(Delimiter.ToString(), entity.Fields.Select(f => f.fieldname));
                        writer.WriteLine(headerLine);
                    }

                    // Clear existing entities for this datasource - typically only one entity per CSV file
                    Entities.Clear();
                    EntitiesNames.Clear();
                }

                // Update the entity list
                Entities.Add(entity);
                EntitiesNames.Add(entity.EntityName);

                // Save the updated entity structure
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities
                {
                    datasourcename = DatasourceName,
                    Entities = Entities
                });

                DMEEditor.AddLogMessage("Success", $"Entity {entity.EntityName} created/updated successfully", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Failed to create/update entity {entity.EntityName}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            return DMEEditor.ErrorObject;
        }
        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return null;
        }
        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
           return null;
        }
        public IEnumerable<string> GetEntitesList()
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
        /// <summary>
        /// Retrieves data from a CSV file with optional filtering
        /// </summary>
        /// <param name="EntityName">Name of the entity (CSV file) to query</param>
        /// <param name="filter">List of filters to apply to the data</param>
        /// <returns>An ObservableBindingList containing the filtered data</returns>
        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;
            var results = new List<object>();
            try
            {
                if (string.IsNullOrWhiteSpace(EntityName))
                {
                    DMEEditor.AddLogMessage("Fail", "EntityName is null or empty", DateTime.Now, 0, null, Errors.Failed);
                    return results;
                }

                // Ensure entity structure
                var entityStructure = GetEntityStructure(EntityName, false);
                if (entityStructure == null || entityStructure.Fields == null || entityStructure.Fields.Count == 0)
                {
                    DMEEditor.AddLogMessage("Fail", $"Entity structure not found or empty for {EntityName}", DateTime.Now, 0, null, Errors.Failed);
                    return results;
                }

                // Build runtime type (if possible)
                Type runtimeType = null;
                try
                {
                    runtimeType = GetEntityType(EntityName);
                }
                catch
                {
                    runtimeType = null; // fallback to dictionary mode
                }

                // Map field index for faster access
                var fieldIndexMap = entityStructure.Fields
                    .Select((f, i) => new { f.fieldname, Index = i, FieldDef = f })
                    .ToDictionary(x => x.fieldname, x => x, StringComparer.OrdinalIgnoreCase);

                // Normalize filters (ignore invalid)
                var activeFilters = (filter ?? new List<AppFilter>())
                    .Where(f => f != null && !string.IsNullOrWhiteSpace(f.FieldName) && !string.IsNullOrWhiteSpace(f.Operator))
                    .ToList();

                if (activeFilters.Count == 0 && (filter != null && filter.Count > 0))
                {
                    DMEEditor.AddLogMessage("Warning", "All provided filters were invalid and ignored", DateTime.Now, 0, null, Errors.Warning);
                }

                // Open file
                if (string.IsNullOrEmpty(CombineFilePath))
                {
                    CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
                }
                if (!File.Exists(CombineFilePath))
                {
                    DMEEditor.AddLogMessage("Fail", $"File not found: {CombineFilePath}", DateTime.Now, 0, null, Errors.Failed);
                    return results;
                }

                using var parser = new CsvTextFieldParser(CombineFilePath);
                parser.SetDelimiter(Delimiter);

                // Read header
                var header = parser.ReadFields();
                if (header == null)
                {
                    DMEEditor.AddLogMessage("Warning", "CSV file is empty", DateTime.Now, 0, null, Errors.Warning);
                    return results;
                }

                // Build mapping header->entity field index
                var headerToEntityIndex = new Dictionary<int, int>();
                for (int h = 0; h < header.Length; h++)
                {
                    var matchIdx = entityStructure.Fields.FindIndex(f =>
                        f.fieldname.Equals(header[h], StringComparison.OrdinalIgnoreCase) ||
                        f.Originalfieldname?.Equals(header[h], StringComparison.OrdinalIgnoreCase) == true);
                    if (matchIdx >= 0) headerToEntityIndex[h] = matchIdx;
                }

                // Row processing
                while (!parser.EndOfData)
                {
                    string[] row;
                    try
                    {
                        row = parser.ReadFields();
                        if (row == null) break;
                    }
                    catch
                    {
                        continue; // skip malformed line
                    }

                    // Apply filters
                    bool include = true;
                    foreach (var f in activeFilters)
                    {
                        if (!fieldIndexMap.TryGetValue(f.FieldName, out var info))
                        {
                            include = false;
                            break;
                        }

                        int entityFieldIdx = info.Index;
                        // Find corresponding CSV column index
                        int csvIdx = headerToEntityIndex.FirstOrDefault(kv => kv.Value == entityFieldIdx).Key;
                        if (csvIdx < 0 || csvIdx >= row.Length)
                        {
                            include = false;
                            break;
                        }

                        string rawValue = row[csvIdx];
                        if (!EvaluateFilter(rawValue, info.FieldDef, f))
                        {
                            include = false;
                            break;
                        }
                    }

                    if (!include) continue;

                    // Materialize row
                    if (runtimeType != null)
                    {
                        // strongly-typed object creation
                        object instance;
                        try
                        {
                            instance = Activator.CreateInstance(runtimeType);
                        }
                        catch
                        {
                            runtimeType = null; // fallback next iteration
                            goto fallbackDict;
                        }

                        foreach (var ef in entityStructure.Fields)
                        {
                            var prop = runtimeType.GetProperty(ef.fieldname);
                            if (prop == null || !prop.CanWrite) continue;

                            int csvIndex = headerToEntityIndex.FirstOrDefault(kv => kv.Value == fieldIndexMap[ef.fieldname].Index).Key;
                            if (csvIndex < 0 || csvIndex >= row.Length) continue;

                            var strVal = row[csvIndex];
                            if (string.IsNullOrWhiteSpace(strVal))
                            {
                                prop.SetValue(instance, null);
                                continue;
                            }

                            try
                            {
                                var targetType = Type.GetType(ef.fieldtype) ?? typeof(string);
                                object converted = ConvertString(strVal, targetType);
                                prop.SetValue(instance, converted);
                            }
                            catch
                            {
                                prop.SetValue(instance, null);
                            }
                        }
                        results.Add(instance);
                        continue;
                    }

                // Dictionary fallback
                fallbackDict:
                    var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var ef in entityStructure.Fields)
                    {
                        int csvIndex = headerToEntityIndex.FirstOrDefault(kv => kv.Value == fieldIndexMap[ef.fieldname].Index).Key;
                        if (csvIndex < 0 || csvIndex >= row.Length)
                        {
                            dict[ef.fieldname] = null;
                            continue;
                        }
                        var strVal = row[csvIndex];
                        if (string.IsNullOrWhiteSpace(strVal))
                        {
                            dict[ef.fieldname] = null;
                            continue;
                        }
                        try
                        {
                            var targetType = Type.GetType(ef.fieldtype) ?? typeof(string);
                            dict[ef.fieldname] = ConvertString(strVal, targetType);
                        }
                        catch
                        {
                            dict[ef.fieldname] = null;
                        }
                    }
                    results.Add(dict);
                }

                return results;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Fail", $"Error in GetEntity: {ex.Message}", DateTime.Now, 0, EntityName, Errors.Failed);
                return results;
            }
        }

        // Evaluate a single filter condition
        private bool EvaluateFilter(string rawValue, EntityField fieldDef, AppFilter f)
        {
            string op = (f.Operator ?? "=").Trim().ToLowerInvariant();

            // Null checks
            if (rawValue == null || rawValue.Length == 0)
            {
                return op switch
                {
                    "isnull" or "is null" => true,
                    "isnotnull" or "is not null" => false,
                    "=" or "equals" => string.IsNullOrEmpty(f.FilterValue),
                    "!=" or "<>" or "notequals" or "not equals" => !string.IsNullOrEmpty(f.FilterValue),
                    _ => false
                };
            }

            var fieldType = Type.GetType(fieldDef.fieldtype) ?? typeof(string);

            // Handle string
            if (fieldType == typeof(string))
            {
                string v = rawValue;
                string fv = f.FilterValue ?? string.Empty;
                switch (op)
                {
                    case "=":
                    case "equals": return string.Equals(v, fv, StringComparison.OrdinalIgnoreCase);
                    case "!=":
                    case "<>":
                    case "notequals":
                    case "not equals": return !string.Equals(v, fv, StringComparison.OrdinalIgnoreCase);
                    case "contains": return v.IndexOf(fv, StringComparison.OrdinalIgnoreCase) >= 0;
                    case "notcontains":
                    case "not contains":
                    case "!contains": return v.IndexOf(fv, StringComparison.OrdinalIgnoreCase) < 0;
                    case "startswith":
                    case "starts with": return v.StartsWith(fv, StringComparison.OrdinalIgnoreCase);
                    case "endswith":
                    case "ends with": return v.EndsWith(fv, StringComparison.OrdinalIgnoreCase);
                    case "in":
                        return (f.FilterValue ?? "")
                            .Split(',')
                            .Select(s => s.Trim())
                            .Any(s => string.Equals(s, v, StringComparison.OrdinalIgnoreCase));
                    case "notin":
                    case "not in":
                        return !(f.FilterValue ?? "")
                            .Split(',')
                            .Select(s => s.Trim())
                            .Any(s => string.Equals(s, v, StringComparison.OrdinalIgnoreCase));
                    case "regex":
                    case "matches":
                        try { return Regex.IsMatch(v, f.FilterValue); } catch { return false; }
                    default:
                        return false;
                }
            }

            // Numeric
            if (IsNumeric(fieldType))
            {
                if (!decimal.TryParse(rawValue, out var numVal)) return false;
                if (op == "between")
                {
                    if (!string.IsNullOrWhiteSpace(f.FilterValue1))
                    {
                        if (decimal.TryParse(f.FilterValue, out var n1) && decimal.TryParse(f.FilterValue1, out var n2))
                            return numVal >= Math.Min(n1, n2) && numVal <= Math.Max(n1, n2);
                        return false;
                    }
                    // legacy: FilterValue contains comma
                    if (f.FilterValue?.Contains(',') == true)
                    {
                        var parts = f.FilterValue.Split(',');
                        if (parts.Length == 2 &&
                            decimal.TryParse(parts[0], out var n1) &&
                            decimal.TryParse(parts[1], out var n2))
                            return numVal >= Math.Min(n1, n2) && numVal <= Math.Max(n1, n2);
                    }
                    return false;
                }
                if (!decimal.TryParse(f.FilterValue, out var cmp)) return false;
                return op switch
                {
                    "=" or "equals" => numVal == cmp,
                    "!=" or "<>" or "notequals" or "not equals" => numVal != cmp,
                    ">" => numVal > cmp,
                    ">=" or "=>" => numVal >= cmp,
                    "<" => numVal < cmp,
                    "<=" or "=<" => numVal <= cmp,
                    _ => false
                };
            }

            // DateTime
            if (fieldType == typeof(DateTime))
            {
                if (!DateTime.TryParse(rawValue, out var dateVal)) return false;
                if (op == "between")
                {
                    if (!string.IsNullOrWhiteSpace(f.FilterValue1))
                    {
                        if (DateTime.TryParse(f.FilterValue, out var d1) && DateTime.TryParse(f.FilterValue1, out var d2))
                            return dateVal >= (d1 < d2 ? d1 : d2) && dateVal <= (d1 > d2 ? d1 : d2);
                        return false;
                    }
                    if (f.FilterValue?.Contains(',') == true)
                    {
                        var parts = f.FilterValue.Split(',');
                        if (parts.Length == 2 &&
                            DateTime.TryParse(parts[0], out var d1) &&
                            DateTime.TryParse(parts[1], out var d2))
                            return dateVal >= (d1 < d2 ? d1 : d2) && dateVal <= (d1 > d2 ? d1 : d2);
                    }
                    return false;
                }
                if (!DateTime.TryParse(f.FilterValue, out var cmpDate)) return false;
                return op switch
                {
                    "=" or "equals" => dateVal.Date == cmpDate.Date,
                    "!=" or "<>" or "notequals" or "not equals" => dateVal.Date != cmpDate.Date,
                    ">" or "after" => dateVal > cmpDate,
                    ">=" or "=>" or "on or after" => dateVal >= cmpDate,
                    "<" or "before" => dateVal < cmpDate,
                    "<=" or "=<" or "on or before" => dateVal <= cmpDate,
                    _ => false
                };
            }

            // Bool
            if (fieldType == typeof(bool))
            {
                if (!bool.TryParse(NormalizeBool(rawValue), out var bv)) return false;
                bool cmpBool = bool.TryParse(NormalizeBool(f.FilterValue), out var fb) && fb;
                return op switch
                {
                    "=" or "equals" or "is" => bv == cmpBool,
                    "!=" or "<>" or "notequals" or "not equals" or "is not" => bv != cmpBool,
                    _ => false
                };
            }

            // Fallback string compare
            return rawValue.Equals(f.FilterValue ?? "", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNumeric(Type t)
        {
            return t == typeof(int) || t == typeof(long) || t == typeof(short) ||
                   t == typeof(decimal) || t == typeof(double) || t == typeof(float);
        }

        private static string NormalizeBool(string s)
        {
            if (s == null) return "false";
            var v = s.Trim().ToLowerInvariant();
            return (v == "1" || v == "true" || v == "yes" || v == "y") ? "true" : "false";
        }

        private static object ConvertString(string value, Type targetType)
        {
            if (targetType == typeof(string)) return value;
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (targetType == typeof(bool)) return NormalizeBool(value) == "true";
            if (targetType.IsEnum) return Enum.Parse(targetType, value, true);
            return Convert.ChangeType(value, targetType);
        }

        private static string EscapeCsv(string value, char delimiter)
        {
            if (value == null) return string.Empty;
            bool mustQuote = value.Contains(delimiter) || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            if (!mustQuote) return value;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        // Improved implementation of GetEntity with paging support
        // Improved implementation with extended filtering operators
        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (string.IsNullOrWhiteSpace(EntityName))
                {
                    DMEEditor.AddLogMessage("Fail", "EntityName is null or empty", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize <= 0) pageSize = int.MaxValue;

                // Ensure entity structure
                var entityStructure = GetEntityStructure(EntityName, false);
                if (entityStructure == null || entityStructure.Fields == null || entityStructure.Fields.Count == 0)
                {
                    DMEEditor.AddLogMessage("Fail", $"Entity structure not found or empty for {EntityName}", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }

                // Build runtime type (if possible)
                Type runtimeType = null;
                try
                {
                    runtimeType = GetEntityType(EntityName);
                }
                catch
                {
                    runtimeType = null;
                }

                // Map field index for faster access
                var fieldIndexMap = entityStructure.Fields
                    .Select((f, i) => new { f.fieldname, Index = i, FieldDef = f })
                    .ToDictionary(x => x.fieldname, x => x, StringComparer.OrdinalIgnoreCase);

                var activeFilters = (filter ?? new List<AppFilter>())
                    .Where(f => f != null && !string.IsNullOrWhiteSpace(f.FieldName) && !string.IsNullOrWhiteSpace(f.Operator))
                    .ToList();

                if (string.IsNullOrEmpty(CombineFilePath))
                {
                    CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
                }
                if (!File.Exists(CombineFilePath))
                {
                    DMEEditor.AddLogMessage("Fail", $"File not found: {CombineFilePath}", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }

                int skip = (pageNumber - 1) * pageSize;
                int total = 0;
                var pageItems = new List<object>(Math.Min(pageSize, 1024));

                using var parser = new CsvTextFieldParser(CombineFilePath);
                parser.SetDelimiter(Delimiter);

                var header = parser.ReadFields();
                if (header == null)
                {
                    // empty file
                    var empty = new PagedResult { PageNumber = pageNumber, PageSize = pageSize };
                    var emptyType = empty.GetType();
                    var dp = emptyType.GetProperty("Data");
                    if (dp != null && dp.CanWrite) dp.SetValue(empty, pageItems);
                    return empty;
                }

                var headerToEntityIndex = new Dictionary<int, int>();
                for (int h = 0; h < header.Length; h++)
                {
                    var matchIdx = entityStructure.Fields.FindIndex(f =>
                        f.fieldname.Equals(header[h], StringComparison.OrdinalIgnoreCase) ||
                        f.Originalfieldname?.Equals(header[h], StringComparison.OrdinalIgnoreCase) == true);
                    if (matchIdx >= 0) headerToEntityIndex[h] = matchIdx;
                }

                while (!parser.EndOfData)
                {
                    string[] row;
                    try
                    {
                        row = parser.ReadFields();
                        if (row == null) break;
                    }
                    catch
                    {
                        continue;
                    }

                    bool include = true;
                    foreach (var f in activeFilters)
                    {
                        if (!fieldIndexMap.TryGetValue(f.FieldName, out var info))
                        {
                            include = false;
                            break;
                        }

                        int entityFieldIdx = info.Index;
                        int csvIdx = headerToEntityIndex.FirstOrDefault(kv => kv.Value == entityFieldIdx).Key;
                        if (csvIdx < 0 || csvIdx >= row.Length)
                        {
                            include = false;
                            break;
                        }

                        string rawValue = row[csvIdx];
                        if (!EvaluateFilter(rawValue, info.FieldDef, f))
                        {
                            include = false;
                            break;
                        }
                    }

                    if (!include) continue;
                    total++;

                    // Collect only the requested page
                    if (total <= skip) continue;
                    if (pageItems.Count >= pageSize) continue;

                    if (runtimeType != null)
                    {
                        object instance;
                        try
                        {
                            instance = Activator.CreateInstance(runtimeType);
                        }
                        catch
                        {
                            runtimeType = null;
                            instance = null;
                        }

                        if (instance != null)
                        {
                            foreach (var ef in entityStructure.Fields)
                            {
                                var prop = runtimeType.GetProperty(ef.fieldname);
                                if (prop == null || !prop.CanWrite) continue;

                                int csvIndex = headerToEntityIndex.FirstOrDefault(kv => kv.Value == fieldIndexMap[ef.fieldname].Index).Key;
                                if (csvIndex < 0 || csvIndex >= row.Length) continue;

                                var strVal = row[csvIndex];
                                if (string.IsNullOrWhiteSpace(strVal))
                                {
                                    prop.SetValue(instance, null);
                                    continue;
                                }

                                try
                                {
                                    var targetType = Type.GetType(ef.fieldtype) ?? typeof(string);
                                    object converted = ConvertString(strVal, targetType);
                                    prop.SetValue(instance, converted);
                                }
                                catch
                                {
                                    prop.SetValue(instance, null);
                                }
                            }
                            pageItems.Add(instance);
                            continue;
                        }
                    }

                    var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (var ef in entityStructure.Fields)
                    {
                        int csvIndex = headerToEntityIndex.FirstOrDefault(kv => kv.Value == fieldIndexMap[ef.fieldname].Index).Key;
                        if (csvIndex < 0 || csvIndex >= row.Length)
                        {
                            dict[ef.fieldname] = null;
                            continue;
                        }

                        var strVal = row[csvIndex];
                        if (string.IsNullOrWhiteSpace(strVal))
                        {
                            dict[ef.fieldname] = null;
                            continue;
                        }

                        try
                        {
                            var targetType = Type.GetType(ef.fieldtype) ?? typeof(string);
                            dict[ef.fieldname] = ConvertString(strVal, targetType);
                        }
                        catch
                        {
                            dict[ef.fieldname] = null;
                        }
                    }
                    pageItems.Add(dict);
                }

                var result = new PagedResult
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                // Populate richer metadata if properties exist (backward compatibility with minimal PagedResult)
                var type = result.GetType();
                void SetProp(string name, object value)
                {
                    var p = type.GetProperty(name);
                    if (p != null && p.CanWrite) p.SetValue(result, value);
                }

                SetProp("Data", pageItems);
                SetProp("TotalRecords", total);
                SetProp("TotalPages", total > 0 && pageSize > 0 ? (int)Math.Ceiling((double)total / pageSize) : 0);
                SetProp("HasNextPage", pageSize > 0 && pageNumber * pageSize < total);
                SetProp("HasPreviousPage", pageNumber > 1);

                return result;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Fail", $"Error in paged GetEntity: {ex.Message}", DateTime.Now, 0, EntityName, Errors.Failed);
                return null;
            }
        }
        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
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
            return DMTypeBuilder.MyType;
        }
         public  IEnumerable<object> RunQuery( string qrystr)
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
                fieldParser.SetDelimiter(Delimiter);
                fieldParser.WriteEntityStructureToFile(DMEEditor, Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName), UploadData);
                
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep",$"Error in updating File {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo UpdateEntity(string entityName, object uploadDataRow)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                var entityStructure = GetEntityStructure(entityName, false);
                if (entityStructure == null || entityStructure.Fields.Count == 0)
                {
                    Logger.WriteLog($"Entity '{entityName}' structure not found.");
                    return ErrorObject;
                }

                var pkField = entityStructure.PrimaryKeys?.FirstOrDefault() ?? entityStructure.Fields.FirstOrDefault();
                if (pkField == null)
                {
                    ErrorObject.Flag = Errors.Failed;
                    Logger.WriteLog($"Entity '{entityName}' has no fields to identify records.");
                    return ErrorObject;
                }

                var pkProp = uploadDataRow?.GetType().GetProperty(pkField.fieldname);
                var pkVal = pkProp?.GetValue(uploadDataRow)?.ToString();
                if (string.IsNullOrWhiteSpace(pkVal))
                {
                    ErrorObject.Flag = Errors.Failed;
                    Logger.WriteLog($"Primary key value missing for update on '{entityName}'.");
                    return ErrorObject;
                }

                string[] BuildRowFromObject(string[] headers)
                {
                    var values = new string[headers.Length];
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var colName = headers[i];
                        var prop = uploadDataRow.GetType().GetProperty(colName) ?? uploadDataRow.GetType().GetProperty(colName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        values[i] = prop?.GetValue(uploadDataRow)?.ToString() ?? string.Empty;
                    }
                    return values;
                }

                bool isUpdated = false;
                var tempPath = CombineFilePath + ".tmp";
                using (var parser = new CsvTextFieldParser(CombineFilePath))
                using (var writer = new StreamWriter(tempPath, false, Encoding.UTF8))
                {
                    parser.SetDelimiter(Delimiter);
                    var headers = parser.ReadFields();
                    if (headers == null)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        Logger.WriteLog($"CSV file is empty for entity '{entityName}'.");
                        return ErrorObject;
                    }

                    int pkIndex = Array.FindIndex(headers, h => string.Equals(h, pkField.fieldname, StringComparison.OrdinalIgnoreCase));
                    if (pkIndex < 0)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        Logger.WriteLog($"Primary key column '{pkField.fieldname}' not found in CSV header.");
                        return ErrorObject;
                    }

                    writer.WriteLine(string.Join(Delimiter.ToString(), headers.Select(h => EscapeCsv(h, Delimiter))));

                    while (!parser.EndOfData)
                    {
                        string[] row;
                        try { row = parser.ReadFields(); } catch { continue; }
                        if (row == null) break;

                        var currentPk = pkIndex < row.Length ? row[pkIndex] : null;
                        if (!isUpdated && string.Equals(currentPk, pkVal, StringComparison.OrdinalIgnoreCase))
                        {
                            var updated = BuildRowFromObject(headers);
                            writer.WriteLine(string.Join(Delimiter.ToString(), updated.Select(v => EscapeCsv(v, Delimiter))));
                            isUpdated = true;
                        }
                        else
                        {
                            // Preserve existing row as parsed values
                            writer.WriteLine(string.Join(Delimiter.ToString(), row.Select(v => EscapeCsv(v, Delimiter))));
                        }
                    }
                }

                if (isUpdated)
                {
                    if (File.Exists(CombineFilePath))
                    {
                        var backup = CombineFilePath + ".bak";
                        File.Replace(tempPath, CombineFilePath, backup, ignoreMetadataErrors: true);
                        try { File.Delete(backup); } catch { }
                    }
                    else
                    {
                        File.Move(tempPath, CombineFilePath);
                    }
                    Logger.WriteLog($"Entity '{entityName}' updated successfully.");
                }
                else
                {
                    try { File.Delete(tempPath); } catch { }
                    Logger.WriteLog($"No matching record found to update in entity '{entityName}'.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error updating entity '{entityName}': {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
            }
            return ErrorObject;
        }
        public IErrorsInfo DeleteEntity(string entityName, object uploadDataRow)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                var entityStructure = GetEntityStructure(entityName, false);
                if (entityStructure == null || entityStructure.Fields.Count == 0)
                {
                    Logger.WriteLog($"Entity '{entityName}' structure not found.");
                    return ErrorObject;
                }

                var pkField = entityStructure.PrimaryKeys?.FirstOrDefault() ?? entityStructure.Fields.FirstOrDefault();
                if (pkField == null)
                {
                    ErrorObject.Flag = Errors.Failed;
                    Logger.WriteLog($"Entity '{entityName}' has no fields to identify records.");
                    return ErrorObject;
                }

                var pkProp = uploadDataRow?.GetType().GetProperty(pkField.fieldname);
                var pkVal = pkProp?.GetValue(uploadDataRow)?.ToString();
                if (string.IsNullOrWhiteSpace(pkVal))
                {
                    ErrorObject.Flag = Errors.Failed;
                    Logger.WriteLog($"Primary key value missing for delete on '{entityName}'.");
                    return ErrorObject;
                }

                bool isDeleted = false;
                var tempPath = CombineFilePath + ".tmp";
                using (var parser = new CsvTextFieldParser(CombineFilePath))
                using (var writer = new StreamWriter(tempPath, false, Encoding.UTF8))
                {
                    parser.SetDelimiter(Delimiter);
                    var headers = parser.ReadFields();
                    if (headers == null)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        Logger.WriteLog($"CSV file is empty for entity '{entityName}'.");
                        return ErrorObject;
                    }

                    int pkIndex = Array.FindIndex(headers, h => string.Equals(h, pkField.fieldname, StringComparison.OrdinalIgnoreCase));
                    if (pkIndex < 0)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        Logger.WriteLog($"Primary key column '{pkField.fieldname}' not found in CSV header.");
                        return ErrorObject;
                    }

                    writer.WriteLine(string.Join(Delimiter.ToString(), headers.Select(h => EscapeCsv(h, Delimiter))));

                    while (!parser.EndOfData)
                    {
                        string[] row;
                        try { row = parser.ReadFields(); } catch { continue; }
                        if (row == null) break;

                        var currentPk = pkIndex < row.Length ? row[pkIndex] : null;
                        if (!isDeleted && string.Equals(currentPk, pkVal, StringComparison.OrdinalIgnoreCase))
                        {
                            isDeleted = true;
                            continue;
                        }

                        writer.WriteLine(string.Join(Delimiter.ToString(), row.Select(v => EscapeCsv(v, Delimiter))));
                    }
                }

                if (isDeleted)
                {
                    if (File.Exists(CombineFilePath))
                    {
                        var backup = CombineFilePath + ".bak";
                        File.Replace(tempPath, CombineFilePath, backup, ignoreMetadataErrors: true);
                        try { File.Delete(backup); } catch { }
                    }
                    else
                    {
                        File.Move(tempPath, CombineFilePath);
                    }
                    Logger.WriteLog($"Entity '{entityName}' deleted successfully.");
                }
                else
                {
                    try { File.Delete(tempPath); } catch { }
                    Logger.WriteLog($"No matching record found to delete in entity '{entityName}'.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error deleting entity '{entityName}': {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
            }
            return ErrorObject;
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                var entityStructure = GetEntityStructure(EntityName, false);
                if (entityStructure == null || entityStructure.Fields.Count == 0)
                {
                    DMEEditor.AddLogMessage("Error", $"Entity structure not found for {EntityName}", DateTime.Now, 0, null, Errors.Failed);
                    ErrorObject.Flag = Errors.Failed;
                    return ErrorObject;
                }

                // Read all existing data
                var lines = new List<string>();
                bool fileExists = File.Exists(CombineFilePath);

                if (fileExists)
                {
                    using (var reader = new StreamReader(CombineFilePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                }
                else
                {
                    // Create headers if file doesn't exist
                    lines.Add(string.Join(Delimiter.ToString(),
                        entityStructure.Fields.Select(f => f.fieldname)));
                }

                // Add new data row
                var newRow = new List<string>();
                foreach (var field in entityStructure.Fields)
                {
                    var prop = InsertedData.GetType().GetProperty(field.fieldname);
                    if (prop != null)
                    {
                        var value = prop.GetValue(InsertedData)?.ToString() ?? string.Empty;
                        // Escape quotes and add quotes around values with delimiters
                        if (value.Contains(Delimiter) || value.Contains('"'))
                        {
                            value = $"\"{value.Replace("\"", "\"\"")}\"";
                        }
                        newRow.Add(value);
                    }
                    else
                    {
                        newRow.Add(string.Empty);
                    }
                }

                lines.Add(string.Join(Delimiter.ToString(), newRow));

                // Write back to file
                File.WriteAllLines(CombineFilePath, lines);

                DMEEditor.AddLogMessage("Success", $"Record inserted into {EntityName}", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Failed to insert record: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }

            return ErrorObject;
        }
        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
           var retval =  Task.Run(() => GetEntity(EntityName, Filter)) ;
           return retval;
        }
        // Add properties to track transaction state
        private List<string> _transactionBackupLines;
        private bool _inTransaction = false;

        public  IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (_inTransaction)
                {
                    DMEEditor.AddLogMessage("Warning", "Transaction already in progress", DateTime.Now, 0, null, Errors.Failed);
                    ErrorObject.Flag = Errors.Failed;
                    return ErrorObject;
                }

                // Take a snapshot of the current file content
                _transactionBackupLines = new List<string>();
                if (File.Exists(CombineFilePath))
                {
                    _transactionBackupLines.AddRange(File.ReadAllLines(CombineFilePath));
                }

                _inTransaction = true;
                DMEEditor.AddLogMessage("Success", "Transaction started", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Failed to begin transaction: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
        }

        public  IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // Just clear transaction state - commit happens separately
                _inTransaction = false;
                _transactionBackupLines = null;
                DMEEditor.AddLogMessage("Success", "Transaction ended", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Error ending transaction: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
        }

        public  IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (!_inTransaction)
                {
                    DMEEditor.AddLogMessage("Warning", "No transaction in progress to commit", DateTime.Now, 0, null, Errors.Failed);
                    ErrorObject.Flag = Errors.Failed;
                    return ErrorObject;
                }

                // Nothing special needed for commit as changes are written directly to file
                // Just end transaction
                _inTransaction = false;
                _transactionBackupLines = null;
                DMEEditor.AddLogMessage("Success", "Transaction committed", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Failed to commit transaction: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
        }

        public IErrorsInfo Rollback(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (!_inTransaction)
                {
                    DMEEditor.AddLogMessage("Warning", "No transaction in progress to rollback", DateTime.Now, 0, null, Errors.Failed);
                    ErrorObject.Flag = Errors.Failed;
                    return ErrorObject;
                }

                // Restore file from backup
                if (_transactionBackupLines != null)
                {
                    File.WriteAllLines(CombineFilePath, _transactionBackupLines);
                }

                _inTransaction = false;
                _transactionBackupLines = null;
                DMEEditor.AddLogMessage("Success", "Transaction rolled back", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Failed to rollback transaction: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
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
        #region "Helpers"
        // Helper method to detect delimiter automatically from a CSV file
        private char DetectDelimiter(string filePath)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    string firstLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(firstLine))
                        return ','; // Default to comma

                    // Only count delimiters outside of quotes
                    Func<char, int> countDelimiter = (delim) =>
                    {
                        bool inQuotes = false;
                        int count = 0;
                        foreach (char c in firstLine)
                        {
                            if (c == '\"') inQuotes = !inQuotes;
                            else if (c == delim && !inQuotes) count++;
                        }
                        return count;
                    };

                    var delimiters = new[] { ',', ';', '\t', '|' };
                    return delimiters.OrderByDescending(d => countDelimiter(d)).First();
                }
            }
            catch
            {
                return ','; // Default to comma if detection fails
            }
        }
        public IErrorsInfo BulkInsert(string EntityName, List<object> InsertedData)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (InsertedData == null || InsertedData.Count == 0)
                {
                    DMEEditor.AddLogMessage("Warning", "No data provided for bulk insert", DateTime.Now, 0, null, Errors.Failed);
                    ErrorObject.Flag = Errors.Failed;
                    return ErrorObject;
                }

                var entityStructure = GetEntityStructure(EntityName, false);
                if (entityStructure == null || entityStructure.Fields.Count == 0)
                {
                    DMEEditor.AddLogMessage("Error", $"Entity structure not found for {EntityName}", DateTime.Now, 0, null, Errors.Failed);
                    ErrorObject.Flag = Errors.Failed;
                    return ErrorObject;
                }

                // Read headers if file exists
                var lines = new List<string>();
                bool fileExists = File.Exists(CombineFilePath);

                if (fileExists)
                {
                    using (var reader = new StreamReader(CombineFilePath))
                    {
                        string header = reader.ReadLine();
                        lines.Add(header); // Add header line
                    }
                }
                else
                {
                    // Create headers if file doesn't exist
                    lines.Add(string.Join(Delimiter.ToString(),
                        entityStructure.Fields.Select(f => f.fieldname)));
                }

                // Add all data rows efficiently
                foreach (var dataItem in InsertedData)
                {
                    var newRow = new List<string>();
                    foreach (var field in entityStructure.Fields)
                    {
                        var prop = dataItem.GetType().GetProperty(field.fieldname);
                        if (prop != null)
                        {
                            var value = prop.GetValue(dataItem)?.ToString() ?? string.Empty;
                            // Escape quotes and add quotes around values with delimiters
                            if (value.Contains(Delimiter) || value.Contains('"'))
                            {
                                value = $"\"{value.Replace("\"", "\"\"")}\"";
                            }
                            newRow.Add(value);
                        }
                        else
                        {
                            newRow.Add(string.Empty);
                        }
                    }

                    lines.Add(string.Join(Delimiter.ToString(), newRow));
                }

                // Write all at once for better performance
                File.WriteAllLines(CombineFilePath, lines);

                DMEEditor.AddLogMessage("Success", $"Bulk insert of {InsertedData.Count} records completed", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Failed in bulk insert: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }

            return ErrorObject;
        }
        private bool ValidateCSVHeaders(string filePath, EntityStructure entityStructure)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine))
                        return false;

                    string[] headers = headerLine.Split(Delimiter);
                    var entityFieldNames = entityStructure.Fields.Select(f => f.fieldname).ToList();

                    // Check if all entity fields exist in the headers
                    foreach (var fieldName in entityFieldNames)
                    {
                        if (!headers.Contains(fieldName))
                        {
                            DMEEditor.AddLogMessage("Warning", $"CSV header missing field: {fieldName}", DateTime.Now, 0, null, Errors.Failed);
                            return false;
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Failed to validate CSV headers: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }
        public IErrorsInfo ExportDataToCSV(string entityName, string targetFilePath, bool includeHeaders = true, char delimiter = ',')
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                var entity = GetEntityStructure(entityName, false);
                if (entity == null)
                {
                    DMEEditor.AddLogMessage("Error", $"Entity {entityName} not found", DateTime.Now, 0, null, Errors.Failed);
                    ErrorObject.Flag = Errors.Failed;
                    return ErrorObject;
                }

                if (string.IsNullOrEmpty(CombineFilePath))
                {
                    CombineFilePath = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
                }
                if (!File.Exists(CombineFilePath))
                {
                    DMEEditor.AddLogMessage("Error", $"Source CSV not found: {CombineFilePath}", DateTime.Now, 0, null, Errors.Failed);
                    ErrorObject.Flag = Errors.Failed;
                    return ErrorObject;
                }

                using var parser = new CsvTextFieldParser(CombineFilePath);
                parser.SetDelimiter(Delimiter);

                using var writer = new StreamWriter(targetFilePath, false, Encoding.UTF8);

                var header = parser.ReadFields();
                if (header == null)
                {
                    // empty file
                    return ErrorObject;
                }

                if (includeHeaders)
                {
                    writer.WriteLine(string.Join(delimiter.ToString(), header.Select(h => EscapeCsv(h, delimiter))));
                }

                while (!parser.EndOfData)
                {
                    string[] row;
                    try { row = parser.ReadFields(); } catch { continue; }
                    if (row == null) break;
                    writer.WriteLine(string.Join(delimiter.ToString(), row.Select(v => EscapeCsv(v, delimiter))));
                }

                DMEEditor.AddLogMessage("Success", $"Data exported to {targetFilePath}", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Failed to export data: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
        }
        // Support for additional CSV options in the ConnectionProperty
        /// <summary>
        /// Sanitizes column names according to configured rules
        /// </summary>
        /// <summary>
        /// Detects file encoding from a CSV file
        /// </summary>
        private Encoding DetectEncoding(string filePath)
        {
            try
            {
                // Try to detect BOM (Byte Order Mark)
                byte[] bom = new byte[4];
                using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    file.Read(bom, 0, 4);
                }

                // Check for UTF-8 BOM
                if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                    return Encoding.UTF8;

                // Check for UTF-16 LE BOM
                if (bom[0] == 0xFF && bom[1] == 0xFE)
                    return Encoding.Unicode;

                // Check for UTF-16 BE BOM
                if (bom[0] == 0xFE && bom[1] == 0xFF)
                    return Encoding.BigEndianUnicode;

                // Check for UTF-32 LE BOM
                if (bom[0] == 0xFF && bom[1] == 0xFE && bom[2] == 0 && bom[3] == 0)
                    return Encoding.UTF32;

                // If no BOM, try to analyze content
                using (StreamReader reader = new StreamReader(filePath, Encoding.ASCII, true))
                {
                    string sample = reader.ReadToEnd();

                    // Check for UTF-8 patterns
                    bool couldBeUTF8 = true;
                    for (int i = 0; i < sample.Length; i++)
                    {
                        if (sample[i] > 127)
                        {
                            couldBeUTF8 = false;
                            break;
                        }
                    }

                    if (couldBeUTF8)
                        return Encoding.UTF8;
                }

                // Default to system default encoding
                return Encoding.Default;
            }
            catch
            {
                return Encoding.UTF8; // Default to UTF-8 if detection fails
            }
        }
        /// <summary>
        /// Validates a data row against the entity structure
        /// </summary>
        private (bool IsValid, List<string> Errors) ValidateRow(EntityStructure entityStructure, Dictionary<string, object> dataRow)
        {
            var errors = new List<string>();
            bool isValid = true;

            foreach (var field in entityStructure.Fields)
            {
                // Check required fields
                if (!field.AllowDBNull && (!dataRow.ContainsKey(field.fieldname) || dataRow[field.fieldname] == null))
                {
                    errors.Add($"Field '{field.fieldname}' is required but no value was provided");
                    isValid = false;
                    continue;
                }

                // Skip validation for null values in nullable fields
                if (!dataRow.ContainsKey(field.fieldname) || dataRow[field.fieldname] == null)
                    continue;

                // Type validation
                try
                {
                    Type fieldType = Type.GetType(field.fieldtype);
                    if (fieldType != null)
                    {
                        Convert.ChangeType(dataRow[field.fieldname], fieldType);
                    }
                }
                catch
                {
                    errors.Add($"Field '{field.fieldname}' has an invalid value of '{dataRow[field.fieldname]}' for type {field.fieldtype}");
                    isValid = false;
                }

                // Size validation for string fields
                if (field.fieldtype == "System.String" && field.Size1 > 0)
                {
                    string value = dataRow[field.fieldname]?.ToString() ?? string.Empty;
                    if (value.Length > field.Size1)
                    {
                        errors.Add($"Field '{field.fieldname}' value exceeds maximum length of {field.Size1}");
                        isValid = false;
                    }
                }
            }

            return (isValid, errors);
        }
        /// <summary>
        /// Returns a DataReader-like interface for efficiently working with large CSV files
        /// </summary>
        public ICSVDataReader GetDataReader(string entityName, List<string> columnNames = null)
        {
            try
            {
                var entityStructure = GetEntityStructure(entityName, false);
                if (entityStructure == null)
                {
                    DMEEditor.AddLogMessage("Error", $"Entity structure not found for {entityName}", DateTime.Now, 0, null, Errors.Failed);
                    return null;
                }

                return new CSVDataReader(CombineFilePath, Delimiter, entityStructure, columnNames);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Failed to create data reader: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }
        // Support for additional CSV options in the ConnectionProperty
      

        #endregion "Helpers"
    }
}
