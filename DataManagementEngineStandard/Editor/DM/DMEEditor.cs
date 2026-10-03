

using System.Collections.Generic;
using System.Data;
using TheTechIdea.Beep.Utilities;
using System.Linq;
using System;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow;
using System.ComponentModel;

using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Report;
using System.Threading.Tasks;
using System.Collections;

using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Connections;
using TheTechIdea.Beep.Addin;
using static TheTechIdea.Beep.Utils.Util;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;



namespace TheTechIdea.Beep
{

    /// <summary>
    /// Data Management Enterprize Editor (DMEEditor)
    /// This is the Class that encapsulate all functionality of Data Management.
    /// </summary>
    public partial class DMEEditor : IDMEEditor,IDisposable
    {
        private int _disposeStarted;
        /// <summary>
        /// Container Properties to allow multi-tenant application
        /// </summary>
        /// 
        #region "Properties"
        public bool ContainerMode { get; set; } = false;
        public IProgress<PassedArgs> progress { get; set; }
        public string ContainerName { get; set; } = null;
        /// <summary>
        /// List of Datasources used in the Platform
        /// </summary>
        public List<IDataSource> DataSources { get; set; } = new List<IDataSource>();
        /// <summary>
        /// Extract Tranform and Load Class 
        /// </summary>
        public IETL ETL { get; set; }
        /// <summary>
        /// Configuration Editor class that handles all confiuration loading and saving
        /// </summary>
        public IConfigEditor ConfigEditor { get; set; }
        /// <summary>
        /// Data Type Helper handles the Type Management for and Mapping between different Sourcs
        /// </summary>
        public IDataTypesHelper typesHelper { get; set; }
        /// <summary>
        /// Utilitiy Class 
        /// </summary>
        public IUtil Utilfunction { get; set; }
        /// <summary>
        /// Assembly Class that handle loading and extracting Plaform Class (IDatasource,IAddin,...)
        /// </summary>
        public IAssemblyHandler assemblyHandler { get; set; }
        /// <summary>
        /// Error Object Handler 
        /// </summary>
        public IErrorsInfo ErrorObject { get; set; }
        /// <summary>
        /// Logging Class 
        /// </summary>
        public IDMLogger Logger { get; set; }
        /// <summary>
        /// WorkFlow Editor that handles and manage datawork flow's
        /// </summary>
        public IWorkFlowEditor WorkFlowEditor { get; set; }
        /// <summary>
        /// Class and Type Creator based of EntityStructure and Data objects
        /// </summary>
        public IClassCreator classCreator { get; set; }
        /// <summary>
        /// Shared form registry — all FormsManager instances register here.
        /// Lazy-initialized; set explicitly before first use to inject a pre-populated
        /// registry. Available at both design time (IDE) and runtime (WinForms/WPF).
        /// </summary>
        public IFormRegistry FormRegistry
        {
            get => _formRegistry ??= new TheTechIdea.Beep.Editor.Forms.Helpers.FormRegistry();
            set => _formRegistry = value;
        }
        private IFormRegistry _formRegistry;

        /// <summary>
        /// Shared inter-form message bus — all FormsManager instances built on this
        /// editor post and receive here. Lazy-initialized; set explicitly to inject
        /// your own.
        /// </summary>
        /// <remarks>
        /// This mirrors <see cref="FormRegistry"/> deliberately. FormsManager used to
        /// construct a PRIVATE FormMessageBus per instance, one line below where it
        /// picks up the shared registry, so two forms on the same editor each held
        /// their own bus: PostMessage to another form reported success and was
        /// delivered to nobody. (2026-08-03)
        /// </remarks>
        public IFormMessageBus FormMessageBus
        {
            get => _formMessageBus ??= new TheTechIdea.Beep.Editor.Forms.Helpers.FormMessageBus();
            set => _formMessageBus = value;
        }
        private IFormMessageBus _formMessageBus;

        public IReadOnlyList<string> GetActiveFormNames() => FormRegistry.GetActiveFormNames();

        public IUnitofWorksManager? GetFormManager(string formName) => FormRegistry.GetForm(formName);

