using System;

namespace TheTechIdea.Beep.Editor.EntityDiscovery
{
    /// <summary>
    /// Filter / scope options for <see cref="EntityDiscoveryService"/> discovery methods.
    /// The wizard builds one of these from its UI state; the engine applies it
    /// uniformly so the UI never has to reach into the reflection layer.
    /// </summary>
    public class EntityDiscoveryOptions
    {
        // ── Scope ─────────────────────────────────────────────────────────────

        /// <summary>How the service picks the set of assemblies to scan. Default
        /// is <see cref="DiscoveryScope.Project"/> — entry assembly + its user-code
        /// references. Set to <see cref="DiscoveryScope.AllLoaded"/> to include the
        /// whole AppDomain (noisy, opt-in). <see cref="DiscoveryScope.Explicit"/>
        /// requires <see cref="Assemblies"/> to be set.</summary>
        public DiscoveryScope Scope { get; init; } = DiscoveryScope.Project;

        /// <summary>Explicit list of assemblies for <see cref="DiscoveryScope.Explicit"/>. Ignored otherwise.</summary>
        public System.Collections.Generic.IReadOnlyList<System.Reflection.Assembly> Assemblies { get; init; }

        /// <summary>If set, only types whose namespace starts with this prefix (or equals it) are returned.</summary>
        public string Namespace { get; init; }

        /// <summary>If true, sub-namespaces under <see cref="Namespace"/> are also scanned. Ignored when <see cref="Namespace"/> is null/empty.</summary>
        public bool IncludeSubNamespaces { get; init; } = true;

        /// <summary>Optional assembly filter — only types in this assembly are returned. Null means "all assemblies in the active scope".</summary>
        public System.Reflection.Assembly Assembly { get; init; }

        // ── Free-text filter (case-insensitive substring match) ──────────────

        /// <summary>Match against <see cref="DiscoveredEntity.Name"/> OR <see cref="DiscoveredEntity.FullName"/> OR <see cref="DiscoveredEntity.Namespace"/>.</summary>
        public string NameFilter { get; init; }

        // ── Category filter (bit flags so the UI can pick any combination) ──

        /// <summary>Categories to keep. Default = all four.</summary>
        public EntityCategory Categories { get; init; } = EntityCategory.All;

        // ── Toggles ──────────────────────────────────────────────────────────

        /// <summary>If true, abstract types and interfaces are hidden. Default true — they can't be migrated anyway.</summary>
        public bool ExcludeAbstract { get; init; } = true;

        /// <summary>If true, open generic type definitions are hidden. Default true.</summary>
        public bool ExcludeOpenGenerics { get; init; } = true;

        /// <summary>If true, types with no parameterless constructor are hidden. Default false — the wizard can decide.</summary>
        public bool RequireParameterlessConstructor { get; init; } = false;

        // ── Convenience accessors used by the engine ─────────────────────────

        public bool IncludesCategory(EntityCategory category) => (Categories & category) == category;

        public bool PassesFreeText(DiscoveredEntity e)
        {
            if (string.IsNullOrWhiteSpace(NameFilter)) return true;
            var f = NameFilter.Trim();
            return (e.Name?.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)
                || (e.FullName?.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)
                || (e.Namespace?.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public bool PassesNamespace(DiscoveredEntity e)
        {
            if (string.IsNullOrWhiteSpace(Namespace)) return true;
            if (string.IsNullOrEmpty(e.Namespace)) return false;
            if (IncludeSubNamespaces)
                return e.Namespace.Equals(Namespace, StringComparison.Ordinal)
                    || e.Namespace.StartsWith(Namespace + ".", StringComparison.Ordinal);
            return e.Namespace.Equals(Namespace, StringComparison.Ordinal);
        }
    }
}
