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
    /// Entity Structure and Metadata partial class for WebAPIDataSource
    /// Contains IDataSource methods for entity structure and metadata operations
    /// </summary>
    public partial class WebAPIDataSource
    {
        #region IDataSource Entity Structure Methods

        /// <summary>Obtains the structure of a specified entity (overload with EntityStructure parameter)</summary>
        public virtual EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            try
            {
                this.Logger?.WriteLog($"Getting entity structure for entity: {fnd?.EntityName}");
                if (fnd == null) return null;
                
                return GetEntityStructure(fnd.EntityName, refresh);
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error getting entity structure: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return null;
            }
        }

        /// <summary>Retrieves foreign keys for a specified entity</summary>
        public virtual List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            try
            {
                this.Logger?.WriteLog($"Getting foreign keys for entity '{entityname}' - Web API typically doesn't have formal foreign keys");
                // Web APIs typically don't have formal foreign key relationships
                return new List<RelationShipKeys>();
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error getting foreign keys: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return new List<RelationShipKeys>();
            }
        }

        /// <summary>Generates scripts for creating entities</summary>
        public virtual List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            try
            {
                this.Logger?.WriteLog("GetCreateEntityScript called for Web API data source");
                
                // Web APIs don't typically support creating entities through scripts
                // Return empty list indicating no scripts are available
                return new List<ETLScriptDet>();
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error in GetCreateEntityScript: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return new List<ETLScriptDet>();
            }
        }

        #endregion
    }
}
