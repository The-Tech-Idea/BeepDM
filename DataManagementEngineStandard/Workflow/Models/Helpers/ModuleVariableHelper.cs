using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Workflow.Models.Base;
using TheTechIdea.Beep.Workflow.Models;

namespace TheTechIdea.Beep.Workflow.Models.Helpers
{
    /// <summary>
    /// Helper class for ModuleVariable operations
    /// </summary>
    public static class ModuleVariableHelper
    {
        /// <summary>
        /// Creates a new module variable
        /// </summary>
        public static ModuleVariable CreateVariable(
            int moduleId,
            string name,
            object value = null,
            string dataType = "String",
            string description = null,
            bool isRequired = false,
            VariableScope scope = VariableScope.Module)
        {
            return ModuleVariable.CreateVariable(moduleId, name, value, dataType, description, isRequired, scope);
        }

        /// <summary>
        /// Sets the variable value with type conversion
        /// </summary>
        public static void SetValue(ModuleVariable variable, object value)
        {
            variable.SetValue(value);
        }

        /// <summary>
        /// Gets the variable value with type conversion
        /// </summary>
        public static T GetValue<T>(ModuleVariable variable)
        {
            return variable.GetValue<T>();
        }

        /// <summary>
        /// Resets the variable to its default value
        /// </summary>
        public static void ResetToDefault(ModuleVariable variable)
        {
            variable.ResetToDefault();
        }

        /// <summary>
        /// Validates the variable value
        /// </summary>
        public static ValidationResult Validate(ModuleVariable variable)
        {
            return variable.Validate();
        }

        /// <summary>
        /// Validates the variable name format
        /// </summary>
        public static ValidationResult ValidateName(ModuleVariable variable)
        {
            return variable.ValidateName();
        }

        /// <summary>
        /// Gets the variable value as a string
        /// </summary>
        public static string GetValueAsString(ModuleVariable variable)
        {
            return variable.GetValueAsString();
        }

        /// <summary>
        /// Gets the default value as a string
        /// </summary>
        public static string GetDefaultValueAsString(ModuleVariable variable)
        {
            return variable.GetDefaultValueAsString();
        }

        /// <summary>
        /// Checks if the variable has a value set
        /// </summary>
        public static bool HasValue(ModuleVariable variable)
        {
            return variable.HasValue();
        }

        /// <summary>
        /// Checks if the variable value has changed from default
        /// </summary>
        public static bool HasChangedFromDefault(ModuleVariable variable)
        {
            return variable.HasChangedFromDefault();
        }

        /// <summary>
        /// Gets the list of allowed values
        /// </summary>
        public static List<string> GetAllowedValues(ModuleVariable variable)
        {
            return variable.GetAllowedValues();
        }

        /// <summary>
        /// Sets the list of allowed values
        /// </summary>
        public static void SetAllowedValues(ModuleVariable variable, List<string> values)
        {
            variable.SetAllowedValues(values);
        }

        /// <summary>
        /// Creates a copy of the variable
        /// </summary>
        public static ModuleVariable Clone(ModuleVariable variable)
        {
            return variable.Clone();
        }

        /// <summary>
        /// Gets a display-friendly representation of the variable
        /// </summary>
        public static string GetDisplayValue(ModuleVariable variable)
        {
            return variable.GetDisplayValue();
        }

        /// <summary>
        /// Updates the variable's metadata
        /// </summary>
        public static void UpdateMetadata(ModuleVariable variable, string modifiedBy = null)
        {
            variable.UpdateMetadata(modifiedBy);
        }

        /// <summary>
        /// Validates all variables in a collection
        /// </summary>
        public static ValidationResult ValidateVariables(IEnumerable<ModuleVariable> variables)
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
        public static List<ModuleVariable> GetVariablesByScope(IEnumerable<ModuleVariable> variables, VariableScope scope)
        {
            return variables.Where(v => v.Scope == scope).ToList();
        }

        /// <summary>
        /// Gets required variables
        /// </summary>
        public static List<ModuleVariable> GetRequiredVariables(IEnumerable<ModuleVariable> variables)
        {
            return variables.Where(v => v.IsRequired).ToList();
        }

        /// <summary>
        /// Gets input variables
        /// </summary>
        public static List<ModuleVariable> GetInputVariables(IEnumerable<ModuleVariable> variables)
        {
            return variables.Where(v => v.IsInput).ToList();
        }

        /// <summary>
        /// Gets output variables
        /// </summary>
        public static List<ModuleVariable> GetOutputVariables(IEnumerable<ModuleVariable> variables)
        {
            return variables.Where(v => v.IsOutput).ToList();
        }

