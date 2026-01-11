using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Tools.Interfaces;
using TheTechIdea.Beep.Tools.Helpers;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Partial class for database-related class generation functionality
    /// </summary>
    public partial class ClassCreator
    {
        #region Private Fields

        private DatabaseClassGeneratorHelper _databaseHelper;

        #endregion

        #region Properties

        /// <summary>Gets the Database class generator helper (lazy-loaded)</summary>
        protected DatabaseClassGeneratorHelper DatabaseGenerator
        {
            get
            {
                if (_databaseHelper == null)
                {
                    _databaseHelper = new DatabaseClassGeneratorHelper(DMEEditor);
                }
                return _databaseHelper;
            }
        }

        #endregion

        #region Database Class Generation Methods

        /// <summary>
        /// Generates a data access layer class for an entity
        /// </summary>
        /// <param name="entity">The EntityStructure to generate the DAL class for</param>
        /// <param name="outputPath">The output path to save the class file</param>
        /// <returns>The path to the generated DAL class file</returns>
        public string GenerateDataAccessLayer(EntityStructure entity, string outputPath)
        {
            try
            {
                ValidateEntityForGeneration(entity);
                return DatabaseGenerator.GenerateDataAccessLayer(entity, outputPath);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating data access layer for {entity?.EntityName}: {ex.Message}", 
                    Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Generates an EF DbContext class for the given list of entities
        /// </summary>
        /// <param name="entities">The list of EntityStructures</param>
        /// <param name="namespaceString">The namespace for the DbContext class</param>
        /// <param name="outputPath">The output path for the generated DbContext file</param>
        /// <returns>Path to the generated DbContext file</returns>
        public string GenerateDbContext(List<EntityStructure> entities, string namespaceString, string outputPath)
        {
            try
            {
                if (entities == null || entities.Count == 0)
                    throw new ArgumentException("Entities list cannot be null or empty", nameof(entities));

                if (string.IsNullOrWhiteSpace(namespaceString))
                    throw new ArgumentException("Namespace string cannot be null or empty", nameof(namespaceString));

                // Validate all entities
                foreach (var entity in entities)
                {
                    ValidateEntityForGeneration(entity);
                }

                return DatabaseGenerator.GenerateDbContext(entities, namespaceString, outputPath);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating DbContext: {ex.Message}", Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Generates EF Core configuration classes for the given entity
        /// </summary>
        /// <param name="entity">The EntityStructure to generate configuration for</param>
        /// <param name="namespaceString">The namespace for the configuration class</param>
        /// <param name="outputPath">The output path for the generated configuration file</param>
        /// <returns>Path to the generated configuration file</returns>
        public string GenerateEntityConfiguration(EntityStructure entity, string namespaceString, string outputPath)
        {
            try
            {
                ValidateEntityForGeneration(entity);
                
                if (string.IsNullOrWhiteSpace(namespaceString))
                    throw new ArgumentException("Namespace string cannot be null or empty", nameof(namespaceString));

                return DatabaseGenerator.GenerateEntityConfiguration(entity, namespaceString, outputPath);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating entity configuration for {entity?.EntityName}: {ex.Message}", 
                    Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Generates repository pattern implementation for an entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <param name="interfaceOnly">Whether to generate interface only</param>
        /// <returns>The generated repository code path</returns>
        public string GenerateRepositoryImplementation(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectRepositories", bool interfaceOnly = false)
        {
            try
            {
                ValidateEntityForGeneration(entity);
                
                if (string.IsNullOrWhiteSpace(namespaceName))
                    throw new ArgumentException("Namespace name cannot be null or empty", nameof(namespaceName));

                return DatabaseGenerator.GenerateRepositoryImplementation(entity, outputPath, namespaceName, interfaceOnly);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating repository for {entity?.EntityName}: {ex.Message}", 
                    Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Generates Entity Framework Core migration code for entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <returns>The migration code path</returns>
        public string GenerateEFCoreMigration(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectMigrations")
        {
            try
            {
                ValidateEntityForGeneration(entity);
                
                if (string.IsNullOrWhiteSpace(namespaceName))
                    throw new ArgumentException("Namespace name cannot be null or empty", nameof(namespaceName));

                return DatabaseGenerator.GenerateEFCoreMigration(entity, outputPath, namespaceName);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating EF Core migration for {entity?.EntityName}: {ex.Message}", 
                    Errors.Failed);
                throw;
            }
        }

        #endregion

        #region Validation Helper

        /// <summary>
        /// Validates an entity structure before database class generation
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        private void ValidateEntityForGeneration(EntityStructure entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var validationErrors = ValidateEntityStructure(entity);
            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Entity validation failed: {string.Join(", ", validationErrors)}";
                throw new ArgumentException(errorMessage, nameof(entity));
            }
        }

        #endregion
    }
}
