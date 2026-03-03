using System;
using System.Collections.Generic;
using System.Reflection;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.Interfaces;

namespace TheTechIdea.Beep.Editor.Importing.Quality
{
    /// <summary>
    /// Evaluates a set of <see cref="IDataQualityRule"/> instances against a single record.
    /// Called per-record inside the batch processing pipeline.
    /// </summary>
    public static class DataQualityEvaluator
    {
        /// <summary>
        /// Evaluates all rules for the given record.
        /// </summary>
        /// <param name="record">The source record to evaluate.</param>
        /// <param name="rules">Rules to apply.</param>
        /// <param name="status">Live status — blocked/quarantined/warned counters are incremented.</param>
        /// <param name="errorStore">Optional error store to persist quarantined records.</param>
        /// <param name="contextKey">Pipeline identifier used as the error store partition key.</param>
        /// <param name="batchNumber">Current batch index (for error record metadata).</param>
        /// <param name="recordIndex">Index within the batch (for error record metadata).</param>
        /// <returns>
        /// <c>true</c> when the record should be written to the destination;
        /// <c>false</c> when it was blocked.
        /// </returns>
        public static bool Evaluate(
            object                record,
            IEnumerable<IDataQualityRule> rules,
            ImportStatus          status,
            IImportErrorStore?    errorStore,
            string                contextKey,
            int                   batchNumber,
            int                   recordIndex)
        {
            if (rules == null) return true;

            bool blocked = false;

            foreach (var rule in rules)
            {
                var fieldValue = ExtractFieldValue(record, rule.FieldName);
                bool pass = rule.Evaluate(fieldValue, record);
                if (pass) continue;

                var errorRecord = new ImportErrorRecord
                {
                    ContextKey  = contextKey,
                    BatchNumber = batchNumber,
                    RecordIndex = recordIndex,
                    RawRecord   = record,
                    Reason      = rule.FailureMessage(fieldValue),
                    RuleName    = rule.RuleName,
                    OccurredAt  = DateTime.UtcNow
                };

                switch (rule.OnFailure)
                {
                    case DataQualityAction.Block:
                        lock (status) { status.RecordsBlocked++; }
                        errorStore?.SaveAsync(errorRecord).GetAwaiter().GetResult();
                        blocked = true;
                        break;

                    case DataQualityAction.Quarantine:
                        lock (status) { status.RecordsQuarantined++; }
                        errorStore?.SaveAsync(errorRecord).GetAwaiter().GetResult();
                        blocked = true;
                        break;

                    case DataQualityAction.Warn:
                        lock (status) { status.RecordsWarned++; }
                        break;
                }
            }

            return !blocked;
        }

        private static object? ExtractFieldValue(object record, string fieldName)
        {
            if (record == null) return null;

            var prop = record.GetType().GetProperty(fieldName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return prop?.GetValue(record);
        }
    }
}
