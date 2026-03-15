namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Central repository for all pipeline framework constants.
    /// Never use magic numbers or strings in pipeline code — reference these instead.
    /// </summary>
    public static class PipelineConstants
    {
        // ── Default execution values ───────────────────────────────────────
        public const int DefaultBatchSize      = 500;
        public const int MaxParallelBatches    = 4;
        public const int DefaultMaxRetries     = 3;
        public const int DefaultStopErrorCount = 10;
        public const int MaxRowLogPerStep      = 100;

        // ── Built-in plugin IDs ────────────────────────────────────────────
        public static class PluginIds
        {
            public const string DbSource            = "beep.source.db";
            public const string CsvSource           = "beep.source.csv";
            public const string JsonSource          = "beep.source.json";
            public const string ExcelSource         = "beep.source.excel";
            public const string DbSink              = "beep.sink.db";
            public const string CsvSink             = "beep.sink.csv";
            public const string JsonSink            = "beep.sink.json";
            public const string ErrorLogSink        = "beep.sink.errorlog";
            public const string FieldMapTransform   = "beep.transform.fieldmap";
            public const string ExpressionTransform = "beep.transform.expression";
            public const string TypeCastTransform   = "beep.transform.typecast";
            public const string DedupTransform      = "beep.transform.dedup";
            public const string NotNullValidator    = "beep.validate.notnull";
            public const string RegexValidator      = "beep.validate.regex";
            public const string RangeValidator      = "beep.validate.range";
            public const string CronScheduler       = "beep.schedule.cron";
            public const string FileWatchScheduler  = "beep.schedule.filewatch";
            public const string ManualScheduler     = "beep.schedule.manual";
            public const string EmailNotifier       = "beep.notify.email";
            public const string WebhookNotifier     = "beep.notify.webhook";
        }

        // ── Config dictionary keys ─────────────────────────────────────────
        public static class ConfigKeys
        {
            public const string ConnectionName      = "ConnectionName";
            public const string EntityName          = "EntityName";
            public const string BatchSize           = "BatchSize";
            public const string Expression          = "Expression";
            public const string CronExpression      = "CronExpression";
            public const string FilePath            = "FilePath";
            public const string Delimiter           = "Delimiter";
            public const string HasHeader           = "HasHeader";
            public const string ToAddress          = "ToAddress";
            public const string Subject             = "Subject";
            public const string WebhookUrl          = "WebhookUrl";
        }
    }
}
