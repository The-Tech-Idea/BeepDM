

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
using static TheTechIdea.Beep.Utilities.Util;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.ETL;


namespace TheTechIdea.Beep
{

    /// <summary>
    /// Data Management Enterprize Editor (DMEEditor)
    /// This is the Class that encapsulate all functionality of Data Management.
    /// </summary>
    public partial class DMEEditor : IDMEEditor,IDisposable
    {
        private bool disposedValue;
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
        IDataSource ds1;
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
                    formattedMessage += $" | Stack: {Environment.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None).FirstOrDefault(s => s.Contains("DMEEditor"))}";
                }

                // Use Task.Run with ConfigureAwait to prevent blocking
                Task.Run(() => Logger.WriteLog(formattedMessage)).ConfigureAwait(false);
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
            if (string.IsNullOrEmpty(identifier))
                return null;

            // Use existing cached datasource if it matches
            if (ds1 != null)
            {
                bool matches = useGuid
                    ? identifier.Equals(ds1.GuidID, StringComparison.InvariantCultureIgnoreCase)
                    : identifier.Equals(ds1.DatasourceName, StringComparison.InvariantCultureIgnoreCase);

                if (matches)
                    return ds1;
            }

            // Find in current sources
            IDataSource dataSource = null;
            try
            {
                dataSource = useGuid
                    ? DataSources.FirstOrDefault(f => f.GuidID.Equals(identifier, StringComparison.InvariantCultureIgnoreCase))
                    : DataSources.FirstOrDefault(f => f.DatasourceName.Equals(identifier, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception ex)
            {
                AddLogMessage(ex.Message, "Could not find data source", DateTime.Now, -1, identifier, Errors.Failed);
            }

            // Create if not found
            if (dataSource == null)
            {
                dataSource = useGuid
                    ? CreateNewDataSourceConnectionUsingGuidID(identifier)
                    : CreateNewDataSourceConnection(identifier);
            }

            // Load entities if needed
            if (dataSource?.Entities.Count == 0 && dataSource?.Dataconnection?.ConnectionProp?.IsInMemory == false)
            {
                var entitiesData = useGuid
                    ? ConfigEditor.LoadDataSourceEntitiesValues(dataSource.DatasourceName)
                    : ConfigEditor.LoadDataSourceEntitiesValues(dataSource.DatasourceName);

                if (entitiesData != null)
                    dataSource.Entities = entitiesData.Entities;
            }

            // Cache the result
            if (dataSource != null)
            {
                ds1 = dataSource;
                if (useGuid && !string.IsNullOrEmpty(dataSource.GuidID))
                    dataSource.GuidID = identifier;
            }

            return dataSource;
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
            return await ds.GetEntityAsync(CurrentEntity, filter);
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
        /// Open DataSource and add it list of DataSources , if the samename exist in connections list
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual ConnectionState OpenDataSource(string pdatasourcename)
        {
            try
            {
                ds1 = DataSources.Where(f => f.DatasourceName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (ds1 == null)
                {
                    GetDataSource(pdatasourcename);

                }
                if (ds1 != null)
                {
                    return ds1.Openconnection();
                }
                else
                {
                    AddLogMessage("Fail", $"Could not Open DataSource Connection ", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return ConnectionState.Broken;
                }

            }
            catch (Exception ex)
            {

                AddLogMessage("Fail", $"Could not Open DataSource Connection {ex.Message}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                return ConnectionState.Broken;
            }

        }
        /// <summary>
        /// Close DataSource
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual bool CloseDataSource(string pdatasourcename)
        {
            try
            {
                ConnectionState st = ConnectionState.Closed;
                IDataSource ds1 = GetDataSource(pdatasourcename);
                if (ds1 != null)
                {
                    st = ds1.Dataconnection.CloseConn();
                }
                else
                {
                    return false;
                }
                if (st == ConnectionState.Open)
                {
                    return true;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {

                AddLogMessage("Fail", $"Could not close DataSource Connection {ex.Message}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                return false;
            }
        }
        /// <summary>
        /// Get Existing DataSource Created and exist in List of DataSources
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual IDataSource GetDataSource(string pdatasourcename)
        {
            if (pdatasourcename == null)
            {
                return null;
            }
            else
            {
                if (ds1 != null)
                {
                    if (pdatasourcename.Equals(ds1.DatasourceName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return ds1;
                    }
                }

                try
                {
                    ds1 = DataSources.Where(f => !string.IsNullOrEmpty(f.DatasourceName) && f.DatasourceName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    AddLogMessage(ex.Message, "Could not Open Datasource ", DateTime.Now, -1, "", Errors.Failed);
                };
                if (ds1 == null) //|| ds1.ConnectionStatus==ConnectionState.Closed
                {
                    try
                    {
                        try
                        {
                            // Try to create via lifecycle helper using configured connection properties
                            var cp = ConfigEditor.DataConnections.Where(c => c.ConnectionName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                            if (cp != null)
                            {
                                ds1 = DataSourceLifecycleHelper.CreateDataSourceAsync(cp, this).GetAwaiter().GetResult();
                            }
                        }
                        catch
                        {
                            ds1 = null;
                        }

                        if (ds1 == null)
                            ds1 = CreateNewDataSourceConnection(pdatasourcename);

                        if (ds1 != null)
                        {
                            if (ds1.Entities.Count == 0 && !ds1.Dataconnection.ConnectionProp.IsInMemory)
                            {
                                if (ConfigEditor.LoadDataSourceEntitiesValues(ds1.DatasourceName) != null)
                                {
                                    ds1.Entities = ConfigEditor.LoadDataSourceEntitiesValues(ds1.DatasourceName).Entities;
                                }
                            }
                        }
                        else
                        {
                            AddLogMessage("Fail", $"Error in Opening Connection ({ErrorObject.Message})", DateTime.Now, -1, "", Errors.Failed);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLogMessage("Fail", $"Error in Opening Connection ({ex.Message})", DateTime.Now, -1, "", Errors.Failed);
                        return null;
                    }
                }
            }
            if (ds1 != null)
            {
                ds1.GuidID = ds1.Dataconnection.ConnectionProp.GuidID;
            }
            return ds1;
        }
        /// <summary>
        /// Open DataSource and add it list of DataSources , if the samename exist in connections list
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual ConnectionState OpenDataSourceUsingGuidID(string guidID)
        {
            try
            {
                ds1 = DataSources.Where(f => f.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (ds1 == null)
                {
                    GetDataSourceUsingGuidID(guidID);

                }
                if (ds1 != null)
                {
                    return ds1.Openconnection();
                }
                else
                {
                    AddLogMessage("Fail", $"Could not Open DataSource Connection ", DateTime.Now, 0, guidID, Errors.Failed);
                    return ConnectionState.Broken;
                }

            }
            catch (Exception ex)
            {

                AddLogMessage("Fail", $"Could not Open DataSource Connection {ex.Message}", DateTime.Now, 0, guidID, Errors.Failed);
                return ConnectionState.Broken;
            }

        }
        /// <summary>
        /// Close DataSource
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual bool CloseDataSourceUsingGuidID(string guidID)
        {
            try
            {
                ConnectionState st = ConnectionState.Closed;
                IDataSource ds1 = GetDataSourceUsingGuidID(guidID);
                if (ds1 != null)
                {
                    st = ds1.Dataconnection.CloseConn();
                }
                else
                {
                    return false;
                }
                if (st == ConnectionState.Open)
                {
                    return true;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {

                AddLogMessage("Fail", $"Could not close DataSource Connection {ex.Message}", DateTime.Now, 0, guidID, Errors.Failed);
                return false;
            }
        }
        /// <summary>
        /// Get Existing DataSource Created and exist in List of DataSources
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual IDataSource GetDataSourceUsingGuidID(string guidID)
        {
            if (guidID == null)
            {
                return null;
            }
            else
            {
                if (ds1 != null)
                {
                    if (guidID.Equals(ds1.GuidID, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return ds1;
                    }
                }

                try
                {
                    ds1 = DataSources.Where(f => f.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    AddLogMessage(ex.Message, "Could not Open Datasource ", DateTime.Now, -1, "", Errors.Failed);
                };
                if (ds1 == null) //|| ds1.ConnectionStatus==ConnectionState.Closed
                {
                    try
                    {
                        ds1 = CreateNewDataSourceConnectionUsingGuidID(guidID);
                        if (ds1 != null)
                        {
                            if (ds1.Entities.Count == 0 && !ds1.Dataconnection.ConnectionProp.IsInMemory)
                            {

                                if (ConfigEditor.LoadDataSourceEntitiesValues(ds1.GuidID) != null)
                                {
                                    ds1.Entities = ConfigEditor.LoadDataSourceEntitiesValues(ds1.GuidID).Entities;
                                }
                            }
                        }
                        else
                        {
                            AddLogMessage("Fail", $"Error in Opening Connection ({ErrorObject.Message})", DateTime.Now, -1, "", Errors.Failed);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLogMessage("Fail", $"Error in Opening Connection ({ex.Message})", DateTime.Now, -1, "", Errors.Failed);
                        return null;
                    }
                }
            }
            if (ds1 != null)
            {
                ds1.GuidID = guidID;
            }
            return ds1;
        }
        /// <summary>
        /// Check DataSource Exist in List
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual bool CheckDataSourceExistUsingGuidID(string guidID)
        {
            try
            {
                if (DataSources.Count > 0)
                {
                    return DataSources.Any(x => x.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase));
                }
                else
                    return false;



                // AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {


                AddLogMessage("Beep", $"Could not check Datasource Exist {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            };

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
                IDataSource ds = DataSources.Where(x => x.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (ds != null)
                {
                    if (ds.Dataconnection.DataSourceDriver.CreateLocal)
                    {
                        int x = ConfigEditor.DataConnections.FindIndex(x => x.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase));
                        if (x >= 0)
                        {
                            ConfigEditor.DataConnections.Remove(ConfigEditor.DataConnections[x]);
                        }
                    }
                    DataSources.Remove(ds);
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                AddLogMessage("Beep", $"Could not remove Datasource  {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            };
        }
        /// <summary>
        /// Get DataSource Assembly and Class Handling Class
        /// </summary>
        /// <param name="DatasourceName"></param>
        /// <returns></returns>
        public virtual AssemblyClassDefinition GetDataSourceClassUsingGuidID(string guidID)
        {
            AssemblyClassDefinition retval = null;
            try
            {
                ConnectionProperties cn = ConfigEditor.DataConnections.Where(f => f.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
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
        /// Create New Datasource and add to the List
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual IDataSource CreateNewDataSourceConnectionUsingGuidID(string guidID)
        {
            ConnectionProperties cn = ConfigEditor.DataConnections.Where(f => f.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            ErrorObject.Flag = Errors.Ok;
            if (cn != null)
            {
                // Try centralized lifecycle-based creation first; if it fails, fall back to the existing path
                try
                {
                    var ds = DataSourceLifecycleHelper.CreateDataSourceAsync(cn, this).GetAwaiter().GetResult();
                    if (ds != null)
                        return ds;
                }
                catch
                {
                    // ignore and fallback
                }

                return CreateNewDataSourceConnection(cn, guidID);
            }
            else
            {
                AddLogMessage("Failure", "Error occured in  DataSource Creation " + guidID, DateTime.Now, 0, null, Errors.Ok);
                return null;
            }
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
        /// Check DataSource Exist in List
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual bool CheckDataSourceExist(string pdatasourcename)
        {
            try
            {
                if (DataSources.Count > 0)
                {
                    return DataSources.Any(x => x.DatasourceName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase));
                }
                else
                    return false;



                // AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {


                AddLogMessage("Beep", $"Could not check Datasource Exist {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            };

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
                IDataSource ds = DataSources.Where(x => x.DatasourceName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (ds != null)
                {
                    if (ds.Dataconnection.DataSourceDriver.CreateLocal)
                    {
                        ConfigEditor.RemoveDataSourceEntitiesValues(ds.DatasourceName);
                    }
                    DataSources.Remove(ds);
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                AddLogMessage("Beep", $"Could not remove Datasource  {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            };
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
            var ds = DataSources.FirstOrDefault(f => f.DatasourceName.Equals(dataSourceName, StringComparison.InvariantCultureIgnoreCase));
            if (ds == null)
            {
                ds = await CreateNewDataSourceConnectionAsync(dataSourceName);
            }

            return ds != null ?  ds.Openconnection() : ConnectionState.Broken;
        }
        /// <summary>
        /// Create New Datasource and add to the List
        /// </summary>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual IDataSource CreateNewDataSourceConnection(string pdatasourcename)
        {
            ConnectionProperties cn = ConfigEditor.DataConnections.Where(f => f.ConnectionName != null && f.ConnectionName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            ErrorObject.Flag = Errors.Ok;
            if (cn != null)
            {
                // Prefer lifecycle helper to create datasource (centralized logic). Keep fallback to existing implementation.
                try
                {
                    var ds = DataSourceLifecycleHelper.CreateDataSourceAsync(cn, this).GetAwaiter().GetResult();
                    if (ds != null)
                        return ds;
                }
                catch
                {
                    // ignore and fallback
                }

                return CreateNewDataSourceConnection(cn, pdatasourcename);
            }
            else
            {
                AddLogMessage("Failure", "Error occured in  DataSource Creation " + pdatasourcename, DateTime.Now, 0, null, Errors.Ok);
                return null;
            }
        }
        public virtual async Task<IDataSource> CreateNewDataSourceConnectionAsync(string pdatasourcename)
        {
            ConnectionProperties cn = ConfigEditor.DataConnections.Where(f => f.ConnectionName != null && f.ConnectionName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            ErrorObject.Flag = Errors.Ok;
            if (cn != null)
            {
                // Try lifecycle helper asynchronously first, fallback to synchronous implementation if needed
                try
                {
                    var ds = await DataSourceLifecycleHelper.CreateDataSourceAsync(cn, this).ConfigureAwait(false);
                    if (ds != null)
                        return ds;
                }
                catch
                {
                    // ignore and fallback
                }

                return CreateNewDataSourceConnection(cn, pdatasourcename);
            }
            else
            {
                AddLogMessage("Failure", "Error occured in  DataSource Creation " + pdatasourcename, DateTime.Now, 0, null, Errors.Ok);
                return null;
            }
        }
        /// <summary>
        /// Create New Datasource and add to the List by passing new Connection Properties 
        /// </summary>
        /// <param name="cn"></param>
        /// <param name="pdatasourcename"></param>
        /// <returns></returns>
        public virtual IDataSource CreateNewDataSourceConnection(ConnectionProperties cn, string pdatasourcename)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                // Link connection properties to driver configuration
                ConnectionDriversConfig driversConfig = Utilfunction.LinkConnection2Drivers(cn);
                if (driversConfig == null)
                {
                    AddLogMessage("Fail", $"Error: Could not find Data Source Connector/Driver for {pdatasourcename}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }

                // Find the associated class definition for the driver
                AssemblyClassDefinition ase = ConfigEditor.DataSourcesClasses
                    .FirstOrDefault(x => x.className != null &&
                                         x.className.Equals(driversConfig.classHandler, StringComparison.InvariantCultureIgnoreCase));

                if (ase == null)
                {
                    AddLogMessage("Fail", $"Error: No matching Data Source Class found for {driversConfig.classHandler}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }

                // Get the type and constructor
                Type adc = assemblyHandler.GetType(ase.type.AssemblyQualifiedName);
                if (adc == null)
                {
                    AddLogMessage("Fail", $"Error: Could not load type {ase.type.AssemblyQualifiedName} for {pdatasourcename}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }

                ConstructorInfo ctor = adc.GetConstructors()
                    .FirstOrDefault(c => c.GetParameters().Length == 5) ?? adc.GetConstructors().FirstOrDefault();

                if (ctor == null)
                {
                    AddLogMessage("Fail", $"Error: No suitable constructor found for {adc.FullName}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }

                // Create an instance of the IDataSource implementation
                ObjectActivator<IDataSource> createdActivator = GetActivator<IDataSource>(ctor);
                IDataSource ds = createdActivator(cn.ConnectionName, Logger, this, cn.DatabaseType, ErrorObject);

                if (ds == null)
                {
                    AddLogMessage("Fail", "Error: Failed to create DataSource instance", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }

                // Configure and add the DataSource
                ds.Dataconnection ??= new DefaulDataConnection();
                ds.Dataconnection.ConnectionProp = cn;
                ds.Dataconnection.DataSourceDriver = driversConfig;
                DataSources.Add(ds);

                return ds;
            }
            catch (Exception ex)
            {
                AddLogMessage("Fail", $"Error in Opening Connection: {ex.Message} (Check DLLs, connection string, or network issues)", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                return null;
            }
        }
    public virtual async Task<IDataSource> CreateNewDataSourceConnectionAsync(ConnectionProperties cn, string pdatasourcename)
    {
            // ensure method is actually asynchronous to avoid compiler warnings
            await Task.Yield();
            ErrorObject.Flag = Errors.Ok;

            try
            {
                // Link connection properties to driver configuration
                ConnectionDriversConfig driversConfig = Utilfunction.LinkConnection2Drivers(cn);
                if (driversConfig == null)
                {
                    AddLogMessage("Fail", $"Error: Could not find Data Source Connector/Driver for {pdatasourcename}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }

                // Find the associated class definition for the driver
                AssemblyClassDefinition ase = ConfigEditor.DataSourcesClasses
                    .FirstOrDefault(x => x.className != null &&
                                         x.className.Equals(driversConfig.classHandler, StringComparison.InvariantCultureIgnoreCase));

                if (ase == null)
                {
                    AddLogMessage("Fail", $"Error: No matching Data Source Class found for {driversConfig.classHandler}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }

                // Get the type and constructor
                Type adc = assemblyHandler.GetType(ase.type.AssemblyQualifiedName);
                if (adc == null)
                {
                    AddLogMessage("Fail", $"Error: Could not load type {ase.type.AssemblyQualifiedName} for {pdatasourcename}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }

                ConstructorInfo ctor = adc.GetConstructors()
                    .FirstOrDefault(c => c.GetParameters().Length == 5) ?? adc.GetConstructors().FirstOrDefault();

                if (ctor == null)
                {
                    AddLogMessage("Fail", $"Error: No suitable constructor found for {adc.FullName}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }

                // Create an instance of the IDataSource implementation
                ObjectActivator<IDataSource> createdActivator = GetActivator<IDataSource>(ctor);
                IDataSource ds = createdActivator(cn.ConnectionName, Logger, this, cn.DatabaseType, ErrorObject);

                if (ds == null)
                {
                    AddLogMessage("Fail", "Error: Failed to create DataSource instance", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }

                // Configure and add the DataSource
                ds.Dataconnection ??= new DefaulDataConnection();
                ds.Dataconnection.ConnectionProp = cn;
                ds.Dataconnection.DataSourceDriver = driversConfig;

                // Assuming the connection opening operation is asynchronous
                var connectionState = ds.Openconnection();
                if (connectionState != ConnectionState.Open)
                {
                    AddLogMessage("Fail", $"Error: Unable to open connection for {pdatasourcename}", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }

                DataSources.Add(ds);
                return ds;
            }
            catch (Exception ex)
            {
                AddLogMessage("Fail", $"Error in Opening Connection: {ex.Message} (Check DLLs, connection string, or network issues)", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        ///  Create New Datasource and add to the List by passing new Connection Properties and Datasource Class Handler
        /// </summary>
        /// <param name="dataConnection"></param>
        /// <param name="pdatasourcename"></param>
        /// <param name="ClassDBHandlerName"></param>
        /// <returns></returns>
        public virtual  IDataSource CreateLocalDataSourceConnection(ConnectionProperties dataConnection, string pdatasourcename, string ClassDBHandlerName)
        {
            ErrorObject.Flag = Errors.Ok;
            IDataSource ds = null;
            ConnectionDriversConfig package = null;
            if (ConfigEditor.DataDriversClasses.Where(x => x.classHandler != null && x.classHandler.Equals(ClassDBHandlerName, StringComparison.InvariantCultureIgnoreCase)).Any())
            {
                package = ConfigEditor.DataDriversClasses.Where(x => x.classHandler != null && x.classHandler.Equals(ClassDBHandlerName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                string packagename = ConfigEditor.DataSourcesClasses.Where(x => x.className != null && x.className.Equals(package.classHandler, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault().PackageName;
                AssemblyClassDefinition ase = ConfigEditor.DataSourcesClasses.Where(x => x.className != null && x.className.Equals(package.classHandler, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (ase != null)
                {
                    Type adc = assemblyHandler.GetType(ase.type.AssemblyQualifiedName);
                    ConstructorInfo ctor = adc.GetConstructors().Where(o => o.GetParameters().Count() == 5).FirstOrDefault();
                    if (ctor == null)
                    {
                        ctor = adc.GetConstructors().FirstOrDefault();
                    }
                    ObjectActivator<IDataSource> createdActivator = GetActivator<IDataSource>(ctor);

                    //create an instance:
                    ds = createdActivator(dataConnection.ConnectionName, Logger, this, dataConnection.DatabaseType, ErrorObject);
                }
            }
            try
            {
                if (ds != null)
                {
                    if (ds.Dataconnection == null)
                    {
                        ds.Dataconnection = new DefaulDataConnection();
                    }
                    ds.Dataconnection.ConnectionProp = dataConnection;
                    ds.Dataconnection.DataSourceDriver = package;
                    ds.Dataconnection.ReplaceValueFromConnectionString();
                    ILocalDB dB = (ILocalDB)ds;
                    DataSources.Add(ds);

                    AddLogMessage("Fail", $"Success Created Local Database  {pdatasourcename}", DateTime.Now, -1, "", Errors.Failed);
                    return ds;
                }
                else
                {
                    AddLogMessage("Fail", "Could Find DataSource Drivers", DateTime.Now, 0, pdatasourcename, Errors.Failed);
                    return null;
                }
            }
            catch (Exception ex)
            {
                AddLogMessage("Fail", $"Error in Opening Connection (Check DLL for Connection drivers,connect string, Datasource down,Firewall, .. etc)({ex.Message})", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }
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
            FileConnectionHelper.Initialize(this);

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
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Call partial method for extensions cleanup
                    OnDisposing();

                    // Use safe disposal for DataSources
                    if (DataSources != null)
                    {
                        foreach (var dataSource in DataSources)
                        {
                            try
                            {
                                if (dataSource?.ConnectionStatus == ConnectionState.Open)
                                    dataSource.Closeconnection();

                                dataSource?.Dispose();
                            }
                            catch (Exception ex)
                            {
                                Logger?.WriteLog($"Error disposing data source: {ex.Message}");
                            }
                        }
                        DataSources.Clear();
                        DataSources = null;
                    }

                    // Dispose other disposable objects safely
                    ConfigEditor?.Dispose();
                    ETL?.Dispose();
                    typesHelper?.Dispose();
                    assemblyHandler?.Dispose();
                    WorkFlowEditor = null;
                    classCreator = null;
                    Utilfunction = null;
                    Logger = null;
                    ErrorObject = null;
                    progress = null;
                    ds1 = null;
                }

                disposedValue = true;
            }
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
