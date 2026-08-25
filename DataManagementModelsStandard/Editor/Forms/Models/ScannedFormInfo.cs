using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models;

// The result of scanning source for form surfaces.
//
// The block half of this file used to declare its own copy of the block
// definition. It now derives from the shared one in BlockDefinition.cs, so a
// scanned block and an authored block are the same object rather than two
// descriptions that have to be translated between.

/// <summary>Form surface hosting blocks (WinForms BeepForms or WPF BeepWpfForms).</summary>
public class ScannedFormInfo
{
    /// <summary>Class name of the form.</summary>
    public string FormName { get; set; }

    /// <summary>Path of the form's own source file (.cs or .xaml.cs).</summary>
    public string FormFilePath { get; set; }

    /// <summary>Path of the generated half (.Designer.cs or .xaml).</summary>
    public string DesignerFilePath { get; set; }

    /// <summary>Directory of the owning project.</summary>
    public string ProjectPath { get; set; }

    /// <summary>Discovered on the WPF path.</summary>
    public bool IsWpf { get; set; }

    /// <summary>Derives from Form or Window.</summary>
    public bool IsForm { get; set; }

    /// <summary>Derives from UserControl, or from Page on the WPF path.</summary>
    public bool IsUserControl { get; set; }

    /// <summary>Blocks declared on this form.</summary>
    public List<ScannedBlockInfo> Blocks { get; set; } = new();

    /// <summary>
    /// Form-scope triggers registered on this form — the ones belonging to the
    /// form itself rather than to any block or item (WHEN-NEW-FORM-INSTANCE,
    /// PRE-COMMIT, POST-COMMIT, WHEN-LOGON, …).
    /// </summary>
    /// <remarks>
    /// Triggers used to hang off <see cref="ScannedItemInfo"/> only, so a
    /// registration whose <c>TriggerScope</c> was Form had nowhere to be read
    /// back into and no navigator row could show it. Scope comes from the
    /// <c>TriggerDefinition</c> constructor's second argument; a registration
    /// that omits it is read as Item, which is what every file written before
    /// this existed meant.
    /// </remarks>
    public List<ScannedTriggerInfo> Triggers { get; set; } = new();

    /// <summary>
    /// Named Record Groups registered on this form (engine:
    /// <c>IUnitofWorksManager.CreateRecordGroup</c>). Form-scoped, name-only —
    /// there is no block/field key, unlike LOVs/triggers/validation.
    /// </summary>
    public List<ScannedRecordGroupInfo> RecordGroups { get; set; } = new();

    /// <summary>
    /// Named Parameter Lists registered on this form (engine:
    /// <c>IUnitofWorksManager.CreateParameterList</c>/<c>AddParameter</c>).
    /// </summary>
    public List<ScannedParameterListInfo> ParameterLists { get; set; } = new();

    /// <summary>
    /// Named Alerts registered on this form (engine:
    /// <c>IUnitofWorksManager.CreateAlert</c>, added 2026-08-25).
    /// </summary>
    public List<ScannedAlertInfo> Alerts { get; set; } = new();

    /// <summary>
    /// Named Editor objects registered on this form (engine:
    /// <c>IUnitofWorksManager.CreateEditor</c>, added 2026-08-25).
    /// </summary>
    public List<ScannedEditorInfo> Editors { get; set; } = new();

    /// <summary>
    /// Named Object Groups authored on this form. Unlike every other list on
    /// this class, these have no engine counterpart at all — see
    /// <see cref="ObjectGroupDefinition"/>'s own remarks.
    /// </summary>
    public List<ObjectGroupDefinition> ObjectGroups { get; set; } = new();

    /// <summary>Form hosts declared on this form.</summary>
    public List<ScannedHostInfo> Hosts { get; set; } = new();

    /// <summary>True when the integrated Forms runtime is in play.</summary>
    public bool UsesIntegratedForms => Hosts.Count > 0 || Blocks.Exists(b => b.IsIntegrated);

    /// <summary>Containers a generated block surface could be placed into.</summary>
    public List<ScannedContainerTargetInfo> EligibleContainerTargets { get; set; } = new();
}

/// <summary>A container control (Panel, GroupBox, TabPage, Grid) eligible as a block drop target.</summary>
public class ScannedContainerTargetInfo
{
    /// <summary>Control name.</summary>
    public string TargetName { get; set; }

