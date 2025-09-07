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
    /// Script and Transaction Operations partial class for WebAPIDataSource
    /// Contains IDataSource methods for script execution and transaction management
    /// </summary>
    public partial class WebAPIDataSource
    {
        #region IDataSource Script and Transaction Methods

        /// <summary>Executes a provided script and returns any errors</summary>
        public virtual IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            try
            {
                this.Logger?.WriteLog("RunScript not supported for Web API data sources");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "ETL scripts not supported for Web API data sources";
                return ErrorObject;
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error in RunScript: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return ErrorObject;
            }
        }

        /// <summary>Begins a database transaction</summary>
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            try
            {
                this.Logger?.WriteLog("BeginTransaction not supported for Web API data sources");
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = "Transactions not supported for Web API data sources";
                return ErrorObject;
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error in BeginTransaction: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return ErrorObject;
            }
        }

        /// <summary>Ends a database transaction</summary>
        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            try
            {
                this.Logger?.WriteLog("EndTransaction not supported for Web API data sources");
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = "Transactions not supported for Web API data sources";
                return ErrorObject;
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error in EndTransaction: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return ErrorObject;
            }
        }

        /// <summary>Commits the current database transaction</summary>
        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            try
            {
                this.Logger?.WriteLog("Commit not supported for Web API data sources");
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = "Transactions not supported for Web API data sources";
                return ErrorObject;
            }
            catch (Exception ex)
            {
                this.Logger?.WriteLog($"Error in Commit: {ex.Message}");
                ErrorObject.Ex = ex;
                ErrorObject.Flag = Errors.Failed;
                return ErrorObject;
            }
        }

        #endregion
    }
}
