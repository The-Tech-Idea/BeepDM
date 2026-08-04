using System.Collections.Generic;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Engine-side store for a form's menu. Holds the definition and finds items
    /// by id; invocation lives on FormsManager where the form operations are.
    /// </summary>
    public class FormMenuManager : IFormMenuManager
    {
        private FormMenuDefinition _menu;

        public void RegisterMenu(FormMenuDefinition menu) => _menu = menu;

        public FormMenuDefinition GetMenu() => _menu;

        public FormMenuItem FindItem(string itemId)
        {
            if (_menu == null || string.IsNullOrWhiteSpace(itemId)) return null;
            return Find(_menu.Items, itemId);
        }

        private static FormMenuItem Find(IEnumerable<FormMenuItem> items, string itemId)
        {
            if (items == null) return null;
            foreach (var item in items)
            {
                if (item == null) continue;
                if (string.Equals(item.Id, itemId, System.StringComparison.OrdinalIgnoreCase))
                    return item;
                var inChild = Find(item.Children, itemId);
                if (inChild != null) return inChild;
            }
            return null;
        }
    }
}
