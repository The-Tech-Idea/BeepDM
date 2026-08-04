using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models;

// The design-time definition of a data block, owned by BeepDM.
//
// This is the contract every platform and every tool shares: the WinForms and
// WPF hosts, the IDE's scanners, and the code the IDE generates into a target
// project all name these types.
//
// It was previously duplicated three times inside the IDE extension
// (BlockEntityDefinition / BlockEntityFieldDefinition / BlockNavigationDefinition
// parse buffers, BlockSpec / BlockFieldSpec emitter inputs, and an *empty*
// `internal sealed class BlockDefinition { }`), with ~100 lines of translation
// between them. Because the real types lived nowhere, the .Designer.cs the IDE
// emitted — `BlockDefinition Ord = new BlockDefinition();` …
// `this._formsHost.Definition.Blocks.Add(Ord);` — could not compile in the
// project it was written into.
//
// ScannedBlockInfo and friends in ScannedFormInfo.cs now derive from these, so
// the scanner contract is the same objects rather than a parallel description.

/// <summary>
/// A data block: its identity, its data binding, its fields and its navigation.
/// </summary>
public class BlockDefinition
{
    /// <summary>Block name as the engine knows it.</summary>
    public string BlockName { get; set; }

    /// <summary>Alias of <see cref="BlockName"/>, for designer-style assignment.</summary>
    public string Name { get => BlockName; set => BlockName = value; }

    /// <summary>Entity (table/view) this block is bound to.</summary>
    public string EntityName { get; set; }

    /// <summary>Named connection the entity is read through.</summary>
    public string ConnectionName { get; set; }

    /// <summary>Master block name when this block is a detail.</summary>
    public string ParentBlock { get; set; }

    /// <summary>Field on the master that the detail joins to.</summary>
    public string MasterKeyPropertyName { get; set; }

    /// <summary>Field on this block that carries the master's key.</summary>
    public string ForeignKeyPropertyName { get; set; }

    /// <summary>Name of the host surface that owns this block.</summary>
    public string HostName { get; set; }

    /// <summary>Human-readable caption.</summary>
    public string Caption { get; set; }

    /// <summary>Platform-specific presentation hint (grid, label pairs, …).</summary>
    public string PresentationMode { get; set; }

    /// <summary>
    /// Name the runtime <c>FormsManager</c> registers this block under.
    /// Usually the same as <see cref="BlockName"/>.
    /// </summary>
    public string ManagerBlockName { get; set; }

    /// <summary>Variable name this definition was declared as in generated source.</summary>
    public string DefinitionVariableName { get; set; }

    /// <summary>Optional WHERE clause applied when the block queries.</summary>
    public string QueryString { get; set; }

    /// <summary>How the block is realised at runtime.</summary>
    public ScannedBlockRuntimeKind RuntimeKind { get; set; }

    /// <summary>Items (fields) discovered or declared for this block.</summary>
    public List<ScannedItemInfo> Items { get; set; } = new();

    /// <summary>Anything carried through that has no first-class member.</summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>Entity binding and field definitions.</summary>
    public BlockEntityDefinition EntityDefinition { get; set; }

    /// <summary>
    /// Alias of <see cref="EntityDefinition"/>. Generated designer code assigns
    /// <c>Ord.Entity.EntityName = "Orders";</c>, so the short name has to exist.
    /// </summary>
    public BlockEntityDefinition Entity
    {
        get => EntityDefinition ??= new BlockEntityDefinition();
        set => EntityDefinition = value;
    }

    /// <summary>Navigation bar definition.</summary>
    public BlockNavigationDefinition Navigation { get; set; }

    /// <summary>True for anything the integrated runtime drives.</summary>
    public bool IsIntegrated => RuntimeKind != ScannedBlockRuntimeKind.Legacy;
}

/// <summary>Entity binding for a block, plus its field definitions.</summary>
public class BlockEntityDefinition
{
    /// <summary>Entity name as the block refers to it.</summary>
    public string EntityName { get; set; }

    /// <summary>Named connection the entity is read through.</summary>
    public string ConnectionName { get; set; }

