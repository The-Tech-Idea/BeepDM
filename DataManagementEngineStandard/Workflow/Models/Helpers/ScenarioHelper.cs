using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Workflow.Models.Base;

namespace TheTechIdea.Beep.Workflow.Models.Helpers
{
    /// <summary>
    /// Helper class for Scenario operations
    /// </summary>
    public static class ScenarioHelper
    {
        /// <summary>
        /// Creates a deep clone of a scenario
        /// </summary>
        public static Scenario CloneScenario(Scenario original)
        {
            if (original == null) return null;

            var cloned = new Scenario
            {
                Name = original.Name,
                Description = original.Description,
                Status = original.Status,
                CreatedDate = original.CreatedDate,
                ModifiedDate = original.ModifiedDate,
                LastRunDate = original.LastRunDate,
                CreatedByUserId = original.CreatedByUserId,
                CreatedByUserName = original.CreatedByUserName,
                IsActive = original.IsActive,
                IsTemplate = original.IsTemplate,
                Category = original.Category,
                FolderId = original.FolderId,
                IsScheduled = original.IsScheduled,
                ScheduleExpression = original.ScheduleExpression,
                NextScheduledRun = original.NextScheduledRun,
                ExecutionTimeoutMinutes = original.ExecutionTimeoutMinutes,
                MaxRetryAttempts = original.MaxRetryAttempts,
                ContinueOnError = original.ContinueOnError,
                Tags = original.Tags,
                Version = original.Version,
                VersionNotes = original.VersionNotes
            };

            // Clone modules
            if (original.Modules != null)
            {
                cloned.Modules = original.Modules.Select(ModuleHelper.CloneModule).ToList();
            }

            // Clone variables
            if (original.Variables != null)
            {
                cloned.Variables = original.Variables.Select(v => new ScenarioVariable
                {
                    Name = v.Name,
                    Value = v.Value,
                    DataType = v.DataType,
                    IsRequired = v.IsRequired,
                    Description = v.Description
                }).ToList();
            }

            return cloned;
        }

        /// <summary>
        /// Validates a scenario and returns detailed validation results
        /// </summary>
        public static ScenarioValidationResult ValidateScenarioDetailed(Scenario scenario)
        {
            var result = new ScenarioValidationResult();

            if (scenario == null)
            {
                result.IsValid = false;
                result.Errors.Add("Scenario is null");
                return result;
            }

            // Basic validation
            if (string.IsNullOrWhiteSpace(scenario.Name))
            {
                result.Errors.Add("Scenario name is required");
            }

            if (scenario.Name?.Length > 200)
            {
                result.Errors.Add("Scenario name cannot exceed 200 characters");
            }

            // Module validation
            if (scenario.Modules == null || !scenario.Modules.Any())
            {
                result.Errors.Add("Scenario must contain at least one module");
            }
            else
            {
                ValidateModules(scenario, result);
            }

            // Variable validation
            if (scenario.Variables != null)
            {
                ValidateVariables(scenario, result);
            }

            // Scheduling validation
            if (scenario.IsScheduled)
            {
                ValidateScheduling(scenario, result);
            }

            result.IsValid = !result.Errors.Any() && !result.Warnings.Any();
            return result;
        }

        /// <summary>
        /// Validates modules within a scenario
        /// </summary>
        private static void ValidateModules(Scenario scenario, ScenarioValidationResult result)
        {
            var moduleIds = new HashSet<int>();
            var triggerCount = 0;

            foreach (var module in scenario.Modules)
            {
                // Check for duplicate IDs
                if (!moduleIds.Add(module.Id))
                {
                    result.Errors.Add($"Duplicate module ID found: {module.Id}");
                }

                // Count triggers
                if (module.ModuleType == ModuleType.Trigger)
                {
                    triggerCount++;
                }

                // Validate module-specific rules
                var moduleValidation = ModuleHelper.ValidateModule(module);
                result.Errors.AddRange(moduleValidation.Errors);
                result.Warnings.AddRange(moduleValidation.Warnings);
            }

            if (triggerCount == 0)
            {
                result.Errors.Add("Scenario must have at least one trigger module");
            }

            // Check for orphaned modules (modules with no connections)
            var connectedModuleIds = new HashSet<int>();
            foreach (var module in scenario.Modules)
            {
                if (module.InputModules != null)
                {
                    connectedModuleIds.UnionWith(module.InputModules);
                }
            }

            var orphanedModules = scenario.Modules.Where(m =>
                !connectedModuleIds.Contains(m.Id) && m.ModuleType != ModuleType.Trigger).ToList();

            if (orphanedModules.Any())
            {
                result.Warnings.Add($"Found {orphanedModules.Count} orphaned modules (not connected to any trigger)");
            }
        }

