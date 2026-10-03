using System;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>A failed sink write with explicit batch replay safety.</summary>
    public sealed class PipelineWriteException : Exception
    {
        public long AcknowledgedRecords { get; }
        public bool CanRetry { get; }

        public PipelineWriteException(string message, long acknowledgedRecords, bool canRetry,
            Exception innerException = null) : base(message, innerException)
        {
            AcknowledgedRecords = acknowledgedRecords;
            CanRetry = canRetry;
        }
    }
}