    /// <summary>Entity name as it exists in the datasource, when it differs.</summary>
    public string DatasourceEntityName { get; set; }

    /// <summary>Human-readable caption.</summary>
    public string Caption { get; set; }

    /// <summary>Free-text description.</summary>
    public string Description { get; set; }

    /// <summary>Platform control type hint for the block surface.</summary>
    public string ControlType { get; set; }

    /// <summary>Property the block surface binds through.</summary>
    public string BindingProperty { get; set; }

    /// <summary>Datasource identifier, when the block pins one.</summary>
    public string DataSourceId { get; set; }

    /// <summary>Editor key hint for the block surface.</summary>
    public string EditorKey { get; set; }

    /// <summary>Platform-specific presentation hint.</summary>
    public string PresentationMode { get; set; }

    /// <summary>Master block name when this block is a detail.</summary>
    public string MasterBlockName { get; set; }

    /// <summary>Field on the master that the detail joins to.</summary>
    public string MasterKeyField { get; set; }

    /// <summary>Field on this block that carries the master's key.</summary>
    public string ForeignKeyField { get; set; }

    /// <summary>True when other blocks join to this one.</summary>
    public bool IsMasterBlock { get; set; }

    /// <summary>Field definitions belonging to this entity.</summary>
    public List<BlockFieldDefinition> Fields { get; set; } = new();

    /// <summary>
    /// Copies this definition, including its fields.
    /// <para>
    /// The field list is copied, not shared. A shallow copy would let two
    /// blocks cloned from the same template edit each other's fields, which is
    /// exactly what happens when a form declares several blocks from one
    /// definition.
    /// </para>
    /// </summary>
    public BlockEntityDefinition Clone()
    {
        var copy = (BlockEntityDefinition)MemberwiseClone();
        copy.Fields = new List<BlockFieldDefinition>(Fields.Count);
        foreach (var field in Fields) copy.Fields.Add(field.Clone());
        return copy;
    }
}

/// <summary>One field within a block's entity.</summary>
public class BlockFieldDefinition
{
    /// <summary>Column name.</summary>
    public string FieldName { get; set; }

    /// <summary>Human-readable caption.</summary>
    public string Caption { get; set; }

    /// <summary>Free-text description.</summary>
    public string Description { get; set; }

    /// <summary>Database type name.</summary>
    public string DataType { get; set; }

    /// <summary>Platform control type hint.</summary>
    public string ControlType { get; set; }

    /// <summary>Property the control binds through.</summary>
    public string BindingProperty { get; set; }

    /// <summary>
    /// Key the platform's field-presenter registry resolves to a control.
    /// Assigned from the engine's <c>FieldTypeMapper</c> so design time and run
    /// time cannot drift.
    /// </summary>
    public string EditorKey { get; set; }

    /// <summary>Label shown beside the control.</summary>
    public string Label { get; set; }

    /// <summary>Display order, ascending.</summary>
    public int Order { get; set; }

    /// <summary>Maximum length for text types.</summary>
    public int Size { get; set; }

    /// <summary>Precision for numeric types.</summary>
    public short NumericPrecision { get; set; }

    /// <summary>Scale for numeric types.</summary>
    public short NumericScale { get; set; }

    /// <summary>Field must have a value.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Field can be edited.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Column accepts NULL.</summary>
    public bool AllowDBNull { get; set; }

    /// <summary>Part of the primary key.</summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>Carries a unique constraint.</summary>
    public bool IsUnique { get; set; }

    /// <summary>Backed by an index.</summary>
    public bool IsIndexed { get; set; }

    /// <summary>Database-generated on insert.</summary>
    public bool IsAutoIncrement { get; set; }

    /// <summary>Not editable at runtime.</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>Carries a check constraint.</summary>
    public bool IsCheck { get; set; }

    /// <summary>
    /// Field category, as the name of a
    /// <c>TheTechIdea.Beep.Utilities.DbFieldCategory</c> member.
    /// <para>
    /// Held as a string rather than the enum so this contract does not drag the
    /// Utilities enum into every consumer, and so an unrecognised value from an
    /// older generated file round-trips instead of silently becoming the
    /// default. Parse with <c>Enum.TryParse</c> against that enum — never
    /// against a locally declared copy, which is how Integer, Long, Short,
    /// Date, Json and Complex were previously lost.
    /// </para>
    /// </summary>
    public string Category { get; set; }

