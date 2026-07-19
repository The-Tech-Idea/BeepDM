using System;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Explicit opt-in marker: the decorated class is a migration entity. Lets entity discovery pick
    /// up a plain POCO without subclassing <c>Entity</c> or carrying EF-Core attributes, and makes it
    /// eligible even in an unscoped (no-namespace-filter) scan. See also <see cref="BeepIgnoreAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class BeepEntityAttribute : Attribute
    {
    }

    /// <summary>
    /// Explicit opt-out marker: the decorated class is never treated as a migration entity, even if it
    /// otherwise looks discoverable (public, concrete, parameterless ctor, EF-decorated). Takes priority
    /// over every acceptance rule.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class BeepIgnoreAttribute : Attribute
    {
    }
}
