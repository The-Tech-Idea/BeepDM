using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models;

/// <summary>Form surface hosting blocks (WinForms BeepForms or WPF BeepWpfForms).</summary>
public class ScannedFormInfo
{
    public string FormName { get; set; }
    public string FormFilePath { get; set; }
    public string DesignerFilePath { get; set; }
    public string ProjectPath { get; set; }
    public bool IsWpf { get; set; }
    public bool IsForm { get; set; }
    public bool IsUserControl { get; set; }
    public List<ScannedBlockInfo> Blocks { get; set; } = new();
    public List<ScannedHostInfo> Hosts { get; set; } = new();
    public bool UsesIntegratedForms => Hosts.Count > 0 || Blocks.Exists(b => b.IsIntegrated);
}

/// <summary>A block surface (BeepBlock) discovered in a designer file.</summary>
public class ScannedBlockInfo
{
    public string BlockName { get; set; }
    public string EntityName { get; set; }
    public string ConnectionName { get; set; }
    public string ParentBlock { get; set; }
    public string MasterKeyPropertyName { get; set; }
    public string ForeignKeyPropertyName { get; set; }
    public string HostName { get; set; }
    public string Caption { get; set; }
    public string PresentationMode { get; set; }
    public ScannedBlockRuntimeKind RuntimeKind { get; set; }
    public List<ScannedItemInfo> Items { get; set; } = new();
    public bool IsIntegrated => RuntimeKind != ScannedBlockRuntimeKind.Legacy;
}

/// <summary>Platform-specific host (BeepForms or BeepWpfForms).</summary>
public class ScannedHostInfo
{
    public string HostName { get; set; }
    public string LogicalFormName { get; set; }
    public string Title { get; set; }
    public bool AutoCreateBlocksFromDefinition { get; set; } = true;
}

/// <summary>A control (Item) within a block.</summary>
public class ScannedItemInfo
{
    public string ItemName { get; set; }
    public string ControlType { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public List<ScannedTriggerInfo> Triggers { get; set; } = new();
    public List<ScannedLovInfo> LOVs { get; set; } = new();
    public List<ScannedValidationInfo> Validations { get; set; } = new();
}

/// <summary>Event handler (Trigger) information.</summary>
public class ScannedTriggerInfo
{
    public string EventName { get; set; }
    public string HandlerName { get; set; }
    public bool IsIntegrated { get; set; }
}

/// <summary>List of Values (LOV) information.</summary>
public class ScannedLovInfo
{
    public string LOVName { get; set; }
    public string ConnectionName { get; set; }
    public string EntityName { get; set; }
    public List<string> DisplayFields { get; set; } = new();
    public List<string> ReturnFields { get; set; } = new();
}

/// <summary>Validation rule information.</summary>
public class ScannedValidationInfo
{
    public string ValidationName { get; set; }
    public string ValidationRule { get; set; }
    public string RuleType { get; set; }
    public string Expression { get; set; }
    public string Message { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public enum ScannedBlockRuntimeKind
{
    Legacy,
    Integrated,
    FormsDefinition,
    WpfIntegrated
}
