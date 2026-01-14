using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Tools.Helpers;
using TheTechIdea.Beep.ConfigUtil;
using System.Linq;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Partial class for database-related class generation functionality
    /// </summary>
    public partial class ClassCreator
    {
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
                return _databaseHelper.GenerateDataAccessLayer(entity, outputPath);
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

                return _databaseHelper.GenerateDbContext(entities, namespaceString, outputPath);
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

                return _databaseHelper.GenerateEntityConfiguration(entity, namespaceString, outputPath);
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

                return _databaseHelper.GenerateRepositoryImplementation(entity, outputPath, namespaceName, interfaceOnly);
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

                return _databaseHelper.GenerateEFCoreMigration(entity, outputPath, namespaceName);
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

        #region Batch Code Generation Methods

        /// <summary>
        /// Generates data access layer classes for multiple entities
        /// </summary>
        public List<string> GenerateDataAccessLayers(List<EntityStructure> entities, string outputPath)
        {
            return entities?.Select(entity => GenerateDataAccessLayer(entity, outputPath)).ToList() ?? new List<string>();
        }

        /// <summary>
        /// Generates Entity Framework configurations for multiple entities
        /// </summary>
        public List<string> GenerateEntityConfigurations(List<EntityStructure> entities, string namespaceString, string outputPath)
        {
            return entities?.Select(entity => GenerateEntityConfiguration(entity, namespaceString, outputPath)).ToList() ?? new List<string>();
        }

        /// <summary>
        /// Generates repository implementations for multiple entities
        /// </summary>
        public List<string> GenerateRepositoryImplementations(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectRepositories", bool interfaceOnly = false)
        {
            return entities?.Select(entity => GenerateRepositoryImplementation(entity, outputPath, namespaceName, interfaceOnly)).ToList() ?? new List<string>();
        }

        /// <summary>
        /// Generates Entity Framework Core migrations for multiple entities
        /// </summary>
        public List<string> GenerateEFCoreMigrations(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectMigrations")
        {
            return entities?.Select(entity => GenerateEFCoreMigration(entity, outputPath, namespaceName)).ToList() ?? new List<string>();
        }

        #endregion

        #endregion
    }
}
