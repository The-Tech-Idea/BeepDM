using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Telemetry.Context;

namespace TheTechIdea.Beep.Services.Logging.Bridges
{
    /// <summary>
    /// Bridges <see cref="Microsoft.Extensions.Logging.ILogger.BeginScope{TState}(TState)"/>
    /// onto <see cref="BeepActivityScope"/> so MEL scopes (HTTP request id,
    /// EF command id, ASP.NET Core <c>{RequestPath}</c>) automatically flow
    /// into the Beep correlation/trace stack.
    /// </summary>
    /// <remarks>
    /// Returned <see cref="IDisposable"/> handles always succeed and never
    /// throw; if the scope state is empty or unrecognised a no-op handle is
    /// returned. The scope name defaults to <c>"mel.scope"</c> when no
    /// better name can be derived from the state.
    /// </remarks>
    internal static class MicrosoftLoggerScope
    {
        private const string DefaultScopeName = "mel.scope";

        public static IDisposable Noop { get; } = NoopHandle.Instance;

        public static IDisposable Begin<TState>(TState state)
        {
            string name;
            IDictionary<string, object> tags = null;

            if (state is IReadOnlyList<KeyValuePair<string, object>> structured && structured.Count > 0)
            {
                tags = new Dictionary<string, object>(structured.Count, StringComparer.Ordinal);
                string template = null;
                for (int i = 0; i < structured.Count; i++)
                {
                    KeyValuePair<string, object> kv = structured[i];
                    if (string.IsNullOrEmpty(kv.Key)) continue;
                    if (kv.Key == "{OriginalFormat}")
                    {
                        template = kv.Value as string;
                        continue;
                    }
                    tags[kv.Key] = kv.Value;
                }
                name = string.IsNullOrEmpty(template) ? DefaultScopeName : template;
            }
            else
            {
                name = state?.ToString();
                if (string.IsNullOrEmpty(name))
                {
                    name = DefaultScopeName;
                }
            }

            return BeepActivityScope.Begin(name, tags);
        }

        private sealed class NoopHandle : IDisposable
        {
            public static readonly NoopHandle Instance = new NoopHandle();
            public void Dispose() { }
        }
    }
}
