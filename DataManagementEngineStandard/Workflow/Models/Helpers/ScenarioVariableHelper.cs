using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Workflow.Models.Base;
using TheTechIdea.Beep.Workflow.Models;

namespace TheTechIdea.Beep.Workflow.Models.Helpers
{
    /// <summary>
    /// Helper class for ScenarioVariable operations
    /// </summary>
    public static class ScenarioVariableHelper
    {
        /// <summary>
        /// Creates a new scenario variable
        /// </summary>
        public static ScenarioVariable CreateVariable(
            int scenarioId,
            string name,
            object value = null,
            string dataType = "String",
            string description = null,
            bool isRequired = false,
            VariableScope scope = VariableScope.Scenario)
        {
            return ScenarioVariable.CreateVariable(scenarioId, name, value, dataType, description, isRequired, scope);
        }

        /// <summary>
        /// Sets the variable value with type conversion
        /// </summary>
        public static void SetValue(ScenarioVariable variable, object value)
        {
            variable.SetValue(value);
        }

        /// <summary>
        /// Gets the variable value with type conversion
        /// </summary>
        public static T GetValue<T>(ScenarioVariable variable)
        {
            return variable.GetValue<T>();
        }

        /// <summary>
        /// Resets the variable to its default value
        /// </summary>
        public static void ResetToDefault(ScenarioVariable variable)
        {
            variable.ResetToDefault();
        }

        /// <summary>
        /// Validates the variable value
        /// </summary>
        public static ValidationResult Validate(ScenarioVariable variable)
        {
            return variable.Validate();
        }

        /// <summary>
        /// Validates the variable name format
        /// </summary>
        public static ValidationResult ValidateName(ScenarioVariable variable)
        {
            return variable.ValidateName();
        }

        /// <summary>
        /// Gets the variable value as a string
        /// </summary>
        public static string GetValueAsString(ScenarioVariable variable)
        {
            return variable.GetValueAsString();
        }

        /// <summary>
        /// Gets the default value as a string
        /// </summary>
        public static string GetDefaultValueAsString(ScenarioVariable variable)
        {
            return variable.GetDefaultValueAsString();
        }

        /// <summary>
        /// Checks if the variable has a value set
        /// </summary>
        public static bool HasValue(ScenarioVariable variable)
        {
            return variable.HasValue();
        }

        /// <summary>
        /// Checks if the variable value has changed from default
        /// </summary>
        public static bool HasChangedFromDefault(ScenarioVariable variable)
        {
            return variable.HasChangedFromDefault();
        }

        /// <summary>
        /// Gets the list of allowed values
        /// </summary>
        public static List<string> GetAllowedValues(ScenarioVariable variable)
        {
            return variable.GetAllowedValues();
        }

        /// <summary>
        /// Sets the list of allowed values
        /// </summary>
        public static void SetAllowedValues(ScenarioVariable variable, List<string> values)
        {
            variable.SetAllowedValues(values);
        }

        /// <summary>
        /// Creates a copy of the variable
        /// </summary>
        public static ScenarioVariable Clone(ScenarioVariable variable)
        {
            return variable.Clone();
        }

        /// <summary>
        /// Gets a display-friendly representation of the variable
        /// </summary>
        public static string GetDisplayValue(ScenarioVariable variable)
        {
            return variable.GetDisplayValue();
        }

        /// <summary>
        /// Updates the variable's metadata
        /// </summary>
        public static void UpdateMetadata(ScenarioVariable variable, string modifiedBy = null)
        {
            variable.UpdateMetadata(modifiedBy);
        }

        /// <summary>
        /// Validates all variables in a collection
        /// </summary>
        public static ValidationResult ValidateVariables(IEnumerable<ScenarioVariable> variables)
        {
            var result = new ValidationResult();

            foreach (var variable in variables)
            {
                var variableValidation = variable.Validate();
                if (!variableValidation.IsValid)
                {
                    result.Errors.AddRange(variableValidation.Errors.Select(error => $"Variable '{variable.Name}': {error}"));
                }
            }

            return result;
        }

        /// <summary>
        /// Gets variables by scope
        /// </summary>
        public static List<ScenarioVariable> GetVariablesByScope(IEnumerable<ScenarioVariable> variables, VariableScope scope)
        {
            return variables.Where(v => v.Scope == scope).ToList();
        }

        /// <summary>
        /// Gets required variables
        /// </summary>
        public static List<ScenarioVariable> GetRequiredVariables(IEnumerable<ScenarioVariable> variables)
        {
            return variables.Where(v => v.IsRequired).ToList();
        }

        /// <summary>
        /// Gets global variables
        /// </summary>
        public static List<ScenarioVariable> GetGlobalVariables(IEnumerable<ScenarioVariable> variables)
        {
            return variables.Where(v => v.IsGlobal).ToList();
        }

