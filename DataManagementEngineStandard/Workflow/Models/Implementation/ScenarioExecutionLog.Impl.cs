using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Workflow.Models.Base;
using TheTechIdea.Beep.Workflow.Models;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Implementation partial class for ScenarioExecutionLog - contains business logic methods
    /// </summary>
    public partial class ScenarioExecutionLog
    {
        /// <summary>
        /// Creates a new scenario execution log entry
        /// </summary>
        public static ScenarioExecutionLog CreateExecutionLog(
            int scenarioId,
            string executionMode = "Manual",
            string triggerSource = null,
            string executedBy = null,
            string machineName = null,
            string clientIp = null)
        {
            return new ScenarioExecutionLog
            {
                ScenarioId = scenarioId,
                Status = ExecutionStatus.Pending,
                StartedAt = DateTime.UtcNow,
                ExecutionMode = executionMode,
                TriggerSource = triggerSource,
                ExecutedBy = executedBy,
                MachineName = machineName ?? Environment.MachineName,
                ClientIp = clientIp,
                CreatedDate = DateTime.UtcNow,
                AttemptNumber = 1,
                TotalModules = 0,
                SuccessfulModules = 0,
                FailedModules = 0,
                SkippedModules = 0
            };
        }

        /// <summary>
        /// Starts the scenario execution
        /// </summary>
        public void StartExecution(int totalModules = 0)
        {
            Status = ExecutionStatus.Running;
            StartedAt = DateTime.UtcNow;
            TotalModules = totalModules;
        }

        /// <summary>
        /// Completes the scenario execution successfully
        /// </summary>
        public void CompleteExecution()
        {
            Status = ExecutionStatus.Success;
            CompletedAt = DateTime.UtcNow;
            Duration = CompletedAt.Value - StartedAt;
            CalculateStatistics();
        }

        /// <summary>
        /// Fails the scenario execution
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

            CalculateStatistics();
        }

        /// <summary>
        /// Cancels the scenario execution
        /// </summary>
        public void CancelExecution()
        {
            if (Status == ExecutionStatus.Running)
            {
                Status = ExecutionStatus.Cancelled;
                CompletedAt = DateTime.UtcNow;
                Duration = CompletedAt.Value - StartedAt;
                CalculateStatistics();
            }
        }

        /// <summary>
        /// Times out the scenario execution
        /// </summary>
        public void TimeoutExecution()
        {
            Status = ExecutionStatus.Timeout;
            CompletedAt = DateTime.UtcNow;
            Duration = CompletedAt.Value - StartedAt;
            ErrorMessage = "Execution timed out";
            CalculateStatistics();
        }

        /// <summary>
        /// Updates module execution statistics
        /// </summary>
        public void UpdateModuleStatistics(ExecutionStatus moduleStatus)
        {
            switch (moduleStatus)
            {
                case ExecutionStatus.Success:
                    SuccessfulModules++;
                    break;
                case ExecutionStatus.Failed:
                case ExecutionStatus.Timeout:
                    FailedModules++;
                    break;
                case ExecutionStatus.Skipped:
                case ExecutionStatus.Cancelled:
                    SkippedModules++;
                    break;
            }
        }

        /// <summary>
        /// Calculates execution statistics from module logs
        /// </summary>
        public void CalculateStatistics()
        {
            if (ModuleLogs == null || !ModuleLogs.Any())
            {
                return;
            }

            TotalModules = ModuleLogs.Count;
            SuccessfulModules = ModuleLogs.Count(m => m.Status == ExecutionStatus.Success);
            FailedModules = ModuleLogs.Count(m => m.IsFailure);
            SkippedModules = ModuleLogs.Count(m => m.Status == ExecutionStatus.Skipped || m.Status == ExecutionStatus.Cancelled);

            // Calculate performance metrics
            var completedModules = ModuleLogs.Where(m => m.IsCompleted && m.Duration.HasValue).ToList();
            if (completedModules.Any())
            {
                TotalMemoryUsageBytes = completedModules.Sum(m => m.MemoryUsageBytes);
                AverageCpuUsagePercent = completedModules.Average(m => m.CpuUsagePercent);

                var totalDuration = completedModules.Sum(m => m.GetDurationMs());
                if (TotalModules > 0)
                {
                    AverageModuleDuration = TimeSpan.FromMilliseconds(totalDuration / (double)TotalModules);
                }
            }
        }

        /// <summary>
        /// Creates a retry execution log
        /// </summary>
        public ScenarioExecutionLog CreateRetry()
        {
            return new ScenarioExecutionLog
            {
                ScenarioId = ScenarioId,
                Status = ExecutionStatus.Pending,
                StartedAt = DateTime.UtcNow,
                ExecutionMode = ExecutionMode,
                TriggerSource = TriggerSource,
                ExecutionParameters = ExecutionParameters,
                ExecutedBy = ExecutedBy,
                MachineName = MachineName,
                ClientIp = ClientIp,
                CreatedDate = DateTime.UtcNow,
                AttemptNumber = AttemptNumber + 1,
                MaxRetries = MaxRetries,
                ScenarioVersion = ScenarioVersion,
                ScenarioSnapshot = ScenarioSnapshot
            };
        }

        /// <summary>
        /// Validates the scenario execution log
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (ScenarioId <= 0)
            {
                result.Errors.Add("Scenario ID must be greater than 0");
            }

            if (StartedAt == default)
            {
                result.Errors.Add("Started date is required");
            }

            if (CompletedAt.HasValue && CompletedAt.Value < StartedAt)
            {
                result.Errors.Add("Completed date cannot be before started date");
            }

            if (TotalModules < 0)
            {
                result.Errors.Add("Total modules cannot be negative");
            }

            if (SuccessfulModules < 0 || SuccessfulModules > TotalModules)
            {
                result.Errors.Add("Successful modules count is invalid");
            }

            if (FailedModules < 0)
            {
                result.Errors.Add("Failed modules count cannot be negative");
            }

            if (SkippedModules < 0)
            {
                result.Errors.Add("Skipped modules count cannot be negative");
            }

            if (TotalMemoryUsageBytes < 0)
            {
                result.Errors.Add("Total memory usage cannot be negative");
            }

            if (AverageCpuUsagePercent < 0 || AverageCpuUsagePercent > 100)
            {
                result.Errors.Add("Average CPU usage percent must be between 0 and 100");
            }

            if (AttemptNumber < 1)
            {
                result.Errors.Add("Attempt number must be at least 1");
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
        /// Checks if the execution is completed
        /// </summary>
        public bool IsCompleted => Status == ExecutionStatus.Success ||
                                  Status == ExecutionStatus.Failed ||
                                  Status == ExecutionStatus.Cancelled ||
                                  Status == ExecutionStatus.Timeout;

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
        /// Gets the success rate as a percentage
        /// </summary>
        public double GetSuccessRate()
        {
            if (TotalModules == 0) return 0;
            return (double)SuccessfulModules / TotalModules * 100;
        }

        /// <summary>
        /// Gets a formatted summary of the execution result
        /// </summary>
        public string GetExecutionSummary()
        {
            var summary = $"Scenario {ScenarioId} - {Status}";

            if (Duration.HasValue)
            {
                summary += $" in {GetDurationMs()}ms";
            }

            if (TotalModules > 0)
            {
                summary += $" ({SuccessfulModules}/{TotalModules} modules successful)";
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
        /// Sets trigger data from an object
        /// </summary>
        public void SetTriggerData(object triggerData)
        {
            if (triggerData != null)
            {
                TriggerData = System.Text.Json.JsonSerializer.Serialize(triggerData);
            }
        }

        /// <summary>
        /// Gets trigger data as a typed object
        /// </summary>
        public T GetTriggerData<T>()
        {
            if (string.IsNullOrWhiteSpace(TriggerData))
            {
                return default;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(TriggerData);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Sets execution parameters from a dictionary
        /// </summary>
        public void SetExecutionParameters(Dictionary<string, object> parameters)
        {
            if (parameters != null && parameters.Any())
            {
                ExecutionParameters = System.Text.Json.JsonSerializer.Serialize(parameters);
            }
        }

        /// <summary>
        /// Gets execution parameters as a typed object
        /// </summary>
        public Dictionary<string, object> GetExecutionParameters()
        {
            if (string.IsNullOrWhiteSpace(ExecutionParameters))
            {
                return new Dictionary<string, object>();
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ExecutionParameters);
            }
            catch
            {
                return new Dictionary<string, object>();
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
        /// Creates a snapshot of the scenario configuration
        /// </summary>
        public void CreateScenarioSnapshot(Scenario scenario)
        {
            if (scenario != null)
            {
                ScenarioVersion = scenario.Version.ToString();
                ScenarioSnapshot = System.Text.Json.JsonSerializer.Serialize(new
                {
                    scenario.Name,
                    scenario.Description,
                    scenario.Status,
                    scenario.IsActive,
                    scenario.ExecutionTimeoutMinutes,
                    scenario.MaxRetryAttempts,
                    scenario.ContinueOnError,
                    TotalModules = scenario.Modules?.Count ?? 0
                });
            }
        }

        /// <summary>
        /// Gets the scenario snapshot as a typed object
        /// </summary>
        public ScenarioSnapshot GetScenarioSnapshot()
        {
            if (string.IsNullOrWhiteSpace(ScenarioSnapshot))
            {
                return null;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<ScenarioSnapshot>(ScenarioSnapshot);
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

        /// <summary>
        /// Gets the module execution logs ordered by execution order
        /// </summary>
        public IEnumerable<ModuleExecutionLog> GetOrderedModuleLogs()
        {
            return ModuleLogs?.OrderBy(m => m.StartedAt) ?? Enumerable.Empty<ModuleExecutionLog>();
        }
    }

    /// <summary>
    /// Scenario snapshot for execution logging
    /// </summary>
    public class ScenarioSnapshot
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ScenarioStatus Status { get; set; }
        public bool IsActive { get; set; }
        public int ExecutionTimeoutMinutes { get; set; }
        public int MaxRetryAttempts { get; set; }
        public bool ContinueOnError { get; set; }
        public int TotalModules { get; set; }
    }
}
