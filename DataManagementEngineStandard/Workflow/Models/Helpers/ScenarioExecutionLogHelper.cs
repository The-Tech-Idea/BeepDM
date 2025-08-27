using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Workflow.Models.Base;
using TheTechIdea.Beep.Workflow.Models;

namespace TheTechIdea.Beep.Workflow.Models.Helpers
{
    /// <summary>
    /// Helper class for ScenarioExecutionLog operations
    /// </summary>
    public static class ScenarioExecutionLogHelper
    {
        /// <summary>
        /// Creates a new scenario execution log
        /// </summary>
        public static ScenarioExecutionLog CreateExecutionLog(
            int scenarioId,
            string executionMode = "Manual",
            string triggerSource = null,
            string executedBy = null,
            string machineName = null,
            string clientIp = null)
        {
            return ScenarioExecutionLog.CreateExecutionLog(scenarioId, executionMode, triggerSource, executedBy, machineName, clientIp);
        }

        /// <summary>
        /// Starts the execution of a scenario
        /// </summary>
        public static void StartExecution(ScenarioExecutionLog log, int totalModules = 0)
        {
            log.StartExecution(totalModules);
        }

        /// <summary>
        /// Marks a scenario execution as completed successfully
        /// </summary>
        public static void CompleteExecution(ScenarioExecutionLog log)
        {
            log.CompleteExecution();
        }

        /// <summary>
        /// Marks a scenario execution as failed
        /// </summary>
        public static void FailExecution(
            ScenarioExecutionLog log,
            string errorMessage,
            string errorDetails = null,
            Exception exception = null)
        {
            log.FailExecution(errorMessage, errorDetails, exception);
        }

        /// <summary>
        /// Gets the latest execution log for a scenario
        /// </summary>
        public static ScenarioExecutionLog GetLatestExecutionLog(IEnumerable<ScenarioExecutionLog> logs, int scenarioId)
        {
            return logs
                .Where(log => log.ScenarioId == scenarioId)
                .OrderByDescending(log => log.StartedAt)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets execution logs for a specific scenario within a date range
        /// </summary>
        public static List<ScenarioExecutionLog> GetExecutionLogsForScenario(
            IEnumerable<ScenarioExecutionLog> logs,
            int scenarioId,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var filteredLogs = logs.Where(log => log.ScenarioId == scenarioId);

            if (fromDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.StartedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.StartedAt <= toDate.Value);
            }

            return filteredLogs
                .OrderByDescending(log => log.StartedAt)
                .ToList();
        }

        /// <summary>
        /// Calculates execution statistics for a scenario
        /// </summary>
        public static ScenarioExecutionStatistics CalculateStatistics(IEnumerable<ScenarioExecutionLog> logs, int scenarioId)
        {
            var scenarioLogs = logs.Where(log => log.ScenarioId == scenarioId).ToList();

            return new ScenarioExecutionStatistics
            {
                ScenarioId = scenarioId,
                TotalExecutions = scenarioLogs.Count,
                SuccessfulExecutions = scenarioLogs.Count(log => log.Status == ExecutionStatus.Success),
                FailedExecutions = scenarioLogs.Count(log => log.Status == ExecutionStatus.Failed),
                AverageExecutionTime = scenarioLogs
                    .Where(log => log.Status == ExecutionStatus.Success && log.Duration.HasValue)
                    .Select(log => log.Duration.Value.TotalMilliseconds)
                    .DefaultIfEmpty(0)
                    .Average(),
                SuccessRate = scenarioLogs.Any() ? (double)scenarioLogs.Count(log => log.Status == ExecutionStatus.Success) / scenarioLogs.Count * 100 : 0,
                LastExecutionDate = scenarioLogs.OrderByDescending(log => log.StartedAt).FirstOrDefault()?.StartedAt,
                TotalRetries = scenarioLogs.Sum(log => log.AttemptNumber - 1),
                AverageModulesExecuted = scenarioLogs
                    .Where(log => log.Status == ExecutionStatus.Success)
                    .Select(log => log.TotalModules)
                    .DefaultIfEmpty(0)
                    .Average()
            };
        }

