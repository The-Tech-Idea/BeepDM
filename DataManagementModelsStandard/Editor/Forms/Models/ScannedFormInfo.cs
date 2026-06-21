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
    public List<ScannedContainerTargetInfo> EligibleContainerTargets { get; set; } = new();
}

/// <summary>A container control (Panel, GroupBox, TabPage) eligible as a block drop target.</summary>
public class ScannedContainerTargetInfo
{
    public string TargetName { get; set; }
    public string ControlType { get; set; }
    public string DisplayName { get; set; }
    public string ParentControlName { get; set; }
}

/// <summary>A block surface (BeepBlock) discovered in a designer file.</summary>
public class ScannedBlockInfo
{
    public string BlockName { get; set; }
    public string Name { get => BlockName; set => BlockName = value; }
    public string EntityName { get; set; }
    public string ConnectionName { get; set; }
    public string ParentBlock { get; set; }
    public string MasterKeyPropertyName { get; set; }
    public string ForeignKeyPropertyName { get; set; }
    public string HostName { get; set; }
    public string Caption { get; set; }
    public string PresentationMode { get; set; }
    public string ManagerBlockName { get; set; }
    public string DefinitionVariableName { get; set; }
    public string QueryString { get; set; }
    public ScannedBlockRuntimeKind RuntimeKind { get; set; }
    public List<ScannedItemInfo> Items { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public ScannedEntityDefinition? EntityDefinition { get; set; }
    public ScannedNavigationDefinition? Navigation { get; set; }
    public bool IsIntegrated => RuntimeKind != ScannedBlockRuntimeKind.Legacy;
}

/// <summary>Platform-agnostic entity definition metadata for a block.</summary>
public class ScannedEntityDefinition
{
    public string EntityName { get; set; }
    public string ConnectionName { get; set; }
    public string DatasourceEntityName { get; set; }
    public string Caption { get; set; }
    public string Description { get; set; }
    public string ControlType { get; set; }
    public string BindingProperty { get; set; }
    public string DataSourceId { get; set; }
    public string EditorKey { get; set; }
    public string PresentationMode { get; set; }
    public string MasterBlockName { get; set; }
    public string MasterKeyField { get; set; }
    public string ForeignKeyField { get; set; }
    public bool IsMasterBlock { get; set; }
    public List<ScannedEntityFieldDefinition> Fields { get; set; } = new();
}

/// <summary>Platform-agnostic field definition within an entity.</summary>
public class ScannedEntityFieldDefinition
{
    public string FieldName { get; set; }
    public string Caption { get; set; }
    public string Description { get; set; }
    public string DataType { get; set; }
    public string ControlType { get; set; }
    public string BindingProperty { get; set; }
    public string EditorKey { get; set; }
    public string Label { get; set; }
    public int Order { get; set; }
    public int Size { get; set; }
    public short NumericPrecision { get; set; }
    public short NumericScale { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool AllowDBNull { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsUnique { get; set; }
    public bool IsIndexed { get; set; }
    public bool IsAutoIncrement { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsCheck { get; set; }
    public string Category { get; set; }
}

/// <summary>Platform-agnostic navigation bar definition for a block.</summary>
public class ScannedNavigationDefinition
{
    public bool Enabled { get; set; }
    public ScannedNavigationCommand? First { get; set; }
    public ScannedNavigationCommand? Previous { get; set; }
    public ScannedNavigationCommand? Next { get; set; }
    public ScannedNavigationCommand? Last { get; set; }
    public ScannedNavigationCommand? NewRecord { get; set; }
    public ScannedNavigationCommand? Delete { get; set; }
    public ScannedNavigationCommand? Query { get; set; }
    public ScannedNavigationCommand? Execute { get; set; }
    public ScannedNavigationCommand? Save { get; set; }
    public ScannedNavigationCommand? Rollback { get; set; }
}

/// <summary>Platform-agnostic navigation command visibility/enabled state.</summary>
public class ScannedNavigationCommand
{
    public bool Visible { get; set; }
    public bool Enabled { get; set; }
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
