using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Tools.Helpers;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Partial class for testing and validation functionality
    /// </summary>
    public partial class ClassCreator
    {
        #region Testing and Validation Methods

        /// <summary>
        /// Generates a unit test class template for an entity
        /// </summary>
        /// <param name="entity">The EntityStructure to generate the test class for</param>
        /// <param name="outputPath">The output path to save the test class file</param>
        /// <returns>The path to the generated test class file</returns>
        public string GenerateUnitTestClass(EntityStructure entity, string outputPath)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                var validationErrors = ValidateEntityStructure(entity);
                if (validationErrors.Count > 0)
                {
                    var errorMessage = $"Entity validation failed: {string.Join(", ", validationErrors)}";
                    throw new ArgumentException(errorMessage, nameof(entity));
                }
                
                return _validationTestingHelper.GenerateUnitTestClass(entity, outputPath);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating unit test class for {entity?.EntityName}: {ex.Message}", 
                    Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Generates FluentValidation validators for entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <returns>The validator class code</returns>
        public string GenerateFluentValidators(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectValidators")
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                var validationErrors = ValidateEntityStructure(entity);
                if (validationErrors.Count > 0)
                {
                    var errorMessage = $"Entity validation failed: {string.Join(", ", validationErrors)}";
                    throw new ArgumentException(errorMessage, nameof(entity));
                }
                
                if (string.IsNullOrWhiteSpace(namespaceName))
                    throw new ArgumentException("Namespace name cannot be null or empty", nameof(namespaceName));

                return _validationTestingHelper.GenerateFluentValidators(entity, outputPath, namespaceName);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating FluentValidation validators for {entity?.EntityName}: {ex.Message}", 
                    Errors.Failed);
                throw;
            }
        }

        #endregion
    }
}