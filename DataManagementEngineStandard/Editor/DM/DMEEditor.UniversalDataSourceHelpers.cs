using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep
{
    /// <summary>
    /// DMEEditor partial class for Universal DataSource Helpers framework integration (Phase 2).
    /// Provides POCO-to-Entity conversion and datasource capability detection.
    /// </summary>
    public partial class DMEEditor : IDMEEditor
    {
        #region "Universal DataSource Helpers - Phase 2 Implementation"

        /// <summary>
        /// Initializes the Universal DataSource Helpers components.
        /// Called during DMEEditor initialization to set up datasource helpers registry.
        /// </summary>
        private void InitializeUniversalDataSourceHelpers()
        {
            try
            {
                // Factory pattern handles all helper creation
                Logger?.WriteLog("Universal DataSource Helpers initialized successfully");
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = $"Failed to initialize Universal DataSource Helpers: {ex.Message}";
                Logger?.WriteLog($"ERROR: {ErrorObject.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the appropriate IDataSourceHelper for a datasource type using the factory pattern.
        /// </summary>
        public IDataSourceHelper GetDataSourceHelper(DataSourceType datasourceType)
        {
            try
            {
                // Use factory to create helper
                var factory = new DataSourceHelperFactory(this);
                var helper = factory.CreateHelper(datasourceType);

                if (helper != null)
                {
                    Logger?.WriteLog($"Retrieved {helper.GetType().Name} for datasource {datasourceType}");
                    return helper;
                }

                // Log when helper not implemented yet
                Logger?.WriteLog($"No helper implemented yet for datasource type: {datasourceType}");
                return null;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = $"Failed to get datasource helper for {datasourceType}: {ex.Message}";
                Logger?.WriteLog($"ERROR: {ErrorObject.Message}");
                
                return null;
            }
        }

        #endregion "Universal DataSource Helpers - Phase 2 Implementation"

        #region "Helper Setup Methods"

        /// <summary>
        /// Registers a custom datasource helper using the factory pattern.
        /// </summary>
        public void RegisterDataSourceHelper(DataSourceType datasourceType, IDataSourceHelper helper)
        {
            try
            {
                if (helper == null)
                {
                    throw new ArgumentNullException(nameof(helper));
                }

                // Register with factory - create factory instance and register
                var factory = new DataSourceHelperFactory(this);
                factory.RegisterHelper(datasourceType, (dme) => helper);
                Logger?.WriteLog($"Registered custom helper for datasource {datasourceType}");
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
                ErrorObject.Message = $"Failed to register datasource helper for {datasourceType}: {ex.Message}";
                Logger?.WriteLog($"ERROR: {ErrorObject.Message}");
                throw;
            }
        }

        #endregion "Helper Setup Methods"
    }
}
