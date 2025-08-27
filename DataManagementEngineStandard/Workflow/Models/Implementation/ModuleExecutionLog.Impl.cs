using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Workflow.Models.Base;
using TheTechIdea.Beep.Workflow.Models;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Implementation partial class for ModuleExecutionLog - contains business logic methods
    /// </summary>
    public partial class ModuleExecutionLog
    {
        /// <summary>
        /// Creates a new module execution log entry
        /// </summary>
        public static ModuleExecutionLog CreateExecutionLog(
            int moduleId,
            int scenarioExecutionId,
            string executedBy = null,
            string machineName = null)
        {
            return new ModuleExecutionLog
            {
                ModuleId = moduleId,
                ScenarioExecutionId = scenarioExecutionId,
                Status = ExecutionStatus.Pending,
                StartedAt = DateTime.UtcNow,
                ExecutedBy = executedBy,
                MachineName = machineName ?? Environment.MachineName,
                CreatedDate = DateTime.UtcNow,
                AttemptNumber = 1
            };
        }

        /// <summary>
        /// Starts the module execution
        /// </summary>
        public void StartExecution()
        {
            Status = ExecutionStatus.Running;
            StartedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Completes the module execution successfully
        /// </summary>
        public void CompleteExecution(object outputData = null, long memoryUsageBytes = 0, double cpuUsagePercent = 0)
        {
            Status = ExecutionStatus.Success;
            CompletedAt = DateTime.UtcNow;
            Duration = CompletedAt.Value - StartedAt;

            if (outputData != null)
            {
                SetOutputData(outputData);
            }

            MemoryUsageBytes = memoryUsageBytes;
            CpuUsagePercent = cpuUsagePercent;
        }

        /// <summary>
        /// Fails the module execution
        /// </summary>
        public void FailExecution(string errorMessage, string errorDetails = null, Exception exception = null)
        {
            Status = ExecutionStatus.Failed;
            CompletedAt = DateTime.UtcNow;
            Duration = CompletedAt.Value - StartedAt;
            ErrorMessage = errorMessage;

            if (exception != null)
            {
                SetErrorDetails(new Dictionary<string, object>
                {
                    ["ExceptionType"] = exception.GetType().Name,
                    ["StackTrace"] = exception.StackTrace,
                    ["InnerException"] = exception.InnerException?.Message
                });
            }
            else if (!string.IsNullOrWhiteSpace(errorDetails))
            {
                SetErrorDetails(new Dictionary<string, object>
                {
                    ["Details"] = errorDetails
                });
            }
        }

        /// <summary>
        /// Cancels the module execution
        /// </summary>
        public void CancelExecution()
        {
            if (Status == ExecutionStatus.Running)
            {
                Status = ExecutionStatus.Cancelled;
                CompletedAt = DateTime.UtcNow;
                Duration = CompletedAt.Value - StartedAt;
            }
        }

        /// <summary>
        /// Times out the module execution
        /// </summary>
        public void TimeoutExecution()
        {
            Status = ExecutionStatus.Timeout;
            CompletedAt = DateTime.UtcNow;
            Duration = CompletedAt.Value - StartedAt;
            ErrorMessage = "Execution timed out";
        }

        /// <summary>
        /// Skips the module execution
        /// </summary>
        public void SkipExecution(string reason = null)
        {
            Status = ExecutionStatus.Skipped;
            CompletedAt = DateTime.UtcNow;
            Duration = CompletedAt.Value - StartedAt;

            if (!string.IsNullOrWhiteSpace(reason))
            {
                SetErrorDetails(new Dictionary<string, object>
                {
                    ["SkipReason"] = reason
                });
            }
        }

        /// <summary>
        /// Retries the module execution
        /// </summary>
        public ModuleExecutionLog CreateRetry()
        {
            return new ModuleExecutionLog
            {
                ModuleId = ModuleId,
                ScenarioExecutionId = ScenarioExecutionId,
                Status = ExecutionStatus.Pending,
                StartedAt = DateTime.UtcNow,
                ExecutedBy = ExecutedBy,
                MachineName = MachineName,
                CreatedDate = DateTime.UtcNow,
                AttemptNumber = AttemptNumber + 1,
                MaxRetries = MaxRetries,
                InputData = InputData, // Keep the same input data
                ModuleVersion = ModuleVersion,
                ConfigurationSnapshot = ConfigurationSnapshot
            };
        }

        /// <summary>
        /// Validates the module execution log
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (ModuleId <= 0)
            {
                result.Errors.Add("Module ID must be greater than 0");
            }

            if (ScenarioExecutionId <= 0)
            {
                result.Errors.Add("Scenario execution ID must be greater than 0");
            }

            if (StartedAt == default)
            {
                result.Errors.Add("Started date is required");
            }

            if (CompletedAt.HasValue && CompletedAt.Value < StartedAt)
            {
                result.Errors.Add("Completed date cannot be before started date");
            }

            if (AttemptNumber < 1)
            {
                result.Errors.Add("Attempt number must be at least 1");
            }

            if (MemoryUsageBytes < 0)
            {
                result.Errors.Add("Memory usage cannot be negative");
            }

            if (CpuUsagePercent < 0 || CpuUsagePercent > 100)
            {
                result.Errors.Add("CPU usage percent must be between 0 and 100");
            }

            return result;
        }

        /// <summary>
        /// Gets the execution duration in milliseconds
        /// </summary>
        public long GetDurationMs()
        {
            return (long)(Duration?.TotalMilliseconds ?? 0);
        }

        /// <summary>
        /// Checks if the execution is completed (success, failed, cancelled, timeout, skipped)
        /// </summary>
        public bool IsCompleted => Status == ExecutionStatus.Success ||
                                  Status == ExecutionStatus.Failed ||
                                  Status == ExecutionStatus.Cancelled ||
                                  Status == ExecutionStatus.Timeout ||
                                  Status == ExecutionStatus.Skipped;

        /// <summary>
        /// Checks if the execution was successful
        /// </summary>
        public bool IsSuccess => Status == ExecutionStatus.Success;

        /// <summary>
        /// Checks if the execution failed
        /// </summary>
        public bool IsFailure => Status == ExecutionStatus.Failed ||
                                Status == ExecutionStatus.Timeout;

        /// <summary>
        /// Gets a formatted summary of the execution result
        /// </summary>
        public string GetExecutionSummary()
        {
            var summary = $"Module {ModuleId} - {Status}";

            if (Duration.HasValue)
            {
                summary += $" in {GetDurationMs()}ms";
            }

            if (AttemptNumber > 1)
            {
                summary += $" (Attempt {AttemptNumber})";
            }

            if (!string.IsNullOrWhiteSpace(ErrorMessage))
            {
                summary += $": {ErrorMessage}";
            }

            return summary;
        }

        /// <summary>
        /// Formats the execution result for logging
        /// </summary>
        public string ToLogString()
        {
            return $"[{StartedAt:yyyy-MM-dd HH:mm:ss}] {GetExecutionSummary()}";
        }

        /// <summary>
        /// Sets input data from an object
        /// </summary>
        public void SetInputData(object inputData)
        {
            if (inputData != null)
            {
                InputData = System.Text.Json.JsonSerializer.Serialize(inputData);
            }
        }

        /// <summary>
        /// Gets input data as a typed object
        /// </summary>
        public T GetInputData<T>()
        {
            if (string.IsNullOrWhiteSpace(InputData))
            {
                return default;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(InputData);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Sets output data from an object
        /// </summary>
        public void SetOutputData(object outputData)
        {
            if (outputData != null)
            {
                OutputData = System.Text.Json.JsonSerializer.Serialize(outputData);
            }
        }

        /// <summary>
        /// Gets output data as a typed object
        /// </summary>
        public T GetOutputData<T>()
        {
            if (string.IsNullOrWhiteSpace(OutputData))
            {
                return default;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(OutputData);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Sets error details from a dictionary
        /// </summary>
        public void SetErrorDetails(Dictionary<string, object> details)
        {
            if (details != null && details.Any())
            {
                ErrorDetails = System.Text.Json.JsonSerializer.Serialize(details);
            }
        }

        /// <summary>
        /// Gets error details as a typed object
        /// </summary>
        public Dictionary<string, object> GetErrorDetails()
        {
            if (string.IsNullOrWhiteSpace(ErrorDetails))
            {
                return new Dictionary<string, object>();
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ErrorDetails);
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Creates a snapshot of the module configuration
        /// </summary>
        public void CreateConfigurationSnapshot(Module module)
        {
            if (module != null)
            {
                ConfigurationSnapshot = System.Text.Json.JsonSerializer.Serialize(new
                {
                    module.Name,
                    module.ModuleType,
                    module.ModuleDefinitionName,
                    module.IsEnabled,
                    module.ExecutionOrder,
                    module.RetryCount,
                    module.MaxRetryCount,
                    module.ContinueOnError
                });
            }
        }

        /// <summary>
        /// Gets the configuration snapshot as a typed object
        /// </summary>
        public ModuleConfigurationSnapshot GetConfigurationSnapshot()
        {
            if (string.IsNullOrWhiteSpace(ConfigurationSnapshot))
            {
                return null;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<ModuleConfigurationSnapshot>(ConfigurationSnapshot);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if this execution can be retried
        /// </summary>
        public bool CanRetry()
        {
            return IsFailure && AttemptNumber < MaxRetries;
        }

        /// <summary>
        /// Gets the remaining retry attempts
        /// </summary>
        public int GetRemainingRetries()
        {
            return Math.Max(0, MaxRetries - AttemptNumber);
        }
    }

    /// <summary>
    /// Module configuration snapshot for execution logging
    /// </summary>
    public class ModuleConfigurationSnapshot
    {
        public string Name { get; set; }
        public ModuleType ModuleType { get; set; }
        public string ModuleDefinitionName { get; set; }
        public bool IsEnabled { get; set; }
        public int ExecutionOrder { get; set; }
        public int RetryCount { get; set; }
        public int MaxRetryCount { get; set; }
        public bool ContinueOnError { get; set; }
    }
}
