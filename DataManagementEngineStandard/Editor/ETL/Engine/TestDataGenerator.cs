using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Engine
{
    /// <summary>
    /// Generates synthetic <see cref="PipelineRecord"/> streams for local pipeline testing.
    /// Supports deterministic data (from inline dictionaries) and random generation.
    /// </summary>
    public static class TestDataGenerator
    {
        /// <summary>
        /// Creates a <see cref="PipelineSchema"/> from a list of field names.
        /// All fields default to <see cref="string"/> type, nullable, non-key.
        /// </summary>
        public static PipelineSchema CreateSchema(string entityName, IEnumerable<string> fieldNames)
        {
            var fields = fieldNames.Select(n => new PipelineField(n, typeof(string)));
            return new PipelineSchema(entityName, fields);
        }

        /// <summary>
        /// Creates a schema with typed field definitions.
        /// </summary>
        public static PipelineSchema CreateSchema(string entityName, IEnumerable<(string Name, Type Type)> fields)
        {
            return new PipelineSchema(entityName,
                fields.Select(f => new PipelineField(f.Name, f.Type)));
        }

        /// <summary>
        /// Converts a list of dictionaries into an async record stream.
        /// Used by <see cref="PipelineTestHarness"/> to inject inline test data.
        /// </summary>
        public static async IAsyncEnumerable<PipelineRecord> FromDictionaries(
            PipelineSchema schema,
            IReadOnlyList<Dictionary<string, object?>> rows,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            foreach (var row in rows)
            {
                if (token.IsCancellationRequested) yield break;

                var record = new PipelineRecord(schema);
                foreach (var kv in row)
                    record[kv.Key] = kv.Value;
                yield return record;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// Generates N records with auto-incremented integer values.
        /// Useful for throughput and stress testing.
        /// </summary>
        public static async IAsyncEnumerable<PipelineRecord> GenerateSequential(
            PipelineSchema schema,
            int count,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            for (int i = 0; i < count; i++)
            {
                if (token.IsCancellationRequested) yield break;

                var record = new PipelineRecord(schema);
                for (int f = 0; f < schema.Fields.Count; f++)
                {
                    var field = schema.Fields[f];
                    record.Values[f] = GenerateValue(field, i);
                }
                yield return record;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// Generates a single value appropriate for the field type and row index.
        /// </summary>
        private static object? GenerateValue(PipelineField field, int rowIndex)
        {
            if (field.DataType == typeof(int))    return rowIndex;
            if (field.DataType == typeof(long))   return (long)rowIndex;
            if (field.DataType == typeof(double))  return (double)rowIndex;
            if (field.DataType == typeof(decimal)) return (decimal)rowIndex;
            if (field.DataType == typeof(bool))    return rowIndex % 2 == 0;
            if (field.DataType == typeof(DateTime)) return DateTime.UtcNow.AddMinutes(rowIndex);
            // default: string
            return $"{field.Name}_{rowIndex}";
        }
    }
}
