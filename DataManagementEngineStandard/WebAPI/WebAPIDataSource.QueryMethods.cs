using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.ComponentModel;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep;

namespace TheTechIdea.Beep.WebAPI
{
    /// <summary>
    /// Query and Data Operations partial class for WebAPIDataSource
    /// Contains IDataSource query methods and data retrieval operations
    /// </summary>
    public partial class WebAPIDataSource
    {
        #region IDataSource Query Methods

        /// <summary>Executes a SQL command and returns any errors encountered</summary>
        public virtual IErrorsInfo ExecuteSql(string sql)
        {
            try
            {
                this.Logger?.WriteLog("ExecuteSql not supported for Web API data sources");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "SQL execution not supported for Web API data sources";
                return ErrorObject;
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error in ExecuteSql: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return ErrorObject;
            }
        }

        /// <summary>Retrieves the type of the specified entity</summary>
        public virtual Type GetEntityType(string EntityName)
        {
            try
            {
                this.Logger?.WriteLog($"Getting entity type for '{EntityName}'");
                
                // For Web API, we typically return a generic object type
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
                this.Logger?.WriteLog($"Error getting entity type for '{EntityName}': {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return typeof(object);
            }
        }

        /// <summary>Gets a list of child tables related to a specified table</summary>
        public virtual List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            try
            {
                this.Logger?.WriteLog($"Getting child tables list for '{tablename}' - Web API typically doesn't have formal relationships");
                // Web APIs typically don't have formal parent-child table relationships like databases
                return new List<ChildRelation>();
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error getting child tables list: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return new List<ChildRelation>();
            }
        }

        /// <summary>Retrieves a scalar value based on the provided query</summary>
        public virtual double GetScalar(string query)
        {
            try
            {
                this.Logger?.WriteLog($"Getting scalar value for query: {query}");
                return GetScalarAsync(query).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error getting scalar: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return 0;
            }
        }

        #endregion
    }
}
