using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Workflow.Models.Base;

namespace TheTechIdea.Beep.Workflow.Models.Implementation
{
    /// <summary>
    /// Implementation partial class for Scenario - contains business logic methods
    /// </summary>
    public partial class Scenario
    {
        /// <summary>
        /// Validates the scenario configuration
        /// </summary>
        /// <returns>Validation result with any errors</returns>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(Name))
            {
                result.Errors.Add("Scenario name is required");
            }

            if (Modules == null || !Modules.Any())
            {
                result.Errors.Add("Scenario must contain at least one module");
            }

            // Check for trigger modules
            var hasTrigger = Modules.Any(m => m.ModuleType == ModuleType.Trigger);
            if (!hasTrigger)
            {
                result.Errors.Add("Scenario must have at least one trigger module");
            }

            // Validate module connections
            ValidateModuleConnections(result);

            return result;
        }

        /// <summary>
        /// Validates that all modules are properly connected
        /// </summary>
        private void ValidateModuleConnections(ValidationResult result)
        {
            if (Modules == null) return;

            var modulesWithInputs = Modules.Where(m => m.InputModules != null && m.InputModules.Any()).ToList();

            foreach (var module in modulesWithInputs)
            {
                foreach (var inputModuleId in module.InputModules)
                {
                    var inputModule = Modules.FirstOrDefault(m => m.Id == inputModuleId);
                    if (inputModule == null)
                    {
                        result.Errors.Add($"Module '{module.Name}' references non-existent input module with ID '{inputModuleId}'");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the trigger modules for this scenario
        /// </summary>
        public IEnumerable<Module> GetTriggerModules()
        {
            return Modules?.Where(m => m.ModuleType == ModuleType.Trigger) ?? Enumerable.Empty<Module>();
        }

        /// <summary>
        /// Gets the action modules for this scenario
        /// </summary>
        public IEnumerable<Module> GetActionModules()
        {
            return Modules?.Where(m => m.ModuleType == ModuleType.Action) ?? Enumerable.Empty<Module>();
        }

        /// <summary>
        /// Gets the router modules for this scenario
        /// </summary>
        public IEnumerable<Module> GetRouterModules()
        {
            return Modules?.Where(m => m.ModuleType == ModuleType.Router) ?? Enumerable.Empty<Module>();
        }

        /// <summary>
        /// Gets a module by its ID
        /// </summary>
        public Module GetModuleById(int moduleId)
        {
            return Modules?.FirstOrDefault(m => m.Id == moduleId);
        }

        /// <summary>
        /// Gets the execution path starting from triggers
        /// </summary>
        public IEnumerable<Module> GetExecutionPath()
        {
            var triggerModules = GetTriggerModules().ToList();
            var executionPath = new List<Module>();
            var visitedModules = new HashSet<int>();

            foreach (var trigger in triggerModules)
            {
                GetExecutionPathRecursive(trigger, executionPath, visitedModules);
            }

            return executionPath;
        }

        /// <summary>
        /// Recursively builds the execution path
        /// </summary>
        private void GetExecutionPathRecursive(Module currentModule, List<Module> executionPath, HashSet<int> visitedModules)
        {
            if (visitedModules.Contains(currentModule.Id)) return;

            visitedModules.Add(currentModule.Id);
            executionPath.Add(currentModule);

            // Get output modules (modules that have this module as input)
            var outputModules = Modules.Where(m =>
                m.InputModules != null && m.InputModules.Contains(currentModule.Id));

            foreach (var outputModule in outputModules)
            {
                GetExecutionPathRecursive(outputModule, executionPath, visitedModules);
            }
        }

        /// <summary>
        /// Gets all variables for the scenario
        /// </summary>
        public Dictionary<string, object> GetAllVariables()
        {
            var variables = new Dictionary<string, object>();

            if (Variables != null)
            {
                foreach (var variable in Variables)
                {
                    variables[variable.Name] = variable.Value;
                }
            }

            return variables;
        }

        /// <summary>
        /// Updates the scenario's last run date
        /// </summary>
        public void UpdateLastRunDate()
        {
            LastRunDate = DateTime.UtcNow;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new version of the scenario
        /// </summary>
        public Scenario CreateNewVersion(string versionNotes = null)
        {
            return new Scenario
            {
                Name = Name,
                Description = Description,
                Status = ScenarioStatus.Draft,
                CreatedDate = DateTime.UtcNow,
                CreatedByUserId = CreatedByUserId,
                CreatedByUserName = CreatedByUserName,
                IsActive = false,
                IsTemplate = IsTemplate,
                Category = Category,
                FolderId = FolderId,
                IsScheduled = IsScheduled,
                ScheduleExpression = ScheduleExpression,
                ExecutionTimeoutMinutes = ExecutionTimeoutMinutes,
                MaxRetryAttempts = MaxRetryAttempts,
                ContinueOnError = ContinueOnError,
                Tags = Tags,
                Version = Version + 1,
                VersionNotes = versionNotes
            };
        }
    }

    /// <summary>
    /// Validation result class
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new List<string>();
        public bool IsValid => !Errors.Any();
    }
}
