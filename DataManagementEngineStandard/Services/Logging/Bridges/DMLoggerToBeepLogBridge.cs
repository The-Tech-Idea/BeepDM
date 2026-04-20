using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.Services.Logging.Bridges
{
    /// <summary>
    /// Adapts the legacy <see cref="IDMLogger"/> surface onto an
    /// <see cref="IBeepLog"/> instance so existing call sites
    /// (<c>lg.WriteLog(...)</c>, <c>lg.LogError(...)</c>) flow through the
    /// unified telemetry pipeline without code changes.
    /// </summary>
    /// <remarks>
    /// Phase 09 wires this bridge in place of the original
    /// <c>TheTechIdea.Beep.Logger.DMLogger</c> registration when
    /// <see cref="BeepLoggingOptions.Enabled"/> and
    /// <see cref="BeepLoggingOptions.ReplaceDMLogger"/> are both <c>true</c>.
    /// When the unified logger is disabled the original
    /// <c>DMLogger</c> is left in place so the baseline behavior is unchanged.
    /// The bridge raises <see cref="Onevent"/> for every routed message so
    /// existing UI subscribers (status bars, toast hosts) continue to work.
    /// </remarks>
    public sealed class DMLoggerToBeepLogBridge : IDMLogger
    {
        private const string LegacyCategory = "DMLogger";

        private readonly IBeepLog _log;
        private readonly List<Func<string, bool>> _filters = new List<Func<string, bool>>();
        private readonly object _filtersLock = new object();
        private bool _paused;

        /// <summary>Creates a new bridge that forwards to the supplied beep logger.</summary>
        /// <param name="log">Target beep logger; must not be <c>null</c>.</param>
        public DMLoggerToBeepLogBridge(IBeepLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <inheritdoc />
        public event EventHandler<string> Onevent;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public void WriteLog(string info)
            => Forward(BeepLogLevel.Information, info);

        /// <inheritdoc />
        public void LogError(string error)
            => Forward(BeepLogLevel.Error, error);

        /// <inheritdoc />
        public void LogWarning(string warning)
            => Forward(BeepLogLevel.Warning, warning);

        /// <inheritdoc />
        public void LogInfo(string info)
            => Forward(BeepLogLevel.Information, info);

        /// <inheritdoc />
        public void LogCritical(string error)
            => Forward(BeepLogLevel.Critical, error);

        /// <inheritdoc />
        public void LogDebug(string message)
            => Forward(BeepLogLevel.Debug, message);

        /// <inheritdoc />
        public void LogTrace(string message)
            => Forward(BeepLogLevel.Trace, message);

        /// <inheritdoc />
        public void LogWithContext(string message, object context)
        {
            if (!Accept(message)) return;
            IReadOnlyDictionary<string, object> bag = ToReadOnlyBag(context);
            _log.Log(BeepLogLevel.Information, LegacyCategory, message, bag, null);
            RaiseOnevent(message);
        }

        /// <inheritdoc />
        public void LogStructured(string message, object properties)
        {
            if (!Accept(message)) return;
            IReadOnlyDictionary<string, object> bag = ToReadOnlyBag(properties);
            _log.Log(BeepLogLevel.Information, LegacyCategory, message, bag, null);
            RaiseOnevent(message);
        }

        /// <inheritdoc />
        public void StartLog() => _paused = false;

        /// <inheritdoc />
        public void StopLog() => _paused = true;

        /// <inheritdoc />
        public void PauseLog() => _paused = true;

        /// <inheritdoc />
        public void Flush()
        {
            try
            {
                _log.FlushAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // never throw on the legacy data path
            }
        }

        /// <inheritdoc />
        public void ConfigureLogger(Action<object> configure)
        {
            configure?.Invoke(_log);
        }

        /// <inheritdoc />
        public void AddLogFilter(Func<string, bool> filter)
        {
            if (filter is null)
            {
                return;
            }
            lock (_filtersLock)
            {
                _filters.Add(filter);
            }
        }

        private void Forward(BeepLogLevel level, string message)
        {
            if (!Accept(message)) return;
            _log.Log(level, LegacyCategory, message, null, null);
            RaiseOnevent(message);
        }

        private bool Accept(string message)
        {
            if (_paused) return false;
            if (string.IsNullOrEmpty(message)) return false;
            lock (_filtersLock)
            {
                foreach (Func<string, bool> filter in _filters)
                {
                    try
                    {
                        if (!filter(message)) return false;
                    }
                    catch
                    {
                        // a misbehaving filter must not break the pipeline
                    }
                }
            }
            return true;
        }

        private void RaiseOnevent(string message)
        {
            EventHandler<string> handler = Onevent;
            if (handler is null) return;
            try
            {
                handler(this, message);
            }
            catch
            {
                // subscriber faults must not break the legacy data path
            }
        }

        /// <summary>
        /// Reserved for symmetry with the legacy logger which exposed
        /// <see cref="INotifyPropertyChanged"/>; the bridge has no observable
        /// state so the helper simply raises the event for the named
        /// property when invoked by a future caller.
        /// </summary>
        internal void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler is null) return;
            try
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
            catch
            {
                // ignore subscriber faults
            }
        }

        private static IReadOnlyDictionary<string, object> ToReadOnlyBag(object source)
        {
            if (source is null) return null;

            if (source is IReadOnlyDictionary<string, object> ro)
            {
                return ro;
            }

            if (source is IDictionary<string, object> dict)
            {
                Dictionary<string, object> copy = new Dictionary<string, object>(dict, StringComparer.Ordinal);
                return copy;
            }

            Dictionary<string, object> bag = new Dictionary<string, object>(StringComparer.Ordinal);
            PropertyInfo[] props = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in props)
            {
                if (prop.GetIndexParameters().Length != 0)
                {
                    continue;
                }
                try
                {
                    bag[prop.Name] = prop.GetValue(source);
                }
                catch
                {
                    // skip uncooperative property
                }
            }
            return bag;
        }
    }
}
