using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Editor.Defaults;

namespace TheTechIdea.Beep
{
    /// <summary>
    /// Lightweight IDMEEditor implementation that uses helper classes (DataSourceLifecycleHelper, ValidationHelper, ErrorHandlingHelper, CacheManager).
    /// This is intended as an additional implementation for experiments and refactoring; it preserves the IDMEEditor contract.
    /// </summary>
    public class DMEEditorHelpers : IDMEEditor
    {
        /// <summary>
        /// Gets or sets the list of active data sources.
        /// </summary>
        public List<IDataSource> DataSources { get; set; } = new List<IDataSource>();
        /// <summary>
        /// Gets or sets the progress handler for reporting progress.
        /// </summary>
        public IProgress<PassedArgs> progress { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the editor is running in container mode.
        /// </summary>
        public bool ContainerMode { get; set; }
        /// <summary>
        /// Gets or sets the name of the container.
        /// </summary>
        public string ContainerName { get; set; }
        /// <summary>
        /// Gets or sets the name of the current entity.
        /// </summary>
        public string EntityName { get; set; }
        /// <summary>
        /// Gets or sets the name of the current data source.
        /// </summary>
        public string DataSourceName { get; set; }
        /// <summary>
        /// Gets or sets the ETL (Extract, Transform, Load) component.
        /// </summary>
        public IETL ETL { get; set; }
        /// <summary>
        /// Gets or sets the error information object.
        /// </summary>
        public IErrorsInfo ErrorObject { get; set; } = new ErrorsInfo();
        /// <summary>
        /// Gets or sets the logger instance.
        /// </summary>
        public IDMLogger Logger { get; set; }
        /// <summary>
        /// Gets or sets the data types helper.
        /// </summary>
        public IDataTypesHelper typesHelper { get; set; }
        /// <summary>
        /// Gets or sets the utility functions provider.
        /// </summary>
        public IUtil Utilfunction { get; set; }
        /// <summary>
        /// Gets or sets the configuration editor.
        /// </summary>
        public IConfigEditor ConfigEditor { get; set; }
        /// <summary>
        /// Gets or sets the workflow editor.
        /// </summary>
        public IWorkFlowEditor WorkFlowEditor { get; set; }
        /// <summary>
        /// Gets or sets the class creator for dynamic type generation.
        /// </summary>
        public IClassCreator classCreator { get; set; }
        /// <summary>
        /// Gets or sets the assembly handler for managing loaded assemblies.
        /// </summary>
        public IAssemblyHandler assemblyHandler { get; set; }
        /// <summary>
        /// Gets or sets the collection of log and error messages.
        /// </summary>
        public BindingList<ILogAndError> Loganderrors { get; set; } = new BindingList<ILogAndError>();
        /// <summary>
        /// Gets or sets the arguments passed in events.
        /// </summary>
        public IPassedArgs Passedarguments { get; set; }

        /// <summary>
        /// Event triggered to pass arguments and notifications.
        /// </summary>
        public event EventHandler<PassedArgs> PassEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="DMEEditorHelpers"/> class.
        /// </summary>
        public DMEEditorHelpers()
        {
            // noop
        }

        /// <summary>
        /// Retrieves a data source by name. It first checks the cache, then attempts to create it if not found.
        /// </summary>
        /// <param name="pdatasourcename">The name of the data source to retrieve.</param>
        /// <returns>An IDataSource instance or null if not found.</returns>
        public IDataSource GetDataSource(string pdatasourcename)
        {
            if (string.IsNullOrEmpty(pdatasourcename)) return null;

            // Try cache first
            var cached = DataSourceLifecycleHelper.GetCachedDataSource(pdatasourcename);
            if (cached != null)
            {
                return cached;
            }

            // Find connection properties and create via helper
            try
            {
                var cp = ConfigEditor?.DataConnections?.FirstOrDefault(c => c.ConnectionName != null && c.ConnectionName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase));
                if (cp != null)
                {
                    var ds = DataSourceLifecycleHelper.CreateDataSourceAsync(cp, this).GetAwaiter().GetResult();
                    if (ds != null)
                    {
                        if (!DataSources.Contains(ds)) DataSources.Add(ds);
                        return ds;
                    }
                }

                // Fallback: try to find in local list
                var local = DataSources.FirstOrDefault(d => d.DatasourceName != null && d.DatasourceName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase));
                return local;
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, $"GetDataSource({pdatasourcename})", this);
                return null;
            }
        }

        /// <summary>
        /// Creates a new data source connection using the provided properties. Delegates creation to DataSourceLifecycleHelper.
        /// </summary>
        /// <param name="cn">The connection properties.</param>
        /// <param name="pdatasourcename">The desired name for the data source.</param>
        /// <returns>A new IDataSource instance or null on failure.</returns>
        public IDataSource CreateNewDataSourceConnection(ConnectionProperties cn, string pdatasourcename)
        {
            if (cn == null) return null;
            try
            {
                var ds = DataSourceLifecycleHelper.CreateDataSourceAsync(cn, this).GetAwaiter().GetResult();
                if (ds != null && !DataSources.Contains(ds)) DataSources.Add(ds);
                return ds;
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, $"CreateNewDataSourceConnection({pdatasourcename})", this);
                return null;
            }
        }

        /// <summary>
        /// Creates a local data source connection. This method delegates to CreateNewDataSourceConnection.
        /// </summary>
        /// <param name="dataConnection">The connection properties.</param>
        /// <param name="pdatasourcename">The name for the data source.</param>
        /// <param name="ClassDBHandlerName">The class handler name (used by helper to resolve driver).</param>
        /// <returns>A new IDataSource instance or null on failure.</returns>
        public IDataSource CreateLocalDataSourceConnection(ConnectionProperties dataConnection, string pdatasourcename, string ClassDBHandlerName)
        {
            // For local creation we reuse CreateNewDataSourceConnection (helpers will handle driver resolution)
            return CreateNewDataSourceConnection(dataConnection, pdatasourcename);
        }

        /// <summary>
        /// Removes a data source from the editor and attempts to dispose it.
        /// </summary>
        /// <param name="pdatasourcename">The name of the data source to remove.</param>
        /// <returns>True if the data source was found and removed; otherwise, false.</returns>
        public bool RemoveDataDource(string pdatasourcename)
        {
            try
            {
                var ds = DataSources.FirstOrDefault(d => d.DatasourceName != null && d.DatasourceName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase));
                if (ds == null)
                {
                    return false;
                }

                // Try dispose via helper
                DataSourceLifecycleHelper.UnregisterDataSource(ds.DatasourceName);
                try { ds.Dispose(); } catch { }
                DataSources.Remove(ds);
                return true;
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, $"RemoveDataDource({pdatasourcename})", this);
                return false;
            }
        }

        /// <summary>
        /// Checks if a data source with the specified name exists in the editor.
        /// </summary>
        /// <param name="pdatasourcename">The name of the data source to check.</param>
        /// <returns>True if the data source exists; otherwise, false.</returns>
        public bool CheckDataSourceExist(string pdatasourcename)
        {
            return DataSources.Any(d => d.DatasourceName != null && d.DatasourceName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Opens the connection for a specified data source.
        /// </summary>
        /// <param name="pdatasourcename">The name of the data source to open.</param>
        /// <returns>The state of the connection after the open attempt.</returns>
        public ConnectionState OpenDataSource(string pdatasourcename)
        {
            try
            {
                var ds = GetDataSource(pdatasourcename);
                if (ds == null) return ConnectionState.Broken;
                return ds.Openconnection();
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, $"OpenDataSource({pdatasourcename})", this);
                return ConnectionState.Broken;
            }
        }

        /// <summary>
        /// Closes the connection for a specified data source.
        /// </summary>
        /// <param name="pdatasourcename">The name of the data source to close.</param>
        /// <returns>True if the connection was successfully closed.</returns>
        public bool CloseDataSource(string pdatasourcename)
        {
            try
            {
                var ds = DataSources.FirstOrDefault(d => d.DatasourceName != null && d.DatasourceName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase));
                if (ds == null) return false;
                var st = ds.Dataconnection?.CloseConn();
                return st == ConnectionState.Closed || st == ConnectionState.Open ? true : st == ConnectionState.Closed;
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, $"CloseDataSource({pdatasourcename})", this);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the assembly and class definition for a given data source name.
        /// </summary>
        /// <param name="DatasourceName">The name of the data source.</param>
        /// <returns>The AssemblyClassDefinition or null if not found.</returns>
        public AssemblyClassDefinition GetDataSourceClass(string DatasourceName)
        {
            try
            {
                var cn = ConfigEditor?.DataConnections?.FirstOrDefault(c => c.ConnectionName != null && c.ConnectionName.Equals(DatasourceName, StringComparison.InvariantCultureIgnoreCase));
                var driversConfig = cn != null ? Utilfunction?.LinkConnection2Drivers(cn) : null;
                if (driversConfig == null) return null;
                return ConfigEditor?.DataSourcesClasses?.FirstOrDefault(x => x.className != null && x.className.Equals(driversConfig.classHandler, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, $"GetDataSourceClass({DatasourceName})", this);
                return null;
            }
        }

        /// <summary>
        /// Adds a log message to the logger and updates the error object.
        /// </summary>
        /// <param name="pLogType">The type of the log (e.g., "Error", "Info").</param>
        /// <param name="pLogMessage">The message to log.</param>
        /// <param name="pLogData">The timestamp of the log entry.</param>
        /// <param name="pRecordID">An optional record ID related to the log.</param>
        /// <param name="pMiscData">Miscellaneous data related to the log.</param>
        /// <param name="pFlag">An error flag indicating the status.</param>
        public void AddLogMessage(string pLogType, string pLogMessage, DateTime pLogData, int pRecordID, string pMiscData, Errors pFlag)
        {
            try
            {
                if (Logger != null)
                {
                    var formatted = $"{pLogType}: {pLogMessage}";
                    Logger.WriteLog(formatted);
                }

                if (ErrorObject != null)
                {
                    ErrorObject.Flag = pFlag;
                    ErrorObject.Message = pLogMessage;
                }
            }
            catch (Exception ex)
            {
                // best-effort
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Adds a simple log message.
        /// </summary>
        /// <param name="pLogMessage">The message to log.</param>
        public void AddLogMessage(string pLogMessage)
        {
            try
            {
                Logger?.WriteLog(pLogMessage);
                if (ErrorObject != null)
                {
                    ErrorObject.Flag = Errors.Ok;
                    ErrorObject.Message = pLogMessage;
                }
            }
            catch { }
        }

        /// <summary>
        /// Raises the PassEvent to notify listeners.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="args">The event arguments.</param>
        public void RaiseEvent(object sender, PassedArgs args)
        {
            PassEvent?.Invoke(sender, args);
        }

        /// <summary>
        /// Placeholder method for asking a question to the user.
        /// </summary>
        /// <param name="args">Arguments for the question.</param>
        /// <returns>An IErrorsInfo object.</returns>
        public IErrorsInfo AskQuestion(IPassedArgs args)
        {
            // Minimal pass-through: copy into ErrorObject and return
            if (ErrorObject == null) ErrorObject = new ErrorsInfo();
            return ErrorObject;
        }

        /// <summary>
        /// Retrieves data for a specific entity from a data source.
        /// </summary>
        /// <param name="ds">The data source.</param>
        /// <param name="entity">The entity structure defining what data to retrieve.</param>
        /// <returns>Data as an object (e.g., DataTable) or null on failure.</returns>
        public object GetData(IDataSource ds, EntityStructure entity)
        {
            try
            {
                if (ds == null) return null;
                if (ds.ConnectionStatus != ConnectionState.Open)
                {
                    var state = ds.Openconnection();
                    if (state != ConnectionState.Open) return null;
                }
                return ds.GetEntity(entity.EntityName, entity.Filters);
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, "GetData", this);
                return null;
            }
        }

        /// <summary>
        /// Gets default values for a data source using the DefaultsManager.
        /// </summary>
        /// <param name="DatasourceName">The name of the data source.</param>
        /// <returns>A list of default values.</returns>
        public List<DefaultValue> Getdefaults(string DatasourceName)
        {
            try
            {
                return DefaultsManager.GetDefaults(this, DatasourceName);
            }
            catch
            {
                return new List<DefaultValue>();
            }
        }

        /// <summary>
        /// Saves default values for a data source using the DefaultsManager.
        /// </summary>
        /// <param name="defaults">The list of default values to save.</param>
        /// <param name="DatasourceName">The name of the data source.</param>
        /// <returns>An IErrorsInfo object indicating the result.</returns>
        public IErrorsInfo Savedefaults(List<DefaultValue> defaults, string DatasourceName)
        {
            try
            {
                return DefaultsManager.SaveDefaults(this, defaults, DatasourceName);
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, "Savedefaults", this);
                return ErrorObject;
            }
        }

        /// <summary>
        /// Removes a data source from the editor using its unique GUID.
        /// </summary>
        /// <param name="guidID">The GUID of the data source to remove.</param>
        /// <returns>True if the data source was found and removed; otherwise, false.</returns>
        public bool RemoveDataDourceUsingGuidID(string guidID)
        {
            try
            {
                var ds = DataSources.FirstOrDefault(d => d.GuidID != null && d.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase));
                if (ds == null) return false;
                DataSourceLifecycleHelper.UnregisterDataSource(ds.DatasourceName);
                try { ds.Dispose(); } catch { }
                DataSources.Remove(ds);
                return true;
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, $"RemoveDataDourceUsingGuidID({guidID})", this);
                return false;
            }
        }

        /// <summary>
        /// Checks if a data source with the specified GUID exists.
        /// </summary>
        /// <param name="guidID">The GUID to check.</param>
        /// <returns>True if the data source exists; otherwise, false.</returns>
        public bool CheckDataSourceExistUsingGuidID(string guidID)
        {
            return DataSources.Any(d => d.GuidID != null && d.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Opens the connection for a data source identified by its GUID.
        /// </summary>
        /// <param name="guidID">The GUID of the data source to open.</param>
        /// <returns>The state of the connection after the open attempt.</returns>
        public ConnectionState OpenDataSourceUsingGuidID(string guidID)
        {
            try
            {
                var ds = DataSources.FirstOrDefault(d => d.GuidID != null && d.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase));
                if (ds == null) return ConnectionState.Broken;
                return ds.Openconnection();
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, $"OpenDataSourceUsingGuidID({guidID})", this);
                return ConnectionState.Broken;
            }
        }

        /// <summary>
        /// Closes the connection for a data source identified by its GUID.
        /// </summary>
        /// <param name="guidID">The GUID of the data source to close.</param>
        /// <returns>True if the connection was successfully closed.</returns>
        public bool CloseDataSourceUsingGuidID(string guidID)
        {
            try
            {
                var ds = DataSources.FirstOrDefault(d => d.GuidID != null && d.GuidID.Equals(guidID, StringComparison.InvariantCultureIgnoreCase));
                if (ds == null) return false;
                var st = ds.Dataconnection?.CloseConn();
                return st == ConnectionState.Closed;
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, $"CloseDataSourceUsingGuidID({guidID})", this);
                return false;
            }
        }

        /// <summary>
        /// Disposes all resources used by the editor, including all cached and local data sources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Dispose cached datasource helpers
                DataSourceLifecycleHelper.DisposeAllAsync().GetAwaiter().GetResult();
            }
            catch { }

            // Dispose local datasources
            foreach (var ds in DataSources.ToList())
            {
                try { ds.Dispose(); } catch { }
            }
            DataSources.Clear();
        }
    }
}
