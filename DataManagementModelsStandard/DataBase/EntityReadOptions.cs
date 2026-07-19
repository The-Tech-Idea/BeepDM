namespace TheTechIdea.Beep.Editor
{
    /// <summary>How an enum property is stored when a .NET type is read into an <c>EntityStructure</c>.</summary>
    public enum EnumStorageStrategy
    {
        /// <summary>Store as the underlying integer (default). Field type becomes <c>System.Int32</c>.</summary>
        Int,
        /// <summary>Store as the enum member name. Field type becomes <c>System.String</c>.</summary>
        String
    }

    /// <summary>
    /// Options controlling how a .NET class is read into an <see cref="TheTechIdea.Beep.DataBase.EntityStructure"/>
    /// (Phase 7). Defaults reflect the improved reader; each switch can restore the historical behavior
    /// for a caller that depends on it.
    /// </summary>
    public sealed class EntityReadOptions
    {
        /// <summary>How the primary key is detected (attribute, convention, or both).</summary>
        public KeyDetectionStrategy KeyStrategy { get; init; } = KeyDetectionStrategy.AttributeThenConvention;

        /// <summary>Populate <c>EntityStructure.Relations</c> from navigation properties and <c>[ForeignKey]</c>.</summary>
        public bool DetectRelationships { get; init; } = true;

        /// <summary>Populate <c>EntityStructure.Indexes</c> from <c>[Index]</c> attributes and unique fields.</summary>
        public bool ReadIndexes { get; init; } = true;

        /// <summary>How enum fields are stored.</summary>
        public EnumStorageStrategy EnumStorage { get; init; } = EnumStorageStrategy.Int;

        /// <summary>
        /// Honor C# nullable-reference-type annotations for reference-typed columns (<c>string</c> vs
        /// <c>string?</c>). When false, the historical rule (all reference types nullable) applies.
        /// </summary>
        public bool HonorNullableReferenceTypes { get; init; } = true;

        /// <summary>
        /// When true (historical behavior), a convention-detected int/long key is marked auto-increment
        /// even without <c>[DatabaseGenerated(Identity)]</c>. Default false — identity must be declared.
        /// </summary>
        public bool ConventionKeyImpliesIdentity { get; init; } = false;

        /// <summary>Overrides the entity name (else <c>[Table]</c> or the type name).</summary>
        public string EntityNameOverride { get; init; }

        /// <summary>Datasource id stamped onto the structure.</summary>
        public string DatasourceName { get; init; }

        /// <summary>The reader defaults.</summary>
        public static EntityReadOptions Default { get; } = new EntityReadOptions();
    }
}
