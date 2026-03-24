using System;
using System.Collections.Generic;
using System.Data;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Extensions
{
    /// <summary>
    /// Maps AppFilterQueryDefinition into IDbCommand text and parameters.
    /// </summary>
    public static class AppFilterCommandExtensions
    {
        /// <summary>
        /// Applies query text and parameters from an AppFilterQueryDefinition to a command.
        /// </summary>
        public static IDbCommand ApplyFilterQueryDefinition(
            this IDbCommand command,
            AppFilterQueryDefinition definition,
            IDataSource dataSource = null,
            bool clearParameters = true)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            command.CommandText = definition.QueryText ?? string.Empty;
            if (clearParameters)
            {
                command.Parameters.Clear();
            }

            var parameterPrefix = ResolveParameterPrefix(dataSource, definition.QueryText);
            BindParameters(command, definition.Parameters, parameterPrefix);
            return command;
        }

        /// <summary>
        /// Binds a parameter dictionary to a command using a given prefix (@, :, ?).
        /// </summary>
        public static IDbCommand BindParameters(
            this IDbCommand command,
            IReadOnlyDictionary<string, object> parameters,
            string parameterPrefix = "@")
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (parameters == null || parameters.Count == 0)
            {
                return command;
            }

            foreach (var kvp in parameters)
            {
                var p = command.CreateParameter();
                p.ParameterName = BuildParameterName(kvp.Key, parameterPrefix);
                p.Value = kvp.Value ?? DBNull.Value;
                ApplyDbTypeHint(p, kvp.Value);
                command.Parameters.Add(p);
            }

            return command;
        }

        private static string ResolveParameterPrefix(IDataSource dataSource, string queryText)
        {
            if (dataSource != null)
            {
                return dataSource.GetParameterPrefix();
            }

            if (!string.IsNullOrWhiteSpace(queryText))
            {
                if (queryText.Contains(":p_", StringComparison.Ordinal))
                {
                    return ":";
                }

                if (queryText.Contains("?p_", StringComparison.Ordinal))
                {
                    return "?";
                }
            }

            return "@";
        }

        private static string BuildParameterName(string parameterKey, string prefix)
        {
            var key = (parameterKey ?? string.Empty).Trim();
            if (key.Length == 0)
            {
                key = "p_auto";
            }

            if (key.StartsWith("@", StringComparison.Ordinal) ||
                key.StartsWith(":", StringComparison.Ordinal) ||
                key.StartsWith("?", StringComparison.Ordinal))
            {
                return key;
            }

            return $"{prefix}{key}";
        }

        private static void ApplyDbTypeHint(IDataParameter parameter, object value)
        {
            if (parameter == null || value == null)
            {
                return;
            }

            var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
            if (type == typeof(string))
            {
                parameter.DbType = DbType.String;
            }
            else if (type == typeof(int))
            {
                parameter.DbType = DbType.Int32;
            }
            else if (type == typeof(long))
            {
                parameter.DbType = DbType.Int64;
            }
            else if (type == typeof(short))
            {
                parameter.DbType = DbType.Int16;
            }
            else if (type == typeof(decimal))
            {
                parameter.DbType = DbType.Decimal;
            }
            else if (type == typeof(double))
            {
                parameter.DbType = DbType.Double;
            }
            else if (type == typeof(float))
            {
                parameter.DbType = DbType.Single;
            }
            else if (type == typeof(bool))
            {
                parameter.DbType = DbType.Boolean;
            }
            else if (type == typeof(DateTime))
            {
                parameter.DbType = DbType.DateTime;
            }
            else if (type == typeof(Guid))
            {
                parameter.DbType = DbType.Guid;
            }
            else if (type == typeof(byte[]))
            {
                parameter.DbType = DbType.Binary;
            }
        }
    }
}
