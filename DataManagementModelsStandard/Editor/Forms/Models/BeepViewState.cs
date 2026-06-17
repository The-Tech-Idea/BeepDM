using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models;

public enum BeepMessageSeverity
{
    None,
    Info,
    Success,
    Warning,
    Error
}

public enum BootstrapState
{
    Idle,
    Running,
    Succeeded,
    PartialSuccess,
    Failed
}

public class BeepWorkflowEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string Text { get; init; } = string.Empty;
    public BeepMessageSeverity Severity { get; init; }
}

public class BeepViewState
{
    private readonly List<BeepWorkflowEntry> _workflowHistory = new();

    public bool IsDirty { get; set; }
    public bool IsQueryMode { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string CoordinationText { get; set; } = string.Empty;
    public string WorkflowText { get; set; } = string.Empty;
    public string SavepointText { get; set; } = string.Empty;
    public string AlertText { get; set; } = string.Empty;
    public string CurrentMessage { get; set; } = string.Empty;
    public BeepMessageSeverity CoordinationSeverity { get; set; }
    public BeepMessageSeverity WorkflowSeverity { get; set; }
    public BeepMessageSeverity SavepointSeverity { get; set; }
    public BeepMessageSeverity AlertSeverity { get; set; }
    public BeepMessageSeverity MessageSeverity { get; set; }
    public string? ActiveBlockName { get; set; }
    public string? ActiveItemName { get; set; }
    public IReadOnlyList<BeepWorkflowEntry> WorkflowHistory => _workflowHistory;
    public List<BeepWorkflowEntry> WorkflowHistoryItems => _workflowHistory;

    public BootstrapState BootstrapState { get; set; } = BootstrapState.Idle;
    public string RecordPositionText { get; set; } = string.Empty;
    public string ConnectionName { get; set; } = string.Empty;
    public int ErrorCount { get; set; }
    public string? FirstErrorBlockName { get; set; }
    public string? FirstErrorFieldName { get; set; }
    public string AggregateText { get; set; } = string.Empty;
}
