using System;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Holds a form's menu (the Oracle Forms menu module, engine-side). The menu
    /// is stored and read here; INVOCATION lives on
    /// <c>IUnitofWorksManager.InvokeMenuItemAsync</c>, where the form operations
    /// a built-in item dispatches to already are.
    /// </summary>
    public interface IFormMenuManager
    {
        /// <summary>
        /// Raised when the menu is registered or replaced, so a host that renders
        /// it can rebuild its menu bar. The engine notifies; the UI reacts — the
        /// host never polls.
        /// </summary>
        event EventHandler MenuChanged;

        /// <summary>Registers (or replaces) the form's menu.</summary>
        void RegisterMenu(FormMenuDefinition menu);

        /// <summary>The registered menu, or null when the form has none.</summary>
        FormMenuDefinition GetMenu();

        /// <summary>The menu item with this id anywhere in the tree, or null.</summary>
        FormMenuItem FindItem(string itemId);
    }
}
