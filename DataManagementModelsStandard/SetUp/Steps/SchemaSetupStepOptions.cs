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
        /// At least one type is required.
        /// </summary>
        public IReadOnlyList<Type> EntityTypes { get; set; }

        /// <summary>
        /// Extra assemblies to register with <c>MigrationManager</c> for type discovery.
        /// Optional — only needed when entity types reside in assemblies not yet loaded.
        /// </summary>
        public IReadOnlyList<Assembly> ExtraAssemblies { get; set; }

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
