using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.FileManager.Readers;
using TheTechIdea.Beep.FileManager.Resilience;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.FileManager
{
    // ── Phase 5 types ────────────────────────────────────────────────────────

    /// <summary>Controls how write operations respond to row validation failures.</summary>
    public enum RowValidationMode
    {
        /// <summary>Accept all rows regardless of validation outcome.</summary>
        Accept,
        /// <summary>Log warnings but continue writing.</summary>
        Warn,
        /// <summary>Reject rows that fail validation; send them to the dead-letter store.</summary>
        Strict
    }

    /// <summary>A structured, machine-readable diagnostic emitted by FileDataSource operations.</summary>
    public sealed class FileOperationDiagnostic
    {
        /// <summary>Short machine-readable reason code, e.g. "NULL_NOT_ALLOWED", "TYPE_MISMATCH".</summary>
        public string            Code        { get; init; }
        public string            Message     { get; init; }
        public DiagnosticSeverity Severity   { get; init; }
        public string            EntityName  { get; init; }
        public string            FieldName   { get; init; }
        public long?             RowIndex    { get; init; }
        /// <summary>The operation that triggered this diagnostic (Insert, Update, Delete, Query).</summary>
        public string            Operation   { get; init; }
        public DateTimeOffset    OccurredAt  { get; init; } = DateTimeOffset.UtcNow;
    }

    /// <summary>Result of validating a single data row against an entity's field definitions.</summary>
    public sealed class RowValidationResult
    {
        public bool IsValid { get; init; }
        public IReadOnlyList<FileOperationDiagnostic> Issues { get; init; }
            = Array.Empty<FileOperationDiagnostic>();
    }

    /// <summary>
    /// Diagnostics / validation partial — ValidateRow, CanConvert, dead-letter helpers.
    /// </summary>
    public partial class FileDataSource
    {
        // ── Row validation ────────────────────────────────────────────────────

        /// <summary>
        /// Validates a row of raw string values against the entity's field definitions.
        /// Emits <see cref="FileOperationDiagnostic"/> entries for null violations and type mismatches.
        /// </summary>
        internal RowValidationResult ValidateRow(
            EntityStructure entity,
            string[]        values,
            string[]        headers,
            long            rowIndex,
            string          operation = "Write")
        {
            if (entity == null || entity.Fields.Count == 0)
                return new RowValidationResult { IsValid = true };

            var issues  = new List<FileOperationDiagnostic>();
            var hIdx    = BuildHeaderIndex(headers);

            foreach (EntityField field in entity.Fields)
            {
                if (!hIdx.TryGetValue(field.FieldName, out int idx)) continue;
                string raw = idx < values.Length ? values[idx] : null;

                // Null / empty check
                if (!field.AllowDBNull && string.IsNullOrEmpty(raw))
                {
                    issues.Add(new FileOperationDiagnostic
                    {
                        Code       = "NULL_NOT_ALLOWED",
                        Message    = $"Null value in non-nullable field '{field.FieldName}'",
                        Severity   = DiagnosticSeverity.Error,
                        EntityName = entity.EntityName,
                        FieldName  = field.FieldName,
                        RowIndex   = rowIndex,
                        Operation  = operation
                    });
                    continue;
                }

                // Type conversion check
                if (!string.IsNullOrEmpty(raw) && !CanConvertValue(raw, field.Fieldtype))
                {
                    issues.Add(new FileOperationDiagnostic
                    {
                        Code       = "TYPE_MISMATCH",
                        Message    = $"Cannot convert '{raw}' to {field.Fieldtype} for field '{field.FieldName}'",
                        Severity   = DiagnosticSeverity.Warning,
                        EntityName = entity.EntityName,
                        FieldName  = field.FieldName,
                        RowIndex   = rowIndex,
                        Operation  = operation
                    });
                }
            }

            bool hasErrors = issues.Any(d => d.Severity == DiagnosticSeverity.Error);
            return new RowValidationResult { IsValid = !hasErrors, Issues = issues };
        }

        /// <summary>
        /// Handles the outcome of <see cref="ValidateRow"/> according to <see cref="ValidationMode"/>:
        /// Accept → always pass; Warn → log issues; Strict → reject row.
        /// Returns true when the row should be written.
        /// </summary>
        internal bool ShouldWriteRow(RowValidationResult result, string operation, long rowIndex,
                                     string rawLine = null, string jobId = null)
        {
            if (result.IsValid) return true;
            if (ValidationMode == RowValidationMode.Accept) return true;

            // Log warnings
            foreach (var issue in result.Issues)
            {
                DMEEditor?.AddLogMessage(
                    issue.Severity == DiagnosticSeverity.Error ? "Error" : "Warn",
                    $"[{operation}] Row {rowIndex}: [{issue.Code}] {issue.Message}",
                    DateTime.Now, (int)rowIndex, issue.EntityName, Errors.Warning);
            }

            if (ValidationMode == RowValidationMode.Warn) return true;

            // Strict — send to dead-letter and reject
            if (DeadLetterStore != null)
            {
                var entry = new DeadLetterEntry
                {
                    JobId          = jobId ?? DatasourceName,
                    SourceRowIndex = rowIndex,
                    RawLine        = rawLine,
                    ErrorCategory  = result.Issues.FirstOrDefault()?.Code,
                    ErrorMessage   = string.Join("; ", result.Issues.Select(i => i.Message))
                };
                // Fire-and-forget; non-critical path
                _ = DeadLetterStore.WriteAsync(entry);
            }

            return false;
        }

        // ── Conversion helpers ────────────────────────────────────────────────

        private static bool CanConvertValue(string raw, string typeName)
        {
            Type target = Type.GetType(typeName) ?? typeof(string);
            if (target == typeof(string))   return true;
            if (target == typeof(int))      return int.TryParse(raw, out _);
            if (target == typeof(long))     return long.TryParse(raw, out _);
            if (target == typeof(decimal))  return decimal.TryParse(raw, NumberStyles.Any,
                                                CultureInfo.InvariantCulture, out _);
            if (target == typeof(double))   return double.TryParse(raw, NumberStyles.Any,
                                                CultureInfo.InvariantCulture, out _);
            if (target == typeof(bool))     return bool.TryParse(raw, out _);
            if (target == typeof(DateTime)) return DateTime.TryParse(raw, out _);
            return true;
        }
    }
}
