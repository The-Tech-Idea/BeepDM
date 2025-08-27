using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Workflow.Models.Base;
using TheTechIdea.Beep.Workflow.Models;

namespace TheTechIdea.Beep.Workflow.Models.Helpers
{
    /// <summary>
    /// Helper class for Module operations
    /// </summary>
    public static class ModuleHelper
    {
        /// <summary>
        /// Creates a deep clone of a module
        /// </summary>
        public static Module CloneModule(Module original)
        {
            if (original == null) return null;

            var cloned = new Module
            {
                Name = original.Name,
                Description = original.Description,
                ModuleType = original.ModuleType,
                ModuleDefinitionName = original.ModuleDefinitionName,
                ScenarioId = original.ScenarioId,
                InputModules = new List<int>(original.InputModules ?? new List<int>()),
                OutputModules = new List<int>(original.OutputModules ?? new List<int>()),
                PositionX = original.PositionX,
                PositionY = original.PositionY,
                ConfigurationJson = original.ConfigurationJson,
                IsEnabled = original.IsEnabled,
                ExecutionOrder = original.ExecutionOrder,
                EstimatedExecutionTime = original.EstimatedExecutionTime,
                RetryCount = original.RetryCount,
                MaxRetryCount = original.MaxRetryCount,
                ContinueOnError = original.ContinueOnError,
                CreatedDate = original.CreatedDate,
                ModifiedDate = original.ModifiedDate,
                CreatedBy = original.CreatedBy,
                ModifiedBy = original.ModifiedBy
            };

            return cloned;
        }

        /// <summary>
        /// Validates a module and returns detailed validation results
        /// </summary>
        public static ValidationResult ValidateModule(Module module)
        {
            var result = new ValidationResult();

            if (module == null)
            {
                result.Errors.Add("Module is null");
                return result;
            }

            // Basic validation
            if (string.IsNullOrWhiteSpace(module.Name))
            {
                result.Errors.Add("Module name is required");
            }

            if (module.Name?.Length > 200)
            {
                result.Errors.Add("Module name cannot exceed 200 characters");
            }

            if (string.IsNullOrWhiteSpace(module.ModuleDefinitionName))
            {
                result.Errors.Add("Module definition name is required");
            }

            if (module.ScenarioId <= 0)
            {
                result.Errors.Add("Valid scenario ID is required");
            }

            // Connection validation
            ValidateModuleConnections(module, result);

            // Configuration validation
            ValidateModuleConfiguration(module, result);

            return result;
        }

        /// <summary>
        /// Validates module connections
        /// </summary>
        private static void ValidateModuleConnections(Module module, ValidationResult result)
        {
            if (module.InputModules == null)
            {
                module.InputModules = new List<int>();
            }

            if (module.OutputModules == null)
            {
                module.OutputModules = new List<int>();
            }

            // Check for self-connections
            if (module.InputModules.Contains(module.Id))
            {
                result.Errors.Add($"Module '{module.Name}' cannot connect to itself as input");
            }

            if (module.OutputModules.Contains(module.Id))
            {
                result.Errors.Add($"Module '{module.Name}' cannot connect to itself as output");
            }

            // Check for duplicate connections
            var duplicateInputs = module.InputModules.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var duplicate in duplicateInputs)
            {
                result.Errors.Add($"Module '{module.Name}' has duplicate input connection to module {duplicate}");
            }

            var duplicateOutputs = module.OutputModules.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var duplicate in duplicateOutputs)
            {
                result.Errors.Add($"Module '{module.Name}' has duplicate output connection to module {duplicate}");
            }
        }

        /// <summary>
        /// Validates module configuration
        /// </summary>
        private static void ValidateModuleConfiguration(Module module, ValidationResult result)
        {
            if (module.RetryCount < 0)
            {
                result.Errors.Add($"Module '{module.Name}': Retry count cannot be negative");
            }

            if (module.MaxRetryCount < 0)
            {
                result.Errors.Add($"Module '{module.Name}': Max retry count cannot be negative");
            }

            if (module.MaxRetryCount > 10)
            {
                result.Errors.Add($"Module '{module.Name}': Max retry count cannot exceed 10");
            }

            if (module.ExecutionOrder < 0)
            {
                result.Errors.Add($"Module '{module.Name}': Execution order cannot be negative");
            }

            // Validate JSON configuration if present
            if (!string.IsNullOrWhiteSpace(module.ConfigurationJson))
            {
                try
                {
                    System.Text.Json.JsonDocument.Parse(module.ConfigurationJson);
                }
                catch
                {
                    result.Errors.Add($"Module '{module.Name}': Invalid JSON configuration");
                }
            }
        }

        /// <summary>
        /// Gets module statistics
        /// </summary>
        public static ModuleStatistics GetModuleStatistics(Module module)
        {
            if (module == null) return new ModuleStatistics();

            return new ModuleStatistics
            {
                InputConnectionCount = module.InputModules?.Count ?? 0,
                OutputConnectionCount = module.OutputModules?.Count ?? 0,
                VariableCount = module.Variables?.Count ?? 0,
                RequiredVariableCount = module.Variables?.Count(v => v.IsRequired) ?? 0,
                ExecutionLogCount = module.ExecutionLogs?.Count ?? 0,
                SuccessfulExecutionCount = module.ExecutionLogs?.Count(e => e.Status == ExecutionStatus.Success) ?? 0,
                FailedExecutionCount = module.ExecutionLogs?.Count(e => e.Status == ExecutionStatus.Failed) ?? 0
            };
        }

        /// <summary>
        /// Checks if a module has circular dependencies
        /// </summary>
        public static bool HasCircularDependency(Module module, IEnumerable<Module> allModules)
        {
            if (module?.OutputModules == null || !module.OutputModules.Any())
            {
                return false;
            }

            var visited = new HashSet<int>();
            var recursionStack = new HashSet<int>();

            return HasCircularDependencyRecursive(module.Id, allModules, visited, recursionStack);
        }

        /// <summary>
        /// Recursive helper for circular dependency detection
        /// </summary>
        private static bool HasCircularDependencyRecursive(int moduleId, IEnumerable<Module> allModules,
            HashSet<int> visited, HashSet<int> recursionStack)
        {
            visited.Add(moduleId);
            recursionStack.Add(moduleId);

            var module = allModules.FirstOrDefault(m => m.Id == moduleId);
            if (module?.OutputModules != null)
            {
                foreach (var outputId in module.OutputModules)
                {
                    if (!visited.Contains(outputId))
                    {
                        if (HasCircularDependencyRecursive(outputId, allModules, visited, recursionStack))
                        {
                            return true;
                        }
                    }
                    else if (recursionStack.Contains(outputId))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Remove(moduleId);
            return false;
        }
    }

    /// <summary>
    /// Module statistics
    /// </summary>
    public class ModuleStatistics
    {
        public int InputConnectionCount { get; set; }
        public int OutputConnectionCount { get; set; }
        public int VariableCount { get; set; }
        public int RequiredVariableCount { get; set; }
        public int ExecutionLogCount { get; set; }
        public int SuccessfulExecutionCount { get; set; }
        public int FailedExecutionCount { get; set; }
    }
}