        /// <summary>
        /// Validates variables within a scenario
        /// </summary>
        private static void ValidateVariables(Scenario scenario, ScenarioValidationResult result)
        {
            var variableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var variable in scenario.Variables)
            {
                // Check for duplicate names
                if (!variableNames.Add(variable.Name))
                {
                    result.Errors.Add($"Duplicate variable name found: {variable.Name}");
                }

                // Validate variable properties
                if (string.IsNullOrWhiteSpace(variable.Name))
                {
                    result.Errors.Add("Variable name cannot be empty");
                }

                if (variable.IsRequired && variable.Value == null)
                {
                    result.Errors.Add($"Required variable '{variable.Name}' has no value");
                }
            }
        }

        /// <summary>
        /// Validates scheduling configuration
        /// </summary>
        private static void ValidateScheduling(Scenario scenario, ScenarioValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(scenario.ScheduleExpression))
            {
                result.Errors.Add("Schedule expression is required when scenario is scheduled");
            }
            else
            {
                // Basic cron expression validation
                if (!IsValidCronExpression(scenario.ScheduleExpression))
                {
                    result.Errors.Add("Invalid cron expression format");
                }
            }
        }

        /// <summary>
        /// Basic cron expression validation
        /// </summary>
        private static bool IsValidCronExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return false;

            var parts = expression.Split(' ');
            return parts.Length == 6; // Standard cron has 6 parts
        }

        /// <summary>
        /// Gets scenario statistics
        /// </summary>
        public static ScenarioStatistics GetScenarioStatistics(Scenario scenario)
        {
            if (scenario == null) return new ScenarioStatistics();

            return new ScenarioStatistics
            {
                TotalModules = scenario.Modules?.Count ?? 0,
                TriggerModules = scenario.Modules?.Count(m => m.ModuleType == ModuleType.Trigger) ?? 0,
                ActionModules = scenario.Modules?.Count(m => m.ModuleType == ModuleType.Action) ?? 0,
                RouterModules = scenario.Modules?.Count(m => m.ModuleType == ModuleType.Router) ?? 0,
                TotalVariables = scenario.Variables?.Count ?? 0,
                RequiredVariables = scenario.Variables?.Count(v => v.IsRequired) ?? 0,
                TotalExecutions = scenario.ExecutionLogs?.Count ?? 0,
                SuccessfulExecutions = scenario.ExecutionLogs?.Count(e => e.Status == ExecutionStatus.Success) ?? 0,
                FailedExecutions = scenario.ExecutionLogs?.Count(e => e.Status == ExecutionStatus.Failed) ?? 0
            };
        }

        /// <summary>
        /// Exports scenario to JSON format
        /// </summary>
        public static string ExportToJson(Scenario scenario)
        {
            // This would use a JSON serializer in a real implementation
            return System.Text.Json.JsonSerializer.Serialize(scenario);
        }

        /// <summary>
        /// Imports scenario from JSON format
        /// </summary>
        public static Scenario ImportFromJson(string json)
        {
            // This would use a JSON deserializer in a real implementation
            return System.Text.Json.JsonSerializer.Deserialize<Scenario>(json);
        }
    }

    /// <summary>
    /// Detailed validation result for scenarios
    /// </summary>
    public class ScenarioValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
    }

    /// <summary>
    /// Scenario statistics
    /// </summary>
    public class ScenarioStatistics
    {
        public int TotalModules { get; set; }
        public int TriggerModules { get; set; }
        public int ActionModules { get; set; }
        public int RouterModules { get; set; }
        public int TotalVariables { get; set; }
        public int RequiredVariables { get; set; }
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
    }
}
