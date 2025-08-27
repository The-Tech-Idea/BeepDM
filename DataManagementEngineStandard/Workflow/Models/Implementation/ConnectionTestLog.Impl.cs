using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Workflow.Models.Base;
using TheTechIdea.Beep.Workflow.Models;

namespace TheTechIdea.Beep.Workflow.Models.Base
{
    /// <summary>
    /// Implementation partial class for ConnectionTestLog - contains business logic methods
    /// </summary>
    public partial class ConnectionTestLog
    {
        /// <summary>
        /// Creates a new connection test log entry
        /// </summary>
        public static ConnectionTestLog CreateTestLog(
            int connectionId,
            ConnectionTestStatus status,
            TimeSpan? duration = null,
            string errorMessage = null,
            string errorDetails = null,
            string responseData = null,
            long responseTimeMs = 0,
            int responseSizeBytes = 0,
            int httpStatusCode = 0,
            string testParameters = null,
            string testMethod = "Default",
            string testedBy = null,
            string machineName = null)
        {
            return new ConnectionTestLog
            {
                ConnectionId = connectionId,
                Status = status,
                TestedAt = DateTime.UtcNow,
                Duration = duration,
                ErrorMessage = errorMessage,
                ErrorDetails = errorDetails,
                ResponseData = responseData,
                ResponseTimeMs = responseTimeMs,
                ResponseSizeBytes = responseSizeBytes,
                HttpStatusCode = httpStatusCode,
                TestParameters = testParameters,
                TestMethod = testMethod,
                TestedBy = testedBy,
                MachineName = machineName ?? Environment.MachineName,
                CreatedDate = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a successful test log entry
        /// </summary>
        public static ConnectionTestLog CreateSuccessLog(
            int connectionId,
            long responseTimeMs = 0,
            int responseSizeBytes = 0,
            int httpStatusCode = 200,
            string responseData = null,
            string testParameters = null,
            string testMethod = "Default",
            string testedBy = null)
        {
            return CreateTestLog(
                connectionId: connectionId,
                status: ConnectionTestStatus.Success,
                responseTimeMs: responseTimeMs,
                responseSizeBytes: responseSizeBytes,
                httpStatusCode: httpStatusCode,
                responseData: responseData,
                testParameters: testParameters,
                testMethod: testMethod,
                testedBy: testedBy);
        }

        /// <summary>
        /// Creates a failed test log entry
        /// </summary>
        public static ConnectionTestLog CreateFailureLog(
            int connectionId,
            string errorMessage,
            string errorDetails = null,
            ConnectionTestStatus status = ConnectionTestStatus.Failed,
            long responseTimeMs = 0,
            string testParameters = null,
            string testMethod = "Default",
            string testedBy = null)
        {
            return CreateTestLog(
                connectionId: connectionId,
                status: status,
                errorMessage: errorMessage,
                errorDetails: errorDetails,
                responseTimeMs: responseTimeMs,
                testParameters: testParameters,
                testMethod: testMethod,
                testedBy: testedBy);
        }

        /// <summary>
        /// Validates the connection test log
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (ConnectionId <= 0)
            {
                result.Errors.Add("Connection ID must be greater than 0");
            }

            if (TestedAt == default)
            {
                result.Errors.Add("Tested date is required");
            }

            if (ResponseTimeMs < 0)
            {
                result.Errors.Add("Response time cannot be negative");
            }

            if (ResponseSizeBytes < 0)
            {
                result.Errors.Add("Response size cannot be negative");
            }

            if (HttpStatusCode < 0)
            {
                result.Errors.Add("HTTP status code cannot be negative");
            }

            return result;
        }

        /// <summary>
        /// Gets a formatted summary of the test result
        /// </summary>
        public string GetTestSummary()
        {
            var summary = $"Test {Status} in {ResponseTimeMs}ms";

            if (HttpStatusCode > 0)
            {
                summary += $" (HTTP {HttpStatusCode})";
            }

            if (!string.IsNullOrWhiteSpace(ErrorMessage))
            {
                summary += $": {ErrorMessage}";
            }

            return summary;
        }

        /// <summary>
        /// Checks if the test was successful
        /// </summary>
        public bool IsSuccess => Status == ConnectionTestStatus.Success;

        /// <summary>
        /// Checks if the test failed
        /// </summary>
        public bool IsFailure => Status != ConnectionTestStatus.Success;

        /// <summary>
        /// Gets the test duration in milliseconds
        /// </summary>
        public long GetDurationMs()
        {
            // If Duration is set, use it; otherwise use ResponseTimeMs
            return (long)(Duration?.TotalMilliseconds ?? ResponseTimeMs);
        }

        /// <summary>
        /// Formats the test result for logging
        /// </summary>
        public string ToLogString()
        {
            return $"[{TestedAt:yyyy-MM-dd HH:mm:ss}] Connection {ConnectionId} - {GetTestSummary()}";
        }

        /// <summary>
        /// Creates a snapshot of the connection configuration
        /// </summary>
        public void CreateConnectionSnapshot(Connection connection)
        {
            if (connection != null)
            {
                ConnectionSnapshot = System.Text.Json.JsonSerializer.Serialize(new
                {
                    connection.Name,
                    connection.ServiceName,
                    connection.ConnectionType,
                    connection.BaseUrl,
                    connection.TimeoutSeconds,
                    connection.RetryCount
                });
            }
        }

        /// <summary>
        /// Gets the connection snapshot as a typed object
        /// </summary>
        public ConnectionSnapshot GetConnectionSnapshot()
        {
            if (string.IsNullOrWhiteSpace(ConnectionSnapshot))
            {
                return null;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<ConnectionSnapshot>(ConnectionSnapshot);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets test parameters as a typed object
        /// </summary>
        public Dictionary<string, object> GetTestParameters()
        {
            if (string.IsNullOrWhiteSpace(TestParameters))
            {
                return new Dictionary<string, object>();
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(TestParameters);
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Sets test parameters from a dictionary
        /// </summary>
        public void SetTestParameters(Dictionary<string, object> parameters)
        {
            if (parameters != null && parameters.Any())
            {
                TestParameters = System.Text.Json.JsonSerializer.Serialize(parameters);
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
        /// Gets response data as a typed object
        /// </summary>
        public Dictionary<string, object> GetResponseData()
        {
            if (string.IsNullOrWhiteSpace(ResponseData))
            {
                return new Dictionary<string, object>();
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ResponseData);
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Sets response data from a dictionary
        /// </summary>
        public void SetResponseData(Dictionary<string, object> data)
        {
            if (data != null && data.Any())
            {
                ResponseData = System.Text.Json.JsonSerializer.Serialize(data);
            }
        }
    }

    /// <summary>
    /// Connection snapshot for test logging
    /// </summary>
    public class ConnectionSnapshot
    {
        public string Name { get; set; }
        public string ServiceName { get; set; }
        public ConnectionType ConnectionType { get; set; }
        public string BaseUrl { get; set; }
        public int TimeoutSeconds { get; set; }
        public int RetryCount { get; set; }
    }
}