        /// <summary>
        /// Gets variables by data type
        /// </summary>
        public static List<ScenarioVariable> GetVariablesByDataType(IEnumerable<ScenarioVariable> variables, string dataType)
        {
            return variables.Where(v => v.DataType?.ToLower() == dataType?.ToLower()).ToList();
        }

        /// <summary>
        /// Gets variables by category
        /// </summary>
        public static List<ScenarioVariable> GetVariablesByCategory(IEnumerable<ScenarioVariable> variables, string category)
        {
            return variables.Where(v => v.Category?.ToLower() == category?.ToLower()).ToList();
        }

        /// <summary>
        /// Finds a variable by name
        /// </summary>
        public static ScenarioVariable FindVariableByName(IEnumerable<ScenarioVariable> variables, string name)
        {
            return variables.FirstOrDefault(v => v.Name?.ToLower() == name?.ToLower());
        }

        /// <summary>
        /// Gets variables with validation errors
        /// </summary>
        public static List<ScenarioVariable> GetVariablesWithErrors(IEnumerable<ScenarioVariable> variables)
        {
            return variables.Where(v => !v.Validate().IsValid).ToList();
        }

        /// <summary>
        /// Gets variables that have values set
        /// </summary>
        public static List<ScenarioVariable> GetVariablesWithValues(IEnumerable<ScenarioVariable> variables)
        {
            return variables.Where(v => v.HasValue()).ToList();
        }

        /// <summary>
        /// Gets variables that have changed from their default values
        /// </summary>
        public static List<ScenarioVariable> GetVariablesChangedFromDefault(IEnumerable<ScenarioVariable> variables)
        {
            return variables.Where(v => v.HasChangedFromDefault()).ToList();
        }

        /// <summary>
        /// Creates a dictionary of variable values
        /// </summary>
        public static Dictionary<string, object> GetVariableValues(IEnumerable<ScenarioVariable> variables)
        {
            return variables.ToDictionary(v => v.Name, v => v.Value);
        }

        /// <summary>
        /// Sets multiple variable values from a dictionary
        /// </summary>
        public static void SetVariableValues(IEnumerable<ScenarioVariable> variables, Dictionary<string, object> values)
        {
            foreach (var variable in variables)
            {
                if (values.TryGetValue(variable.Name, out var value))
                {
                    variable.SetValue(value);
                }
            }
        }

        /// <summary>
        /// Resets all variables to their default values
        /// </summary>
        public static void ResetAllToDefault(IEnumerable<ScenarioVariable> variables)
        {
            foreach (var variable in variables)
            {
                variable.ResetToDefault();
            }
        }

        /// <summary>
        /// Gets variable statistics
        /// </summary>
        public static VariableStatistics GetVariableStatistics(IEnumerable<ScenarioVariable> variables)
        {
            var variableList = variables.ToList();

            return new VariableStatistics
            {
                TotalVariables = variableList.Count,
                RequiredVariables = variableList.Count(v => v.IsRequired),
                GlobalVariables = variableList.Count(v => v.IsGlobal),
                VariablesWithValues = variableList.Count(v => v.HasValue()),
                VariablesWithErrors = variableList.Count(v => !v.Validate().IsValid),
                DataTypeCounts = variableList
                    .Where(v => !string.IsNullOrWhiteSpace(v.DataType))
                    .GroupBy(v => v.DataType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ScopeCounts = variableList
                    .GroupBy(v => v.Scope)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// Validates variable names for uniqueness
        /// </summary>
        public static ValidationResult ValidateVariableNames(IEnumerable<ScenarioVariable> variables)
        {
            var result = new ValidationResult();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var variable in variables)
            {
                if (!names.Add(variable.Name))
                {
                    result.Errors.Add($"Duplicate variable name: '{variable.Name}'");
                }
            }

            return result;
        }

        /// <summary>
        /// Sorts variables by display order
        /// </summary>
        public static List<ScenarioVariable> SortByDisplayOrder(IEnumerable<ScenarioVariable> variables)
        {
            return variables.OrderBy(v => v.DisplayOrder).ThenBy(v => v.Name).ToList();
        }

        /// <summary>
        /// Filters variables by search term
        /// </summary>
        public static List<ScenarioVariable> FilterVariables(IEnumerable<ScenarioVariable> variables, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return variables.ToList();
            }

            return variables.Where(v =>
                v.Name?.ToLower().Contains(searchTerm.ToLower()) == true ||
                v.Description?.ToLower().Contains(searchTerm.ToLower()) == true ||
                v.DisplayName?.ToLower().Contains(searchTerm.ToLower()) == true ||
                v.Category?.ToLower().Contains(searchTerm.ToLower()) == true
            ).ToList();
        }
    }
}
