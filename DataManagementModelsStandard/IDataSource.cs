using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Logger;

using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep
{
    public interface IDataSource:IDisposable
    {

        /// <summary>
        /// Gets or sets the delimiter used to separate columns in queries or data representations.
        /// This property is particularly useful for formatting data for certain types of data sources
        /// or for parsing query results.
        /// </summary>
        string ColumnDelimiter { get; set; }

        /// <summary>
        /// Gets or sets the delimiter used for parameters in SQL queries.
        /// This property allows for customization of the parameter syntax in queries,
        /// which can be essential for compatibility with different types of data sources.
        /// </summary>
        string ParameterDelimiter { get; set; }
        /// <summary>
        /// Gets or sets the unique identifier for the data source.
        /// </summary>
        string GuidID { get; set; }

        /// <summary>
        /// Occurs when a specific action or event is passed.
        /// </summary>
        event EventHandler<PassedArgs> PassEvent;

        /// <summary>
        /// Gets or sets the type of the data source.
        /// </summary>
        DataSourceType DatasourceType { get; set; }

        /// <summary>
        /// Gets or sets the category of the data source.
        /// </summary>
        DatasourceCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the data connection interface.
        /// </summary>
        IDataConnection Dataconnection { get; set; }

        /// <summary>
        /// Gets or sets the name of the data source.
        /// </summary>
        string DatasourceName { get; set; }

        /// <summary>
        /// Gets or sets the error handling object.
        /// </summary>
        IErrorsInfo ErrorObject { get; set; }

        /// <summary>
        /// Gets or sets a secondary identifier for the data source.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets the logger for data management activities.
        /// </summary>
        IDMLogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the list of entity names in the data source.
        /// </summary>
        List<string> EntitiesNames { get; set; }

        /// <summary>
        /// Gets or sets the list of entity structures.
        /// </summary>
        List<EntityStructure> Entities { get; set; }

        /// <summary>
        /// Gets or sets the data manipulation and exploration editor.
        /// </summary>
        IDMEEditor DMEEditor { get; set; }

        /// <summary>
        /// Gets or sets the current connection status.
        /// </summary>
        ConnectionState ConnectionStatus { get; set; }
        // ... continuing from previous properties and methods ...

        /// <summary>
        /// Retrieves a list of entity names from the data source.
        /// </summary>
        /// <returns>List of entity names.</returns>
        List<string> GetEntitesList();

        /// <summary>
        /// Executes a query and returns the results.
        /// </summary>
        /// <param name="qrystr">The query string to be executed.</param>
        /// <returns>The result of the query execution.</returns>
        object RunQuery(string qrystr);

        /// <summary>
        /// Executes a SQL command and returns any errors encountered.
        /// </summary>
        /// <param name="sql">The SQL command to be executed.</param>
        /// <returns>Error information, if any.</returns>
        IErrorsInfo ExecuteSql(string sql);

        /// <summary>
        /// Creates a new entity based on the provided structure.
        /// </summary>
        /// <param name="entity">The structure of the entity to be created.</param>
        /// <returns>True if creation is successful, false otherwise.</returns>
        bool CreateEntityAs(EntityStructure entity);

        /// <summary>
        /// Retrieves the type of the specified entity.
        /// </summary>
        /// <param name="EntityName">The name of the entity.</param>
        /// <returns>The type of the entity.</returns>
        Type GetEntityType(string EntityName);

        /// <summary>
        /// Checks if the specified entity exists in the data source.
        /// </summary>
        /// <param name="EntityName">The name of the entity to check.</param>
        /// <returns>True if the entity exists, false otherwise.</returns>
        bool CheckEntityExist(string EntityName);

        /// <summary>
        /// Retrieves the index of a given entity.
        /// </summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <returns>The index of the entity.</returns>
        int GetEntityIdx(string entityName);

        /// <summary>
        /// Gets a list of child tables related to a specified table.
        /// </summary>
        /// <param name="tablename">The name of the table.</param>
        /// <param name="SchemaName">The name of the schema.</param>
        /// <param name="Filterparamters">Filter parameters for the child tables.</param>
        /// <returns>List of child relations.</returns>
        List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters);

        /// <summary>
        /// Retrieves foreign keys for a specified entity.
        /// </summary>
        /// <param name="entityname">The name of the entity.</param>
        /// <param name="SchemaName">The schema name.</param>
        /// <returns>List of relationship keys.</returns>
        List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName);

        /// <summary>
        /// Obtains the structure of a specified entity.
        /// </summary>
        /// <param name="EntityName">The name of the entity.</param>
        /// <param name="refresh">Whether to refresh the entity structure.</param>
        /// <returns>The structure of the entity.</returns>
        EntityStructure GetEntityStructure(string EntityName, bool refresh);

        /// <summary>
        /// Overloaded method to get the structure of an entity.
        /// </summary>
        /// <param name="fnd">The entity structure to find.</param>
        /// <param name="refresh">Whether to refresh the entity structure.</param>
        /// <returns>The structure of the entity.</returns>
        EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false);

        /// <summary>
        /// Executes a provided script and returns any errors.
        /// </summary>
        /// <param name="dDLScripts">The script to be executed.</param>
        /// <returns>Error information, if any.</returns>
        IErrorsInfo RunScript(ETLScriptDet dDLScripts);

        /// <summary>
        /// Generates scripts for creating entities.
        /// </summary>
        /// <param name="entities">List of entities to generate scripts for.</param>
        /// <returns>List of ETL script details.</returns>
        List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null);

        /// <summary>
        /// Creates multiple entities and returns any errors.
        /// </summary>
        /// <param name="entities">List of entities to be created.</param>
        /// <returns>Error information, if any.</returns>
        IErrorsInfo CreateEntities(List<EntityStructure> entities);

        // ... continuing from previous methods ...

        /// <summary>
        /// Updates specified entities with the provided data.
        /// </summary>
        /// <param name="EntityName">The name of the entity to update.</param>
        /// <param name="UploadData">The data to update the entity with.</param>
        /// <param name="progress">Progress reporting object.</param>
        /// <returns>Error information, if any.</returns>
        IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress);

        /// <summary>
        /// Updates a single entity with the provided data row.
        /// </summary>
        /// <param name="EntityName">The name of the entity to update.</param>
        /// <param name="UploadDataRow">The data row for the update.</param>
        /// <returns>Error information, if any.</returns>
        IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow);

        /// <summary>
        /// Deletes an entity based on the provided data row.
        /// </summary>
        /// <param name="EntityName">The name of the entity to delete.</param>
        /// <param name="UploadDataRow">The data row that specifies the entity to delete.</param>
        /// <returns>Error information, if any.</returns>
        IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow);

        /// <summary>
        /// Inserts a new entity with the provided data.
        /// </summary>
        /// <param name="EntityName">The name of the entity to insert.</param>
        /// <param name="InsertedData">The data for the new entity.</param>
        /// <returns>Error information, if any.</returns>
        IErrorsInfo InsertEntity(string EntityName, object InsertedData);

        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        /// <param name="args">Arguments related to the transaction.</param>
        /// <returns>Error information, if any.</returns>
        IErrorsInfo BeginTransaction(PassedArgs args);

        /// <summary>
        /// Ends a database transaction.
        /// </summary>
        /// <param name="args">Arguments related to the transaction.</param>
        /// <returns>Error information, if any.</returns>
        IErrorsInfo EndTransaction(PassedArgs args);

        /// <summary>
        /// Commits the current database transaction.
        /// </summary>
        /// <param name="args">Arguments related to the transaction.</param>
        /// <returns>Error information, if any.</returns>
        IErrorsInfo Commit(PassedArgs args);

        /// <summary>
        /// Retrieves an entity based on the provided name and filters.
        /// </summary>
        /// <param name="EntityName">The name of the entity to retrieve.</param>
        /// <param name="filter">The filters to apply on the entity retrieval.</param>
        /// <returns>The requested entity.</returns>
        object GetEntity(string EntityName, List<AppFilter> filter);
        /// <summary>
        /// Retrieves an entity based on the provided name and filters.
        /// </summary>
        /// <param name="EntityName">The name of the entity to retrieve.</param>
        /// <param name="filter">The filters to apply on the entity retrieval.</param>
        /// <param name="pageNumber">The page number to retrieve.</param>
        /// <param name="pageSize">The size of the page to retrieve.</param>
        /// <returns>The requested entity.</returns>
        object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize);

        /// <summary>
        /// Asynchronously retrieves an entity based on the provided name and filters.
        /// </summary>
        /// <param name="EntityName">The name of the entity to retrieve.</param>
        /// <param name="Filter">The filters to apply on the entity retrieval.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the requested entity.</returns>
        Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter);

        /// <summary>
        /// Asynchronously retrieves a scalar value based on the provided query.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the scalar value.</returns>
        Task<double> GetScalarAsync(string query);

        /// <summary>
        /// Retrieves a scalar value based on the provided query.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>The scalar value from the query.</returns>
        double GetScalar(string query);

        /// <summary>
        /// Opens a connection to the data source.
        /// </summary>
        /// <returns>The state of the connection after attempting to open it.</returns>
        ConnectionState Openconnection();

        /// <summary>
        /// Closes the connection to the data source.
        /// </summary>
        /// <returns>The state of the connection after attempting to close it.</returns>
        ConnectionState Closeconnection();


    }
}