    /// <summary>Control type name.</summary>
    public string ControlType { get; set; }

    /// <summary>Label shown in pickers.</summary>
    public string DisplayName { get; set; }

    /// <summary>Nearest named ancestor.</summary>
    public string ParentControlName { get; set; }
}

/// <summary>
/// A block discovered by a scanner.
/// <para>
/// Adds nothing to <see cref="BlockDefinition"/> — it exists so
/// <see cref="Hosts.IFormScanner"/> and its callers keep a name that says where
/// the instance came from. A scanned block and an authored block are the same
/// shape, because they are the same thing.
/// </para>
/// </summary>
public class ScannedBlockInfo : BlockDefinition
{
}

/// <summary>Entity definition of a scanned block. See <see cref="BlockEntityDefinition"/>.</summary>
public class ScannedEntityDefinition : BlockEntityDefinition
{
}

/// <summary>Field definition of a scanned block. See <see cref="BlockFieldDefinition"/>.</summary>
public class ScannedEntityFieldDefinition : BlockFieldDefinition
{
}

/// <summary>Navigation definition of a scanned block. See <see cref="BlockNavigationDefinition"/>.</summary>
public class ScannedNavigationDefinition : BlockNavigationDefinition
{
}

/// <summary>Navigation command state. See <see cref="BlockNavigationCommand"/>.</summary>
public class ScannedNavigationCommand : BlockNavigationCommand
{
}

/// <summary>Platform-specific host (BeepForms or BeepWpfForms).</summary>
public class ScannedHostInfo
{
    /// <summary>Field name of the host in generated source.</summary>
    public string HostName { get; set; }

    /// <summary>Logical form name the host registers with the engine.</summary>
    public string LogicalFormName { get; set; }

    /// <summary>Window or form title.</summary>
    public string Title { get; set; }

    /// <summary>Whether the host builds block surfaces from its definition.</summary>
    public bool AutoCreateBlocksFromDefinition { get; set; } = true;

    /// <summary>
    /// Variable the host's definition was assigned to in generated source, when
    /// the generator used a local rather than <c>host.Definition</c> directly.
    /// Blocks are matched to their host through it.
    /// </summary>
    public string DefinitionReferenceName { get; set; }
}

/// <summary>A control (Item) within a block.</summary>
public class ScannedItemInfo
{
    /// <summary>Field name.</summary>
    public string ItemName { get; set; }

    /// <summary>Control type name.</summary>
    public string ControlType { get; set; }

    /// <summary>Anything carried through that has no first-class member.</summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>Triggers registered against this item.</summary>
    public List<ScannedTriggerInfo> Triggers { get; set; } = new();

    /// <summary>Lists of values attached to this item.</summary>
    public List<ScannedLovInfo> LOVs { get; set; } = new();

    /// <summary>Validation rules attached to this item.</summary>
    public List<ScannedValidationInfo> Validations { get; set; } = new();

    /// <summary>
    /// Name of the visual attribute applied to this item, or empty when none is.
    /// </summary>
    /// <remarks>
    /// Only the name: the attribute's colours and font live in the engine's
    /// <c>VisualAttribute</c> and are edited through the visual-attribute editor,
    /// which parses them off the same generated line. The scanned model carries
    /// the binding so the Object Navigator can show *which* item wears one —
    /// authoring it and then having no row say so is the same defect the
    /// master-detail relation had.
    /// </remarks>
    public string VisualAttributeName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the Editor object (Oracle Forms EDIT_TEXTITEM popup) attached to
    /// this item, or empty when none is.
    /// </summary>
    /// <remarks>
    /// Like <see cref="VisualAttributeName"/>: only the binding. The Editor
    /// object itself (title/width/height/wrap/scroll) is defined once, by name,
    /// through the Editor object editor, and applied per item via
    /// <c>ItemProperties.SetItemEditor(block, item, editorName)</c> — a plain
    /// name attachment, not a <see cref="BlockFieldDefinition"/> property,
    /// because <c>ItemInfo.EditorName</c> is resolved by <c>FormsManager</c> at
    /// call time (<c>ShowEditorAsync</c>), not applied at block-registration
    /// time the way the FormatMask/DefaultValue cluster is.
    /// </remarks>
    public string EditorName { get; set; } = string.Empty;
}

