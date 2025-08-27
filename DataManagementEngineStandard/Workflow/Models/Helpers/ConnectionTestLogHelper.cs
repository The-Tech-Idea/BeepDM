using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Workflow.Models.Base;
using TheTechIdea.Beep.Workflow.Models;

namespace TheTechIdea.Beep.Workflow.Models.Helpers
{
    /// <summary>
    /// Helper class for ConnectionTestLog operations
    /// </summary>
    public static class ConnectionTestLogHelper
    {
        /// <summary>
        /// Creates a successful connection test log
        /// </summary>
        public static ConnectionTestLog CreateSuccessLog(
            int connectionId,
            string connectionName,
            string testType = "Connectivity",
            string responseMessage = "Connection successful",
            long responseTimeMs = 0,
            Dictionary<string, object> metadata = null)
        {
            var testParameters = metadata != null ? 
                System.Text.Json.JsonSerializer.Serialize(metadata) : null;
                
            return ConnectionTestLog.CreateSuccessLog(
                connectionId: connectionId,
                responseTimeMs: responseTimeMs,
                responseData: responseMessage,
                testParameters: testParameters,
                testMethod: testType);
        }

        /// <summary>
        /// Creates a failed connection test log
        /// </summary>
        public static ConnectionTestLog CreateFailureLog(
            int connectionId,
            string connectionName,
            string testType = "Connectivity",
            string errorMessage = "Connection failed",
            string errorDetails = null,
            long responseTimeMs = 0,
            Dictionary<string, object> metadata = null)
        {
            var testParameters = metadata != null ? 
                System.Text.Json.JsonSerializer.Serialize(metadata) : null;
                
            return ConnectionTestLog.CreateFailureLog(
                connectionId: connectionId,
                errorMessage: errorMessage,
                errorDetails: errorDetails,
                responseTimeMs: responseTimeMs,
                testParameters: testParameters,
                testMethod: testType);
        }

        /// <summary>
        /// Gets the latest test log for a connection
        /// </summary>
        public static ConnectionTestLog GetLatestTestLog(IEnumerable<ConnectionTestLog> logs, int connectionId)
        {
            return logs
                .Where(log => log.ConnectionId == connectionId)
                .OrderByDescending(log => log.TestedAt)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the success rate for a connection over a time period
        /// </summary>
        public static double GetSuccessRate(IEnumerable<ConnectionTestLog> logs, int connectionId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filteredLogs = logs.Where(log => log.ConnectionId == connectionId);

            if (fromDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.TestedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.TestedAt <= toDate.Value);
            }

            var totalTests = filteredLogs.Count();
            if (totalTests == 0) return 0;

            var successfulTests = filteredLogs.Count(log => log.Status == ConnectionTestStatus.Success);
            return (double)successfulTests / totalTests * 100;
        }

        /// <summary>
        /// Gets the average response time for successful tests
        /// </summary>
        public static double GetAverageResponseTime(IEnumerable<ConnectionTestLog> logs, int connectionId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filteredLogs = logs
                .Where(log => log.ConnectionId == connectionId && log.Status == ConnectionTestStatus.Success)
                .Where(log => log.ResponseTimeMs > 0);

            if (fromDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.TestedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.TestedAt <= toDate.Value);
            }

            var responseTimes = filteredLogs.Select(log => log.ResponseTimeMs).ToList();
            return responseTimes.Any() ? responseTimes.Average() : 0;
        }

        /// <summary>
        /// Gets connection health status based on recent test logs
        /// </summary>
        public static ConnectionHealthStatus GetConnectionHealthStatus(IEnumerable<ConnectionTestLog> logs, int connectionId, int recentTestCount = 10)
        {
            var recentLogs = logs
                .Where(log => log.ConnectionId == connectionId)
                .OrderByDescending(log => log.TestedAt)
                .Take(recentTestCount)
                .ToList();

            if (!recentLogs.Any())
            {
                return ConnectionHealthStatus.Unknown;
            }

            var successRate = (double)recentLogs.Count(log => log.Status == ConnectionTestStatus.Success) / recentLogs.Count * 100;

            if (successRate >= 95)
            {
                return ConnectionHealthStatus.Healthy;
            }
            else if (successRate >= 80)
            {
                return ConnectionHealthStatus.Stale;
            }
            else
            {
                return ConnectionHealthStatus.Error;
            }
        }

