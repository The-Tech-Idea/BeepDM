using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using TheTechIdea.Beep.DataBase;
using System.Reflection;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Helper class for managing the complete lifecycle of data sources including creation, 
    /// caching, validation, and disposal with advanced retry and error handling mechanisms.
    /// </summary>
    public static class DataSourceLifecycleHelper
    {
        private static readonly Dictionary<string, IDataSource> _dataSourceCache = new Dictionary<string, IDataSource>();
        private static readonly object _cacheLock = new object();

        /// <summary>
        /// Creates a new data source asynchronously with comprehensive error handling and validation.
        /// </summary>
        /// <param name="connection">Connection properties for the data source</param>
        /// <param name="editor">DME Editor instance for logging and configuration</param>
        /// <param name="validateConnection">Whether to validate connection before creation</param>
        /// <returns>Created and validated IDataSource instance</returns>
        public static async Task<IDataSource> CreateDataSourceAsync(
            ConnectionProperties connection, 
            IDMEEditor editor,
            bool validateConnection = true)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));

            try
            {
                // Validate connection properties first
                if (validateConnection)
                {
                    var validationResult = ValidationHelper.ValidateConnectionProperties(connection);
                    if (!validationResult.IsValid)
                    {
                        var errorMessage = $"Connection validation failed: {string.Join(", ", validationResult.Errors)}";
                        editor.AddLogMessage("Error", errorMessage, DateTime.Now, 0, connection.ConnectionName, Errors.Failed);
                        return null;
                    }
                }

                // Get data source class definition
                var classDefinition = GetDataSourceClassDefinition(connection, editor);
                if (classDefinition == null)
                {
                    editor.AddLogMessage("Error", "Could not find data source class definition", DateTime.Now, 0, connection.ConnectionName, Errors.Failed);
                    return null;
                }

                // Create the data source instance
                var dataSource = await CreateDataSourceInstanceAsync(connection, classDefinition, editor);
                if (dataSource == null)
                    return null;

                // Configure and validate the data source
                ConfigureDataSource(dataSource, connection, editor);

                // Register in cache if creation successful
                RegisterDataSource(dataSource);

                editor.AddLogMessage("Success", $"Data source '{connection.ConnectionName}' created successfully", 
                    DateTime.Now, 0, connection.ConnectionName, Errors.Ok);

                return dataSource;
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, $"Creating data source '{connection?.ConnectionName}'", editor);
                return null;
            }
        }

        /// <summary>
        /// Gets an existing data source from cache or creates a new one using the provided factory.
        /// </summary>
        /// <param name="name">Data source name</param>
        /// <param name="connectionFactory">Factory function to create connection properties</param>
        /// <param name="editor">DME Editor instance</param>
        /// <returns>Cached or newly created IDataSource instance</returns>
        public static async Task<IDataSource> GetOrCreateDataSourceAsync(
            string name, 
            Func<ConnectionProperties> connectionFactory, 
            IDMEEditor editor)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (connectionFactory == null)
                throw new ArgumentNullException(nameof(connectionFactory));
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));

            lock (_cacheLock)
            {
                // Check cache first
                if (_dataSourceCache.TryGetValue(name, out var cachedDataSource) && 
                    IsDataSourceValid(cachedDataSource))
                {
                    return cachedDataSource;
                }
            }

            // Create new data source
            var connection = connectionFactory();
            return await CreateDataSourceAsync(connection, editor);
        }

        /// <summary>
        /// Validates a data source's health and connectivity.
        /// </summary>
        /// <param name="dataSource">Data source to validate</param>
        /// <returns>True if data source is valid and functional</returns>
        public static async Task<bool> ValidateDataSourceAsync(IDataSource dataSource)
        {
            if (dataSource == null)
                return false;

            try
            {
                // Basic validation
                if (string.IsNullOrEmpty(dataSource.DatasourceName))
                    return false;

                // Check connection status
                if (dataSource.ConnectionStatus == ConnectionState.Broken ||
                    dataSource.ConnectionStatus == ConnectionState.Closed)
                {
                    // Try to reconnect
                    var connectionResult = await OpenWithRetryAsync(dataSource, 2);
                    return connectionResult == ConnectionState.Open;
                }

                // If already open, verify with a simple operation
                if (dataSource.ConnectionStatus == ConnectionState.Open)
                {
                    try
                    {
                        // Try to get entities list (lightweight operation)
                        var entities = dataSource.GetEntitesList();
                        return entities != null;
                    }
                    catch
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Opens a data source connection with retry logic and exponential backoff.
        /// </summary>
        /// <param name="dataSource">Data source to open</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <returns>Final connection state</returns>
        public static async Task<ConnectionState> OpenWithRetryAsync(IDataSource dataSource, int maxRetries = 3)
        {
            if (dataSource == null)
                return ConnectionState.Broken;

            var attempt = 0;
            var delay = TimeSpan.FromMilliseconds(100);

            while (attempt < maxRetries)
            {
                try
                {
                    var state = dataSource.Openconnection();
                    if (state == ConnectionState.Open)
                        return state;

                    attempt++;
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(delay);
                        delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
                    }
                }
                catch (Exception)
                {
                    attempt++;
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(delay);
                        delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
                    }
                }
            }

            return ConnectionState.Broken;
        }

        /// <summary>
        /// Registers a data source in the internal cache.
        /// </summary>
        /// <param name="dataSource">Data source to register</param>
        public static void RegisterDataSource(IDataSource dataSource)
        {
            if (dataSource == null || string.IsNullOrEmpty(dataSource.DatasourceName))
                return;

            lock (_cacheLock)
            {
                _dataSourceCache[dataSource.DatasourceName] = dataSource;
            }
        }

        /// <summary>
        /// Unregisters a data source from the cache without disposing it.
        /// </summary>
        /// <param name="name">Name of the data source to unregister</param>
        public static void UnregisterDataSource(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            lock (_cacheLock)
            {
                _dataSourceCache.Remove(name);
            }
        }

        /// <summary>
        /// Safely disposes a data source with proper cleanup.
        /// </summary>
        /// <param name="dataSource">Data source to dispose</param>
        public static async Task DisposeDataSourceAsync(IDataSource dataSource)
        {
            if (dataSource == null)
                return;

            try
            {
                // Unregister from cache first
                UnregisterDataSource(dataSource.DatasourceName);

                // Close connection if open
                if (dataSource.ConnectionStatus == ConnectionState.Open)
                {
                    await Task.Run(() => dataSource.Closeconnection());
                }

                // Dispose the data source
                dataSource.Dispose();
            }
            catch (Exception)
            {
                // Log but don't throw during disposal
            }
        }

        /// <summary>
        /// Disposes all cached data sources.
        /// </summary>
        public static async Task DisposeAllAsync()
        {
            List<IDataSource> dataSourcesToDispose;

            lock (_cacheLock)
            {
                dataSourcesToDispose = _dataSourceCache.Values.ToList();
                _dataSourceCache.Clear();
            }

            var disposalTasks = dataSourcesToDispose.Select(DisposeDataSourceAsync);
            await Task.WhenAll(disposalTasks);
        }

        /// <summary>
        /// Gets cached data source by name.
        /// </summary>
        /// <param name="name">Data source name</param>
        /// <returns>Cached data source or null if not found</returns>
        public static IDataSource GetCachedDataSource(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            lock (_cacheLock)
            {
                _dataSourceCache.TryGetValue(name, out var dataSource);
                return IsDataSourceValid(dataSource) ? dataSource : null;
            }
        }

        /// <summary>
        /// Gets all cached data sources.
        /// </summary>
        /// <returns>List of cached data sources</returns>
        public static List<IDataSource> GetAllCachedDataSources()
        {
            lock (_cacheLock)
            {
                return _dataSourceCache.Values.Where(IsDataSourceValid).ToList();
            }
        }

        #region Private Helper Methods

    private static AssemblyClassDefinition GetDataSourceClassDefinition(ConnectionProperties connection, IDMEEditor editor)
        {
            try
            {
                var driversConfig = ConnectionHelper.LinkConnection2Drivers(connection, editor.ConfigEditor);
                if (driversConfig == null)
                    return null;

                return editor.ConfigEditor.DataSourcesClasses
                    .FirstOrDefault(x => x.className != null && 
                                   x.className.Equals(driversConfig.classHandler, StringComparison.InvariantCultureIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        // Create a fast activator delegate to invoke constructor
        private static Func<string, IDMLogger, IDMEEditor, DataSourceType, IErrorsInfo, IDataSource> GetActivator<T>(ConstructorInfo constructor)
        {
            return (name, logger, editor, dbType, err) =>
            {
                // Basic fallback activator that matches DMEEditor convention
                var parameters = constructor.GetParameters();
                var args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var pType = parameters[i].ParameterType;
                    if (pType == typeof(string)) args[i] = name;
                    else if (pType == typeof(IDMLogger)) args[i] = logger;
                    else if (pType == typeof(IDMEEditor)) args[i] = editor;
                    else if (pType == typeof(DataSourceType)) args[i] = dbType;
                    else if (pType == typeof(IErrorsInfo)) args[i] = err;
                    else args[i] = null;
                }

                var instance = constructor.Invoke(args);
                return (IDataSource)instance;
            };
        }

        private static async Task<IDataSource> CreateDataSourceInstanceAsync(
            ConnectionProperties connection,
            AssemblyClassDefinition classDefinition,
            IDMEEditor editor)
        {
            try
            {
                Type dataSourceType = editor?.assemblyHandler?.GetType(classDefinition.type.AssemblyQualifiedName);
                if (dataSourceType == null)
                {
                    throw new ArgumentException($"Could not load type {classDefinition.type.AssemblyQualifiedName}");
                }

                var allConstructors = dataSourceType.GetConstructors();

                // Priority 1: The "Original" Beep Constructor
                var originalParams = new Type[] { typeof(string), typeof(IDMLogger), typeof(IDMEEditor), typeof(DataSourceType), typeof(IErrorsInfo) };
                var originalConstructor = allConstructors.FirstOrDefault(c =>
                    c.GetParameters().Length == 5 &&
                    c.GetParameters().Select(p => p.ParameterType).SequenceEqual(originalParams)
                );

                if (originalConstructor != null)
                {
                    try
                    {
                        var args = new object[] { connection.ConnectionName, editor.Logger, editor, connection.DatabaseType, editor.ErrorObject };
                        var instance = originalConstructor.Invoke(args) as IDataSource;
                        if (instance != null) return await Task.FromResult(instance);
                    }
                    catch (Exception ex)
                    {
                        editor.AddLogMessage("Debug", $"Failed to invoke original constructor, trying others: {ex.Message}", DateTime.Now, 0, connection.ConnectionName, Errors.Failed);
                    }
                }

                // Priority 2: Constructor with just IConnectionProperties
                var connectionPropsConstructor = allConstructors.FirstOrDefault(c =>
                    c.GetParameters().Length == 1 &&
                    (c.GetParameters()[0].ParameterType == typeof(IConnectionProperties) || c.GetParameters()[0].ParameterType == typeof(ConnectionProperties))
                );

                if (connectionPropsConstructor != null)
                {
                    try
                    {
                        var args = new object[] { connection };
                        var instance = connectionPropsConstructor.Invoke(args) as IDataSource;
                        if (instance != null) return await Task.FromResult(instance);
                    }
                    catch (Exception ex)
                    {
                        editor.AddLogMessage("Debug", $"Failed to invoke IConnectionProperties constructor, trying others: {ex.Message}", DateTime.Now, 0, connection.ConnectionName, Errors.Failed);
                    }
                }

                // Priority 3: Any other available constructor (most complex first)
                var services = new Dictionary<Type, object>
                {
                    [typeof(ConnectionProperties)] = connection,
                    [typeof(IConnectionProperties)] = connection,
                    [typeof(IDMEEditor)] = editor,
                    [typeof(IDMLogger)] = editor.Logger,
                    [typeof(IErrorsInfo)] = editor.ErrorObject,
                    [typeof(IUtil)] = editor.Utilfunction,
                    [typeof(IConfigEditor)] = editor.ConfigEditor,
                    [typeof(string)] = connection.ConnectionName,
                    [typeof(DataSourceType)] = connection.DatabaseType
                };

                var remainingConstructors = allConstructors
                    .Except(new[] { originalConstructor, connectionPropsConstructor }.Where(c => c != null))
                    .OrderByDescending(c => c.GetParameters().Length);

                foreach (var constructor in remainingConstructors)
                {
                    var parameters = constructor.GetParameters();
                    var args = new object[parameters.Length];
                    bool canCreate = true;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var paramType = parameters[i].ParameterType;
                        if (services.TryGetValue(paramType, out var serviceInstance))
                        {
                            args[i] = serviceInstance;
                        }
                        else if (paramType.IsInterface && services.Any(s => paramType.IsAssignableFrom(s.Key)))
                        {
                            var assignable = services.First(s => paramType.IsAssignableFrom(s.Key));
                            args[i] = assignable.Value;
                        }
                        else
                        {
                            canCreate = false;
                            break;
                        }
                    }

                    if (canCreate)
                    {
                        var instance = constructor.Invoke(args) as IDataSource;
                        return await Task.FromResult(instance);
                    }
                }

                throw new InvalidOperationException($"No suitable constructor found for {dataSourceType.FullName} that can be satisfied with available services.");
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, $"Creating data source instance '{connection.ConnectionName}'", editor);
                return null;
            }
        }

        private static void ConfigureDataSource(IDataSource dataSource, ConnectionProperties connection, IDMEEditor editor)
        {
            try
            {
                if (dataSource != null)
                {
                    // Set GUID if not already set
                    if (string.IsNullOrEmpty(dataSource.GuidID) && !string.IsNullOrEmpty(connection.GuidID))
                    {
                        dataSource.GuidID = connection.GuidID;
                    }

                    // Load entities if not in-memory and available
                    if (dataSource.Entities.Count == 0 && 
                        dataSource.Dataconnection?.ConnectionProp?.IsInMemory == false)
                    {
                        try
                        {
                            var entitiesData = editor.ConfigEditor.LoadDataSourceEntitiesValues(dataSource.DatasourceName);
                            if (entitiesData?.Entities != null)
                            {
                                dataSource.Entities = entitiesData.Entities;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log but don't fail data source creation for this
                            editor.AddLogMessage("Warning", $"Could not load entities for {dataSource.DatasourceName}: {ex.Message}", 
                                DateTime.Now, 0, dataSource.DatasourceName, Errors.Ok);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandlingHelper.HandleException(ex, $"Configuring data source '{connection.ConnectionName}'", editor);
            }
        }

        private static bool IsDataSourceValid(IDataSource dataSource)
        {
            return dataSource != null && 
                   !string.IsNullOrEmpty(dataSource.DatasourceName) &&
                   dataSource.ConnectionStatus != ConnectionState.Broken;
        }

        #endregion
    }
}
