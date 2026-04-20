namespace TheTechIdea.Beep.Services.Telemetry.Context
{
    /// <summary>
    /// Single source of truth for the property keys written by built-in
    /// enrichers. Centralizing the string constants prevents typo-driven
    /// drift between enrichers and downstream sink schemas / queries.
    /// </summary>
    /// <remarks>
    /// Keys are <c>camelCase</c> for OTel compatibility — most exporters
    /// already expect this casing for the trace/correlation triplet.
    /// </remarks>
    internal static class EnrichmentProperties
    {
        public const string CorrelationId = "correlationId";

        public const string TraceId = "traceId";
        public const string SpanId = "spanId";
        public const string ParentSpanId = "parentSpanId";

        public const string ScopeName = "scopeName";
        public const string ScopeStartUtc = "scopeStartUtc";
        public const string ScopeTagsPrefix = "scope.";

        public const string Machine = "machine";
        public const string ProcessId = "processId";
        public const string ProcessName = "processName";
        public const string ThreadId = "threadId";

        public const string EnvName = "envName";
        public const string Region = "region";
        public const string AppVersion = "appVersion";
        public const string AppRepoName = "appRepoName";

        public const string Tenant = "tenant";

        public const string UserId = "userId";
        public const string UserName = "userName";
    }
}