    /// <summary>Display width of the generated control, in pixels. 0 = auto.</summary>
    public int Width { get; set; }

    /// <summary>Whether the generated control is shown.</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Copies this field definition. All members are value types or strings.</summary>
    public BlockFieldDefinition Clone() => (BlockFieldDefinition)MemberwiseClone();
}

/// <summary>Navigation bar definition for a block.</summary>
public class BlockNavigationDefinition
{
    /// <summary>Whether the navigation bar is shown at all.</summary>
    public bool Enabled { get; set; }

    /// <summary>Move to the first record.</summary>
    public BlockNavigationCommand First { get; set; }

    /// <summary>Move to the previous record.</summary>
    public BlockNavigationCommand Previous { get; set; }

    /// <summary>Move to the next record.</summary>
    public BlockNavigationCommand Next { get; set; }

    /// <summary>Move to the last record.</summary>
    public BlockNavigationCommand Last { get; set; }

    /// <summary>Begin a new record.</summary>
    public BlockNavigationCommand NewRecord { get; set; }

    /// <summary>Delete the current record.</summary>
    public BlockNavigationCommand Delete { get; set; }

    /// <summary>Enter query mode.</summary>
    public BlockNavigationCommand Query { get; set; }

    /// <summary>Execute the query.</summary>
    public BlockNavigationCommand Execute { get; set; }

    /// <summary>Commit pending changes.</summary>
    public BlockNavigationCommand Save { get; set; }

    /// <summary>Discard pending changes.</summary>
    public BlockNavigationCommand Rollback { get; set; }

    /// <summary>
    /// Copies this navigation definition, including each declared command.
    /// A command left null stays null — null means "not declared", which is
    /// not the same as declared-and-hidden.
    /// </summary>
    public BlockNavigationDefinition Clone() => new()
    {
        Enabled = Enabled,
        First = First?.Clone(),
        Previous = Previous?.Clone(),
        Next = Next?.Clone(),
        Last = Last?.Clone(),
        NewRecord = NewRecord?.Clone(),
        Delete = Delete?.Clone(),
        Query = Query?.Clone(),
        Execute = Execute?.Clone(),
        Save = Save?.Clone(),
        Rollback = Rollback?.Clone()
    };
}

/// <summary>Visibility and enabled state of one navigation command.</summary>
public class BlockNavigationCommand
{
    /// <summary>Whether the command is shown.</summary>
    public bool Visible { get; set; }

    /// <summary>Whether the command can be invoked.</summary>
    public bool Enabled { get; set; }

    /// <summary>Copies this command's state.</summary>
    public BlockNavigationCommand Clone() => (BlockNavigationCommand)MemberwiseClone();
}

/// <summary>
/// The set of blocks a form host owns.
/// <para>
/// Generated designer code registers a block with
/// <c>this._formsHost.Definition.Blocks.Add(Ord);</c>, so the host's
/// <c>Definition</c> has to be this type rather than <c>object</c>.
/// </para>
/// </summary>
public class FormDefinition
{
    /// <summary>Logical form name the engine registers.</summary>
    public string FormName { get; set; }

    /// <summary>Blocks declared on this form.</summary>
    public List<BlockDefinition> Blocks { get; set; } = new();

    /// <summary>
    /// Whether the host should create block surfaces from <see cref="Blocks"/>
    /// on load, rather than expecting them to be added in code.
    /// </summary>
    public bool AutoCreateBlocksFromDefinition { get; set; } = true;

    /// <summary>
    /// The form's menu (Oracle Forms menu module), or null when the form has
    /// none. A host renders it and invokes items through
    /// <c>IUnitofWorksManager.InvokeMenuItemAsync</c>.
    /// </summary>
    public TheTechIdea.Beep.Editor.UOWManager.Models.FormMenuDefinition Menu { get; set; }
}
