using System;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.SchemaMigration
{
    /// <summary>
    /// Marks a class as a Tier-1 <see cref="ISchemaMigrationProvider"/> override for a
    /// specific <see cref="DataSourceType"/>. Mirrors the existing
    /// <c>[AddinAttribute]</c> discovery pattern. The class must expose a public
    /// constructor taking a single <see cref="IDataSource"/> argument (the live data source
    /// being migrated); the registry invokes it reflectively.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SchemaMigrationProviderAttribute : Attribute
    {
        /// <summary>The data source type this provider overrides.</summary>
        public DataSourceType DataSourceType { get; }

        /// <summary>The data source category this provider belongs to.</summary>
        public DatasourceCategory Category { get; }

        public SchemaMigrationProviderAttribute(DataSourceType dataSourceType, DatasourceCategory category)
        {
            DataSourceType = dataSourceType;
            Category = category;
        }
    }
}
