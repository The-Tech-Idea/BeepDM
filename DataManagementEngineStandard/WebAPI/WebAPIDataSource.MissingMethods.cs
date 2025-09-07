using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Additional interface methods partial class for WebAPIDataSource
    /// Contains methods required by IDataSource interface that were missing
    /// </summary>
    public partial class WebAPIDataSource
    {
        #region Missing IDataSource Interface Methods

        /// <summary>Gets list of entities</summary>
        public virtual List<string> GetEntitesList()
        {
            try
            {
                Logger?.WriteLog("Getting entities list for Web API data source");
                return EntitiesNames ?? new List<string>();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting entities list: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return new List<string>();
            }
        }

        /// <summary>Runs query and returns result</summary>
        public virtual object RunQuery(string qrystr)
        {
            try
            {
                Logger?.WriteLog($"Running query: {qrystr}");
                // For Web API, queries are typically handled through specific endpoints
                // This is a basic implementation that could be enhanced based on API capabilities
                return GetEntity(qrystr, null);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error running query '{qrystr}': {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return null;
            }
        }

        /// <summary>Executes SQL command</summary>
        public virtual IErrorsInfo ExecuteSql(string sql)
        {
            Logger?.WriteLog("ExecuteSql not supported for Web API data sources");
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "SQL execution not supported for Web API data sources";
            return ErrorObject;
        }

        /// <summary>Gets entity type</summary>
        public virtual Type GetEntityType(string EntityName)
        {
            try
            {
                Logger?.WriteLog($"Getting entity type for '{EntityName}'");
                
                // For Web API, we typically return a generic object type
                // This could be enhanced to return specific types based on entity structure
                var entityStructure = GetEntityStructure(EntityName, false);
                if (entityStructure != null)
                {
                    // Return a generic dictionary type for API responses
                    return typeof(Dictionary<string, object>);
                }
                
                return typeof(object);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting entity type for '{EntityName}': {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return typeof(object);
            }
        }

        /// <summary>Gets child tables list</summary>
        public virtual List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            try
            {
                Logger?.WriteLog($"Getting child tables list for '{tablename}' - Web API typically doesn't have formal relationships");
                // Web APIs typically don't have formal parent-child table relationships like databases
                return new List<ChildRelation>();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting child tables list: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return new List<ChildRelation>();
            }
        }

        /// <summary>Runs ETL script</summary>
        public virtual IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            Logger?.WriteLog("RunScript not supported for Web API data sources");
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "ETL scripts not supported for Web API data sources";
            return ErrorObject;
        }

        #endregion
    }
}