        /// <summary>
        /// Gets scenarios with the highest failure rates
        /// </summary>
        public static List<ScenarioFailureRate> GetScenariosWithHighFailureRates(IEnumerable<ScenarioExecutionLog> logs, double minFailureRate = 10.0, int minExecutions = 5)
        {
            return logs
                .GroupBy(log => log.ScenarioId)
                .Where(group => group.Count() >= minExecutions)
                .Select(group =>
                {
                    var total = group.Count();
                    var failures = group.Count(log => log.Status == ExecutionStatus.Failed);
                    var failureRate = (double)failures / total * 100;

                    return new ScenarioFailureRate
                    {
                        ScenarioId = group.Key,
                        ScenarioName = group.First().Scenario?.Name ?? $"Scenario {group.Key}",
                        TotalExecutions = total,
                        FailedExecutions = failures,
                        FailureRate = failureRate
                    };
                })
                .Where(stat => stat.FailureRate >= minFailureRate)
                .OrderByDescending(stat => stat.FailureRate)
                .ToList();
        }

        /// <summary>
        /// Gets the average execution time trend over time
        /// </summary>
        public static List<ExecutionTimeTrend> GetExecutionTimeTrend(IEnumerable<ScenarioExecutionLog> logs, int scenarioId, int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            return logs
                .Where(log => log.ScenarioId == scenarioId && log.Status == ExecutionStatus.Success && log.Duration.HasValue && log.StartedAt >= cutoffDate)
                .GroupBy(log => log.StartedAt.Date)
                .OrderBy(group => group.Key)
                .Select(group => new ExecutionTimeTrend
                {
                    Date = group.Key,
                    AverageExecutionTime = group.Average(log => log.Duration.Value.TotalMilliseconds),
                    ExecutionCount = group.Count()
                })
                .ToList();
        }

        /// <summary>
        /// Finds executions that took longer than a threshold
        /// </summary>
        public static List<ScenarioExecutionLog> GetSlowExecutions(IEnumerable<ScenarioExecutionLog> logs, int scenarioId, long thresholdMs = 30000)
        {
            return logs
                .Where(log => log.ScenarioId == scenarioId && log.Status == ExecutionStatus.Success && log.Duration.HasValue && log.Duration.Value.TotalMilliseconds > thresholdMs)
                .OrderByDescending(log => log.Duration.Value.TotalMilliseconds)
                .ToList();
        }

        /// <summary>
        /// Gets the most common error messages for a scenario
        /// </summary>
        public static Dictionary<string, int> GetCommonErrors(IEnumerable<ScenarioExecutionLog> logs, int scenarioId, int topCount = 10)
        {
            return logs
                .Where(log => log.ScenarioId == scenarioId && log.Status == ExecutionStatus.Failed)
                .Where(log => !string.IsNullOrWhiteSpace(log.ErrorMessage))
                .GroupBy(log => log.ErrorMessage)
                .OrderByDescending(group => group.Count())
                .Take(topCount)
                .ToDictionary(group => group.Key, group => group.Count());
        }

        /// <summary>
        /// Validates all execution logs for a scenario
        /// </summary>
        public static ValidationResult ValidateLogs(IEnumerable<ScenarioExecutionLog> logs, int scenarioId)
        {
            var result = new ValidationResult();
            var scenarioLogs = logs.Where(log => log.ScenarioId == scenarioId).ToList();

            if (!scenarioLogs.Any())
            {
                result.Errors.Add($"No execution logs found for scenario ID {scenarioId}");
                return result;
            }

            foreach (var log in scenarioLogs)
            {
                var logValidation = log.Validate();
                if (!logValidation.IsValid)
                {
                    result.Errors.AddRange(logValidation.Errors.Select(error => $"Log ID {log.Id}: {error}"));
                }
            }

            return result;
        }

