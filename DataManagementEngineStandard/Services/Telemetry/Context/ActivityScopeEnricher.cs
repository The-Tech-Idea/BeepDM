using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry.Context
{
    /// <summary>
    /// Surfaces the current <see cref="BeepActivityScope"/> on each envelope:
    /// scope name, start timestamp and any operator-supplied scope tags.
    /// Tags are written under the <c>scope.&lt;tagKey&gt;</c> namespace to
    /// keep them visually distinct from producer-supplied properties.
    /// </summary>
    /// <remarks>
    /// This enricher is a no-op when no scope is active. It does not touch
    /// trace/correlation ids — that is <see cref="TraceEnricher"/>'s job —
    /// so the two enrichers compose without writing the same key twice.
    /// </remarks>
    public sealed class ActivityScopeEnricher : IEnricher
    {
        /// <inheritdoc/>
        public string Name => "activity-scope";

        /// <inheritdoc/>
        public void Enrich(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return;
            }

            BeepActivity scope = BeepActivityScope.Current;
            if (scope is null)
            {
                return;
            }

            if (envelope.Properties is null)
            {
                envelope.Properties = new Dictionary<string, object>();
            }

            if (!envelope.Properties.ContainsKey(EnrichmentProperties.ScopeName))
            {
                envelope.Properties[EnrichmentProperties.ScopeName] = scope.Name;
            }
            if (!envelope.Properties.ContainsKey(EnrichmentProperties.ScopeStartUtc))
            {
                envelope.Properties[EnrichmentProperties.ScopeStartUtc] = scope.StartUtc;
            }

            if (scope.Tags is null || scope.Tags.Count == 0)
            {
                return;
            }
            foreach (var tag in scope.Tags)
            {
                if (string.IsNullOrEmpty(tag.Key))
                {
                    continue;
                }
                string key = string.Concat(EnrichmentProperties.ScopeTagsPrefix, tag.Key);
                if (!envelope.Properties.ContainsKey(key))
                {
                    envelope.Properties[key] = tag.Value;
                }
            }
        }
    }
}
