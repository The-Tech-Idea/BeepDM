using FileHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.FileManager
{
    [ClassProperties(Category = DatasourceCategory.FILE, DatasourceType = DataSourceType.CSV | DataSourceType.Xls, FileType = "csv,xls,xlsx")]
    public class CVSFileHelpDataSource : IDataSource
    {
        public CVSFileHelpDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
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
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.CSV;
            Dataconnection.ConnectionProp.Category = DatasourceCategory.FILE;

            FileName = Dataconnection.ConnectionProp.FileName;
            FilePath = Dataconnection.ConnectionProp.FilePath;
            CombineFilePath = Path.Combine(FilePath, FileName);
            





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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.CSV;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;

        #region "Other Properties"
        public bool HeaderExist { get; set; }

        // public TxtXlsCSVReader Reader { get; set; }
        FileHelperEngine engine;
        string FileName;
        string FilePath;
        string CombineFilePath;
        char Delimiter;
        DataTable table=new DataTable();
        #endregion


        public event EventHandler<PassedArgs> PassEvent;

        public bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

        public ConnectionState Closeconnection()
        {
            engine = null;
            table = null;
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionState.Closed;

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

        public List<LScript> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                ConnectionStatus = Openconnection();
              
                if (ConnectionStatus == ConnectionState.Open)
                {
                    EntitiesNames = new List<string>();
                    SmartDetect(CombineFilePath);
                 //   Entities.Clear();
                    foreach (var item in EntitiesNames)
                    {
                        Entities.Add(GetEntityStructure(item, true));
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

        public object GetEntity(string EntityName, List<ReportFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                DataTable dt = null;
                string qrystr = "";
                ConnectionStatus = Openconnection();

                if (ConnectionStatus == ConnectionState.Open)
                {
                    dt = ReadDataTable();

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
        }

        public Task<object> GetEntityAsync(string EntityName, List<ReportFilter> Filter)
        {
            throw new NotImplementedException();
        }

        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            Openconnection();
            if (ConnectionStatus == ConnectionState.Open)
            {
                    GetEntityStructures(refresh);
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new ConfigUtil.DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
               

            }
            //if (Entities.Count != EntitiesNames.Count)
            //{
            //    GetEntitesList();
            //}
            return Entities.Where(x => string.Equals(x.EntityName, EntityName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
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
                    SmartDetect(CombineFilePath);
                   // Dataconnection.ConnectionProp.Delimiter = Delimiter;
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new DatasourceEntities { datasourcename = FileName, Entities = Entities });
                    //  ConnProp.Entities = Entities;
                    DMEEditor.ConfigEditor.SaveDataconnectionsValues();

                }
                else
                {
                    ConnectionStatus = ConnectionState.Broken;
                    
                    Entities = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(FileName).Entities;
                  

                }

              
                retval = Entities;
            }
            else
                retval = Entities;

            return retval;

        }
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            ConnectionStatus = Openconnection();
            // Dataconnection.ConnectionStatus = ConnectionStatus;
            if (ConnectionStatus == ConnectionState.Open)
            {
                GetEntitesList();
                if (ConnectionStatus == ConnectionState.Open)
                {
                  
                        GetEntityStructures(refresh);
                   
                }

            }
            return Entities.Where(x => x.EntityName == fnd.EntityName).FirstOrDefault();
        }

        public Type GetEntityType(string EntityName)
        {
             Openconnection();
          
            if (ConnectionStatus == ConnectionState.Open)
            {
                GetEntitesList();
                string filenamenoext = EntityName;
                DMTypeBuilder.CreateNewObject(EntityName, EntityName, Entities.Where(x => x.EntityName == EntityName).FirstOrDefault().Fields);
                return DMTypeBuilder.myType;
            }
            return null;
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }

        public ConnectionState Openconnection()
        {
            if (File.Exists(CombineFilePath))
            {
                ConnectionStatus = ConnectionState.Open;
            }else
            {
                ConnectionStatus = ConnectionState.Closed;
            }
           
            return ConnectionStatus;
        }

        public object RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public LScript RunScript(LScript dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<int> progress)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }
        #region "FileHelper Methods"
        private void SmartDetect(string filename)
        {
            var detector = new FileHelpers.Detection.SmartFormatDetector();
            var formats = detector.DetectFileFormat(filename);

            foreach (var format in formats)
            {
                Console.WriteLine("Format Detected, confidence:" + format.Confidence + "%");
                var delimited = format.ClassBuilderAsDelimited;

                Console.WriteLine("    Delimiter:" + delimited.Delimiter);
                Console.WriteLine("    Fields:");
                EntityStructure entityData = new EntityStructure();

                string sheetname;
                sheetname = FileName;
                entityData.Viewtype = ViewType.File;
                entityData.DatabaseType = DataSourceType.Text;
                entityData.DataSourceID = FileName;
                entityData.DatasourceEntityName = FileName;
                entityData.Caption = FileName;
                entityData.EntityName = FileName;
                List<EntityField> Fields = new List<EntityField>();
                int y = 0;
                foreach (var field in delimited.Fields)
                {
                    Console.WriteLine("        " + field.FieldName + ": " + field.FieldType);


                    EntityField f = new EntityField();


                    //  f.tablename = sheetname;
                    f.fieldname = field.FieldName;
                    f.fieldtype = field.FieldType.ToString();
                    f.ValueRetrievedFromParent = false;
                    f.EntityName = sheetname;
                    f.FieldIndex = y;
                    Fields.Add(f);
                    y += 1;
                }
              
                entityData.Fields = new List<EntityField>();
                entityData.Fields.AddRange(Fields);
                Entities.Clear();
                EntitiesNames.Clear();
                EntitiesNames.Add(filename);
                Entities.Add(entityData);

            }
        }
        private DataTable ReadDataTable()
        {
            Openconnection();
            if (table.Rows.Count == 0)
            {
                SmartDetect(CombineFilePath);
                table = new DataTable();
                if (ConnectionStatus == ConnectionState.Open)
                {
                    engine = new FileHelperEngine(GetEntityType(FileName));
                    var records = engine.ReadFile(CombineFilePath);
                    PropertyDescriptorCollection props = TypeDescriptor.GetProperties(GetEntityType(FileName));
                    for (int i = 0; i < props.Count; i++)
                    {
                        PropertyDescriptor prop = props[i];
                        table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                    }
                    object[] values = new object[props.Count];
                    foreach (var record in records)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            values[i] = props[i].GetValue(record) ?? DBNull.Value;
                            table.Rows.Add(values);
                        }
                    }
                }
            }
          
            return table;
        }
        #endregion
    }
}
