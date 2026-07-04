using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.SchemaMigration
{
    /// <summary>
    /// Builds <see cref="IErrorsInfo"/> outcomes for schema-migration providers. Shared so every
    /// provider (engine-resident bases and datasource-colocated overrides) returns consistent
    /// error shapes. <see cref="Errors"/> has no dedicated "unsupported" value, so unsupported
    /// operations are expressed as <see cref="Errors.Failed"/> with a stable message prefix.
    /// </summary>
    public static class SchemaMigrationResults
    {
        /// <summary>Prefix used for "operation not supported" messages so callers can detect them.</summary>
        public const string UnsupportedPrefix = "Unsupported:";

        public static IErrorsInfo Ok(string message = "")
            => new ErrorsInfo { Flag = Errors.Ok, Message = message ?? string.Empty };

        public static IErrorsInfo Fail(string message, System.Exception ex = null)
            => new ErrorsInfo { Flag = Errors.Failed, Message = message ?? string.Empty, Ex = ex };

        /// <summary>
        /// Standard "this provider cannot perform this operation on this data source" outcome.
        /// Providers return this for every operation their <see cref="SchemaMigrationCapabilities"/>
        /// does not declare as supported.
        /// </summary>
        public static IErrorsInfo Unsupported(string operation, DataSourceType dataSourceType)
            => new ErrorsInfo
            {
                Flag = Errors.Failed,
                Message = $"{UnsupportedPrefix} '{operation}' is not supported for {dataSourceType}."
            };
    }
}
