using System;
using System.Collections.Generic;
using System.Reflection;
using TheTechIdea.Beep.Services.Telemetry;

namespace TheTechIdea.Beep.Services.Logging
{
    /// <summary>
    /// Production <see cref="IBeepLog"/> backed by the shared
    /// <see cref="TelemetryPipeline"/>. Created by
    /// <c>AddBeepLogging</c> when <see cref="BeepLoggingOptions.Enabled"/>
    /// is <c>true</c>. When disabled, callers receive
    /// <see cref="NullBeepLog"/> instead.
    /// </summary>
    /// <remarks>
    /// The class is split across three partial files:
    /// <list type="bullet">
    ///   <item><c>.Core</c> — fields, ctor, <see cref="Log"/> entry point.</item>
    ///   <item><c>.Levels</c> — Trace/Debug/Info/Warn/Error/Critical wrappers.</item>
    ///   <item><c>.Lifetime</c> — flush / dispose semantics.</item>
    /// </list>
    /// </remarks>
    public sealed partial class BeepLog : IBeepLog
    {
        private readonly TelemetryPipeline _pipeline;
        private readonly BeepLoggingOptions _options;

        /// <summary>Creates a new logger bound to the supplied pipeline.</summary>
        public BeepLog(BeepLoggingOptions options, TelemetryPipeline pipeline)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        }

        /// <inheritdoc />
        public bool IsEnabled => _options.Enabled;

        /// <inheritdoc />
        public BeepLogLevel MinLevel => _options.MinLevel;

        /// <inheritdoc />
        public void Log(
            BeepLogLevel level,
            string category,
            string message,
            IReadOnlyDictionary<string, object> properties = null,
            Exception exception = null)
        {
            if (!IsEnabled || level < _options.MinLevel || level == BeepLogLevel.None)
            {
                return;
            }

            TelemetryEnvelope envelope = new TelemetryEnvelope
            {
                Kind = TelemetryKind.Log,
                Level = level,
                Category = category,
                Message = message,
                Exception = exception,
                Properties = properties is null ? null : CopyProperties(properties)
            };

            _pipeline.SubmitLog(envelope);
        }

        private static IDictionary<string, object> CopyProperties(IReadOnlyDictionary<string, object> source)
        {
            Dictionary<string, object> bag = new Dictionary<string, object>(source.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, object> kvp in source)
            {
                bag[kvp.Key] = kvp.Value;
            }
            return bag;
        }

        private static IDictionary<string, object> ToBag(object properties)
        {
            if (properties is null)
            {
                return null;
            }

            if (properties is IDictionary<string, object> dict)
            {
                return new Dictionary<string, object>(dict, StringComparer.Ordinal);
            }

            if (properties is IReadOnlyDictionary<string, object> ro)
            {
                return CopyProperties(ro);
            }

            // Anonymous object / POCO — reflect public instance properties once.
            Dictionary<string, object> bag = new Dictionary<string, object>(StringComparer.Ordinal);
            PropertyInfo[] props = properties.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in props)
            {
                if (prop.GetIndexParameters().Length != 0)
                {
                    continue;
                }
                try
                {
                    bag[prop.Name] = prop.GetValue(properties);
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
