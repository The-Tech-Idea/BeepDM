using System;
using System.Collections.Generic;
using System.Reflection;

namespace TheTechIdea.Beep.SetUp.Steps
{
    /// <summary>
    /// Configuration options for <see cref="SchemaSetupStep"/>.
    /// </summary>
    public class SchemaSetupStepOptions
    {
        /// <summary>
        /// .NET entity types whose schemas must be created in the target datasource.
        /// </summary>
        /// <remarks>
        /// Still honoured, and takes precedence over <see cref="EntityTypeNames"/> when both are set.
        /// </remarks>
        [Obsolete("Use EntityTypeNames. CLR Types cannot be serialized into a SetupDefinition, " +
                  "which blocks versioning, CLI/CI use, and remote storage. " +
                  "This property will be removed in the next major version.")]
        public IReadOnlyList<Type> EntityTypes { get; set; }

        /// <summary>
        /// Names of the entity types whose schemas must be created — assembly-qualified, or simple
        /// names resolvable via <c>IAssemblyHandler</c>. At least one is required.
        /// <para>
        /// Names rather than <see cref="Type"/> objects are what make a definition portable: this is
        /// the property a <c>SetupDefinition</c> serializes.
        /// </para>
        /// </summary>
        public IReadOnlyList<string> EntityTypeNames { get; set; }

        /// <summary>
        /// Extra assemblies to register with <c>MigrationManager</c> for type discovery.
        /// Optional — only needed when entity types reside in assemblies not yet loaded.
        /// </summary>
        /// <remarks>Not serializable; use <see cref="ExtraAssemblyNames"/> in a definition.</remarks>
        public IReadOnlyList<Assembly> ExtraAssemblies { get; set; }

        /// <summary>
        /// Names of extra assemblies to probe when resolving <see cref="EntityTypeNames"/>.
        /// The serializable counterpart to <see cref="ExtraAssemblies"/>.
        /// </summary>
        public IReadOnlyList<string> ExtraAssemblyNames { get; set; }

        /// <summary>
        /// Pass <c>true</c> to ask <c>MigrationManager</c> to auto-detect foreign-key
        /// relationships between entity types.  Default: <c>true</c>.
        /// </summary>
        public bool DetectRelationships { get; set; } = true;

        /// <summary>
        /// When <c>true</c>, treat any policy warning as a blocking error.
        /// Overrides <see cref="SetupOptions.StrictPolicyMode"/> for this step only
        /// when explicitly set.  Default: inherits from <see cref="SetupOptions.StrictPolicyMode"/>.
        /// </summary>
        public bool? StrictPolicyMode { get; set; }

        /// <summary>
        /// Override the approver label stamped on the migration plan.
        /// Default: <c>"SetupWizard"</c>.
        /// </summary>
        public string ApproverLabel { get; set; } = "SetupWizard";
    }
}
