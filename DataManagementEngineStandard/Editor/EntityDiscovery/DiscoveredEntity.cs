using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TheTechIdea.Beep.Editor.EntityDiscovery
{
    /// <summary>
    /// A class the user can include in a migration. Wraps a <see cref="Type"/> with
    /// enough metadata for the wizard UI to filter / group / preview without
    /// re-loading the type from <see cref="System.Reflection.Assembly"/>.
    /// </summary>
    public class DiscoveredEntity
    {
        public string Name { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string AssemblyName { get; init; } = string.Empty;
        public int PropertyCount { get; init; }
        public EntityCategory Category { get; init; } = EntityCategory.Poco;
        public string Namespace { get; init; } = string.Empty;
        public bool HasParameterlessConstructor { get; init; }

        // ── Richer metadata populated by EntityDiscoveryService ────────────────
        // Cheap to compute at discovery time; the UI uses these to filter / preview
        // without re-reflecting the type.

        /// <summary>True when the type carries EF Core attributes ([Table], [Key], [Column], …).</summary>
        public bool IsEfDecorated { get; init; }

        /// <summary>True when the type inherits BeepDM's <c>Entity</c> base class.</summary>
        public bool IsBeepEntity { get; init; }

        /// <summary>Names of the EF / Beep primary-key property(ies), comma-joined. Empty when no key is found.</summary>
        public string PrimaryKeyNames { get; init; } = string.Empty;

        /// <summary>Number of navigation / collection properties (used to show relationship depth in the preview).</summary>
        public int NavigationPropertyCount { get; init; }

        /// <summary>Number of scalar (non-navigation) public properties.</summary>
        public int ScalarPropertyCount { get; init; }

        /// <summary>True when the type is abstract or an interface — wizard can hide these by default.</summary>
        public bool IsAbstract { get; init; }

        /// <summary>True when the type is generic (open or closed).</summary>
        public bool IsGeneric { get; init; }
    }

    /// <summary>
    /// Coarse classification used by the wizard to filter the candidate set.
    /// The flag-style members can be combined with bitwise OR for filter UIs.
    /// </summary>
    [Flags]
    public enum EntityCategory
    {
        /// <summary>No categories selected — used to represent an empty filter.</summary>
        None        = 0,
        /// <summary>Type inherits BeepDM's <c>Entity</c> base class.</summary>
        Entity      = 1 << 0,
        /// <summary>Type is decorated with EF Core attributes ([Table], [Key], …).</summary>
        EfCore     = 1 << 1,
        /// <summary>Plain CLR class with no EF decoration (POCO).</summary>
        Poco        = 1 << 2,
        /// <summary>Could not classify (kept for backward compat — typically hidden in the wizard).</summary>
        Unknown     = 1 << 3,
        /// <summary>Shorthand for "show every category" — used by the default filter.</summary>
        All         = Entity | EfCore | Poco | Unknown
    }
}