        /// <summary>
        /// Cleans up old execution logs
        /// </summary>
        public static void CleanupOldLogs(List<ScenarioExecutionLog> logs, int keepDays = 90)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-keepDays);
            logs.RemoveAll(log => log.StartedAt < cutoffDate);
        }

        /// <summary>
        /// Gets execution logs grouped by status
        /// </summary>
        public static Dictionary<ExecutionStatus, List<ScenarioExecutionLog>> GetLogsGroupedByStatus(IEnumerable<ScenarioExecutionLog> logs, int scenarioId)
        {
            return logs
                .Where(log => log.ScenarioId == scenarioId)
                .GroupBy(log => log.Status)
                .ToDictionary(group => group.Key, group => group.ToList());
        }

        /// <summary>
        /// Creates a retry for a failed scenario execution
        /// </summary>
        public static ScenarioExecutionLog CreateRetry(ScenarioExecutionLog originalLog)
        {
            return originalLog.CreateRetry();
        }

        /// <summary>
        /// Checks if a scenario can be retried based on its execution history
        /// </summary>
        public static bool CanRetryScenario(IEnumerable<ScenarioExecutionLog> logs, int scenarioId, int maxRetries = 3)
        {
            var recentLogs = logs
                .Where(log => log.ScenarioId == scenarioId)
                .OrderByDescending(log => log.StartedAt)
                .Take(maxRetries + 1)
                .ToList();

            if (!recentLogs.Any())
            {
                return true;
            }

            var consecutiveFailures = 0;
            foreach (var log in recentLogs)
            {
                if (log.Status == ExecutionStatus.Failed)
                {
                    consecutiveFailures++;
                }
                else
                {
                    break;
                }
            }

            return consecutiveFailures < maxRetries;
        }

        /// <summary>
        /// Gets the success rate for a scenario over a time period
        /// </summary>
        public static double GetSuccessRate(IEnumerable<ScenarioExecutionLog> logs, int scenarioId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filteredLogs = logs.Where(log => log.ScenarioId == scenarioId);

            if (fromDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.StartedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.StartedAt <= toDate.Value);
            }

            var totalTests = filteredLogs.Count();
            if (totalTests == 0) return 0;

            var successfulTests = filteredLogs.Count(log => log.Status == ExecutionStatus.Success);
            return (double)successfulTests / totalTests * 100;
        }

        /// <summary>
        /// Gets the average execution time for successful executions
        /// </summary>
        public static double GetAverageExecutionTime(IEnumerable<ScenarioExecutionLog> logs, int scenarioId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filteredLogs = logs
                .Where(log => log.ScenarioId == scenarioId && log.Status == ExecutionStatus.Success && log.Duration.HasValue);

            if (fromDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.StartedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.StartedAt <= toDate.Value);
            }

            var durations = filteredLogs.Select(log => log.Duration.Value.TotalMilliseconds).ToList();
            return durations.Any() ? durations.Average() : 0;
        }
    }

    /// <summary>
    /// Scenario execution statistics
    /// </summary>
    public class ScenarioExecutionStatistics
    {
        /// <summary>
        /// Scenario ID
        /// </summary>
        public int ScenarioId { get; set; }

        /// <summary>
        /// Total number of executions
        /// </summary>
        public int TotalExecutions { get; set; }

        /// <summary>
        /// Number of successful executions
        /// </summary>
        public int SuccessfulExecutions { get; set; }

        /// <summary>
        /// Number of failed executions
        /// </summary>
        public int FailedExecutions { get; set; }

        /// <summary>
        /// Average execution time in milliseconds
        /// </summary>
        public double AverageExecutionTime { get; set; }

        /// <summary>
        /// Success rate as percentage
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Last execution date
        /// </summary>
        public DateTime? LastExecutionDate { get; set; }

        /// <summary>
        /// Total number of retries
        /// </summary>
        public int TotalRetries { get; set; }

        /// <summary>
        /// Average number of modules executed
        /// </summary>
        public double AverageModulesExecuted { get; set; }
    }

    /// <summary>
    /// Scenario failure rate information
    /// </summary>
    public class ScenarioFailureRate
    {
        /// <summary>
        /// Scenario ID
        /// </summary>
        public int ScenarioId { get; set; }

        /// <summary>
        /// Scenario name
        /// </summary>
        public string ScenarioName { get; set; }

        /// <summary>
        /// Total executions
        /// </summary>
        public int TotalExecutions { get; set; }

        /// <summary>
        /// Failed executions
        /// </summary>
        public int FailedExecutions { get; set; }

        /// <summary>
        /// Failure rate as percentage
        /// </summary>
        public double FailureRate { get; set; }
    }
}
