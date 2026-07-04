
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.Services.AppMap;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.SchemaMigration;


namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Strategy for detecting the primary key in POCO-to-Entity conversion.
    /// </summary>
    public enum KeyDetectionStrategy
    {
        /// <summary>
        /// Only uses [Key] DataAnnotation attribute. Fails if no [Key] found.
        /// </summary>
        AttributeOnly,

        /// <summary>
        /// Only uses naming conventions: "Id" or "{ClassName}Id". Fails if no match found.
        /// </summary>
        ConventionOnly,

        /// <summary>
        /// Tries [Key] attribute first, then falls back to naming conventions. Default recommended strategy.
        /// </summary>
        AttributeThenConvention
    }
    public interface IDMEEditor: IDisposable
    {

       

        List<IDataSource> DataSources { get; set; }
        IProgress<PassedArgs> progress { get; set; }
        bool ContainerMode { get; set; }
        string ContainerName { get; set; }
        string EntityName { get; set; }
        string DataSourceName { get; set; }
        IETL ETL { get; set; }
        IErrorsInfo ErrorObject { get; set; }
        IDMLogger Logger { get; set; }
        IDataTypesHelper typesHelper { get; set; }
        IUtil Utilfunction { get; set; }
        IConfigEditor ConfigEditor { get; set; }
        IWorkFlowEditor WorkFlowEditor { get; set; }
        IClassCreator classCreator { get; set; }
        IAssemblyHandler assemblyHandler { get; set; }
        BindingList<ILogAndError> Loganderrors { get; set; }
        IPassedArgs Passedarguments { get; set; }

        IDataSource GetDataSource(string pdatasourcename);
        IDataSource CreateNewDataSourceConnection(ConnectionProperties cn, string pdatasourcename);
        IDataSource CreateLocalDataSourceConnection(ConnectionProperties dataConnection, string pdatasourcename, string ClassDBHandlerName);
        bool RemoveDataDource(string pdatasourcename);
        bool CheckDataSourceExist(string pdatasourcename);
        ConnectionState OpenDataSource(string pdatasourcename);
        bool CloseDataSource(string pdatasourcename);
        AssemblyClassDefinition GetDataSourceClass(string DatasourceName);
        void AddLogMessage(string pLogType, string pLogMessage, DateTime pLogData, int pRecordID, string pMiscData, Errors pFlag);
        void AddLogMessage(string pLogMessage);
       
        void RaiseEvent(object sender,PassedArgs args);
        IErrorsInfo AskQuestion(IPassedArgs args);
        object GetData(IDataSource ds, EntityStructure entity);
        List<DefaultValue> Getdefaults(string DatasourceName);
        IErrorsInfo Savedefaults(List<DefaultValue> defaults, string DatasourceName);

        bool RemoveDataDourceUsingGuidID(string guidID);
        bool CheckDataSourceExistUsingGuidID(string guidID);
        ConnectionState OpenDataSourceUsingGuidID(string guidID);
        bool CloseDataSourceUsingGuidID(string guidID);

        #region "Universal DataSource Helpers - Phase 2"

        /// <summary>
        /// Gets the appropriate IDataSourceHelper implementation for a datasource type.
        /// Enables direct access to datasource-specific query generation and operations.
        /// </summary>
        /// <param name="datasourceType">The datasource type to get a helper for</param>
        /// <returns>IDataSourceHelper implementation for the datasource, or null if not yet implemented</returns>
        /// <remarks>
        /// This method provides access to datasource-specific query generators and utilities.
        /// 
        /// Currently Implemented Helpers (Phase 1):
        /// - MongoDB → MongoDBHelper (aggregation pipelines, BSON documents)
        /// - Redis → RedisHelper (hash operations, Lua scripts)
        /// - Cassandra → CassandraHelper (CQL generation, composite keys)
        /// - REST APIs → RestApiHelper (HTTP endpoints, query parameters, JSON bodies)
        /// 
        /// Planned Helpers (Phase 2+):
        /// - RDBMS (SQL Server, MySQL, PostgreSQL, Oracle, etc.) → RdbmsHelper variants
        /// - Elasticsearch → ElasticsearchHelper
        /// - Neo4j → Neo4jHelper
        /// - File-based → FileDataSourceHelper (CSV, JSON, XML)
        /// 
        /// Usage Pattern:
        /// <code>
        /// var helper = dmeEditor.GetDataSourceHelper(DataSourceType.MongoDB);
        /// if (helper != null)
        /// {
        ///     // Generate query
        ///     var (sql, parameters, success, error) = helper.GenerateSelectSql(
        ///         entity, 
        ///         new Dictionary{string, object}() { {"_id", "123"} }
        ///     );
        ///     
        ///     if (success)
        ///     {
        ///         // Execute the query using the datasource
        ///         var mongodb = dmeEditor.GetDataSource("MyMongoDB");
        ///         var result = await mongodb.ExecuteCommandAsync(sql, parameters);
        ///     }
        /// }
        /// </code>
        /// 
        /// Note: Returns null for unsupported or not-yet-implemented datasources.
        /// Check the capability matrix first to determine supported operations.
        /// </remarks>
        IDataSourceHelper GetDataSourceHelper(DataSourceType datasourceType);

        #endregion "Universal DataSource Helpers - Phase 2"

        #region "Schema Migration Providers - Phase 10"

        /// <summary>
        /// Resolves the <see cref="ISchemaMigrationProvider"/> that translates logical schema
        /// operations (create/alter/drop column, index, FK, ...) into the data source's NATIVE
        /// API. Resolution is 3-tier: exact <paramref name="dataSource"/>'s
        /// <see cref="DataSourceType"/> override → <see cref="DatasourceCategory"/> fallback →
        /// the null provider. Never returns null.
        /// </summary>
        /// <remarks>
        /// This is the schema-migration counterpart to <see cref="GetDataSourceHelper"/>. The
        /// returned provider is bound to the live <paramref name="dataSource"/> instance, so it
        /// always reflects the current connection.
        /// </remarks>
        ISchemaMigrationProvider GetMigrationProvider(IDataSource dataSource);

        #endregion "Schema Migration Providers - Phase 10"

        #region "AppMap & Deployment Services"

        /// <summary>Solution discovery: scan .sln, parse .csproj, build dependency graph.</summary>
        ISolutionDiscoveryService SolutionDiscovery { get; }

        /// <summary>AppMap: role detection, manual override, JSON persistence.</summary>
        IAppMapService AppMap { get; }

        /// <summary>Environment management: per-project profiles, standard tiers, promote/switch.</summary>
        IEnvironmentManagementService Environment { get; }

        /// <summary>Version management: database and application version tracking.</summary>
        IVersionManagementService Version { get; }

        /// <summary>Identity management: user/role CRUD, ASP.NET Identity detection.</summary>
        IIdentityManagementService Identity { get; }

        /// <summary>Multi-project sync: shared Data detection, auto-link, schema sync.</summary>
        IMultiProjectSyncService MultiSync { get; }

        /// <summary>App registry: manages the app→environment→datasource lifecycle.</summary>
        IAppRegistry AppRegistry { get; }

        #region Forms Engine
        /// <summary>Get all registered form names from the form registry.</summary>
        IReadOnlyList<string> GetActiveFormNames();

        /// <summary>Get a specific form's unit-of-works manager by name, or null.</summary>
        IUnitofWorksManager? GetFormManager(string formName);
        #endregion

        #endregion
    }
}