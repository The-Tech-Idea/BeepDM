using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>What a form menu item does when invoked.</summary>
    public enum FormMenuItemKind
    {
        /// <summary>Runs a built-in form operation named by <see cref="FormMenuItem.CommandName"/>.</summary>
        BuiltIn = 0,

        /// <summary>Fires the <c>WhenMenuItem</c> form trigger with this item's id in context.</summary>
        MenuItemTrigger = 1,

        /// <summary>Calls another form named by <see cref="FormMenuItem.CommandName"/>.</summary>
        CallForm = 2,

        /// <summary>A separator line; not invocable.</summary>
        Separator = 3,

        /// <summary>A submenu holding <see cref="FormMenuItem.Children"/>; not invocable itself.</summary>
        Submenu = 4,
    }

    /// <summary>
    /// One item in a form's menu. A tree of these hangs off
    /// <see cref="FormMenuDefinition"/>. UI-agnostic — no control types here.
    /// </summary>
    public sealed class FormMenuItem
    {
        /// <summary>Stable id used to invoke the item and to identify it in the trigger context.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Display text.</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Optional accelerator, e.g. "Ctrl+S". Host-interpreted.</summary>
        public string Shortcut { get; set; }

        public FormMenuItemKind Kind { get; set; } = FormMenuItemKind.BuiltIn;

        /// <summary>
        /// For <see cref="FormMenuItemKind.BuiltIn"/>, the operation name (e.g.
        /// "ExecuteQuery", "Commit", "NextRecord", "PreviousRecord",
        /// "FirstRecord", "LastRecord", "CreateRecord", "DeleteRecord",
        /// "EnterQuery", "ExitForm"). For <see cref="FormMenuItemKind.CallForm"/>,
        /// the target form name. Ignored otherwise.
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>The block a record-scoped built-in targets; null = the form's current block.</summary>
        public string BlockName { get; set; }

        /// <summary>Submenu children, when <see cref="Kind"/> is <see cref="FormMenuItemKind.Submenu"/>.</summary>
        public List<FormMenuItem> Children { get; set; } = new();
    }

    /// <summary>
    /// A form's menu — the Oracle Forms menu module, engine-side. Hangs off
    /// <c>FormDefinition.Menu</c>; a host renders it and invokes items through
    /// <c>IUnitofWorksManager.InvokeMenuItemAsync</c>.
    /// </summary>
    public sealed class FormMenuDefinition
    {
        /// <summary>The form this menu belongs to.</summary>
        public string FormName { get; set; } = string.Empty;

        /// <summary>Top-level menu items (each typically a submenu).</summary>
        public List<FormMenuItem> Items { get; set; } = new();
    }
}
