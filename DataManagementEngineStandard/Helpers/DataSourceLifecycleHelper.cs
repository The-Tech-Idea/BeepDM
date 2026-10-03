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
        private static readonly Dictionary<string, IDataSource> _legacyDataSourceCache = new Dictionary<string, IDataSource>();
        private static readonly object _cacheLock = new object();

        /// <summary>
        /// Creates a new data source asynchronously with comprehensive error handling and validation.
        /// </summary>
        /// <param name="connection">Connection properties for the data source</param>
        /// <param name="editor">DME Editor instance for logging and configuration</param>
        /// <param name="validateConnection">Whether to validate connection before creation</param>
        /// <returns>Created and validated IDataSource instance</returns>
        public static Task<IDataSource> CreateDataSourceAsync(
            ConnectionProperties connection, IDMEEditor editor, bool validateConnection = true)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(editor);
            return EditorDataSourceRegistry.GetOrCreateAsync(editor, connection,
                () => CreateUncachedDataSourceAsync(connection, editor, validateConnection));
        }

        internal static Task<IDataSource> CreateLocalDataSourceAsync(ConnectionProperties connection, IDMEEditor editor, string handler)
        {
            ArgumentNullException.ThrowIfNull(connection);
            return EditorDataSourceRegistry.GetOrCreateAsync(editor, connection,
                () => CreateUncachedDataSourceAsync(connection, editor, false, handler));
        }

        private static async Task<IDataSource> CreateUncachedDataSourceAsync(
            ConnectionProperties connection, IDMEEditor editor, bool validateConnection, string localHandler = null)
        {
            IDataSource source = null;
            try
            {
                if (validateConnection)
                {
                    var validation = ValidationHelper.ValidateConnectionProperties(connection);
                    if (!validation.IsValid)
                    {
                        editor.AddLogMessage("Error", string.Join(", ", validation.Errors), DateTime.Now, 0, connection.ConnectionName, Errors.Failed);
                        return null;
                    }
                }
                var definition = localHandler == null ? GetDataSourceClassDefinition(connection, editor) :
                    editor.ConfigEditor.DataSourcesClasses.FirstOrDefault(candidate => string.Equals(candidate.className, localHandler, StringComparison.OrdinalIgnoreCase));
                if (definition == null) throw new InvalidOperationException("No datasource class matches the configured driver.");
                source = await CreateDataSourceInstanceAsync(connection, definition, editor).ConfigureAwait(false);
                if (source == null) return null;
                ConfigureDataSource(source, connection, editor);
                if (localHandler != null)
                {
                    if (source is not ILocalDB) throw new InvalidOperationException("The requested local datasource does not implement ILocalDB.");
                    source.Dataconnection.DataSourceDriver = editor.ConfigEditor.DataDriversClasses.FirstOrDefault(driver =>
                        string.Equals(driver.classHandler, localHandler, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("No driver matches the requested local handler.");
                    source.Dataconnection.ReplaceValueFromConnectionString();
                }
                return source;
            }
            catch (Exception ex)
            {
                EditorDataSourceRegistry.Report(editor, $"Creating datasource '{connection.ConnectionName}' failed", ex);
                if (source != null) await DisposeDataSourceAsync(source).ConfigureAwait(false);
                return null;
            }
        }

        /// <summary>Resolves a datasource only within the supplied editor's ownership.</summary>
        public static Task<IDataSource> GetOrCreateDataSourceAsync(
            string name, Func<ConnectionProperties> connectionFactory, IDMEEditor editor)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(connectionFactory);
            ArgumentNullException.ThrowIfNull(editor);
            var cached = EditorDataSourceRegistry.Find(editor, name);
            if (cached != null) return Task.FromResult(cached);
            var connection = connectionFactory();
            if (connection == null || !string.Equals(connection.ConnectionName, name, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The factory must return the requested connection identity.", nameof(connectionFactory));
            return CreateDataSourceAsync(connection, editor);
        }

        /// <summary>Gets a cached datasource from the supplied editor, never another host.</summary>
        public static IDataSource GetCachedDataSource(IDMEEditor editor, string name) =>
            EditorDataSourceRegistry.Find(editor, name);

        /// <summary>Returns a snapshot of editor-owned datasource references.</summary>
        public static List<IDataSource> GetAllCachedDataSources(IDMEEditor editor) =>
            EditorDataSourceRegistry.Snapshot(editor);

        /// <summary>Stops this editor's registry and releases its current sources.</summary>
        public static Task DisposeAllAsync(IDMEEditor editor)
        {
            EditorDataSourceRegistry.Stop(editor);
            return Task.CompletedTask;
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
        [Obsolete("Process-wide compatibility cache. Normal runtime operations use an explicit editor.")]
        public static void RegisterDataSource(IDataSource dataSource)
        {
            if (dataSource == null || string.IsNullOrEmpty(dataSource.DatasourceName))
                return;

            lock (_cacheLock)
            {
                _legacyDataSourceCache[dataSource.DatasourceName] = dataSource;
            }
        }

        /// <summary>
        /// Unregisters a data source from the cache without disposing it.
        /// </summary>
        /// <param name="name">Name of the data source to unregister</param>
        [Obsolete("Process-wide compatibility cache. Normal runtime operations use an explicit editor.")]
        public static void UnregisterDataSource(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            lock (_cacheLock)
            {
                _legacyDataSourceCache.Remove(name);
            }
        }

        /// <summary>
        /// Safely disposes a data source with proper cleanup.
        /// </summary>
        /// <param name="dataSource">Data source to dispose</param>
        public static Task DisposeDataSourceAsync(IDataSource dataSource)
        {
            if (dataSource == null) return Task.CompletedTask;
            lock (_cacheLock)
            {
                if (_legacyDataSourceCache.TryGetValue(dataSource.DatasourceName ?? "", out var cached) && ReferenceEquals(cached, dataSource))
                    _legacyDataSourceCache.Remove(dataSource.DatasourceName);
            }
            if (EditorDataSourceRegistry.ReleaseOwned(dataSource)) return Task.CompletedTask;
            try
            {
                if (dataSource.ConnectionStatus == ConnectionState.Open) dataSource.Closeconnection();
            }
            catch (Exception ex)
            {
                // This compatibility overload has no editor/logger; still release after failed close.
                System.Diagnostics.Debug.WriteLine($"Datasource close failed: {ex}");
            }
            try { dataSource.Dispose(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Datasource disposal failed: {ex}"); }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes all cached data sources.
        /// </summary>
        [Obsolete("Process-wide compatibility cache. Normal runtime operations use an explicit editor.")]
        public static async Task DisposeAllAsync()
        {
            List<IDataSource> dataSourcesToDispose;

            lock (_cacheLock)
            {
                dataSourcesToDispose = _legacyDataSourceCache.Values.ToList();
                _legacyDataSourceCache.Clear();
            }

            var disposalTasks = dataSourcesToDispose.Select(DisposeDataSourceAsync);
            await Task.WhenAll(disposalTasks);
        }

        /// <summary>
        /// Gets cached data source by name.
        /// </summary>
        /// <param name="name">Data source name</param>
        /// <returns>Cached data source or null if not found</returns>
        [Obsolete("Process-wide compatibility cache. Normal runtime operations use an explicit editor.")]
        public static IDataSource GetCachedDataSource(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            lock (_cacheLock)
            {
                _legacyDataSourceCache.TryGetValue(name, out var dataSource);
                return IsDataSourceValid(dataSource) ? dataSource : null;
            }
        }

        /// <summary>
        /// Gets all cached data sources.
        /// </summary>
        /// <returns>List of cached data sources</returns>
        [Obsolete("Process-wide compatibility cache. Normal runtime operations use an explicit editor.")]
        public static List<IDataSource> GetAllCachedDataSources()
        {
            lock (_cacheLock)
            {
                return _legacyDataSourceCache.Values.Where(IsDataSourceValid).ToList();
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
            catch (Exception ex)
            {
                EditorDataSourceRegistry.Report(editor, "Driver resolution failed", ex);
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
            dataSource.DatasourceName = connection.ConnectionName;
            if (!string.IsNullOrEmpty(connection.GuidID)) dataSource.GuidID = connection.GuidID;
            dataSource.Dataconnection ??= new TheTechIdea.Beep.Connections.DefaulDataConnection();
            dataSource.Dataconnection.ConnectionProp = connection;
            dataSource.Dataconnection.DataSourceDriver = ConnectionHelper.LinkConnection2Drivers(connection, editor.ConfigEditor);
            dataSource.Entities ??= new List<EntityStructure>();
            if (dataSource.Entities.Count == 0 && !connection.IsInMemory)
            {
                var entities = editor.ConfigEditor.LoadDataSourceEntitiesValues(connection.ConnectionName);
                if (entities?.Entities != null) dataSource.Entities = entities.Entities;
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
