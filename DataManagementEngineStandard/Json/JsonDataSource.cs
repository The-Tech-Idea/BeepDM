using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;

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
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.FileName == datasourcename).FirstOrDefault();
            FileName = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
        }
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
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public DataSet ds { get; set; }=new DataSet();
        public bool HeaderExist { get; set; }
        public virtual string ColumnDelimiter { get; set; } = "''";
        public virtual string ParameterDelimiter { get; set; } = ":";
        string FileName;

        #region "DataSource Methods"
        public int GetEntityIdx(string entityName)
        {
            int i = -1;
            if (Entities.Count > 0)
            {
                i = Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                if (i < 0)
                {
                    i = Entities.FindIndex(p => p.DatasourceEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                }
                else
                    if (i < 0)
                {
                    i = Entities.FindIndex(p => p.OriginalEntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
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

                    Getfields();
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
                                ds.Tables.Add(tb);
                            }
                            Entities.Add(entity);
                            EntitiesNames.Add(entity.EntityName);
                            DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {

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
                if (GetFileState() == ConnectionState.Open)
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
                DataTable dt = null;
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

                    if (filter != null)
                    {
                        if (filter.Count > 0)
                        {
                            AppFilter fromlinefilter = filter.FirstOrDefault(p => p.FieldName.Equals("FromLine", StringComparison.InvariantCultureIgnoreCase));
                            if (fromlinefilter != null)
                            {
                                fromline = Convert.ToInt32(fromlinefilter.FilterValue);
                            }
                            AppFilter Tolinefilter = filter.FirstOrDefault(p => p.FieldName.Equals("ToLine", StringComparison.InvariantCultureIgnoreCase));
                            if (fromlinefilter != null)
                            {
                                toline = Convert.ToInt32(fromlinefilter.FilterValue);
                            }
                        }
                    }
                    int idx = -1;
                    if (Entities.Count > 0)
                    {
                        idx = Entities.FindIndex(p => p.EntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase));

                    }

                    if (idx > -1)
                    {
                        entity = Entities[idx];
                        dt = ReadDataTable(EntityName, HeaderExist, fromline, toline);
                        SyncFieldTypes(ref dt, EntityName);
                        if (filter != null)
                        {
                            if (filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                            {

                                foreach (AppFilter item in filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator) && !p.FieldName.Equals("ToLine", StringComparison.InvariantCultureIgnoreCase) && !p.FieldName.Equals("FromLine", StringComparison.InvariantCultureIgnoreCase)))
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
            throw new NotImplementedException();
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

                retval = Entities.Where(x => string.Equals(x.OriginalEntityName, EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (retval == null || refresh)
                {
                    EntityStructure fndval = GetSheetEntity(EntityName);
                    retval = fndval;
                    if (retval == null)
                    {
                        Entities.Add(fndval);
                    }
                    else
                    {

                        Entities[GetEntityIdx(EntityName)] = fndval;
                    }
                }
                if (Entities.Count() == 0)
                {
                    Getfields();
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
                retval = Entities.Where(x => string.Equals(x.OriginalEntityName, fnd.EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (retval == null || refresh)
                {
                    EntityStructure fndval = GetSheetEntity(fnd.EntityName);
                    retval = fndval;
                    if (retval == null)
                    {
                        Entities.Add(fndval);
                    }
                    else
                    {
                        Entities[GetEntityIdx(fnd.EntityName)] = fndval;
                    }
                }
                if (Entities.Count() == 0)
                {
                    Getfields();

                }
                DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
            }
            return retval;
        }
        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }
        public IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            throw new NotImplementedException();
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
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                DataTable dataTable = GetDataTable(EntityName);
                if (dataTable != null)
                {
                    EntityStructure strc = GetEntityStructure(EntityName);
                    DataRow dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, GetEntityType(EntityName), InsertedData, strc);
                    DataRow newrow = dataTable.NewRow();
                    if (dr != null)
                    {
                        foreach (var item in strc.Fields)
                        {
                            newrow[item.fieldname] = dr[item.fieldname];
                        }
                    }
                    dataTable.Rows.Add(newrow);
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
        public Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return (Task<object>)GetEntity(EntityName, Filter);
        }
        #endregion
        #region "Json Reading MEthods"
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
            Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == Dataconnection.ConnectionProp.ConnectionName).FirstOrDefault();
            Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(Dataconnection.ConnectionProp.ConnectionName).Entities;

            if (File.Exists(FileName))
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
        public IErrorsInfo LoadJsonFile()
        {
            try
            {
                DMEEditor.ErrorObject.Ex = null;
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "";
                string json = File.ReadAllText(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));

                ds = DMEEditor.ConfigEditor.JsonLoader.ConverttoDataset(json);
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
                string json = File.ReadAllText(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));

                DMEEditor.ConfigEditor.JsonLoader.Serialize(json, ds);
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


            string filen = Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName);
            if (File.Exists(filen))
            {

                if ((Entities == null) || (Entities.Count == 0) || refresh)
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

                    LoadJsonFile();
                    if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                    {
                        return;
                    }
                    int i = 0;
                    foreach (DataTable tb in ds.Tables)
                    {
                        string sheetname = tb.TableName;
                        EntityStructure entityData = new EntityStructure();
                        int idx = Entities.FindIndex(p => p.EntityName.Equals(tb.TableName, StringComparison.InvariantCultureIgnoreCase));
                        if (idx > -1)
                        {
                            entityData = Entities[idx];
                        }
                        else
                        {

                            entityData = new EntityStructure();
                            entityData.Viewtype = ViewType.File;
                            entityData.DatabaseType = DataSourceType.Text;
                            entityData.DataSourceID = FileName;
                            entityData.DatasourceEntityName = tb.TableName;
                            entityData.Caption = tb.TableName;
                            entityData.EntityName = sheetname;
                            entityData.Id = i;
                            i++;
                            entityData.OriginalEntityName = sheetname;
                            Entities.Add(entityData);
                            entityData.Drawn = true;
                            entityData.Fields = new List<EntityField>();
                            DataTable tbdata = ReadDataTable(tb.TableName, true, entityData.StartRow);
                            entityData.Fields.AddRange(GetFieldsbyTableScan(tbdata, tbdata.TableName, tbdata.Columns));
                            entityData.Drawn = true;
                        }
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
        private List<EntityField> GetFieldsbyTableScan(DataTable tbdata, string sheetname, DataColumnCollection datac)
        {
            var rows = from DataRow a in tbdata.Rows select a;
            IEnumerable<DataRow> tb = rows;
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
            // setup Fields for Entity
            foreach (DataColumn field in datac)
            {
                EntityField f = new EntityField();
                string entspace = Regex.Replace(field.ColumnName, @"\s+", "_");
                f.fieldname = field.ColumnName;
                f.Originalfieldname = field.ColumnName;
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
            // Scan all rows in Table for types
            foreach (DataRow r in tb)
            {
                try
                {
                    // Scan fields in row for Types
                    foreach (EntityField f in flds)
                    {
                        try
                        {
                            if (r[f.fieldname] != DBNull.Value)
                            {
                                valstring = r[f.fieldname].ToString();
                                dateval = DateTime.Now;

                                if (!string.IsNullOrEmpty(valstring) && !string.IsNullOrWhiteSpace(valstring))
                                {
                                    if (f.fieldtype != "System.String")
                                    {
                                        if (decimal.TryParse(valstring, out dval))
                                        {
                                            f.fieldtype = "System.Decimal";
                                            f.Checked = true;
                                        }
                                        else
                                        if (double.TryParse(valstring, out dblval))
                                        {
                                            f.fieldtype = "System.Double";
                                            f.Checked = true;
                                        }
                                        else
                                        if (long.TryParse(valstring, out longval))
                                        {
                                            f.fieldtype = "System.Long";
                                            f.Checked = true;
                                        }
                                        else
                                        if (float.TryParse(valstring, out floatval))
                                        {
                                            f.fieldtype = "System.Float";
                                            f.Checked = true;
                                        }
                                        else
                                        if (int.TryParse(valstring, out intval))
                                        {
                                            f.fieldtype = "System.Int32";
                                            f.Checked = true;
                                        }
                                        else
                                        if (DateTime.TryParse(valstring, out dateval))
                                        {
                                            f.fieldtype = "System.DateTime";
                                            f.Checked = true;
                                        }
                                        else
                                        if (bool.TryParse(valstring, out boolval))
                                        {
                                            f.fieldtype = "System.Bool";
                                            f.Checked = true;
                                        }
                                        else
                                        if (short.TryParse(valstring, out shortval))
                                        {
                                            f.fieldtype = "System.Short";
                                            f.Checked = true;
                                        }
                                        else
                                            f.fieldtype = "System.String";
                                    }
                                    else
                                        f.fieldtype = "System.String";

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
        private DataTable GetDataTable(string entityname)
        {
            DataTable dataTable = null;
            if (Entities.Count > 0)
            {
                if (ds.Tables.Count > 0)
                {
                    dataTable = ds.Tables[entityname];
                }
            }
            return dataTable;
        }
        private List<EntityField> GetSheetColumns(string psheetname)
        {
            DataTable tb = null;
            tb=GetDataTable(psheetname);
            if (tb != null)
            {
                Getfields();
            }
            return GetEntityDataType(psheetname).Fields.Where(x => x.EntityName.Equals(psheetname,StringComparison.CurrentCultureIgnoreCase)).ToList();
        }
        private EntityStructure GetEntityDataType(string psheetname)
        {

            return Entities.Where(x => x.EntityName == psheetname).FirstOrDefault();
        }
        private EntityStructure GetEntityDataType(int sheetno)
        {

            return Entities[sheetno];
        }
        private EntityStructure GetSheetEntity(string EntityName)
        {
            EntityStructure entityData = new EntityStructure();
            if (GetFileState() == ConnectionState.Open)
            {
                try
                {
                    Getfields();
                    if (Entities != null)
                    {
                        int idx = Entities.FindIndex(p => p.EntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase));
                        if (idx > -1)
                            entityData = Entities[idx];
                    }
                }
                catch (Exception ex)
                {
                    entityData = null;
                    DMEEditor.AddLogMessage("Fail", $"Error in getting Entity from File  {ex.Message}", DateTime.Now, 0, FileName, Errors.Failed);

                }
            }

            return entityData;


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
        public DataTable ReadDataTable(int sheetno = 0, bool HeaderExist = true, int fromline = 0, int toline = 100)
        {
            if (GetFileState() == ConnectionState.Open)
            {
                DataTable dataRows = new DataTable();

                dataRows = ds.Tables[sheetno];
                toline = dataRows.Rows.Count;
             //   List<EntityField> flds = GetSheetColumns(ds.Tables[sheetno].TableName);

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
