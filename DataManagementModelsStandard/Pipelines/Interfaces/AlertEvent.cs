using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Describes an alert event raised by the pipeline engine.
    /// Consumed by <see cref="IPipelineNotifier"/> implementations.
    /// </summary>
    public class AlertEvent
    {
        /// <summary>Unique run identifier this alert belongs to.</summary>
        public string RunId { get; init; } = string.Empty;

        /// <summary>Pipeline name for display in notifications.</summary>
        public string PipelineName { get; init; } = string.Empty;

        /// <summary>Severity level of the alert.</summary>
        public AlertSeverity Severity { get; init; } = AlertSeverity.Info;

        /// <summary>Short title for the notification subject / headline.</summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>Detailed alert message body.</summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>UTC time the alert was raised.</summary>
        public DateTime RaisedAtUtc { get; init; } = DateTime.UtcNow;

        /// <summary>Optional additional key-value data for the notification template.</summary>
        public IReadOnlyDictionary<string, object> Data { get; init; }
            = new Dictionary<string, object>();
    }
}
