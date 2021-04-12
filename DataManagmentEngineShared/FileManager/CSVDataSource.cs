using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.ConfigUtil;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.DataManagment_Engine.Editor;
using TheTechIdea.DataManagment_Engine.Report;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.FileManager
{
    public class CSVDataSource : IDataSource
    {
        public CSVDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType pDatasourceType, IErrorsInfo per)
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

            Dataconnection.ConnectionStatus = ConnectionStatus;
           
        }

        private CsvTextFieldParser fieldParser = null;
        public DataSourceType DatasourceType { get; set; } = DataSourceType.CSV;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.FILE;
        public IDataConnection Dataconnection { get ; set ; }
        public string DatasourceName { get ; set ; }
        public IErrorsInfo ErrorObject { get ; set ; }
        public string Id { get ; set ; }
        public IDMLogger Logger { get ; set ; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        private List<EntityStructure> valEntities = new List<EntityStructure>();
        public List<EntityStructure> Entities {
            get { return valEntities; } 
           set 
            {
                valEntities = value;
                EntitiesNames = valEntities.Select(p => p.EntityName).ToList();

            } }
        public IDMEEditor DMEEditor { get ; set ; }
        public ConnectionState ConnectionStatus { get { return Dataconnection.OpenConnection(); } set { } }

        public event EventHandler<PassedArgs> PassEvent;
        #region "Get Fields and Data"
        DataTable tb; 
        private EntityStructure Getfields()
        {
            EntityStructure entityData = new EntityStructure();
            try
            {
                string[] flds = null;
                if (fieldParser == null)
                {
                    fieldParser = new CsvTextFieldParser(Path.Combine(Dataconnection.ConnectionProp.FilePath, Dataconnection.ConnectionProp.FileName));
                    fieldParser.SetDelimiter(Dataconnection.ConnectionProp.Delimiter);

                }
                if (Entities.Count == 0)
                {
                    flds = fieldParser.ReadFields();
                    string sheetname;
                    sheetname = Dataconnection.ConnectionProp.FileName;
                    entityData.Viewtype = ViewType.File;
                    entityData.DatabaseType = DataSourceType.CSV;
                    entityData.DataSourceID = Dataconnection.ConnectionProp.FileName;
                    entityData.EntityName = sheetname;
                    List<EntityField> Fields = new List<EntityField>();
                    tb = new DataTable(DatasourceName);
                    int y = 0;
                    foreach (string fld in flds)
                    {

                        DataColumn dc = new DataColumn(fld);

                        EntityField f = new EntityField();
                        f.fieldname = fld;
                        f.fieldtype = "System.string";
                        f.ValueRetrievedFromParent = false;
                        f.EntityName = sheetname;
                        f.FieldIndex = y;
                        Fields.Add(f);
                        tb.Columns.Add(dc);
                        y += 1;


                    }


                    entityData.Fields = new List<EntityField>();
                    entityData.Fields.AddRange(Fields);
                    Entities.Add(entityData);
                    EntitiesNames.Add(sheetname);
                    DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues(new ConfigUtil.DatasourceEntities { datasourcename = DatasourceName, Entities = Entities });
                }
                else
                {
                    return Entities.FirstOrDefault();
                }


            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error : Could not Create Entity For File {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return entityData;
        }
        private DataTable GetData(int nrofrows)
        {
            
            try
            {

                if (tb.Columns.Count==0)
                {
                    Getfields();

                }
                if (tb.Columns.Count > 0)
                {   int i = 1;
                    while ((fieldParser.EndOfData == false)||(i<=nrofrows))
                    {
                        string[] f= fieldParser.ReadFields();
                        DataRow r = tb.NewRow();
                        foreach (DataColumn cl in tb.Columns)
                        {

                            r[cl.Ordinal] = f[cl.Ordinal];

                        }
                        i += 1;
                    }
                }
                return tb;

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error : Could not Get Data from File {DatasourceName}-  {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        #endregion

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

        public List<LScript> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public List<string> GetEntitesList()
        {
            Getfields();
            return EntitiesNames;
        }

        public object GetEntity(string EntityName, List<ReportFilter> filter)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (filter==null)
                {
                    return GetData(9999999);
                }else
                {
                    return GetData(Convert.ToInt32(filter[0].FilterValue));
                }

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

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
           

            return Getfields();
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            return Getfields();
        }

        public Type GetEntityType(string EntityName)
        {
            Getfields();
            DMTypeBuilder.CreateNewObject(Entities.FirstOrDefault().EntityName, Entities.FirstOrDefault().EntityName, Entities.FirstOrDefault().Fields);
            return DMTypeBuilder.myType;
        }

         public  object RunQuery( string qrystr)
        {
            throw new NotImplementedException();
        }

        public LScript RunScript(LScript dDLScripts)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData)
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
            throw new NotImplementedException();
        }
    }
}
