using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using static Dapper.SqlMapper;


namespace TheTechIdea.Beep.Json
{
    public class JsonDataSource : IDataSource
    {
        public JsonDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType =  DataSourceType.Json;
            Dataconnection = new FileConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject,

            };
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName.Equals(datasourcename,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            
            if(File.Exists(FileName))
            {
                LoadJsonFile();
            }
        }
       
        private bool IsFileRead=false;
        public event EventHandler<PassedArgs> PassEvent;
        public DataSourceType DatasourceType { get; set; }= DataSourceType.Json;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; }
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public List<object> Records { get; set; }
        public DataSet Dataset { get;set; } = new DataSet();
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public bool HeaderExist { get; set; }
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        string FileName => Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
        string jsonData  ;
        JArray jArray ;

        #region "DataSource Methods"
        public int GetEntityIdx(string entityName)
        {
            int i = -1;
            if (Entities.Count > 0)
            {
                i = Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase));
                if (i < 0)
                {
                    i = Entities.FindIndex(p => p.DatasourceEntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase));
                }
                else
                    if (i < 0)
                {
                    i = Entities.FindIndex(p => p.OriginalEntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase));
                }
                return i;
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

                    if (GetFileState() == ConnectionState.Open && !IsFileRead)
                    {
                        Getfields();
                    }
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                }
                else
                {
                    Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;
                };

            }

            return ConnectionStatus;
        }
        public ConnectionState Closeconnection()
        {
            SaveJsonFile();
            return ConnectionStatus = ConnectionState.Closed;
        }
        public bool CheckEntityExist(string EntityName)
        {
            bool retval = false;
            if (GetFileState() == ConnectionState.Open)
            {
                if (Entities != null)
                {
                    if (Entities.Where(x => string.Equals(x.EntityName, EntityName, StringComparison.InvariantCultureIgnoreCase)).Count() > 0)
                    {
                        retval = true;
                    }
                    else
                        retval = false;

                }

            }

            return retval;
        }
        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                if (Entities != null)
                {
                    if (Entities.Count > 0)
                    {
                        if (!CheckEntityExist(entity.EntityName))
                        {
                            Type entype = DMEEditor.Utilfunction.GetEntityType(entity.EntityName, entity.Fields);
                            if (entype == null) return false;
                            if (entity.Fields != null)
                            {
                                DataTable tb = new DataTable(entity.EntityName);
                                foreach (EntityField col in entity.Fields)
                                {
                                    DataColumn co = tb.Columns.Add(col.fieldname);
                                    co.DataType = Type.GetType(col.fieldtype);

                                }
                                Dataset.Tables.Add(tb);
                            }
                            Entities.Add(entity);
                            EntitiesNames.Add(entity.EntityName);
                            DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                        }
                        else
                            DMEEditor.AddLogMessage("Beep", $"Could not Add Entity {entity.EntityName} is Exist already", DateTime.Now, 0, null, Errors.Failed);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not Add Entity {entity.EntityName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

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
        public List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (GetFileState() == ConnectionState.Open && !IsFileRead)
                {
                    Getfields();
                }
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
                return await Task.Run(() => GetEntity(entityname, new List<AppFilter>() { }));
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
                return null;
            }
        }
        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
               // IEnumerable<DataRow> filteredRecords;
                DataTable tb = new DataTable();
                bool FilterExist = false;
                string qrystr = "";
                EntityStructure entity = GetEntityStructure(EntityName);

                int fromline = entity.StartRow;
                int toline = entity.EndRow;
                if (GetFileState() == ConnectionState.Open)
                {
                    if (Entities != null)
                    {
                        if (Entities.Count() == 0)
                        {
                            GetEntitesList();
                        }

                    }

                    //if (filter != null)
                    //{
                    //    if (filter.Count > 0)
                    //    {
                    //        AppFilter fromlinefilter = filter.FirstOrDefault(p => p.FieldName.Equals("FromLine", StringComparison.InvariantCultureIgnoreCase));
                    //        if (fromlinefilter != null)
                    //        {
                    //            fromline = Convert.ToInt32(fromlinefilter.FilterValue);
                    //        }
                    //        AppFilter Tolinefilter = filter.FirstOrDefault(p => p.FieldName.Equals("ToLine", StringComparison.InvariantCultureIgnoreCase));
                    //        if (fromlinefilter != null)
                    //        {
                    //            toline = Convert.ToInt32(fromlinefilter.FilterValue);
                    //        }
                    //    }
                    //}
                    int idx = -1;
                    idx=EntitiesNames.IndexOf(EntityName);
                    tb= ReadEntityFromDataSet(idx);
                    //Records= (List<object>)DMEEditor.ConfigEditor.JsonLoader.DeserializeObject(EntityName);
                    //if (Entities.Count > -1)
                    //{
                    //    if (filter != null)
                    //    {
                    //        if (filter.Any(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.FieldName)))
                    //        {
                    //            FilterExist=true;
                    //            filteredRecords = ApplyAppFiltersNoDataView(tb, filter);
                    //        }
                    //    }
                    //}
                }
                //if (!FilterExist)
                //{
                //    filteredRecords = tb. ;
                //}
                return tb;
            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                Logger.WriteLog($"Error in getting File Data ({ex.Message}) ");
                return null;
            }
        }
        public System.Data.DataView ApplyAppFilters(DataTable records, List<AppFilter> filters)
        {
            System.Data.DataView filteredRecords = new System.Data.DataView(records);

            // Apply each filter from the list
            foreach (AppFilter filter in filters)
            {
                string filterExpression = GenerateFilterExpression(filter);
                filteredRecords.RowFilter += (filteredRecords.RowFilter.Length > 0 ? " AND " : "") + filterExpression;
            }

            return filteredRecords;
        }
        public IEnumerable<DataRow> ApplyAppFiltersNoDataView(DataTable records, List<AppFilter> filters)
        {
            IEnumerable<DataRow> filteredRows = records.AsEnumerable();

            // Apply each filter from the list
            foreach (AppFilter filter in filters)
            {
                filteredRows = filteredRows.Where(row => GenerateFilterExpression(row, filter));
            }

            return filteredRows;
        }

        public string GenerateFilterExpression(AppFilter filter)
        {
            switch (filter.Operator)
            {
                case "equals":
                case "=":
                    return $"{filter.FieldName} = '{filter.FilterValue}'";
                case "contains":
                    return $"{filter.FieldName} LIKE '%{filter.FilterValue}%'";
                case ">":
                    return $"{filter.FieldName} > '{filter.FilterValue}'";
                case "<":
                    return $"{filter.FieldName} < '{filter.FilterValue}'";
                case ">=":
                    return $"{filter.FieldName} >= '{filter.FilterValue}'";
                case "<=":
                    return $"{filter.FieldName} <= '{filter.FilterValue}'";
                case "<>":
                case "!=":
                    return $"{filter.FieldName} <> '{filter.FilterValue}'";
                case "between":
                    return $"{filter.FieldName} >= '{filter.FilterValue}' AND {filter.FieldName} <= '{filter.FilterValue1}'";
                default:
                    throw new ArgumentException($"Invalid filter operator: {filter.Operator}");
            }
        }

        public bool GenerateFilterExpression(DataRow record, AppFilter filter)
        {
            var fieldValue = record[filter.FieldName];

            switch (filter.Operator)
            {
                case "equals":
                case "=":
                    return fieldValue.Equals(filter.FilterValue);
                case "contains":
                    return fieldValue.ToString().Contains(filter.FilterValue);
                case ">":
                    return Comparer.Default.Compare(fieldValue, filter.FilterValue) > 0;
                case "<":
                    return Comparer.Default.Compare(fieldValue, filter.FilterValue) < 0;
                case ">=":
                    return Comparer.Default.Compare(fieldValue, filter.FilterValue) >= 0;
                case "<=":
                    return Comparer.Default.Compare(fieldValue, filter.FilterValue) <= 0;
                case "<>":
                case "!=":
                    return !fieldValue.Equals(filter.FilterValue);
                case "between":
                    var value1 = filter.FilterValue;
                    var value2 = filter.FilterValue1;

                    return Comparer.Default.Compare(fieldValue, value1) >= 0 &&
                           Comparer.Default.Compare(fieldValue, value2) <= 0;
                default:
                    throw new ArgumentException($"Invalid filter operator: {filter.Operator}");
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
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            try
            {
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                //SetObjects(EntityName);
                //DataRow dr = (DataRow)InsertedData;
                //DataTable dataTable = Dataset.Tables[EntityName];
                //dataTable.Rows.Add(dr);
                //foreach (var entity in Entities)
                //{

                //}

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Insert {EntityName}  - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public object RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }
        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            EntityStructure retval = null;

            if (GetFileState() == ConnectionState.Open)
            {
                if (Entities != null)
                {
                    if (Entities.Count == 0)
                    {
                        Getfields();

                    }
                }

                retval = Entities.Where(x => string.Equals(x.OriginalEntityName, EntityName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (retval == null || refresh)
                {
                    IsFileRead = false;
                    Getfields();
                    if (Entities.Count >= 0)
                    {
                        retval = Entities[0];
                    }
                  
                   
                }
               
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
            }
            return retval;
        }
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            EntityStructure retval = null;

            if (GetFileState() == ConnectionState.Open)
            {
                if (Entities != null)
                {
                    if (Entities.Count == 0)
                    {
                        Getfields();
                    }
                }
                retval = Entities.Where(x => string.Equals(x.OriginalEntityName, fnd.EntityName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (retval == null || refresh)
                {
                    GetEntityStructure(fnd.EntityName, true);
                }
              
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
            }
            return retval;
        }
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            try
            {
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                foreach (var item in entities)
                {
                    CreateEntityAs(item);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject;

        }
        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
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
                // Serialize the updated list of  objects back into a JSON array string
                SaveJsonFile();

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        #region "Insert or Update or Delete Objects"
        EntityStructure DataStruct = null;
        IDbCommand command = null;
        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;
        #endregion
        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, true);
               // command = RDBMSConnection.DbConn.CreateCommand();
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            try
            {
               
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                SetObjects(EntityName);
                DataTable dataTable = Dataset.Tables[EntityName];
                DataRow dr = (DataRow)UploadDataRow;
                if (dataTable != null)
                {
                    int idx=dataTable.Rows.IndexOf(dr);
                    if (idx != -1)
                    {
                       // dataTable.Rows[idx]. = dr;
                    }
                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Update {EntityName} - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            try
            {
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                SetObjects(EntityName);
                DataTable dataTable = Dataset.Tables[EntityName];
                DataRow dr = (DataRow)DeletedDataRow;
                if (dataTable != null)
                {
                    dataTable.Rows.Remove(dr);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Delete {EntityName}  - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                SetObjects(EntityName);
                DataRow dr = (DataRow)InsertedData;
                DataTable dataTable = Dataset.Tables[EntityName] ;
                dataTable.Rows.Add(dr);
               
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Insert {EntityName}  - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return (Task<object>)GetEntity(EntityName, Filter);
        }
        #endregion
        #region "Json Reading MEthods"
        public DataSet ParseJsonFile(string filePath)
        {
            string jsonData = File.ReadAllText(filePath);
            Dataset = new DataSet();
            Entities = new List<EntityStructure>();
            string tableName = Path.GetFileNameWithoutExtension(filePath); // Use the file name as table name when dealing with a single JArray

            var token = JToken.Parse(jsonData);
            if (token is JObject jsonObject)
            {
                foreach (var property in jsonObject.Properties())
                {
                    if (property.Value is JArray jsonArray)
                    {
                        var dataTable = JsonConvert.DeserializeObject<DataTable>(jsonArray.ToString());
                        dataTable.TableName = property.Name;
                        Dataset.Tables.Add(dataTable);

                        // Create an EntityStructure for this DataTable
                        var entityStructure = new EntityStructure(property.Name);
                        foreach (DataColumn column in dataTable.Columns)
                        {
                            var entityField = new EntityField
                            {
                                fieldname = column.ColumnName,
                                Originalfieldname = column.ColumnName,
                                fieldtype = column.DataType.ToString()
                            };
                            entityStructure.Fields.Add(entityField);
                        }
                        Entities.Add(entityStructure);
                    }
                }
            }
            else if (token is JArray jArray) // Handle a single JArray
            {
                var dataTable = JsonConvert.DeserializeObject<DataTable>(jArray.ToString());
                dataTable.TableName = tableName;
                Dataset.Tables.Add(dataTable);

                // Create an EntityStructure for this DataTable
                var entityStructure = new EntityStructure(tableName);
                foreach (DataColumn column in dataTable.Columns)
                {
                    var entityField = new EntityField
                    {
                        fieldname = column.ColumnName,
                        Originalfieldname = column.ColumnName,
                        fieldtype = column.DataType.ToString()
                    };
                    entityStructure.Fields.Add(entityField);
                }
                Entities.Add(entityStructure);
            }
            EntitiesNames = new List<string>();
            EntitiesNames.Clear();
            EntitiesNames.AddRange(Entities.Select(p => p.EntityName).ToList());
            return Dataset;
        }

        public List<EntityStructure> ParseJsonFileBk(string filePath)
    {
        var entityStructures = new List<EntityStructure>();
            EntityStructure entityStructure=null;
            jsonData = File.ReadAllText(filePath);
            string filename=Path.GetFileNameWithoutExtension(filePath);
            var token = JToken.Parse(jsonData);
            Records = new List<object>();
            if (token is JObject jsonObject)
            {
                // handle JSON Object
                if (jsonObject.HasValues)
                {
                    if (jsonObject.Children().Count() == 1 && jsonObject.First is JArray jsonArray)
                    {
                        // Single list case
                         entityStructure = new EntityStructure(DatasourceName);
                        foreach (var jObject in jsonArray.Children<JObject>())
                        {
                            var entityField = new EntityField();

                            foreach (var fieldProperty in jObject.Properties())
                            {
                                var fieldName = fieldProperty.Name;
                                var fieldValue = fieldProperty.Value.ToString();

                                // Update the EntityField properties dynamically based on the field properties in JSON
                                var propertyInfo = entityField.GetType().GetProperty(fieldName);
                                if (propertyInfo != null)
                                {
                                    propertyInfo.SetValue(entityField, Convert.ChangeType(fieldValue, propertyInfo.PropertyType));
                                }
                            }

                            entityStructure.Fields.Add(entityField);
                        }

                        entityStructures.Add(entityStructure);
                        EntitiesNames = new List<string>();
                        EntitiesNames.Clear();
                        EntitiesNames.AddRange(entityStructures.Select(p => p.EntityName).ToList());
                    }
                    else
                    {
                        // Multiple lists case
                        foreach (var property in jsonObject.Properties())
                        {
                            if (property.Value is JArray jsonArray2)
                            {
                                 entityStructure = new EntityStructure(property.Name);
                                entityStructure.DatasourceEntityName=property.Name;
                                entityStructure.OriginalEntityName=property.Name;

                                foreach (var jObject in jsonArray2.Children<JObject>())
                                {
                                    var entityField = new EntityField();

                                    foreach (var fieldProperty in jObject.Properties())
                                    {
                                        var fieldName = fieldProperty.Name;
                                        var fieldValue = fieldProperty.Value.ToString();

                                        // Update the EntityField properties dynamically based on the field properties in JSON
                                        var propertyInfo = entityField.GetType().GetProperty(fieldName);
                                        if (propertyInfo != null)
                                        {
                                            propertyInfo.SetValue(entityField, Convert.ChangeType(fieldValue, propertyInfo.PropertyType));
                                        }
                                    }

                                    entityStructure.Fields.Add(entityField);
                                }

                                entityStructures.Add(entityStructure);
                            }
                        }
                        Entities.Clear();
                        Entities = entityStructures;
                        EntitiesNames.Clear();
                        EntitiesNames.AddRange(entityStructures.Select(p=>p.EntityName).ToList());

                    }
                }
            }
            else if (token is JArray jArray)
            {
                if (jArray.Count > 0)
                {
                    entityStructure = new EntityStructure(DatasourceName);
                    entityStructure.DatasourceEntityName = DatasourceName;
                    entityStructure.OriginalEntityName = DatasourceName;
                    entityStructure.Fields = new List<EntityField>();

                    // Get the first record to infer the schema.
                    var firstRecord = jArray[0] as JObject;

                    foreach (var property in firstRecord.Properties())
                    {
                        var entityField = new EntityField
                        {
                            fieldname = property.Name,
                            Originalfieldname = property.Name,
                            fieldtype = MapJsonTypeToDotNetType(property.Value.Type.ToString())
                        };
                        entityStructure.Fields.Add(entityField);
                    }
                  
                }
                Entities.Clear();
                entityStructures.Add(entityStructure);
                Entities.Add(entityStructure);
                EntitiesNames = new List<string>();
                EntitiesNames.Clear();
                EntitiesNames.AddRange(entityStructures.Select(p => p.EntityName).ToList());
            }
            // Check if the JSON object contains multiple lists or a single list
         

        return entityStructures;
    }
      
        public void DataSetToJson( string filePath)
        {
            var tablesDictionary = new Dictionary<string, JArray>();
            foreach (DataTable table in Dataset.Tables)
            {
                string json = JsonConvert.SerializeObject(table);
                tablesDictionary.Add(table.TableName, JArray.Parse(json));
            }

            string outputJson = JsonConvert.SerializeObject(tablesDictionary, Formatting.Indented);
            File.WriteAllText(filePath, outputJson);
        }
        private DataTable ReadEntityFromDataSet(int entityId) {
            return Dataset.Tables[entityId];
        }
        private List<dynamic> ReadEntityDataFromJsonFile( int entityIndex)
        {
            string jsonData = File.ReadAllText(FileName);
            var token = JToken.Parse(jsonData);
            var entityData = new List<dynamic>();

            // If the JSON data is an object potentially containing multiple lists
            if (token is JObject jsonObject)
            {
                // Get the property at the specified index
                var property = jsonObject.Properties().ElementAtOrDefault(entityIndex);
                if (property != null && property.Value is JArray entityArray)
                {
                    foreach (var jObject in entityArray.Children<JObject>())
                    {
                        dynamic record = new ExpandoObject();
                        var recordDictionary = (IDictionary<string, object>)record;

                        // (Your existing code to fill recordDictionary goes here.)

                        entityData.Add(record);
                    }
                }
            }
            // If the JSON data is a single list
            else if (token is JArray jArray && entityIndex == 0)
            {
                // Process the JArray directly if the entityIndex is 0
                foreach (var jObject in jArray.Children<JObject>())
                {
                    dynamic record = new ExpandoObject();
                    var recordDictionary = (IDictionary<string, object>)record;

                    // (Your existing code to fill recordDictionary goes here.)

                    entityData.Add(record);
                }
            }

            return entityData;
        }
        public TypeCode ToConvert(Type dest)
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
                    retval = TypeCode.String;

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
                                string st = dr[item.fieldname].ToString().Trim();
                                if (!string.IsNullOrEmpty(st) && !string.IsNullOrWhiteSpace(st))
                                {
                                    r[item.fieldname] = Convert.ChangeType(dr[item.fieldname], ToConvert(Type.GetType(item.fieldtype)));
                                }

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
        public ConnectionState OpenConnection()
        {
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName.Equals(Dataconnection.ConnectionProp.ConnectionName,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
           

            if (GetFileState()== ConnectionState.Open)
            {
                Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(Dataconnection.ConnectionProp.ConnectionName).Entities;
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
         //   string filen = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.ConnectionName);
            if (File.Exists(FileName))
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
                if (!IsFileRead)
                {
                    LoadJsonFile();
                }

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
        public IErrorsInfo LoadJsonFile()
        {
            try
            {
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                if(GetFileState()  == ConnectionState.Open)
                {
                    ParseJsonFile(FileName);
                    // InferSchemaFromRecords(FileName);
                    IsFileRead = true;
                }
               
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject;
        }
        public IErrorsInfo SaveJsonFile()
        {
            try
            {
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                DataSetToJson(FileName);
                //DMEEditor.ConfigEditor.JsonLoader.Serialize(FileName,Records);
                // Write the updated JSON array string back to the file
                //   File.WriteAllText(FileName, updatedJson);

            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
            }
            return DMEEditor.ErrorObject;
        }
        public List<EntityStructure> GetEntityStructures(bool refresh = false)
        {
            List<EntityStructure> retval = new List<EntityStructure>();


           // string filen = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            if (GetFileState()== ConnectionState.Open)
            {

                if ((Entities == null) || (Entities.Count == 0) || refresh)
                {
                   // Entities = new List<EntityStructure>();
                    Getfields();
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = Dataconnection.ConnectionProp.ConnectionName, Entities = Entities });
                    // Dataconnection.ConnectionProp.Entities = Entities;
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                }
                else
                {
                    if (refresh)
                    {
                        IsFileRead = false;
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
            //ds = new DataSet(); ;
            //Entities = new List<EntityStructure>();

            if (File.Exists(FileName) == true)
            {
                try
                {
                    if(!IsFileRead)
                    {
                        LoadJsonFile();
                    }
                  
                    if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                    {
                        return;
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
        public IErrorsInfo InferSchemaFromRecords(string jsonFilePath)
        {
            Records = new List<object>();
            jsonData = File.ReadAllText(jsonFilePath);
            jArray = JArray.Parse(jsonData);
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            EntityStructure entityStructure = new EntityStructure();
            try
            {
                if (jArray.Count > 0)
                {
                    entityStructure.Fields = new List<EntityField>();

                    // Get the first record to infer the schema.
                    var firstRecord = jArray[0] as JObject;

                    foreach (var property in firstRecord.Properties())
                    {
                        var entityField = new EntityField
                        {
                            fieldname = property.Name,
                            Originalfieldname = property.Name,
                            fieldtype = MapJsonTypeToDotNetType(property.Value.Type.ToString())
                        };
                        entityStructure.Fields.Add(entityField);
                    }
                    foreach (var jObject in jArray.Children<JObject>())
                    {
                        dynamic record = new ExpandoObject();
                        var recordDictionary = (IDictionary<string, object>)record;

                        foreach (var entityField in entityStructure.Fields)
                        {
                            try
                            {
                                var value = ConvertJTokenToType(jObject[entityField.fieldname], entityField.fieldtype);
                                recordDictionary[entityField.fieldname] = value;
                            }
                            catch (Exception ex)
                            {
                                DMEEditor.AddLogMessage("Beep", $"JsonDataSource Error in Mapping Fields {entityField.fieldname} - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                            }
                          
                        }

                        Records.Add(record);
                    }
                }
                Entities.Clear();
                Entities.Add(entityStructure);
                EntitiesNames.Clear();
                EntitiesNames.Add(Dataconnection.ConnectionProp.FileName);
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.AddLogMessage("Error", $"Error Loadin Json File {ex.Message} - {Dataconnection.ConnectionProp.FileName}", DateTime.Now, -1, "", Errors.Failed);
            }




            return DMEEditor.ErrorObject;
        }
        public object ConvertJTokenToType(JToken token, string type)
        {
            // Adjust this conversion according to your needs and possible types.
            if(token == null)
            {
                return string.Empty;
            }
            switch (type)
            {
                case "System.String":
                    return token.ToString();
                case "System.Int32":
                    return token.ToObject<int>();
                case "System.Int64":
                    return token.ToObject<long>();
                case "System.Decimal":
                    return token.ToObject<decimal>();
                case "System.Double":
                    return token.ToObject<double>();
                case "System.Boolean":
                    return token.ToObject<bool>();
                case "System.DateTime":
                    return token.ToObject<DateTime>();
                case "System.Guid":
                    return token.ToObject<Guid>();
                case "System.Collections.Generic.List`1[System.String]":
                    return token.ToObject<List<string>>();
                case "System.Collections.Generic.List`1[System.Int32]":
                    return token.ToObject<List<int>>();
                case "System.Collections.Generic.Dictionary`2[System.String,System.String]":
                    return token.ToObject<Dictionary<string, string>>();
                case "System.Object":
                    return token.ToObject<object>();
                case "System.Array":
                     return token.ToString();  //token.ToObject<List<object>>();
                default:
                    throw new ArgumentException($"Invalid type: {type}");
            }
        }
        public List<dynamic> LoadJsonData(string jsonFilePath, EntityStructure entityStructure)
        {
            var jsonData = File.ReadAllText(jsonFilePath);
            var jArray = JArray.Parse(jsonData);

            var records = new List<dynamic>();

            foreach (var jObject in jArray.Cast<JObject>())
            {
                dynamic record = new ExpandoObject();
                var recordDictionary = (IDictionary<string, object>)record;
                foreach (var entityField in entityStructure.Fields)
                {
                    // Convert the JToken to the type indicated by EntityField.
                    var value = ConvertJTokenToType(jObject[entityField.fieldname], entityField.fieldtype);
                    recordDictionary[entityField.fieldname] = value;
                }
                records.Add(record);
            }

            return records;
        }
        public string MapJsonTypeToDotNetType(string jsonType)
        {
            switch (jsonType.ToLower())
            {
                case "string":
                    return typeof(string).FullName;
                case "number":
                    return typeof(double).FullName; // or Decimal or Float based on your requirement
                case "integer":
                    return typeof(int).FullName; // or Int64 based on your requirement
                case "boolean":
                    return typeof(bool).FullName;
                case "object":
                    return typeof(object).FullName;
                case "array":
                    return typeof(Array).FullName;
                case "null":
                    return typeof(void).FullName;
                case "datetime":
                    return typeof(DateTime).FullName;
                case "guid":
                    return typeof(Guid).FullName;
                default:
                    throw new ArgumentException($"Invalid JSON type: {jsonType}");
            }
        }
        #region "Data Table Methods"
        public DataTable LoadJsonFileToDataTable(string jsonFilePath)
        {
             jsonData = File.ReadAllText(jsonFilePath);
             jArray = JArray.Parse(jsonData);


            DataTable dataTable = new DataTable();
            EntityStructure entityStructure = new EntityStructure();

            if (jArray.Count > 0)
            {
                var firstRecord = (JObject)jArray[0];
                var fieldNames = firstRecord.Properties().Select(p => p.Name).ToList();

                // Set up the fields in the EntityStructure based on field names
                entityStructure.Fields = fieldNames.Select(fieldName => new EntityField
                {
                    fieldname = fieldName,
                    fieldtype = "System.Object" // Set the default field type as System.Object
                }).ToList();

                foreach (var property in firstRecord.Properties())
                {
                    var columnName = property.Name;
                    var columnType = MapJsonTypeToDotNetType(property.Value.Type.ToString());
                    dataTable.Columns.Add(columnName, Type.GetType(columnType));
                }

                foreach (var jObject in jArray.Children<JObject>())
                {
                    var dataRow = dataTable.NewRow();

                    foreach (var property in jObject.Properties())
                    {
                        var columnName = property.Name;
                        var columnValue = ConvertJTokenToType(property.Value, dataTable.Columns[columnName].DataType.FullName);
                        dataRow[columnName] = columnValue;
                    }

                    dataTable.Rows.Add(dataRow);
                }
            }

            Entities.Clear();
            Entities.Add(entityStructure);
            EntitiesNames.Clear();
            EntitiesNames.Add(Dataconnection.ConnectionProp.FileName);
            return dataTable;
        }
        public DataTable ConvertListToDataTable(List<dynamic> records)
        {
            DataTable dataTable = new DataTable();

            if (records.Count > 0)
            {
                var firstRecord = (IDictionary<string, object>)records[0];

                foreach (var propertyName in firstRecord.Keys)
                {
                    var propertyValue = firstRecord[propertyName];
                    var columnType = propertyValue?.GetType() ?? typeof(object);
                    dataTable.Columns.Add(propertyName, columnType);
                }

                foreach (var record in records)
                {
                    var dataRow = dataTable.NewRow();

                    foreach (var propertyName in ((IDictionary<string, object>)record).Keys)
                    {
                        dataRow[propertyName] = ((IDictionary<string, object>)record)[propertyName];
                    }

                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
        }
        public void SaveDataTableToJsonFile(DataTable dataTable, string jsonFilePath)
        {
            // Load the original JSON file into a JArray
            JArray jsonArray;
            using (StreamReader file = File.OpenText(jsonFilePath))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                jsonArray = JArray.Load(reader);
            }

            // Iterate over the rows of the DataTable
            for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
            {
                DataRow row = dataTable.Rows[rowIndex];

                // Find the corresponding JObject in the JArray
                JObject jsonObject = (JObject)jsonArray[rowIndex];

                // Update the properties in the JObject with the values from the DataRow
                foreach (DataColumn column in dataTable.Columns)
                {
                    string columnName = column.ColumnName;
                    object columnValue = row[column];

                    // Update the property in the JObject
                    jsonObject[columnName] = JToken.FromObject(columnValue);
                }
            }

            // Save the modified JArray back to the JSON file
            using (StreamWriter file = File.CreateText(jsonFilePath))
            using (JsonTextWriter writer = new JsonTextWriter(file))
            {
                jsonArray.WriteTo(writer);
            }
        }
        #endregion "Data Table Methods"

        //public void CreateClass(int sheetno = 0)
        //{
        //    if (GetFileState() == ConnectionState.Open)
        //    {
        //        DataTable dataRows = new DataTable();

        //        dataRows = ds.Tables[sheetno];

        //        List<EntityField> flds = GetSheetColumns(ds.Tables[sheetno].TableName);
        //        string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();

        //        DMEEditor.classCreator.CreateClass(ds.Tables[sheetno].TableName, flds, classpath);

        //    }

        //}
        //public void CreateClass(string sheetname)
        //{
        //    if (GetFileState() == ConnectionState.Open)
        //    {
        //        DataTable dataRows = new DataTable();

        //        dataRows = ds.Tables[sheetname];

        //        List<EntityField> flds = GetSheetColumns(ds.Tables[sheetname].TableName);
        //        string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();

        //        DMEEditor.classCreator.CreateClass(ds.Tables[sheetname].TableName, flds, classpath);

        //    }

        //}
        //public List<Object> ReadList(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100)
        //{
        //    if (GetFileState() == ConnectionState.Open)
        //    {
        //        DataTable dataRows = new DataTable();

        //        dataRows = ds.Tables[sheetno];
        //        toline = dataRows.Rows.Count;
        //        List<EntityField> flds = GetSheetColumns(ds.Tables[sheetno].TableName);
        //        string classpath = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.ProjectClass).Select(x => x.FolderPath).FirstOrDefault();
        //        CreateClass(sheetno);
        //        Type a = Type.GetType("TheTechIdea.ProjectClasses." + ds.Tables[sheetno].TableName);
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

        //        dataRows = ds.Tables[sheetname];
        //        toline = dataRows.Rows.Count;
        //        List<EntityField> flds = GetSheetColumns(sheetname);
        //        CreateClass(sheetname);
        //        Type a = Type.GetType("TheTechIdea.ProjectClasses." + dataRows);
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
        #endregion
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
