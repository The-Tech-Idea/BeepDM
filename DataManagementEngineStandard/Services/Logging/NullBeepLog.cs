using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Services.Logging;

namespace TheTechIdea.Beep.Services.Logging
{
    /// <summary>
    /// Default <see cref="IBeepLog"/> used when the logging feature is disabled
    /// or before Phase 02 ships the production pipeline. Every method is a
    /// fast no-op so callers never have to null-check the dependency.
    /// </summary>
    public sealed class NullBeepLog : IBeepLog
    {
        /// <summary>Singleton instance suitable for direct DI registration.</summary>
        public static readonly NullBeepLog Instance = new NullBeepLog();

        /// <inheritdoc />
        public bool IsEnabled => false;

        /// <inheritdoc />
        public BeepLogLevel MinLevel => BeepLogLevel.None;

        /// <inheritdoc />
        public void Log(
            BeepLogLevel level,
            string category,
            string message,
            IReadOnlyDictionary<string, object> properties = null,
            Exception exception = null)
        {
            // intentionally empty
        }

        /// <inheritdoc />
        public void Trace(string message, object properties = null) { }

        /// <inheritdoc />
        public void Debug(string message, object properties = null) { }

        /// <inheritdoc />
        public void Info(string message, object properties = null) { }

        /// <inheritdoc />
        public void Warn(string message, object properties = null) { }

        /// <inheritdoc />
        public void Error(string message, Exception ex = null, object properties = null) { }

        /// <inheritdoc />
        public void Critical(string message, Exception ex = null, object properties = null) { }

        /// <inheritdoc />
        public Task FlushAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
