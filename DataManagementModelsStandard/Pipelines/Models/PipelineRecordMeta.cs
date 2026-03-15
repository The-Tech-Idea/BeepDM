namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Well-known key constants for <see cref="PipelineRecord.Meta"/>.
    /// Use these rather than raw strings to avoid typo bugs.
    /// </summary>
    public static class PipelineRecordMeta
    {
        /// <summary>Original row number in the source file or table.</summary>
        public const string SourceRowNumber = "__src_row";

        /// <summary>Source file path, if applicable.</summary>
        public const string SourceFileName  = "__src_file";

        /// <summary>Correlation ID inherited from the pipeline run.</summary>
        public const string CorrelationId   = "__correlation_id";

        /// <summary>Partition key for parallel processing routing.</summary>
        public const string PartitionKey    = "__partition_key";

        /// <summary>UTC timestamp when this record was created by the source.</summary>
        public const string Timestamp       = "__timestamp";

        /// <summary>Validation rule name if the record was flagged or rejected.</summary>
        public const string ValidationRule  = "__validation_rule";

        /// <summary>Validation message set by a validator step.</summary>
        public const string ValidationMessage = "__validation_msg";
    }
}
