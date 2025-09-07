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
    /// Core Interface Implementation partial class for WebAPIDataSource
    /// Contains the remaining core IDataSource interface methods
    /// </summary>
    public partial class WebAPIDataSource
    {
        #region Core IDataSource Interface Methods

        /// <summary>Creates entity structure</summary>
        public virtual bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                this.Logger?.WriteLog("CreateEntityAs not supported for Web API data sources");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Create Entity not supported for Web API data sources";
                return false;
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error in CreateEntityAs: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return false;
            }
        }

        /// <summary>Gets entity index</summary>
        public virtual int GetEntityIdx(string entityName)
        {
            try
            {
                if (Entities == null) return -1;
                return Entities.FindIndex(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error getting entity index for '{entityName}': {ex.Message}");
                return -1;
            }
        }

        #endregion
    }
}
