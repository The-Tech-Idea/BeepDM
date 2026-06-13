using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using static TheTechIdea.Beep.SetUp.StepErrorHelpers;

namespace TheTechIdea.Beep.SetUp.Seeding
{
    /// <summary>
    /// Convenience base for seeders that insert a fixed, immutable list of reference records
    /// (status codes, role types, country codes, etc.) into a single entity/table.
    /// </summary>
    /// <typeparam name="T">The entity type whose records are inserted.</typeparam>
    public abstract class ReferenceDataSeederBase<T> : SeederBase
        where T : class, new()
    {
        /// <summary>Returns the full set of reference records to insert.</summary>
        protected abstract IReadOnlyList<T> GetRecords();

        /// <inheritdoc/>
        protected override IErrorsInfo SeedCore(IDataSource dataSource, IDMEEditor editor,
            IProgress<PassedArgs> progress)
        {
            var records = GetRecords();
            int total = records.Count;

            for (int i = 0; i < total; i++)
            {
                var result = dataSource.InsertEntity(TargetEntityName, records[i]);
                if (result.Flag == Errors.Failed)
                    return Fail(
                        $"Insert failed for record {i + 1}/{total} in '{TargetEntityName}': {result.Message}",
                        result.Ex);

                StepErrorHelpers.Report(progress,
                    (int)((i + 1) * 100.0 / total),
                    $"Seeding {SeederName}: {i + 1}/{total}");
            }

            return Ok($"Seeded {total} records into '{TargetEntityName}'.");
        }
    }
}