/// <summary>Event handler (Trigger) information.</summary>
public class ScannedTriggerInfo
{
    /// <summary>Trigger event name.</summary>
    public string EventName { get; set; }

    /// <summary>Handler method name in the form source.</summary>
    public string HandlerName { get; set; }

    /// <summary>Registered through the integrated marker regions.</summary>
    public bool IsIntegrated { get; set; }
}

/// <summary>Scanned Record Group registration (Oracle Forms RECORD_GROUP).</summary>
public class ScannedRecordGroupInfo
{
    /// <summary>Record group name.</summary>
    public string Name { get; set; }

    /// <summary>Connection the group queries.</summary>
    public string ConnectionName { get; set; }

    /// <summary>Entity the group queries.</summary>
    public string EntityName { get; set; }
}

/// <summary>Scanned Parameter List registration (Oracle Forms PARAMETER_LIST).</summary>
public class ScannedParameterListInfo
{
    /// <summary>Parameter list name.</summary>
    public string Name { get; set; }

    /// <summary>Parameter names and their literal authored values.</summary>
    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>Scanned named Alert registration (Oracle Forms ALERT).</summary>
public class ScannedAlertInfo
{
    /// <summary>Alert name.</summary>
    public string Name { get; set; }

    /// <summary>Alert title bar text.</summary>
    public string Title { get; set; }

    /// <summary>Alert message body.</summary>
    public string Message { get; set; }

    /// <summary>Alert icon/severity style, as authored (Information/Caution/Stop/Question/None).</summary>
    public string Style { get; set; }

    /// <summary>First button's label.</summary>
    public string Button1Text { get; set; }

    /// <summary>Second button's label, or null.</summary>
    public string Button2Text { get; set; }

    /// <summary>Third button's label, or null.</summary>
    public string Button3Text { get; set; }
}

/// <summary>Scanned named Editor registration (Oracle Forms EDITOR — large-text popup).</summary>
public class ScannedEditorInfo
{
    /// <summary>Editor name.</summary>
    public string Name { get; set; }

    /// <summary>Popup title bar text.</summary>
    public string Title { get; set; }

    /// <summary>Popup width in device-independent pixels.</summary>
    public int Width { get; set; }

    /// <summary>Popup height in device-independent pixels.</summary>
    public int Height { get; set; }

    /// <summary>Whether text wraps at the edit area's width.</summary>
    public bool WrapText { get; set; }

    /// <summary>Whether a scroll bar is shown.</summary>
    public bool ShowScrollBar { get; set; }
}

/// <summary>List of Values (LOV) information.</summary>
public class ScannedLovInfo
{
    /// <summary>LOV name.</summary>
    public string LOVName { get; set; }

    /// <summary>Connection the LOV queries.</summary>
    public string ConnectionName { get; set; }

    /// <summary>Entity the LOV queries.</summary>
    public string EntityName { get; set; }

    /// <summary>Columns shown to the user.</summary>
    public List<string> DisplayFields { get; set; } = new();

    /// <summary>Columns written back on selection.</summary>
    public List<string> ReturnFields { get; set; } = new();
}

/// <summary>Validation rule information.</summary>
public class ScannedValidationInfo
{
    /// <summary>Rule name.</summary>
    public string ValidationName { get; set; }

    /// <summary>Rendered rule summary.</summary>
    public string ValidationRule { get; set; }

    /// <summary>Rule type (Range, Pattern, Required, …).</summary>
    public string RuleType { get; set; }

    /// <summary>Rule expression.</summary>
    public string Expression { get; set; }

    /// <summary>Message shown when the rule fails.</summary>
    public string Message { get; set; }

    /// <summary>Whether the rule is active.</summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>How a block is realised at runtime.</summary>
public enum ScannedBlockRuntimeKind
{
    /// <summary>Pre-integration BeepDataBlock control.</summary>
    Legacy,

    /// <summary>Integrated BeepBlock control.</summary>
    Integrated,

    /// <summary>Driven by a BlockDefinition on the host.</summary>
    FormsDefinition,

    /// <summary>WPF BeepWpfBlock.</summary>
    WpfIntegrated
}
