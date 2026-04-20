namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// Classifies a single <see cref="SchemaDriftEntry"/> produced by
    /// <see cref="IDistributedSchemaService.DetectSchemaDriftAsync"/>.
    /// </summary>
    /// <remarks>
    /// Numeric values are stable so that persisted drift reports stay
    /// comparable across versions. Append new members — never reorder.
    /// </remarks>
    public enum SchemaDriftKind
    {
        /// <summary>The entity is not present on the shard at all.</summary>
        MissingEntity = 0,

        /// <summary>The shard has a column that the reference schema does not.</summary>
        ExtraColumn = 1,

        /// <summary>The shard is missing a column that the reference schema declares.</summary>
        MissingColumn = 2,

        /// <summary>Column type / length / precision differs from the reference.</summary>
        ColumnTypeMismatch = 3,

        /// <summary>Nullability differs from the reference.</summary>
        ColumnNullabilityMismatch = 4,

        /// <summary>Primary-key membership differs from the reference.</summary>
        PrimaryKeyMismatch = 5,

        /// <summary>Auto-increment / identity flag differs from the reference.</summary>
        IdentityMismatch = 6,

        /// <summary>The shard responded with an error while sampling the structure.</summary>
        SamplingFailed = 99
    }
}
