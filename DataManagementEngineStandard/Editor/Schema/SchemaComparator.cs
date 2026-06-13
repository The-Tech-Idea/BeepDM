using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Schema
{
    public sealed class SchemaComparator : ISchemaComparator
    {
        SchemaDriftReport ISchemaComparator.Compare(SchemaSnapshot baseline, SchemaSnapshot current) =>
            Compare(baseline, current, SchemaComparisonOptions.Default);

        SchemaDriftReport ISchemaComparator.Compare(SchemaSnapshot baseline, SchemaSnapshot current, SchemaComparisonOptions options) =>
            Compare(baseline, current, options);

        public SchemaDriftReport Compare(SchemaSnapshot baseline, SchemaSnapshot current) =>
            Compare(baseline, current, SchemaComparisonOptions.Default);

        public SchemaDriftReport Compare(SchemaSnapshot baseline, SchemaSnapshot current, SchemaComparisonOptions options)
        {
            ArgumentNullException.ThrowIfNull(baseline);
            ArgumentNullException.ThrowIfNull(current);
            if (options == null) options = SchemaComparisonOptions.Default;

            var report = new SchemaDriftReport { Baseline = baseline, Current = current };
            var baseMap = BuildMap(baseline.Fields);
            var currMap = BuildMap(current.Fields);

            foreach (var (name, field) in currMap)
                if (!baseMap.ContainsKey(name))
                    report.AddedFields.Add(field);

            foreach (var (name, field) in baseMap)
                if (!currMap.ContainsKey(name))
                    report.RemovedFields.Add(field);

            foreach (var (name, baseField) in baseMap)
            {
                if (!currMap.TryGetValue(name, out var currField)) continue;
                var drift = Diff(baseField, currField, options);
                if (drift != null)
                    report.AlteredFields.Add(drift);
            }

            return report;
        }

        private static Dictionary<string, SnapshotField> BuildMap(IEnumerable<SnapshotField> fields)
        {
            var map = new Dictionary<string, SnapshotField>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in fields)
                map[f.Name] = f;
            return map;
        }

        private static FieldTypeDrift? Diff(SnapshotField b, SnapshotField c, SchemaComparisonOptions options)
        {
            var changes = new List<string>();

            var stringComparison = options.IgnoreCaseInTypeNames
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (!string.Equals(b.DataType, c.DataType, stringComparison))
                changes.Add($"DataType: {b.DataType} → {c.DataType}");

            if (options.IncludeNullableChanges && b.IsNullable != c.IsNullable)
                changes.Add($"IsNullable: {b.IsNullable} → {c.IsNullable}");

            if (options.NormalizeMaxLengthZero)
            {
                var bLen = b.MaxLength == 0 ? -1 : b.MaxLength;
                var cLen = c.MaxLength == 0 ? -1 : c.MaxLength;
                if (bLen != cLen)
                    changes.Add($"MaxLength: {b.MaxLength} → {c.MaxLength}");
            }
            else if (b.MaxLength != c.MaxLength)
            {
                changes.Add($"MaxLength: {b.MaxLength} → {c.MaxLength}");
            }

            if (options.IncludePrecisionScale)
            {
                if (b.Precision != c.Precision)
                    changes.Add($"Precision: {b.Precision} → {c.Precision}");
                if (b.Scale != c.Scale)
                    changes.Add($"Scale: {b.Scale} → {c.Scale}");
            }

            if (changes.Count == 0) return null;

            return new FieldTypeDrift
            {
                FieldName    = b.Name,
                BaselineType = b.DataType,
                CurrentType  = c.DataType,
                Description  = string.Join("; ", changes)
            };
        }
    }
}