        /// <summary>
        /// Gets variables by data type
        /// </summary>
        public static List<ModuleVariable> GetVariablesByDataType(IEnumerable<ModuleVariable> variables, string dataType)
        {
            return variables.Where(v => v.DataType?.ToLower() == dataType?.ToLower()).ToList();
        }

        /// <summary>
        /// Gets variables by category
        /// </summary>
        public static List<ModuleVariable> GetVariablesByCategory(IEnumerable<ModuleVariable> variables, string category)
        {
            return variables.Where(v => v.Category?.ToLower() == category?.ToLower()).ToList();
        }

        /// <summary>
        /// Finds a variable by name
        /// </summary>
        public static ModuleVariable FindVariableByName(IEnumerable<ModuleVariable> variables, string name)
        {
            return variables.FirstOrDefault(v => v.Name?.ToLower() == name?.ToLower());
        }

        /// <summary>
        /// Gets variables with validation errors
        /// </summary>
        public static List<ModuleVariable> GetVariablesWithErrors(IEnumerable<ModuleVariable> variables)
        {
            return variables.Where(v => !v.Validate().IsValid).ToList();
        }

        /// <summary>
        /// Gets variables that have values set
        /// </summary>
        public static List<ModuleVariable> GetVariablesWithValues(IEnumerable<ModuleVariable> variables)
        {
            return variables.Where(v => v.HasValue()).ToList();
        }

        /// <summary>
        /// Gets variables that have changed from their default values
        /// </summary>
        public static List<ModuleVariable> GetVariablesChangedFromDefault(IEnumerable<ModuleVariable> variables)
        {
            return variables.Where(v => v.HasChangedFromDefault()).ToList();
        }

        /// <summary>
        /// Creates a dictionary of variable values
        /// </summary>
        public static Dictionary<string, object> GetVariableValues(IEnumerable<ModuleVariable> variables)
        {
            return variables.ToDictionary(v => v.Name, v => v.Value);
        }

        /// <summary>
        /// Sets multiple variable values from a dictionary
        /// </summary>
        public static void SetVariableValues(IEnumerable<ModuleVariable> variables, Dictionary<string, object> values)
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
        public static void ResetAllToDefault(IEnumerable<ModuleVariable> variables)
        {
            foreach (var variable in variables)
            {
                variable.ResetToDefault();
            }
        }

        /// <summary>
        /// Gets variable statistics
        /// </summary>
        public static VariableStatistics GetVariableStatistics(IEnumerable<ModuleVariable> variables)
        {
            var variableList = variables.ToList();

            return new VariableStatistics
            {
                TotalVariables = variableList.Count,
                RequiredVariables = variableList.Count(v => v.IsRequired),
                InputVariables = variableList.Count(v => v.IsInput),
                OutputVariables = variableList.Count(v => v.IsOutput),
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
        public static ValidationResult ValidateVariableNames(IEnumerable<ModuleVariable> variables)
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
        public static List<ModuleVariable> SortByDisplayOrder(IEnumerable<ModuleVariable> variables)
        {
            return variables.OrderBy(v => v.DisplayOrder).ThenBy(v => v.Name).ToList();
        }

        /// <summary>
        /// Filters variables by search term
        /// </summary>
        public static List<ModuleVariable> FilterVariables(IEnumerable<ModuleVariable> variables, string searchTerm)
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

        /// <summary>
        /// Maps module variables to data mapping
        /// </summary>
        public static List<DataMapping> CreateDataMappings(
            IEnumerable<ModuleVariable> inputVariables,
            IEnumerable<ModuleVariable> outputVariables,
            int sourceModuleId,
            int targetModuleId)
        {
            var mappings = new List<DataMapping>();
            var inputList = inputVariables.Where(v => v.IsOutput).ToList();
            var outputList = outputVariables.Where(v => v.IsInput).ToList();

            foreach (var inputVar in inputList)
            {
                var matchingOutput = outputList.FirstOrDefault(v =>
                    v.Name.ToLower().Contains(inputVar.Name.ToLower()) ||
                    inputVar.Name.ToLower().Contains(v.Name.ToLower()));

                if (matchingOutput != null)
                {
                    mappings.Add(new DataMapping
                    {
                        SourceModuleId = sourceModuleId,
                        TargetModuleId = targetModuleId,
                        SourceField = inputVar.Name,
                        TargetField = matchingOutput.Name,
                        SourceFieldType = inputVar.DataType,
                        TargetFieldType = matchingOutput.DataType,
                        MappingType = MappingType.Direct,
                        IsActive = true
                    });
                }
            }

            return mappings;
        }
    }
}
