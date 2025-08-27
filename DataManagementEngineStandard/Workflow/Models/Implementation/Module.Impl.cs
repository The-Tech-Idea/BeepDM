using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Workflow.Models.Base;

namespace TheTechIdea.Beep.Workflow.Models.Implementation
{
    /// <summary>
    /// Implementation partial class for Module - contains business logic methods
    /// </summary>
    public partial class Module
    {
        /// <summary>
        /// Validates the module configuration
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(Name))
            {
                result.Errors.Add("Module name is required");
            }

            if (string.IsNullOrWhiteSpace(ModuleDefinitionName))
            {
                result.Errors.Add("Module definition name is required");
            }

            if (ScenarioId <= 0)
            {
                result.Errors.Add("Valid scenario ID is required");
            }

            // Validate connections
            ValidateConnections(result);

            // Validate configuration
            ValidateConfiguration(result);

            return result;
        }

        /// <summary>
        /// Validates module connections
        /// </summary>
        private void ValidateConnections(ValidationResult result)
        {
            if (InputModules == null)
            {
                InputModules = new List<int>();
            }

            if (OutputModules == null)
            {
                OutputModules = new List<int>();
            }

            // Check for self-connections
            if (InputModules.Contains(Id) || OutputModules.Contains(Id))
            {
                result.Errors.Add("Module cannot connect to itself");
            }

            // Check for duplicate connections
            if (InputModules.Distinct().Count() != InputModules.Count)
            {
                result.Errors.Add("Duplicate input module connections found");
            }

            if (OutputModules.Distinct().Count() != OutputModules.Count)
            {
                result.Errors.Add("Duplicate output module connections found");
            }
        }

        /// <summary>
        /// Validates module configuration
        /// </summary>
        private void ValidateConfiguration(ValidationResult result)
        {
            if (RetryCount < 0)
            {
                result.Errors.Add("Retry count cannot be negative");
            }

            if (MaxRetryCount < 0)
            {
                result.Errors.Add("Max retry count cannot be negative");
            }

            if (MaxRetryCount > 10)
            {
                result.Errors.Add("Max retry count cannot exceed 10");
            }

            if (ExecutionOrder < 0)
            {
                result.Errors.Add("Execution order cannot be negative");
            }
        }

        /// <summary>
        /// Gets all input modules from the scenario
        /// </summary>
        public IEnumerable<Module> GetInputModules()
        {
            if (Scenario?.Modules == null || InputModules == null)
            {
                return Enumerable.Empty<Module>();
            }

            return Scenario.Modules.Where(m => InputModules.Contains(m.Id));
        }

        /// <summary>
        /// Gets all output modules from the scenario
        /// </summary>
        public IEnumerable<Module> GetOutputModules()
        {
            if (Scenario?.Modules == null || OutputModules == null)
            {
                return Enumerable.Empty<Module>();
            }

            return Scenario.Modules.Where(m => OutputModules.Contains(m.Id));
        }

        /// <summary>
        /// Adds an input connection to this module
        /// </summary>
        public void AddInputConnection(int moduleId)
        {
            if (!InputModules.Contains(moduleId))
            {
                InputModules.Add(moduleId);
                ModifiedDate = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Removes an input connection from this module
        /// </summary>
        public void RemoveInputConnection(int moduleId)
        {
            if (InputModules.Contains(moduleId))
            {
                InputModules.Remove(moduleId);
                ModifiedDate = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Adds an output connection from this module
        /// </summary>
        public void AddOutputConnection(int moduleId)
        {
            if (!OutputModules.Contains(moduleId))
            {
                OutputModules.Add(moduleId);
                ModifiedDate = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Removes an output connection from this module
        /// </summary>
        public void RemoveOutputConnection(int moduleId)
        {
            if (OutputModules.Contains(moduleId))
            {
                OutputModules.Remove(moduleId);
                ModifiedDate = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Gets the configuration object from JSON
        /// </summary>
        public T GetConfiguration<T>() where T : class
        {
            if (string.IsNullOrWhiteSpace(ConfigurationJson))
            {
                return null;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(ConfigurationJson);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the configuration object as JSON
        /// </summary>
        public void SetConfiguration<T>(T configuration) where T : class
        {
            if (configuration == null)
            {
                ConfigurationJson = null;
            }
            else
            {
                ConfigurationJson = System.Text.Json.JsonSerializer.Serialize(configuration);
            }
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a variable by name
        /// </summary>
        public ModuleVariable GetVariable(string name)
        {
            return Variables?.FirstOrDefault(v =>
                string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sets a variable value
        /// </summary>
        public void SetVariable(string name, object value, string dataType = null)
        {
            var variable = GetVariable(name);
            if (variable == null)
            {
                variable = new ModuleVariable
                {
                    Name = name,
                    DataType = dataType ?? value?.GetType().Name ?? "String"
                };
                Variables.Add(variable);
            }

            variable.Value = value;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if the module can be executed
        /// </summary>
        public bool CanExecute()
        {
            if (!IsEnabled)
            {
                return false;
            }

            // Check if all required variables have values
            if (Variables != null)
            {
                foreach (var variable in Variables.Where(v => v.IsRequired))
                {
                    if (variable.Value == null || string.IsNullOrWhiteSpace(variable.Value.ToString()))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Resets the module state for re-execution
        /// </summary>
        public void ResetExecutionState()
        {
            RetryCount = 0;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Increments the retry count
        /// </summary>
        public void IncrementRetryCount()
        {
            RetryCount++;
            ModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if the module has reached max retry count
        /// </summary>
        public bool HasReachedMaxRetries()
        {
            return RetryCount >= MaxRetryCount;
        }

        /// <summary>
        /// Creates a copy of this module for a different scenario
        /// </summary>
        public Module CreateCopy(int targetScenarioId)
        {
            return new Module
            {
                Name = Name,
                Description = Description,
                ModuleType = ModuleType,
                ModuleDefinitionName = ModuleDefinitionName,
                ScenarioId = targetScenarioId,
                InputModules = new List<int>(InputModules),
                OutputModules = new List<int>(OutputModules),
                PositionX = PositionX,
                PositionY = PositionY,
                ConfigurationJson = ConfigurationJson,
                IsEnabled = IsEnabled,
                ExecutionOrder = ExecutionOrder,
                EstimatedExecutionTime = EstimatedExecutionTime,
                RetryCount = 0,
                MaxRetryCount = MaxRetryCount,
                ContinueOnError = ContinueOnError,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = CreatedBy
            };
        }
    }
}
