using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Tools.Helpers;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Partial class for Web API generation functionality
    /// </summary>
    public partial class ClassCreator
    {
        #region Web API Generation Methods

        /// <summary>
        /// Generates Web API controller classes for the provided entities in a specified data source
        /// </summary>
        /// <param name="dataSourceName">The name of the data source</param>
        /// <param name="entities">List of entity structures to generate controllers for</param>
        /// <param name="outputPath">The directory to save the generated controller files</param>
        /// <param name="namespaceName">The namespace for the generated controllers</param>
        /// <returns>A list of paths to the generated controller files</returns>
        public List<string> GenerateWebApiControllers(string dataSourceName, List<EntityStructure> entities, 
            string outputPath, string namespaceName = "TheTechIdea.ProjectControllers")
        {
            try
            {
                return _webApiHelper.GenerateWebApiControllers(dataSourceName, entities, outputPath, namespaceName);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating Web API controllers: {ex.Message}", Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Generates a Web API controller class for a single entity, with data source and entity name as parameters in API methods
        /// </summary>
        /// <param name="className">The name of the controller class to be generated</param>
        /// <param name="outputPath">The directory to save the generated controller file</param>
        /// <param name="namespaceName">The namespace for the generated controller</param>
        /// <returns>The path to the generated controller file</returns>
        public string GenerateWebApiControllerForEntityWithParams(string className, string outputPath,
            string namespaceName = "TheTechIdea.ProjectControllers")
        {
            try
            {
                return _webApiHelper.GenerateWebApiControllerForEntityWithParams(className, outputPath, namespaceName);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating parameterized Web API controller: {ex.Message}", Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Generates a minimal Web API for an entity using .NET 8's Minimal API approach
        /// </summary>
        /// <param name="outputPath">The directory to save the generated API file</param>
        /// <param name="namespaceName">The namespace for the generated API</param>
        /// <returns>The path to the generated API file</returns>
        public string GenerateMinimalWebApi(string outputPath, string namespaceName = "TheTechIdea.ProjectMinimalAPI")
        {
            try
            {
                return _webApiHelper.GenerateMinimalWebApi(outputPath, namespaceName);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating Minimal Web API: {ex.Message}", Errors.Failed);
                throw;
            }
        }

        #endregion
    }
}