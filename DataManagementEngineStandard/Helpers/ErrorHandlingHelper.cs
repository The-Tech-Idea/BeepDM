using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Helper class providing consistent error handling, logging, and exception management
    /// across the data management engine with structured error reporting and recovery strategies.
    /// </summary>
    public static class ErrorHandlingHelper
    {
        private static readonly object _logLock = new object();

        /// <summary>
        /// Handles an exception with consistent logging and error reporting.
        /// </summary>
        /// <param name="ex">Exception to handle</param>
        /// <param name="context">Contextual information about where the error occurred</param>
        /// <param name="editor">DME Editor instance for logging</param>
        /// <param name="includeStackTrace">Whether to include stack trace in logging</param>
        public static void HandleException(Exception ex, string context, IDMEEditor editor, bool includeStackTrace = true)
        {
            if (ex == null || editor == null)
                return;

            try
            {
                lock (_logLock)
                {
                    var errorMessage = BuildErrorMessage(ex, context, includeStackTrace);
                    
                    // Log the structured error
                    editor.AddLogMessage("Error", errorMessage, DateTime.Now, 0, context, Errors.Failed);

                    // Update error object with detailed information
                    if (editor.ErrorObject != null)
                    {
                        editor.ErrorObject.Flag = Errors.Failed;
                        editor.ErrorObject.Message = errorMessage;
                        editor.ErrorObject.Ex = ex;
                    }
                }
            }
            catch (Exception loggingEx)
            {
                // Fallback to console if logging fails
                Console.WriteLine($"Logging failed: {loggingEx.Message}. Original error: {ex.Message} in context: {context}");
            }
        }

        /// <summary>
        /// Executes an async operation with comprehensive error handling and recovery.
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">Async operation to execute</param>
        /// <param name="context">Context description for error reporting</param>
        /// <param name="editor">DME Editor instance for logging</param>
        /// <param name="defaultValue">Default value to return on error</param>
        /// <param name="retryCount">Number of retry attempts</param>
        /// <returns>Result of operation or default value on error</returns>
        public static async Task<T> ExecuteWithErrorHandlingAsync<T>(
            Func<Task<T>> operation, 
            string context, 
            IDMEEditor editor = null,
            T defaultValue = default,
            int retryCount = 0)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var attempt = 0;
            var maxAttempts = Math.Max(1, retryCount + 1);
            Exception lastException = null;

            while (attempt < maxAttempts)
            {
                try
                {
                    var result = await operation();
                    
                    // Log successful retry if this wasn't the first attempt
                    if (attempt > 0 && editor != null)
                    {
                        editor.AddLogMessage("Info", $"Operation succeeded on attempt {attempt + 1}: {context}", 
                            DateTime.Now, 0, context, Errors.Ok);
                    }
                    
                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;

                    if (attempt < maxAttempts)
                    {
                        // Log retry attempt
                        editor?.AddLogMessage("Warning", $"Operation failed on attempt {attempt}, retrying: {context} - {ex.Message}", 
                            DateTime.Now, 0, context, Errors.Ok);
                        
                        // Wait before retry with exponential backoff
                        var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1));
                        await Task.Delay(delay);
                    }
                }
            }

            // All attempts failed, handle the final error
            if (editor != null && lastException != null)
            {
                HandleException(lastException, $"{context} (failed after {maxAttempts} attempts)", editor);
            }

            return defaultValue;
        }

        /// <summary>
        /// Executes a synchronous operation with comprehensive error handling.
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="context">Context description for error reporting</param>
        /// <param name="editor">DME Editor instance for logging</param>
        /// <param name="defaultValue">Default value to return on error</param>
        /// <returns>Result of operation or default value on error</returns>
        public static T ExecuteWithErrorHandling<T>(
            Func<T> operation, 
            string context, 
            IDMEEditor editor = null,
            T defaultValue = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                if (editor != null)
                {
                    HandleException(ex, context, editor);
                }
                return defaultValue;
            }
        }

        /// <summary>
        /// Creates a comprehensive error info object from an exception.
        /// </summary>
        /// <param name="ex">Exception to create error info from</param>
        /// <param name="context">Contextual information</param>
        /// <returns>Structured error information object</returns>
        public static IErrorsInfo CreateErrorInfo(Exception ex, string context)
        {
            var errorInfo = new ErrorsInfo();
            
            if (ex != null)
            {
                errorInfo.Flag = Errors.Failed;
                errorInfo.Ex = ex;
                errorInfo.Message = BuildErrorMessage(ex, context, false);
            }
            else
            {
                errorInfo.Flag = Errors.Ok;
                errorInfo.Message = $"No error in context: {context}";
            }

            return errorInfo;
        }

        /// <summary>
        /// Logs a structured exception with detailed information.
        /// </summary>
        /// <param name="ex">Exception to log</param>
        /// <param name="context">Context where the exception occurred</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="includeStackTrace">Whether to include stack trace</param>
        public static void LogStructuredException(Exception ex, string context, IDMLogger logger, bool includeStackTrace = true)
        {
            if (ex == null || logger == null)
                return;

            try
            {
                var errorMessage = BuildErrorMessage(ex, context, includeStackTrace);
                logger.WriteLog(errorMessage);
            }
            catch (Exception loggingEx)
            {
                Console.WriteLine($"Failed to log exception: {loggingEx.Message}. Original: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely executes a disposal operation with error handling.
        /// </summary>
        /// <param name="disposable">Object to dispose</param>
        /// <param name="objectName">Name of the object for logging</param>
        /// <param name="editor">DME Editor for logging</param>
        public static void SafeDispose(IDisposable disposable, string objectName, IDMEEditor editor = null)
        {
            if (disposable == null)
                return;

            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                var context = $"Disposing {objectName}";
                if (editor != null)
                {
                    HandleException(ex, context, editor, false); // Don't include stack trace for disposal errors
                }
                else
                {
                    Console.WriteLine($"Error disposing {objectName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Validates that an operation completed successfully and throws if not.
        /// </summary>
        /// <param name="condition">Condition that should be true for success</param>
        /// <param name="errorMessage">Error message if condition is false</param>
        /// <param name="context">Context for the validation</param>
        public static void EnsureSuccess(bool condition, string errorMessage, string context = null)
        {
            if (!condition)
            {
                var fullMessage = string.IsNullOrEmpty(context) 
                    ? errorMessage 
                    : $"{errorMessage} (Context: {context})";
                throw new InvalidOperationException(fullMessage);
            }
        }

        /// <summary>
        /// Wraps an action with a try-catch that suppresses exceptions.
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="context">Context for error reporting</param>
        /// <param name="editor">Editor for logging (optional)</param>
        /// <returns>True if action completed successfully, false if an exception was caught</returns>
        public static bool TryExecute(Action action, string context, IDMEEditor editor = null)
        {
            if (action == null)
                return false;

            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                if (editor != null)
                {
                    HandleException(ex, context, editor, false);
                }
                return false;
            }
        }

        /// <summary>
        /// Gets a user-friendly error message from an exception.
        /// </summary>
        /// <param name="ex">Exception to get message from</param>
        /// <returns>User-friendly error message</returns>
        public static string GetUserFriendlyMessage(Exception ex)
        {
            if (ex == null)
                return "An unknown error occurred";

            switch (ex)
            {
                case ArgumentNullException _:
                    return "A required parameter was not provided";
                case ArgumentException _:
                    return "An invalid parameter was provided";
                case InvalidOperationException _:
                    return "The operation could not be completed in the current state";
                case NotSupportedException _:
                    return "This operation is not supported";
                case TimeoutException _:
                    return "The operation timed out";
                case UnauthorizedAccessException _:
                    return "Access was denied";
                case System.IO.FileNotFoundException _:
                    return "A required file could not be found";
                case System.IO.DirectoryNotFoundException _:
                    return "A required directory could not be found";
                case System.Net.NetworkInformation.NetworkInformationException _:
                    return "A network error occurred";
                default:
                    return $"An error occurred: {ex.Message}";
            }
        }

        #region Private Helper Methods

        private static string BuildErrorMessage(Exception ex, string context, bool includeStackTrace)
        {
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error in {context}: {ex.Message}";

            if (ex.InnerException != null)
            {
                message += $" | Inner Exception: {ex.InnerException.Message}";
            }

            if (includeStackTrace && !string.IsNullOrEmpty(ex.StackTrace))
            {
                // Get the most relevant part of the stack trace
                var stackLines = ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                var relevantLine = Array.Find(stackLines, line => 
                    line.Contains("DMEEditor") || 
                    line.Contains("DataSource") || 
                    line.Contains("Helper"));
                
                if (!string.IsNullOrEmpty(relevantLine))
                {
                    message += $" | Stack: {relevantLine.Trim()}";
                }
            }

            // Add process and thread information for debugging
            message += $" | Process: {Process.GetCurrentProcess().ProcessName}";
            message += $" | Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}";

            return message;
        }

        #endregion
    }
}
