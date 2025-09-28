using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Helpers.RDBMSHelpers.DMLHelpers;

namespace TheTechIdea.Beep.Helpers.RDBMSHelpers.EntityHelpers
{
    /// <summary>
    /// Helper class for generating SQL operations on database entities.
    /// </summary>
    public static class DatabaseEntitySqlGenerator
    {
        /// <summary>
        /// Generates SQL to delete records from an entity with provided values.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="values">Dictionary containing values for the WHERE clause</param>
        /// <returns>A tuple containing the SQL statement, success flag, and any error message</returns>
        public static (string Sql, bool Success, string ErrorMessage) GenerateDeleteEntityWithValues(EntityStructure entity, Dictionary<string, object> values)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName) || values == null || !values.Any())
                return (null, false, "Invalid entity or values for delete");
            
            try
            {
                string sql = DatabaseDMLHelper.GenerateDeleteQuery(entity.DatabaseType, entity.EntityName, values);
                return (sql, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (null, false, ex.Message);
            }
        }

        /// <summary>
        /// Generates SQL to insert records into an entity with provided values.
        /// </summary>
        /// <param name="entity">The EntityStructure containing entity information</param>
        /// <param name="values">Dictionary containing field values to insert</param>
        /// <returns>A tuple containing the SQL statement, parameters, success flag, and any error message</returns>
        public static (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertWithValues(EntityStructure entity, Dictionary<string, object> values)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName) || values == null || !values.Any())
                return (null, null, false, "Invalid entity or values for insert");
            
            try
            {
                string sql = DatabaseDMLHelper.GenerateInsertQuery(entity.DatabaseType, entity.EntityName, values);
                return (sql, values, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (null, null, false, ex.Message);
            }
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
            if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName) || values == null || !values.Any() || conditions == null || !conditions.Any())
                return (null, false, "Invalid entity, values, or conditions for update");
            
            try
            {
                string sql = DatabaseDMLHelper.GenerateUpdateQuery(entity.DatabaseType, entity.EntityName, values, conditions);
                return (sql, true, string.Empty);
            }
            catch (Exception ex)
            {
                return (null, false, ex.Message);
            }
        }
    }
}