        /// <summary>
        /// Catalog-aware connection repository for scope/profile-aware connection management.
        /// Lazy-initialized with ConfigEditor's path; set explicitly to inject a custom repository.
        /// </summary>
        public IConnectionCatalogRepository ConnectionCatalogRepository
        {
            get
            {
                if (_catalogRepo == null && ConfigEditor != null)
                {
                    _catalogRepo = ConfigEditor.ConnectionCatalogRepository;
                }
                return _catalogRepo;
            }
            set { _catalogRepo = value; if (ConfigEditor != null) ConfigEditor.ConnectionCatalogRepository = value; }
        }
        private IConnectionCatalogRepository _catalogRepo;
        /// <summary>
        ///  Logs and Error Messeges
        /// </summary>
        public BindingList<ILogAndError> Loganderrors { get; set; } = new BindingList<ILogAndError>();
        /// <summary>
        /// Global Passed Parameters and Arguments
        /// </summary>
        public IPassedArgs Passedarguments { get; set; }
        /// <summary>
        /// Global Event Handler to handle events  in class
        /// </summary>
        /// 
      
        public event EventHandler<PassedArgs> PassEvent;
        public string EntityName { get; set; }
        public string DataSourceName { get; set; }
        #endregion "Properties"
        #region "Log and Error Methods"
        /// <summary>
        /// Raise the Public and Global event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void RaiseEvent(object sender, PassedArgs args)
        {
            PassEvent?.Invoke(sender, args);
        }
        /// <summary>
        /// Functio to Raise Question 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public IErrorsInfo AskQuestion(IPassedArgs args)
        {
            try
            {

            }
            catch (Exception ex)
            {

                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = ex.Message;
            }
            return ErrorObject;
        }
        /// <summary>
        /// Function to Add Log Message 
        /// </summary>
        /// <param name="pLogType"></param>
        /// <param name="pLogMessage"></param>
        /// <param name="pLogData"></param>
        /// <param name="pRecordID"></param>
        /// <param name="pMiscData"></param>
        /// <param name="pFlag"></param>
        // More consistent error handling with detailed information
        public void AddLogMessage(string logType, string logMessage, DateTime logDate, int recordId, string miscData, Errors flag)
        {
            try
            {
                if (Logger == null)
                    return;

                string formattedMessage = $"{logType}: {logMessage}";

                ErrorObject.Flag = flag;
                ErrorObject.Message = formattedMessage;

                if (flag == Errors.Failed)
                {
                    // Include stack trace for errors
                    formattedMessage += $" | Context: {miscData ?? "N/A"} | ID: {recordId}";
                    formattedMessage += $" | Stack: {System.Environment.StackTrace.Split(new[] { System.Environment.NewLine }, StringSplitOptions.None).FirstOrDefault(s => s.Contains("DMEEditor"))}";
                }
                if(Logger==null) 
                    return;
                // Use Task.Run with ConfigureAwait to prevent blocking
                Task.Run(() => {
                    if (Logger == null)
                        return;
                    Logger.WriteLog(formattedMessage);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Fallback logging for logger failures
                Console.WriteLine($"Logger failed: {ex.Message} | Original message: {logMessage}");
            }
        }
        /// <summary>
        /// Function to Add Log Message 
        /// </summary>
        /// <param name="pLogMessage"></param>
        public virtual void AddLogMessage(string pLogMessage)
        {
            if (Logger != null)
            {
                //  LogAndError log = new LogAndError("Beep", pLogMessage,DateTime.Now, 0, null);
                //  Loganderrors.Add(log);
                string errmsg = "Beep" + "," + pLogMessage;
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = errmsg;
                 Logger.WriteLog(errmsg);
            }
        }
        #endregion "Log and Error Methods"
        #region "Entity Structure Methods"
        /// <summary>
        /// Get Entity Structure from DataSource
        /// </summary>
        /// <param name="entityname"></param>
        /// <param name="datasourcename"></param>
        /// <returns></returns>
        public virtual EntityStructure GetEntityStructure(string entityname, string datasourcename)
        {
            IDataSource ds = null;
            EntityStructure entity = null;
            try
            {
                ds = GetDataSource(datasourcename);
                if (ds != null)
                {
                    entity = ds.GetEntityStructure(entityname, true);
                }
                return entity;
            }
            catch (Exception ex)
            {

                return entity;
            }
        }
        #endregion "Entity Structure Methods"
        #region "Get Data Methods"
        // Eliminate duplicate code between GUID and name-based methods
        public virtual IDataSource GetDataSourceById(string identifier, bool useGuid = false)
        {
            if (string.IsNullOrEmpty(identifier)) return null;
            var existing = EditorDataSourceRegistry.Find(this, identifier, useGuid);
            if (existing != null) return existing;
            var connection = ConfigEditor?.DataConnections?.FirstOrDefault(candidate => string.Equals(
                useGuid ? candidate.GuidID : candidate.ConnectionName, identifier, StringComparison.OrdinalIgnoreCase));
            return connection == null ? null : CreateNewDataSourceConnection(connection, connection.ConnectionName);
        }

        // Then use this method in your existing methods:
        //public IDataSource GetDataSource(string dataSourceName) => GetDataSourceById(dataSourceName, false);
       // public IDataSource GetDataSourceUsingGuidID(string guidId) => GetDataSourceById(guidId, true);
        /// <summary>
        /// Run Query on an Opened DataSource 
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="CurrentEntity"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private async Task<dynamic> GetOutputAsync(IDataSource ds, string CurrentEntity, List<AppFilter> filter)
        {
            return await ds.GetEntityAsync(CurrentEntity, filter).ConfigureAwait(false);
        }
        /// <summary>
        /// Get Entity Data from an Opened DataSource
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual object GetData(IDataSource ds, EntityStructure entity)
        {
            object retval = null;
            if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
            {
                if (ds.Category == DatasourceCategory.WEBAPI)
                {
                    try
                    {
                        Task<dynamic> output = GetOutputAsync(ds, entity.EntityName, entity.Filters);
                        output.Wait();
                        dynamic t = output.Result;
                        Type tp = t.GetType();
                        if (!tp.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IList)))

                        {
                            retval = ConfigEditor.JsonLoader.JsonToDataTable(t.ToString());
                        }
                        else
                        {
                            retval = t;
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLogMessage($"{ex.Message}");
                    }
                }
                else
                {
                    try
                    {
                        retval = ds.GetEntity(entity.EntityName, entity.Filters);

                    }
                    catch (Exception ex)
                    {

                        AddLogMessage($"{ex.Message}");
                    }

                }
                if (retval != null)
                {
                    try
                    {
                        entity = Utilfunction.GetEntityStructureFromListorTable(retval);
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }
                }
                else
                {
                    retval = null;
                }

            }
            return retval;
        }
        #endregion "Get Data Methods"
        #region "Data Sources Methods"
        /// <summary>
        /// Open DataSource by name and add to list of DataSources.
        /// </summary>
        public virtual ConnectionState OpenDataSource(string pdatasourcename)
        {
            try
            {
                var ds = GetDataSource(pdatasourcename);
                if (ds != null)
                {
                    return EditorDataSourceRegistry.Open(this, ds);
                }
                AddLogMessage("Fail", $"Could not Open DataSource Connection ", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                return ConnectionState.Broken;
            }
            catch (Exception ex)
            {
                AddLogMessage("Fail", $"Could not Open DataSource Connection {ex.Message}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                return ConnectionState.Broken;
            }
        }
        /// <summary>
        /// Close DataSource by name.
        /// </summary>
        public virtual bool CloseDataSource(string pdatasourcename)
        {
            try
            {
                var ds = GetDataSource(pdatasourcename);
                if (ds != null)
                {
                    return EditorDataSourceRegistry.Close(this, ds);
                }
                return false;
            }
            catch (Exception ex)
            {
                AddLogMessage("Fail", $"Could not close DataSource Connection {ex.Message}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                return false;
            }
        }
        /// <summary>
        /// Get Existing DataSource by name. Delegates to the unified
        /// <see cref="GetDataSourceById(string, bool)"/> lookup.
        /// </summary>
        public virtual IDataSource GetDataSource(string pdatasourcename)
        {
            return GetDataSourceById(pdatasourcename, useGuid: false);
        }
        /// <summary>
        /// Open DataSource by GUID. Delegates to <see cref="OpenDataSource"/>
        /// via the unified lookup.
        /// </summary>
        public virtual ConnectionState OpenDataSourceUsingGuidID(string guidID)
        {
            var ds = GetDataSourceUsingGuidID(guidID);
            if (ds != null)
            {
                return EditorDataSourceRegistry.Open(this, ds);
            }
            AddLogMessage("Fail", $"Could not Open DataSource Connection ", DateTime.Now, 0, guidID, Errors.Failed);
            return ConnectionState.Broken;
        }
        /// <summary>
        /// Close DataSource by GUID. Delegates to the unified lookup.
        /// </summary>
        public virtual bool CloseDataSourceUsingGuidID(string guidID)
        {
            try
            {
                IDataSource ds1 = GetDataSourceUsingGuidID(guidID);
                if (ds1 != null)
                {
                    return EditorDataSourceRegistry.Close(this, ds1);
                }
                return false;
            }
            catch (Exception ex)
            {
                AddLogMessage("Fail", $"Could not close DataSource Connection {ex.Message}", DateTime.Now, 0, guidID, Errors.Failed);
                return false;
            }
        }
        /// <summary>
        /// Get Existing DataSource by GUID. Delegates to the unified
        /// <see cref="GetDataSourceById(string, bool)"/> lookup.
        /// </summary>
        public virtual IDataSource GetDataSourceUsingGuidID(string guidID)
        {
            return GetDataSourceById(guidID, useGuid: true);
        }
        /// <summary>
        /// Check DataSource Exist by GUID. Delegates to the unified lookup.
        /// </summary>
        public virtual bool CheckDataSourceExistUsingGuidID(string guidID)
        {
            try
            {
                return EditorDataSourceRegistry.Find(this, guidID, true) != null;
            }
            catch (Exception ex)
            {
                AddLogMessage("Beep", $"Could not check Datasource Exist {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }
        /// <summary>
        /// Remove DataSource from List
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual bool RemoveDataDourceUsingGuidID(string guidID)
        {
            try
            {
                return EditorDataSourceRegistry.Remove(this, guidID, true, source =>
                {
                    if (source.Dataconnection?.DataSourceDriver?.CreateLocal == true)
                        ConfigEditor?.DataConnections?.RemoveAll(connection => string.Equals(connection.GuidID, guidID, StringComparison.OrdinalIgnoreCase));
                });
            }
            catch (Exception ex)
            {
                EditorDataSourceRegistry.Report(this, "Removing datasource failed", ex);
                return false;
            }
        }
        /// <summary>
        /// Get DataSource class by GUID. Delegates to <see cref="GetDataSourceClass"/>
        /// via connection lookup.
        /// </summary>
        public virtual AssemblyClassDefinition GetDataSourceClassUsingGuidID(string guidID)
        {
            var cn = ConfigEditor.DataConnections.FirstOrDefault(f => f.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase));
            if (cn == null) return null;
            return GetDataSourceClass(cn.ConnectionName);
        }
        /// <summary>
        /// Create New Datasource by GUID. Resolves the connection name and
        /// delegates to <see cref="CreateNewDataSourceConnection(string)"/>.
        /// </summary>
        public virtual IDataSource CreateNewDataSourceConnectionUsingGuidID(string guidID)
        {
            var cn = ConfigEditor.DataConnections.FirstOrDefault(f => f.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase));
            if (cn == null)
            {
                AddLogMessage("Failure", "Error occured in  DataSource Creation " + guidID, DateTime.Now, 0, null, Errors.Ok);
                return null;
            }
            return CreateNewDataSourceConnection(cn.ConnectionName);
        }

        /// <summary>
        /// Get DataSource Assembly and Class Handling Class
        /// </summary>
        /// <param name="DatasourceName"></param>
        /// <returns></returns>
        public virtual AssemblyClassDefinition GetDataSourceClass(string DatasourceName)
        {
            AssemblyClassDefinition retval = null;
            try
            {
                ConnectionProperties cn = ConfigEditor.DataConnections.Where(f => f.ConnectionName.Equals(DatasourceName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                ConnectionDriversConfig driversConfig = Utilfunction.LinkConnection2Drivers(cn);
                if (cn == null || driversConfig == null)
                {
                    AddLogMessage("Fail", "Could not get Datasource class ", DateTime.Now, -1, "", Errors.Failed);
                }
                else
                {
                    retval = ConfigEditor.DataSourcesClasses.Where(x => x.className == driversConfig.classHandler).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                string mes = "";
                AddLogMessage(ex.Message, "Could not get Datasource class " + mes, DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
            return retval;
        }

        /// <summary>
        /// Check DataSource Exist in List by name.
        /// </summary>
        public virtual bool CheckDataSourceExist(string pdatasourcename)
        {
            try
            {
                return EditorDataSourceRegistry.Find(this, pdatasourcename) != null;
            }
            catch (Exception ex)
            {
                AddLogMessage("Beep", $"Could not check Datasource Exist {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }
        /// <summary>
        /// Remove DataSource from List
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual bool RemoveDataDource(string pdatasourcename)
        {
            try
            {
                return EditorDataSourceRegistry.Remove(this, pdatasourcename, false, source =>
                {
                    if (source.Dataconnection?.DataSourceDriver?.CreateLocal == true)
                        ConfigEditor?.RemoveDataSourceEntitiesValues(source.DatasourceName);
                });
            }
            catch (Exception ex)
            {
                EditorDataSourceRegistry.Report(this, "Removing datasource failed", ex);
                return false;
            }
        }
        // Implement a more robust factory pattern for data source creation
        public virtual IDataSource CreateDataSourceFromDefinition(ConnectionProperties connection, AssemblyClassDefinition classDefinition)
        {
            try
            {
                // Get the appropriate type
                Type dataSourceType = assemblyHandler.GetType(classDefinition.type.AssemblyQualifiedName);
                if (dataSourceType == null)
                    throw new ArgumentException($"Could not load type {classDefinition.type.AssemblyQualifiedName}");

                // Find constructor with the right parameters
                var constructors = dataSourceType.GetConstructors()
                    .OrderByDescending(c => c.GetParameters().Length)
                    .ToList();

                ConstructorInfo constructor = constructors.FirstOrDefault(c =>
                    c.GetParameters().Length == 5 &&
                    c.GetParameters()[0].ParameterType == typeof(string) &&
                    c.GetParameters()[1].ParameterType == typeof(IDMLogger) &&
                    c.GetParameters()[2].ParameterType == typeof(IDMEEditor));

                if (constructor == null)
                    constructor = constructors.FirstOrDefault();

                if (constructor == null)
                    throw new InvalidOperationException($"No suitable constructor found for {dataSourceType.FullName}");

                // Create activator and instance
                var activator = GetActivator<IDataSource>(constructor);
                return activator(connection.ConnectionName, Logger, this, connection.DatabaseType, ErrorObject);
            }
            catch (Exception ex)
            {
                AddLogMessage("Error", $"Failed to create data source: {ex.Message}", DateTime.Now, 0, connection.ConnectionName, Errors.Failed);
                return null;
            }
        }
        #endregion "Data Sources Methods"
        #region "Data Sources Open/Close"
        public virtual async Task<ConnectionState> OpenDataSourceAsync(string dataSourceName)
        {
            var source = EditorDataSourceRegistry.Find(this, dataSourceName) ??
                await CreateNewDataSourceConnectionAsync(dataSourceName).ConfigureAwait(false);
            return EditorDataSourceRegistry.Open(this, source);
        }
        /// <summary>
        /// Create New Datasource and add to the List
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual IDataSource CreateNewDataSourceConnection(string pdatasourcename)
        {
            var connection = ConfigEditor?.DataConnections?.FirstOrDefault(candidate =>
                string.Equals(candidate.ConnectionName, pdatasourcename, StringComparison.OrdinalIgnoreCase));
            return connection == null ? null : CreateNewDataSourceConnection(connection, pdatasourcename);
        }
        public virtual Task<IDataSource> CreateNewDataSourceConnectionAsync(string pdatasourcename)
        {
            var connection = ConfigEditor?.DataConnections?.FirstOrDefault(candidate =>
                string.Equals(candidate.ConnectionName, pdatasourcename, StringComparison.OrdinalIgnoreCase));
            return connection == null ? Task.FromResult<IDataSource>(null) : CreateNewDataSourceConnectionAsync(connection, pdatasourcename);
        }
        /// <summary>
        /// Create New Datasource and add to the List by passing new Connection Properties 
        /// </summary>
        /// <param name="cn"></param>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual IDataSource CreateNewDataSourceConnection(ConnectionProperties cn, string pdatasourcename)
        {
            // Keep legacy constructors off the caller's UI context without holding lifecycle locks.
            return Task.Run(() => CreateNewDataSourceConnectionAsync(cn, pdatasourcename)).GetAwaiter().GetResult();
        }
        public virtual Task<IDataSource> CreateNewDataSourceConnectionAsync(ConnectionProperties cn, string pdatasourcename)
        {
            ArgumentNullException.ThrowIfNull(cn);
            if (string.IsNullOrEmpty(cn.ConnectionName)) cn.ConnectionName = pdatasourcename;
            return DataSourceLifecycleHelper.CreateDataSourceAsync(cn, this, validateConnection: false);
        }

        /// <summary>
        ///  Create New Datasource and add to the List by passing new Connection Properties and Datasource Class Handler
        /// </summary>
        /// <param name="dataConnection"></param>
        /// <param name="pdatasourcename"></param>
        /// <param name="ClassDBHandlerName"></param>
        /// <returns></returns>
        public virtual IDataSource CreateLocalDataSourceConnection(ConnectionProperties dataConnection, string pdatasourcename, string ClassDBHandlerName)
        {
            ArgumentNullException.ThrowIfNull(dataConnection);
            if (string.IsNullOrEmpty(dataConnection.ConnectionName)) dataConnection.ConnectionName = pdatasourcename;
            return Task.Run(() => DataSourceLifecycleHelper.CreateLocalDataSourceAsync(dataConnection, this, ClassDBHandlerName)).GetAwaiter().GetResult();
        }

        #endregion "Data Sources Open/Close"
        #region "Constructor"
        // Simplified constructor using dependency injection pattern
        public DMEEditor(
            IDMLogger logger,
            IUtil utilfunctions,
            IErrorsInfo errorObject,
            IConfigEditor configEditor,
            IAssemblyHandler assemblyHandler)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Utilfunction = utilfunctions ?? throw new ArgumentNullException(nameof(utilfunctions));
            ErrorObject = errorObject ?? throw new ArgumentNullException(nameof(errorObject));
            ConfigEditor = configEditor ?? throw new ArgumentNullException(nameof(configEditor));
            this.assemblyHandler = assemblyHandler ?? throw new ArgumentNullException(nameof(assemblyHandler));

            // Initialize dependent components
            Utilfunction.DME = this;
            typesHelper = new DataTypesHelper(this);
            ETL = new ETLEditor(this);
            classCreator = new ClassCreator(this);
        

            // Initialize helpers
            // Static file helpers require explicit legacy initialization; do not
            // bind every new runtime to the first editor in the process.

            // Set up progress reporting
            progress = new Progress<PassedArgs>(ReportProgress);

            // Log initialization
            logger.WriteLog("DMEEditor initialized");
        }

        // Helper method for progress reporting
        private   void ReportProgress(PassedArgs progress)
        {
            if (string.IsNullOrEmpty(progress.Messege))
                return;

            AddLogMessage("Beep", progress.Messege, DateTime.Now, 0, null,
                progress.IsError ? Errors.Failed : Errors.Ok);
        }
        #endregion "Constructor"
        #region "Default Manager"
        public List<DefaultValue> Getdefaults(string DatasourceName)
        {
            return DefaultsManager.GetDefaults(this,DatasourceName);

        }
        public IErrorsInfo Savedefaults(List<DefaultValue> defaults, string DatasourceName)
        {
            return DefaultsManager.SaveDefaults(this,defaults, DatasourceName);
        }
        #endregion "Default Manager"
        //----------------- ------------------------------ -----

        // Improved Dispose pattern
        protected virtual void Dispose(bool disposing)
        {
            if (System.Threading.Interlocked.Exchange(ref _disposeStarted, 1) != 0) return;
            if (!disposing) return;
            EditorDataSourceRegistry.Stop(this);
            try { OnDisposing(); }
            catch (Exception ex) { EditorDataSourceRegistry.Report(this, "Editor extension cleanup failed", ex); }
            var released = new HashSet<object>(ReferenceEqualityComparer.Instance);
            void Release(IDisposable resource)
            {
                if (resource == null || !released.Add(resource)) return;
                try { resource.Dispose(); }
                catch (Exception ex) { EditorDataSourceRegistry.Report(this, "Editor component cleanup failed", ex); }
            }
            Release(ETL);
            Release(typesHelper);
            Release(assemblyHandler);
            Release(ConfigEditor);
            DataSources = null;
            ConfigEditor = null;
            ETL = null;
            typesHelper = null;
            assemblyHandler = null;
            WorkFlowEditor = null;
            classCreator = null;
            Utilfunction = null;
            progress = null;
            ErrorObject = null;
            Logger = null;
            PassEvent = null;
        }

        /// <summary>
        /// Partial method for extension cleanup - implemented in partial classes
        /// </summary>
        partial void OnDisposing();

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DMEEditor()
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
       
    }
}
