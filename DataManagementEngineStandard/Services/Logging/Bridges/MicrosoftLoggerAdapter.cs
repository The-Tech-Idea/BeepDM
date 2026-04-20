using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Services.Telemetry.Context;

namespace TheTechIdea.Beep.Services.Logging.Bridges
{
    /// <summary>
    /// Per-category MEL adapter that forwards every <see cref="ILogger.Log"/>
    /// call to <see cref="IBeepLog"/>. Maps the MEL log level to
    /// <see cref="BeepLogLevel"/>, materialises the formatted message once,
    /// and lifts MEL state into the property bag when it implements
    /// <see cref="IReadOnlyList{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="BeginScope"/> opens a <see cref="BeepActivityScope"/> so
    /// MEL scopes (HTTP request id, EF command id, etc.) automatically
    /// participate in the Beep correlation/trace ids that downstream
    /// enrichers stamp onto every envelope.
    /// </remarks>
    internal sealed class MicrosoftLoggerAdapter : ILogger
    {
        private readonly IBeepLog _log;
        private readonly string _category;

        public MicrosoftLoggerAdapter(IBeepLog log, string category)
        {
            _log = log;
            _category = category;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state is null)
            {
                return MicrosoftLoggerScope.Noop;
            }
            return MicrosoftLoggerScope.Begin(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (!_log.IsEnabled || logLevel == LogLevel.None)
            {
                return false;
            }
            BeepLogLevel mapped = MapLevel(logLevel);
            return mapped >= _log.MinLevel && mapped != BeepLogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            string message = formatter is null ? state?.ToString() : formatter(state, exception);
            if (message is null) return;

            IReadOnlyDictionary<string, object> properties = ExtractProperties(state, eventId);

            _log.Log(MapLevel(logLevel), _category, message, properties, exception);
        }

        private static BeepLogLevel MapLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:       return BeepLogLevel.Trace;
                case LogLevel.Debug:       return BeepLogLevel.Debug;
                case LogLevel.Information: return BeepLogLevel.Information;
                case LogLevel.Warning:     return BeepLogLevel.Warning;
                case LogLevel.Error:       return BeepLogLevel.Error;
                case LogLevel.Critical:    return BeepLogLevel.Critical;
                case LogLevel.None:        return BeepLogLevel.None;
                default:                   return BeepLogLevel.Information;
            }
        }

        private static IReadOnlyDictionary<string, object> ExtractProperties<TState>(TState state, EventId eventId)
        {
            Dictionary<string, object> bag = null;

            if (state is IReadOnlyList<KeyValuePair<string, object>> structured)
            {
                bag = new Dictionary<string, object>(structured.Count + 2, StringComparer.Ordinal);
                for (int i = 0; i < structured.Count; i++)
                {
                    KeyValuePair<string, object> kv = structured[i];
                    if (string.IsNullOrEmpty(kv.Key) || kv.Key == "{OriginalFormat}")
                    {
                        continue;
                    }
                    bag[kv.Key] = kv.Value;
                }
            }

            if (eventId.Id != 0 || !string.IsNullOrEmpty(eventId.Name))
            {
                bag ??= new Dictionary<string, object>(2, StringComparer.Ordinal);
                bag["mel.eventId"] = eventId.Id;
                if (!string.IsNullOrEmpty(eventId.Name))
                {
                    bag["mel.eventName"] = eventId.Name;
                }
            }

            return bag;
        }
    }
}
