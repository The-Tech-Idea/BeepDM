using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Schema
{
    /// <summary>
    /// Compares two <see cref="SchemaSnapshot"/> instances and produces a <see cref="SchemaDriftReport"/>.
    /// </summary>
    public static class SchemaComparator
    {
        public static SchemaDriftReport Compare(SchemaSnapshot baseline, SchemaSnapshot current)
        {
            ArgumentNullException.ThrowIfNull(baseline);
            ArgumentNullException.ThrowIfNull(current);

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
                var drift = Diff(baseField, currField);
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

        private static FieldTypeDrift? Diff(SnapshotField b, SnapshotField c)
        {
            var changes = new List<string>();
            if (!string.Equals(b.DataType, c.DataType, StringComparison.OrdinalIgnoreCase))
                changes.Add($"DataType: {b.DataType} → {c.DataType}");
            if (b.IsNullable != c.IsNullable)
                changes.Add($"IsNullable: {b.IsNullable} → {c.IsNullable}");
            if (b.MaxLength != c.MaxLength && c.MaxLength > 0)
                changes.Add($"MaxLength: {b.MaxLength} → {c.MaxLength}");

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