        /// <summary>
        /// Cleans up old test logs
        /// </summary>
        public static void CleanupOldLogs(List<ConnectionTestLog> logs, int keepDays = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-keepDays);
            logs.RemoveAll(log => log.TestedAt < cutoffDate);
        }

        /// <summary>
        /// Gets test logs grouped by status
        /// </summary>
        public static Dictionary<ConnectionTestStatus, List<ConnectionTestLog>> GetLogsGroupedByStatus(IEnumerable<ConnectionTestLog> logs, int connectionId)
        {
            return logs
                .Where(log => log.ConnectionId == connectionId)
                .GroupBy(log => log.Status)
                .ToDictionary(group => group.Key, group => group.ToList());
        }

        /// <summary>
        /// Gets the most common error messages
        /// </summary>
        public static Dictionary<string, int> GetCommonErrors(IEnumerable<ConnectionTestLog> logs, int connectionId, int topCount = 10)
        {
            return logs
                .Where(log => log.ConnectionId == connectionId && log.Status == ConnectionTestStatus.Failed)
                .Where(log => !string.IsNullOrWhiteSpace(log.ErrorMessage))
                .GroupBy(log => log.ErrorMessage)
                .OrderByDescending(group => group.Count())
                .Take(topCount)
                .ToDictionary(group => group.Key, group => group.Count());
        }

        /// <summary>
        /// Validates all test logs for a connection
        /// </summary>
        public static ValidationResult ValidateLogs(IEnumerable<ConnectionTestLog> logs, int connectionId)
        {
            var result = new ValidationResult();
            var connectionLogs = logs.Where(log => log.ConnectionId == connectionId).ToList();

            if (!connectionLogs.Any())
            {
                result.Errors.Add($"No test logs found for connection ID {connectionId}");
                return result;
            }

            foreach (var log in connectionLogs)
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
        /// Creates a summary report for connection tests
        /// </summary>
        public static ConnectionTestSummary CreateSummaryReport(IEnumerable<ConnectionTestLog> logs, int connectionId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filteredLogs = logs.Where(log => log.ConnectionId == connectionId);

            if (fromDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.TestedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.TestedAt <= toDate.Value);
            }

            var logList = filteredLogs.ToList();

            return new ConnectionTestSummary
            {
                ConnectionId = connectionId,
                TotalTests = logList.Count,
                SuccessfulTests = logList.Count(log => log.Status == ConnectionTestStatus.Success),
                FailedTests = logList.Count(log => log.Status == ConnectionTestStatus.Failed),
                SuccessRate = logList.Any() ? (double)logList.Count(log => log.Status == ConnectionTestStatus.Success) / logList.Count * 100 : 0,
                AverageResponseTime = GetAverageResponseTime(logList, connectionId),
                LastTestDate = logList.OrderByDescending(log => log.TestedAt).FirstOrDefault()?.TestedAt,
                HealthStatus = GetConnectionHealthStatus(logList, connectionId),
                CommonErrors = GetCommonErrors(logList, connectionId)
            };
        }
    }

    /// <summary>
    /// Summary report for connection tests
    /// </summary>
    public class ConnectionTestSummary
    {
        public int ConnectionId { get; set; }
        public int TotalTests { get; set; }
        public int SuccessfulTests { get; set; }
        public int FailedTests { get; set; }
        public double SuccessRate { get; set; }
        public double AverageResponseTime { get; set; }
        public DateTime? LastTestDate { get; set; }
        public ConnectionHealthStatus HealthStatus { get; set; }
        public Dictionary<string, int> CommonErrors { get; set; } = new Dictionary<string, int>();
    }
}
