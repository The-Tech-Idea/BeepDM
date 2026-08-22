using System.Collections.Generic;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Registers named <see cref="PropertyClass"/> bundles and applies them to
    /// items — the engine side of the Oracle Forms Property Class. Mirrors
    /// <c>IVisualAttributeManager</c>: the IDE authors the class and points a
    /// field at it (<see cref="BlockFieldDefinition.PropertyClassName"/>);
    /// <see cref="ApplyToItem"/> is where inheritance actually happens,
    /// called once per field when its block registers
    /// (<c>DefinitionBlockRegistrar</c>).
    /// </summary>
    public interface IPropertyClassManager
    {
        /// <summary>Registers or replaces a named property class (keyed by <see cref="PropertyClass.Name"/>).</summary>
        void RegisterPropertyClass(PropertyClass propertyClass);

        /// <summary>The named class, or null when none is registered under that name.</summary>
        PropertyClass GetPropertyClass(string name);

        /// <summary>Every registered class.</summary>
        IReadOnlyList<PropertyClass> GetPropertyClasses();

        /// <summary>Removes a registered class. Items already resolved from it keep their resolved values — this only stops future resolutions.</summary>
        void RemovePropertyClass(string name);

        /// <summary>
        /// Applies <paramref name="fieldDefinition"/>'s authored overrides onto
        /// <paramref name="item"/>, falling back to
        /// <paramref name="fieldDefinition"/>'s named
        /// <see cref="BlockFieldDefinition.PropertyClassName"/> (when set) for
        /// whichever of those the field itself left unauthored. A property
        /// neither the field nor the class supplies is left exactly as
        /// <paramref name="item"/> already had it. An unregistered or
        /// unauthored class name is a no-op, not an error — a field is never
        /// blocked on a class that hasn't been authored yet.
        /// </summary>
        void ApplyToItem(ItemInfo item, BlockFieldDefinition fieldDefinition);
    }
}
