using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// A single row flowing through the pipeline.
    /// Carries a schema reference, typed field values, and a metadata bag.
    /// </summary>
    public sealed class PipelineRecord
    {
        /// <summary>
        /// Ordered field values. Each index aligns with the corresponding
        /// <see cref="PipelineSchema.Fields"/> entry.
        /// </summary>
        public object?[] Values { get; }

        /// <summary>Schema this record conforms to.</summary>
        public PipelineSchema Schema { get; }

        /// <summary>
        /// Metadata bag: source row number, file path, correlation ID, etc.
        /// Keys are defined as constants in <see cref="PipelineRecordMeta"/>.
        /// </summary>
        public Dictionary<string, object> Meta { get; } = new();

        public PipelineRecord(PipelineSchema schema)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            Values = new object?[schema.Fields.Count];
        }

        /// <summary>Gets or sets a field value by name (case-insensitive).</summary>
        public object? this[string fieldName]
        {
            get
            {
                var idx = Schema.GetFieldIndex(fieldName);
                return idx >= 0 ? Values[idx] : null;
            }
            set
            {
                var idx = Schema.GetFieldIndex(fieldName);
                if (idx >= 0) Values[idx] = value;
            }
        }

        /// <summary>Gets a strongly-typed field value, returning <c>default</c> on type mismatch.</summary>
        public T? Get<T>(string fieldName)
        {
            var v = this[fieldName];
            return v is T typedVal ? typedVal : default;
        }

        /// <summary>
        /// Shallow-clones this record.
        /// Used by transformer steps that need to mutate values without affecting upstream records.
        /// </summary>
        public PipelineRecord Clone()
        {
            var clone = new PipelineRecord(Schema);
            Array.Copy(Values, clone.Values, Values.Length);
            foreach (var kv in Meta) clone.Meta[kv.Key] = kv.Value;
            return clone;
        }
    }
}
