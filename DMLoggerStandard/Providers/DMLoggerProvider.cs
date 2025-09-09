using System;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.Logger.Providers
{
    /// <summary>
    /// Logger provider for DMLogger integration with ASP.NET Core logging
    /// </summary>
    public class DMLoggerProvider : ILoggerProvider
    {
        private readonly DMLogger _dmLogger;
        private bool _disposed = false;

        public DMLoggerProvider()
        {
            _dmLogger = new DMLogger();
        }

        public DMLoggerProvider(DMLogger dmLogger)
        {
            _dmLogger = dmLogger ?? throw new ArgumentNullException(nameof(dmLogger));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DMLoggerAdapter(_dmLogger, categoryName);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _dmLogger?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Adapter to wrap DMLogger for category-specific logging
    /// </summary>
    internal class DMLoggerAdapter : ILogger
    {
        private readonly DMLogger _dmLogger;
        private readonly string _categoryName;

        public DMLoggerAdapter(DMLogger dmLogger, string categoryName)
        {
            _dmLogger = dmLogger ?? throw new ArgumentNullException(nameof(dmLogger));
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return _dmLogger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _dmLogger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var categoryMessage = $"[{_categoryName}] {message}";

            // Create a new formatter that includes the category
            Func<TState, Exception?, string> categoryFormatter = (s, ex) => categoryMessage;

            _dmLogger.Log(logLevel, eventId, state, exception, categoryFormatter);
        }
    }
}