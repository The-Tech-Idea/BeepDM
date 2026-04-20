using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry.Context
{
    /// <summary>
    /// Stamps environment metadata onto every envelope: deployment name
    /// (e.g. <c>"prod"</c> / <c>"staging"</c>), region, application version
    /// and Beep app-repo name. Values are captured at construction time so
    /// the hot path stays allocation-free.
    /// </summary>
    /// <remarks>
    /// All four fields are optional. Empty strings are skipped to avoid
    /// littering the property bag with blank entries when a deployment does
    /// not set them.
    /// </remarks>
    public sealed class EnvironmentEnricher : IEnricher
    {
        private readonly string _envName;
        private readonly string _region;
        private readonly string _appVersion;
        private readonly string _appRepoName;

        /// <summary>
        /// Creates an environment enricher with the supplied static metadata.
        /// </summary>
        /// <param name="envName">Deployment environment name (e.g. <c>"prod"</c>).</param>
        /// <param name="region">Logical region (e.g. <c>"eu-west-1"</c>).</param>
        /// <param name="appVersion">Application semantic version.</param>
        /// <param name="appRepoName">Beep app-repo identifier from <see cref="IBeepService"/>.</param>
        public EnvironmentEnricher(string envName = null,
            string region = null,
            string appVersion = null,
            string appRepoName = null)
        {
            _envName = envName ?? Environment.GetEnvironmentVariable("BEEP_ENV") ?? string.Empty;
            _region = region ?? Environment.GetEnvironmentVariable("BEEP_REGION") ?? string.Empty;
            _appVersion = appVersion ?? string.Empty;
            _appRepoName = appRepoName ?? string.Empty;
        }

        /// <inheritdoc/>
        public string Name => "environment";

        /// <inheritdoc/>
        public void Enrich(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return;
            }
            if (envelope.Properties is null)
            {
                envelope.Properties = new Dictionary<string, object>();
            }
            WriteIfPresent(envelope.Properties, EnrichmentProperties.EnvName, _envName);
            WriteIfPresent(envelope.Properties, EnrichmentProperties.Region, _region);
            WriteIfPresent(envelope.Properties, EnrichmentProperties.AppVersion, _appVersion);
            WriteIfPresent(envelope.Properties, EnrichmentProperties.AppRepoName, _appRepoName);
        }

        private static void WriteIfPresent(IDictionary<string, object> bag, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            if (!bag.ContainsKey(key))
            {
                bag[key] = value;
            }
        }
    }
}
