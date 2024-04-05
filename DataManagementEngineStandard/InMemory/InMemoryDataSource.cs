using DataManagementModels.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.InMemory
{
    public class InMemoryDataSource : IDataSource, IInMemoryDB, IDisposable
    {

        #region "InMemoryDataSource Properties"
        public bool IsStructureCreated { get; set; } = false;
        public bool IsCreated { get; set; } = false;
        public bool IsLoaded { get; set; } = false;
        public bool IsSaved { get; set; } = false;
        public bool IsSynced { get; set; } = false;
        public ETLScriptHDR CreateScript { get; set; } = new ETLScriptHDR();
        public List<EntityStructure> InMemoryStructures { get; set; } = new List<EntityStructure>();
        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }
        public string GuidID { get; set; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.NONE;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.INMEMORY;
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public static string BeepDataPath { get; private set; }
        public static string InMemoryPath { get; private set; }
        public static string Filepath { get; private set; }
        public static string InMemoryStructuresfilepath { get; private set; }
        public static bool Isfoldercreated { get; private set; } = false;
        #endregion
        #region "InMemoryDataSource Methods"

        public virtual string GetConnectionString()
        {
            return Dataconnection.ConnectionProp.ConnectionString;
        }
        public IErrorsInfo CreateStructure(Progress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (IsStructureCreated)
                {
                    DMEEditor.ETL.Script = CreateScript;
                    DMEEditor.ETL.Script.LastRunDateTime = System.DateTime.Now;

                    Task.Run(() => { DMEEditor.ETL.RunCreateScript(progress, token, true, true); });
                    IsStructureCreated = true;

                }
            }
            catch (Exception ex)
            {
                IsStructureCreated = false;
                DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory Structure for {DatasourceName}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo LoadData(Progress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (Isfoldercreated && IsCreated)
                {
                    DMEEditor.ETL.Script = CreateScript;
                    DMEEditor.ETL.Script.LastRunDateTime = System.DateTime.Now;
                    DMEEditor.ETL.RunImportScript(DMEEditor.progress, token);
                    IsLoaded = true;
                }

            }
            catch (Exception ex)
            {
                IsLoaded = false;
                DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory data for {DatasourceName}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo LoadStructure(Progress<PassedArgs> progress, CancellationToken token, bool copydata = false)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                if (Isfoldercreated)
                {
                    ConnectionStatus = ConnectionState.Open;
                    InMemoryStructures = new List<EntityStructure>();
                    Entities = new List<EntityStructure>();
                    EntitiesNames = new List<string>();


                    if (File.Exists(Filepath))
                    {
                        CreateScript = DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<ETLScriptHDR>(Filepath);
                        if (CreateScript == null)
                        {
                            CreateScript = new ETLScriptHDR();
                        }
                        else
                        {

                            IsStructureCreated = false;
                        }
                    }
                    else
                    {

                    }

                    SaveStructure();
                    OnLoadStructure?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
                }
            }
            catch (Exception ex)
            {
                IsStructureCreated = false;
                DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory Structure for {DatasourceName}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo SaveStructure()
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                CancellationTokenSource token = new CancellationTokenSource();
                var progress = new Progress<PassedArgs>(percent => { });
                // GetEntitesList();
                InMemoryStructures = Entities;
                if (InMemoryStructures.Count > 0 && Isfoldercreated)
                {
                    if (CreateScript.ScriptDTL.Count == 0)
                    {
                        CreateScript = new ETLScriptHDR();
                        CreateScript.ScriptDTL.AddRange(DMEEditor.ETL.GetCreateEntityScript(this, Entities, progress, token.Token, true));
                        foreach (var item in CreateScript.ScriptDTL)
                        {
                            item.CopyDataScripts.AddRange(DMEEditor.ETL.GetCopyDataEntityScript(this, new List<EntityStructure>() { item.SourceEntity }, progress, token.Token));
                        }
                    }


                    DMEEditor.ConfigEditor.JsonLoader.Serialize(Filepath, CreateScript);
                    DMEEditor.ConfigEditor.JsonLoader.Serialize(InMemoryStructuresfilepath, InMemoryStructures);
                    OnSaveStructure?.Invoke(this, (PassedArgs)DMEEditor.Passedarguments);
                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not save InMemory Structure for {DatasourceName}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            return DMEEditor.ErrorObject;
        }


        public virtual IErrorsInfo SyncData(Progress<PassedArgs> progress, CancellationToken token)
        {
            return DMEEditor.ErrorObject;
        }
        #endregion
        #region "InMemoryDataSource Events"
        public event EventHandler<PassedArgs> OnLoadData;
        public event EventHandler<PassedArgs> OnLoadStructure;
        public event EventHandler<PassedArgs> OnSaveStructure;
        public event EventHandler<PassedArgs> OnSyncData;
        public event EventHandler<PassedArgs> PassEvent;
        #endregion
        #region "Dispose"
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
        // ~InMemoryDataSource()
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
        #region "IDataSource Methods"
        public virtual List<string> GetEntitesList()
        {
            return Entities is null ? null : Entities.Select(p=>p.EntityName).ToList();
        }

        public virtual object RunQuery(string qrystr)
        {
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo ExecuteSql(string sql)
        {
            return DMEEditor.ErrorObject;
        }

        public virtual bool CreateEntityAs(EntityStructure entity)
        {
            return false;
        }

        public virtual Type GetEntityType(string EntityName)
        {
            throw new NotImplementedException();
        }

        public virtual bool CheckEntityExist(string EntityName)
        {
            throw new NotImplementedException();
        }

        public virtual int GetEntityIdx(string entityName)
        {
            throw new NotImplementedException();
        }

        public virtual List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            throw new NotImplementedException();
        }

        public virtual List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public virtual EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            throw new NotImplementedException();
        }

        public virtual EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            throw new NotImplementedException();
        }

        public virtual List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public virtual object GetEntity(string EntityName, List<AppFilter> filter)
        {
            throw new NotImplementedException();
        }

        public virtual Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return Task.Run(() => GetEntity(EntityName, Filter));
        }

        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }

        public virtual double GetScalar(string query)
        {
            throw new NotImplementedException();
        }

        public virtual ConnectionState Openconnection()
        {
           OpenDatabaseInMemory(DatasourceName);
            return ConnectionState.Open;
        }

        public virtual  ConnectionState Closeconnection()
        {
            return ConnectionState.Closed;
        }
        #endregion
        #region "InMemoryDataSource Constructors"

        public InMemoryDataSource(string pDatasourceName,IDMEEditor pDMEEditor, IDMLogger plogger, DataSourceType databasetype, IErrorsInfo per )
        {
            DMEEditor = pDMEEditor;
            Logger = plogger;
            ErrorObject = per;
            DatasourceName = pDatasourceName;
            DatasourceType = databasetype;
            Createfolder(DatasourceName);

           
        }
      
        #endregion
        #region "Load Save Data Methods"
        private  void Createfolder(string datasourcename)
        {
            if (!string.IsNullOrEmpty(datasourcename))
            {
                try
                {
                    if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep")))
                    {
                        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep"));

                    }
                    BeepDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TheTechIdea", "Beep");
                    if (!Directory.Exists(Path.Combine(BeepDataPath, "InMemory")))
                    {
                        Directory.CreateDirectory(Path.Combine(BeepDataPath, "InMemory"));

                    }
                    InMemoryPath = Path.Combine(BeepDataPath, "InMemory");
                    if (!Directory.Exists(Path.Combine(InMemoryPath, datasourcename)))
                    {
                        Directory.CreateDirectory(Path.Combine(InMemoryPath, datasourcename));

                    }
                    Filepath = Path.Combine(InMemoryPath, "createscripts.json");
                    InMemoryStructuresfilepath = Path.Combine(InMemoryPath, "InMemoryStructures.json");
                    Isfoldercreated = true;
                }
                catch (Exception ex)
                {
                    Isfoldercreated = false;
                    DMEEditor.ErrorObject.Ex= ex;
                    DMEEditor.ErrorObject.Flag = Errors.Failed;

                    DMEEditor.AddLogMessage("Beep", $"Could not create InMemory Structure folders for {datasourcename}- {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
                }
               
            }
           
                
           
        }
        #endregion

    }
}
