using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.WebAPI.Helpers
{
    /// <summary>
    /// Error handling and retry logic helper for Web API operations
    /// Implements exponential backoff, circuit breaker pattern, and comprehensive error categorization
    /// </summary>
    public class WebAPIErrorHelper : IDisposable
    {
        #region Private Fields

        private readonly IDMLogger _logger;
        private readonly string _datasourceName;
        private readonly Dictionary<string, CircuitBreaker> _circuitBreakers;
        private readonly object _lockObject = new object();
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>Default maximum retry attempts</summary>
        public int DefaultMaxRetries { get; set; } = 3;

        /// <summary>Default base delay for exponential backoff in milliseconds</summary>
        public int DefaultBaseDelayMs { get; set; } = 1000;

        /// <summary>Default circuit breaker failure threshold</summary>
        public int DefaultFailureThreshold { get; set; } = 5;

        /// <summary>Default circuit breaker recovery timeout in seconds</summary>
        public int DefaultRecoveryTimeoutSeconds { get; set; } = 60;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the error helper
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="datasourceName">Data source name for logging</param>
        public WebAPIErrorHelper(IDMLogger logger, string datasourceName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _datasourceName = datasourceName ?? throw new ArgumentNullException(nameof(datasourceName));
            _circuitBreakers = new Dictionary<string, CircuitBreaker>();
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Handles and logs an error from an HttpResponseMessage.
        /// </summary>
        /// <param name="response">The HTTP response message.</param>
        public async Task HandleErrorResponseAsync(HttpResponseMessage response)
        {
            var errorInfo = await CategorizeErrorAsync(response);
            _logger.WriteLog($"Web API Error: {errorInfo.Category} - {errorInfo.Message}");
            // You can also update a shared error object here if needed
        }
        /// <summary>
        /// Analyzes an HttpResponseMessage to categorize the error.
        /// </summary>
        /// <param name="response">The HTTP response to analyze.</param>
        /// <returns>A WebApiError object with categorized error information.</returns>
        public async Task<WebApiError> CategorizeErrorAsync(HttpResponseMessage response)
        {
            var error = new WebApiError
            {
                StatusCode = response.StatusCode,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                error.Content = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                error.Content = "Could not read response content.";
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    error.Category = WebApiErrorCategory.ClientError;
                    error.Message = "Bad Request: The server could not understand the request.";
                    break;
                case HttpStatusCode.Unauthorized:
                    error.Category = WebApiErrorCategory.AuthenticationError;
                    error.Message = "Unauthorized: Authentication is required and has failed or has not yet been provided.";
                    break;
                case HttpStatusCode.Forbidden:
                    error.Category = WebApiErrorCategory.AuthorizationError;
                    error.Message = "Forbidden: The server understood the request, but is refusing to fulfill it.";
                    break;
                case HttpStatusCode.NotFound:
                    error.Category = WebApiErrorCategory.ClientError;
                    error.Message = "Not Found: The requested resource could not be found.";
                    break;
                case HttpStatusCode.InternalServerError:
                    error.Category = WebApiErrorCategory.ServerError;
                    error.Message = "Internal Server Error: The server encountered an unexpected condition.";
                    break;
                case HttpStatusCode.ServiceUnavailable:
                    error.Category = WebApiErrorCategory.ServerError;
                    error.Message = "Service Unavailable: The server is currently unable to handle the request.";
                    break;
                default:
                    error.Category = WebApiErrorCategory.Unknown;
                    error.Message = $"An unexpected error occurred with status code {response.StatusCode}.";
                    break;
            }

            return error;
        }
        /// <summary>
        /// Executes an operation with retry logic and circuit breaker protection
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <param name="maxRetries">Maximum retry attempts</param>
        /// <param name="baseDelayMs">Base delay for exponential backoff</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        public async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            int maxRetries = 0,
            int baseDelayMs = 0,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WebAPIErrorHelper));

            var retries = maxRetries > 0 ? maxRetries : DefaultMaxRetries;
            var delay = baseDelayMs > 0 ? baseDelayMs : DefaultBaseDelayMs;
            
            var circuitBreaker = GetOrCreateCircuitBreaker(operationName);

            for (int attempt = 0; attempt <= retries; attempt++)
            {
                try
                {
                    // Check circuit breaker
                    if (!circuitBreaker.CanExecute())
                    {
                        throw new InvalidOperationException($"Circuit breaker is open for {operationName}");
                    }

                    var result = await operation();
                    
                    // Success - reset circuit breaker
                    circuitBreaker.OnSuccess();
                    return result;
                }
                catch (Exception ex)
                {
                    // Record failure
                    circuitBreaker.OnFailure();

                    var errorInfo = AnalyzeError(ex);
                    _logger?.WriteLog($"Attempt {attempt + 1} failed for {operationName}: {errorInfo.ErrorMessage}");

                    // Don't retry on certain error types
                    if (!errorInfo.IsRetryable || attempt >= retries)
                    {
                        _logger?.WriteLog($"Operation {operationName} failed permanently: {errorInfo.ErrorMessage}");
                        throw;
                    }

                    // Calculate exponential backoff delay
                    var currentDelay = CalculateDelay(attempt, delay);
                    _logger?.WriteLog($"Retrying {operationName} in {currentDelay}ms (attempt {attempt + 1}/{retries + 1})");

                    await Task.Delay(currentDelay, cancellationToken);
                }
            }

            throw new InvalidOperationException($"All retry attempts exhausted for {operationName}");
        }

        /// <summary>
        /// Analyzes an exception and returns error information
        /// </summary>
        /// <param name="exception">Exception to analyze</param>
        /// <returns>Error analysis result</returns>
        public ErrorAnalysisResult AnalyzeError(Exception exception)
        {
            var result = new ErrorAnalysisResult
            {
                OriginalException = exception,
                ErrorMessage = exception.Message,
                ErrorType = exception.GetType().Name,
                Timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case HttpRequestException httpEx:
                    result.Category = ErrorCategory.Network;
                    result.IsRetryable = true;
                    result.SuggestedAction = "Check network connectivity and API availability";
                    break;

                case TaskCanceledException timeoutEx when timeoutEx.InnerException is TimeoutException:
                    result.Category = ErrorCategory.Timeout;
                    result.IsRetryable = true;
                    result.SuggestedAction = "Increase timeout or check API performance";
                    break;

                case UnauthorizedAccessException authEx:
                    result.Category = ErrorCategory.Authentication;
                    result.IsRetryable = false;
                    result.SuggestedAction = "Verify API credentials and authentication method";
                    break;

                case ArgumentException argEx:
                    result.Category = ErrorCategory.Configuration;
                    result.IsRetryable = false;
                    result.SuggestedAction = "Check API configuration and parameters";
                    break;

                case InvalidOperationException opEx:
                    result.Category = ErrorCategory.Logic;
                    result.IsRetryable = false;
                    result.SuggestedAction = "Review operation logic and data state";
                    break;

                default:
                    // Handle WebException and other exceptions here
                    if (exception is WebException webEx)
                    {
                        result.Category = ErrorCategory.Network;
                        result.IsRetryable = IsRetryableWebException(webEx);
                        result.SuggestedAction = GetWebExceptionSuggestion(webEx);
                    }
                    else
                    {
                        result.Category = ErrorCategory.Unknown;
                        result.IsRetryable = false;
                        result.SuggestedAction = "Review error details and contact support if needed";
                    }
                    break;
            }

            // Log detailed error information
            _logger?.WriteLog($"Error Analysis - Type: {result.ErrorType}, Category: {result.Category}, " +
                            $"Retryable: {result.IsRetryable}, Message: {result.ErrorMessage}");

            return result;
        }

        /// <summary>
        /// Gets circuit breaker status for an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Circuit breaker status</returns>
        public CircuitBreakerStatus GetCircuitBreakerStatus(string operationName)
        {
            lock (_lockObject)
            {
                if (_circuitBreakers.ContainsKey(operationName))
                {
                    return _circuitBreakers[operationName].GetStatus();
                }

                return new CircuitBreakerStatus
                {
                    OperationName = operationName,
                    State = CircuitBreakerState.Closed,
                    FailureCount = 0,
                    LastFailureTime = null
                };
            }
        }

        /// <summary>
        /// Resets circuit breaker for an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        public void ResetCircuitBreaker(string operationName)
        {
            lock (_lockObject)
            {
                if (_circuitBreakers.ContainsKey(operationName))
                {
                    _circuitBreakers[operationName].Reset();
                    _logger?.WriteLog($"Circuit breaker reset for {operationName}");
                }
            }
        }

        #endregion

        #region Private Methods

        private CircuitBreaker GetOrCreateCircuitBreaker(string operationName)
        {
            lock (_lockObject)
            {
                if (!_circuitBreakers.ContainsKey(operationName))
                {
                    _circuitBreakers[operationName] = new CircuitBreaker(
                        DefaultFailureThreshold,
                        TimeSpan.FromSeconds(DefaultRecoveryTimeoutSeconds),
                        _logger);
                }

                return _circuitBreakers[operationName];
            }
        }

        private int CalculateDelay(int attempt, int baseDelay)
        {
            // Exponential backoff with jitter
            var exponentialDelay = baseDelay * Math.Pow(2, attempt);
            var jitter = new Random().Next(0, (int)(exponentialDelay * 0.1));
            return (int)(exponentialDelay + jitter);
        }

        private bool IsRetryableWebException(WebException webEx)
        {
            if (webEx.Response is HttpWebResponse response)
            {
                var statusCode = response.StatusCode;
                
                // Retry on server errors and certain client errors
                return statusCode == HttpStatusCode.InternalServerError ||
                       statusCode == HttpStatusCode.BadGateway ||
                       statusCode == HttpStatusCode.ServiceUnavailable ||
                       statusCode == HttpStatusCode.GatewayTimeout ||
                       statusCode == HttpStatusCode.TooManyRequests ||
                       statusCode == HttpStatusCode.RequestTimeout;
            }

            return true; // Retry on network-level issues
        }

        private string GetWebExceptionSuggestion(WebException webEx)
        {
            if (webEx.Response is HttpWebResponse response)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        return "Check API authentication credentials";
                    case HttpStatusCode.Forbidden:
                        return "Verify API permissions and authorization";
                    case HttpStatusCode.NotFound:
                        return "Check API endpoint URL and resource path";
                    case HttpStatusCode.TooManyRequests:
                        return "Reduce request rate or implement rate limiting";
                    case HttpStatusCode.InternalServerError:
                        return "Check API server status and try again later";
                    default:
                        return $"HTTP {(int)response.StatusCode}: {response.StatusDescription}";
                }
            }

            return webEx.Status.ToString();
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources used by the error helper
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lockObject)
                {
                    foreach (var circuitBreaker in _circuitBreakers.Values)
                    {
                        circuitBreaker.Dispose();
                    }
                    _circuitBreakers.Clear();
                }

                _disposed = true;
            }
        }

        #endregion

        #region Helper Classes and Enums

        /// <summary>
        /// Web API error information
        /// </summary>
        public class WebApiError
        {
            /// <summary>Error category</summary>
            public WebApiErrorCategory Category { get; set; }
            
            /// <summary>Error message</summary>
            public string Message { get; set; }
            
            /// <summary>HTTP status code</summary>
            public System.Net.HttpStatusCode StatusCode { get; set; }
            
            /// <summary>Response content</summary>
            public string Content { get; set; }
            
            /// <summary>Error timestamp</summary>
            public DateTime Timestamp { get; set; }
        }

        /// <summary>
        /// Web API error category enumeration
        /// </summary>
        public enum WebApiErrorCategory
        {
            /// <summary>Client-side errors (4xx)</summary>
            ClientError,
            
            /// <summary>Authentication errors</summary>
            AuthenticationError,
            
            /// <summary>Authorization errors</summary>
            AuthorizationError,
            
            /// <summary>Server-side errors (5xx)</summary>
            ServerError,
            
            /// <summary>Network or connectivity errors</summary>
            NetworkError,
            
            /// <summary>Unknown or uncategorized errors</summary>
            Unknown
        }

        /// <summary>
        /// Error analysis result
        /// </summary>
        public class ErrorAnalysisResult
        {
            /// <summary>Original exception</summary>
            public Exception OriginalException { get; set; }
            
            /// <summary>Error message</summary>
            public string ErrorMessage { get; set; }
            
            /// <summary>Error type name</summary>
            public string ErrorType { get; set; }
            
            /// <summary>Error category</summary>
            public ErrorCategory Category { get; set; }
            
            /// <summary>Whether the error is retryable</summary>
            public bool IsRetryable { get; set; }
            
            /// <summary>Suggested action to resolve the error</summary>
            public string SuggestedAction { get; set; }
            
            /// <summary>Error timestamp</summary>
            public DateTime Timestamp { get; set; }
        }

        /// <summary>
        /// Circuit breaker state enumeration
        /// </summary>
        public enum CircuitBreakerState
        {
            /// <summary>Circuit breaker is closed (normal operation)</summary>
            Closed,
            
            /// <summary>Circuit breaker is open (failing fast)</summary>
            Open,
            
            /// <summary>Circuit breaker is half-open (testing recovery)</summary>
            HalfOpen
        }

        /// <summary>
        /// Circuit breaker status information
        /// </summary>
        public class CircuitBreakerStatus
        {
            /// <summary>Operation name</summary>
            public string OperationName { get; set; }
            
            /// <summary>Current state</summary>
            public CircuitBreakerState State { get; set; }
            
            /// <summary>Failure count</summary>
            public int FailureCount { get; set; }
            
            /// <summary>Last failure time</summary>
            public DateTime? LastFailureTime { get; set; }
        }

        /// <summary>
        /// Error category enumeration
        /// </summary>
        public enum ErrorCategory
        {
            /// <summary>Network-related errors</summary>
            Network,
            /// <summary>Authentication/authorization errors</summary>
            Authentication,
            /// <summary>Configuration errors</summary>
            Configuration,
            /// <summary>Timeout errors</summary>
            Timeout,
            /// <summary>Logic errors</summary>
            Logic,
            /// <summary>Unknown errors</summary>
            Unknown
        }

        /// <summary>
        /// Circuit breaker implementation with state management
        /// </summary>
        private class CircuitBreaker : IDisposable
        {
            private readonly int _failureThreshold;
            private readonly TimeSpan _recoveryTimeout;
            private readonly IDMLogger _logger;
            private readonly object _lock = new object();
            
            private CircuitBreakerState _state = CircuitBreakerState.Closed;
            private int _failureCount = 0;
            private DateTime _lastFailureTime = DateTime.MinValue;

            public CircuitBreaker(int failureThreshold, TimeSpan recoveryTimeout, IDMLogger logger)
            {
                _failureThreshold = failureThreshold;
                _recoveryTimeout = recoveryTimeout;
                _logger = logger;
            }

            public bool CanExecute()
            {
                lock (_lock)
                {
                    switch (_state)
                    {
                        case CircuitBreakerState.Closed:
                            return true;

                        case CircuitBreakerState.Open:
                            if (DateTime.UtcNow - _lastFailureTime >= _recoveryTimeout)
                            {
                                _state = CircuitBreakerState.HalfOpen;
                                _logger?.WriteLog("Circuit breaker transitioned to Half-Open state");
                                return true;
                            }
                            return false;

                        case CircuitBreakerState.HalfOpen:
                            return true;

                        default:
                            return false;
                    }
                }
            }

            public void OnSuccess()
            {
                lock (_lock)
                {
                    _failureCount = 0;
                    if (_state != CircuitBreakerState.Closed)
                    {
                        _state = CircuitBreakerState.Closed;
                        _logger?.WriteLog("Circuit breaker transitioned to Closed state");
                    }
                }
            }

            public void OnFailure()
            {
                lock (_lock)
                {
                    _failureCount++;
                    _lastFailureTime = DateTime.UtcNow;

                    if (_state == CircuitBreakerState.HalfOpen || _failureCount >= _failureThreshold)
                    {
                        _state = CircuitBreakerState.Open;
                        _logger?.WriteLog($"Circuit breaker opened after {_failureCount} failures");
                    }
                }
            }

            public void Reset()
            {
                lock (_lock)
                {
                    _failureCount = 0;
                    _state = CircuitBreakerState.Closed;
                }
            }

            public CircuitBreakerStatus GetStatus()
            {
                lock (_lock)
                {
                    return new CircuitBreakerStatus
                    {
                        State = _state,
                        FailureCount = _failureCount,
                        LastFailureTime = _lastFailureTime == DateTime.MinValue ? null : _lastFailureTime
                    };
                }
            }

            public void Dispose()
            {
                // No managed resources to dispose
            }
        }

        #endregion
    }
}
