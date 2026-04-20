using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TheTechIdea.Beep.Services.Logging
{
    /// <summary>
    /// Convenience-level wrappers for <see cref="BeepLog"/>. Each method
    /// short-circuits when <see cref="BeepLog.IsEnabled"/> is <c>false</c>
    /// or the configured minimum level filters the call out.
    /// </summary>
    public sealed partial class BeepLog
    {
        /// <inheritdoc />
        public void Trace(string message, object properties = null)
            => LogConvenience(BeepLogLevel.Trace, message, properties, null);

        /// <inheritdoc />
        public void Debug(string message, object properties = null)
            => LogConvenience(BeepLogLevel.Debug, message, properties, null);

        /// <inheritdoc />
        public void Info(string message, object properties = null)
            => LogConvenience(BeepLogLevel.Information, message, properties, null);

        /// <inheritdoc />
        public void Warn(string message, object properties = null)
            => LogConvenience(BeepLogLevel.Warning, message, properties, null);

        /// <inheritdoc />
        public void Error(string message, Exception ex = null, object properties = null)
            => LogConvenience(BeepLogLevel.Error, message, properties, ex);

        /// <inheritdoc />
        public void Critical(string message, Exception ex = null, object properties = null)
            => LogConvenience(BeepLogLevel.Critical, message, properties, ex);

        private void LogConvenience(BeepLogLevel level, string message, object properties, Exception exception)
        {
            if (!IsEnabled || level < MinLevel)
            {
                return;
            }

            IDictionary<string, object> bag = ToBag(properties);
            IReadOnlyDictionary<string, object> ro = bag is null
                ? null
                : new ReadOnlyDictionary<string, object>(bag);
            Log(level, category: null, message: message, properties: ro, exception: exception);
        }
    }
}
