using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.EntityHelpers
{
    /// <summary>
    /// Main helper class for validating entity structures and generating operations on database entities.
    /// This class serves as a facade that delegates to specialized helper classes.
    /// </summary>
    public static partial class DatabaseEntityHelper
    {
        #region SQL Generation Methods (Delegated to DatabaseEntitySqlGenerator)

        /// <summary>
        /// Generates SQL to delete records from an entity with provided values.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="values">Dictionary containing values for the WHERE clause</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateDeleteEntityWithValues(EntityStructure entity, Dictionary<string, object> values)
        {
            return DatabaseEntitySqlGenerator.GenerateDeleteEntityWithValues(entity, values);
        }

        /// <summary>
        /// Generates SQL to insert records into an entity with provided values.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="values">Dictionary containing field values to insert</param>
        /// <returns>A tuple containing the SQL statement, parameters, success flag, and any error message</returns>
        public static (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertWithValues(EntityStructure entity, Dictionary<string, object> values)
        {
            return DatabaseEntitySqlGenerator.GenerateInsertWithValues(entity, values);
        }

        /// <summary>
        /// Generates SQL to update records in an entity with provided values and conditions.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="values">Dictionary containing field values to update</param>
        /// <param name="conditions">Dictionary containing values for the WHERE clause</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateUpdateEntityWithValues(EntityStructure entity, Dictionary<string, object> values, Dictionary<string, object> conditions)
        {
            return DatabaseEntitySqlGenerator.GenerateUpdateEntityWithValues(entity, values, conditions);
        }

        #endregion

        #region Validation Methods (Delegated to DatabaseEntityValidator)

        /// <summary>
        /// Validates an entity structure and returns errors if any.
        /// </summary>
        /// <param name="entity">The EntityStructure to validate</param>
        /// <returns>Tuple with validation result and error list</returns>
        public static (bool IsValid, List<string> ValidationErrors) ValidateEntityStructure(EntityStructure entity)
        {
            return DatabaseEntityValidator.ValidateEntityStructure(entity);
        }

        #endregion

        #region Analysis Methods (Delegated to DatabaseEntityAnalyzer)

        /// <summary>
        /// Gets entity compatibility information for different database types.
        /// </summary>
        /// <param name="entity">The entity to analyze</param>
        /// <returns>Dictionary containing compatibility information</returns>
        public static Dictionary<string, object> GetEntityCompatibilityInfo(EntityStructure entity)
        {
            return DatabaseEntityAnalyzer.GetEntityCompatibilityInfo(entity);
        }

        /// <summary>
        /// Suggests improvements for an entity structure.
        /// </summary>
        /// <param name="entity">The entity to analyze</param>
        /// <returns>List of improvement suggestions</returns>
        public static List<string> SuggestEntityImprovements(EntityStructure entity)
        {
            return DatabaseEntityAnalyzer.SuggestEntityImprovements(entity);
        }

        /// <summary>
        /// Gets statistics about the entity structure.
        /// </summary>
        /// <param name="entity">The entity to analyze</param>
        /// <returns>Dictionary containing entity statistics</returns>
        public static Dictionary<string, object> GetEntityStatistics(EntityStructure entity)
        {
            return DatabaseEntityAnalyzer.GetEntityStatistics(entity);
        }

        #endregion

        #region Field Creation Methods (Delegated to DatabaseEntityTypeHelper)

        /// <summary>
        /// Creates a basic EntityField with common defaults.
        /// </summary>
        /// <param name="FieldName">Name of the field</param>
        /// <param name="Fieldtype">Type of the field</param>
        /// <param name="allowNull">Whether the field allows null values</param>
        /// <param name="isKey">Whether the field is a primary key</param>
        /// <returns>A new EntityField with the specified properties</returns>
        public static EntityField CreateBasicField(string FieldName, string Fieldtype, bool allowNull = true, bool isKey = false)
        {
            return DatabaseEntityTypeHelper.CreateBasicField(FieldName, Fieldtype, allowNull, isKey);
        }

        #endregion
    }
}