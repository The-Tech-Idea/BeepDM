using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.ETL
{
    public partial class ETLEntityProcessor
    {
        public delegate object TransformRecordDelegate(object sourceRecord);

        /// <summary>
        /// Validates a list of records against custom rules.
        /// </summary>
        /// <param name="records">The list of records to validate.</param>
        /// <param name="validationRules">The custom validation rules.</param>
        /// <returns>A tuple containing valid and invalid records.</returns>
        public (List<object> ValidRecords, List<object> InvalidRecords) ValidateRecords(
            IEnumerable<object> records,
            Func<object, bool> validationRules)
        {
            var validRecords = new List<object>();
            var invalidRecords = new List<object>();

            foreach (var record in records)
            {
                if (validationRules(record))
                {
                    validRecords.Add(record);
                }
                else
                {
                    invalidRecords.Add(record);
                }
            }

            return (validRecords, invalidRecords);
        }

        /// <summary>
        /// Transforms a list of records using a custom delegate.
        /// </summary>
        /// <param name="records">The list of records to transform.</param>
        /// <param name="transformDelegate">The transformation logic.</param>
        /// <returns>A list of transformed records.</returns>
        public List<object> TransformRecords(
            IEnumerable<object> records,
            TransformRecordDelegate transformDelegate)
        {
            var transformedRecords = new List<object>();

            foreach (var record in records)
            {
                var transformed = transformDelegate(record);
                if (transformed != null)
                {
                    transformedRecords.Add(transformed);
                }
            }

            return transformedRecords;
        }

        /// <summary>
        /// Processes a batch of records with optional parallelism.
        /// </summary>
        /// <param name="records">The records to process.</param>
        /// <param name="processAction">The processing logic for each record.</param>
        /// <param name="parallel">Whether to process the records in parallel.</param>
        public async Task ProcessRecordsAsync(
            IEnumerable<object> records,
            Func<object, Task> processAction,
            bool parallel = false)
        {
            if (parallel)
            {
                var tasks = records.Select(record => processAction(record));
                await Task.WhenAll(tasks);
            }
            else
            {
                foreach (var record in records)
                {
                    await processAction(record);
                }
            }
        }

       
    }

    
}
