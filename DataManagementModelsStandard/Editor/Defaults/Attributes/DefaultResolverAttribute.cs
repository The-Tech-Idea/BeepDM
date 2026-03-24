using System;

namespace TheTechIdea.Beep.Editor.Defaults.Attributes
{
    /// <summary>
    /// Decorate any <c>IDefaultValueResolver</c> implementation with this attribute so that
    /// <c>DefaultResolverRegistry</c> can auto-discover and register it at startup via
    /// <c>AssemblyHandler</c> — the same pattern as <c>[AddinAttribute]</c> connectors,
    /// <c>[PipelinePluginAttribute]</c> pipeline plugins, and <c>[FileReaderAttribute]</c>
    /// file-format readers.
    /// </summary>
    /// <example>
    /// <code>
    /// [DefaultResolver("TenantContextResolver", "Tenant Context Resolver",
    ///     SupportedTokens = "TENANT,TENANTID")]
    /// public class TenantContextResolver : IDefaultValueResolver
    /// {
    ///     public string ResolverName => "TenantContextResolver";
    ///     // …
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DefaultResolverAttribute : Attribute
    {
        /// <summary>
        /// Unique resolver name used for registration and look-up, e.g. "TenantContextResolver".
        /// Must match <c>IDefaultValueResolver.ResolverName</c>.
        /// </summary>
        public string ResolverName { get; }

        /// <summary>Human-readable display name shown in tooling UI, e.g. "Tenant Context Resolver".</summary>
        public string DisplayName { get; }

        /// <summary>Optional description of what this resolver does.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Author or team name.</summary>
        public string Author { get; set; } = "The-Tech-Idea";

        /// <summary>Semantic version string, e.g. "1.0.0".</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>Optional path to an icon (used by designer tooling).</summary>
        public string IconPath { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated list of rule tokens this resolver handles, e.g. "TENANT,TENANTID,ORGID".
        /// Used by tooling for documentation and auto-complete — not enforced at runtime.
        /// </summary>
        public string SupportedTokens { get; set; } = string.Empty;

        /// <summary>
        /// Initialises the attribute with the required identifying information.
        /// </summary>
        /// <param name="resolverName">
        /// Unique name for registration (e.g. "TenantContextResolver").
        /// Should match <c>IDefaultValueResolver.ResolverName</c>.
        /// </param>
        /// <param name="displayName">Human-readable label for tooling UI.</param>
        public DefaultResolverAttribute(string resolverName, string displayName)
        {
            ResolverName = resolverName ?? throw new ArgumentNullException(nameof(resolverName));
            DisplayName  = displayName  ?? throw new ArgumentNullException(nameof(displayName));
        }
    }
}
