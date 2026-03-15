using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Describes the ordered set of fields in a record stream flowing through the pipeline.
    /// Immutable after construction — transformer plugins produce new schemas, never mutate existing ones.
    /// </summary>
    public sealed class PipelineSchema
    {
        /// <summary>Logical name of this schema, typically the entity / table name.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Ordered field definitions.</summary>
        public IReadOnlyList<PipelineField> Fields { get; init; } = Array.Empty<PipelineField>();

        private readonly Dictionary<string, int> _index;

        public PipelineSchema(string name, IEnumerable<PipelineField> fields)
        {
            Name   = name;
            var list = fields.ToList();
            Fields = list.AsReadOnly();
            _index = list.Select((f, i) => (f.Name, i))
                         .ToDictionary(x => x.Name, x => x.i, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Returns the zero-based index of <paramref name="name"/>, or -1 if not found.</summary>
        public int GetFieldIndex(string name) =>
            _index.TryGetValue(name, out var idx) ? idx : -1;

        /// <summary>
        /// Creates a <see cref="PipelineSchema"/> from a BeepDM <see cref="EntityStructure"/>.
        /// Allows sources backed by <c>IDataSource</c> to describe their output schema.
        /// </summary>
        public static PipelineSchema FromEntityStructure(EntityStructure entity)
        {
            var fields = entity.Fields.Select(f => new PipelineField(
                f.FieldName,
                MapBeepType(f.Fieldtype),
                f.IsKey,
                f.AllowDBNull));
            return new PipelineSchema(entity.EntityName, fields);
        }

        private static Type MapBeepType(string beepType) => beepType?.ToLowerInvariant() switch
        {
            "int"     or "integer"          => typeof(int),
            "long"    or "bigint"           => typeof(long),
            "decimal" or "numeric"          => typeof(decimal),
            "double"  or "float"            => typeof(double),
            "bool"    or "boolean"          => typeof(bool),
            "datetime"                      => typeof(DateTime),
            "guid"    or "uniqueidentifier" => typeof(Guid),
            "byte[]"  or "binary"           => typeof(byte[]),
            _                               => typeof(string)
        };
    }
}
