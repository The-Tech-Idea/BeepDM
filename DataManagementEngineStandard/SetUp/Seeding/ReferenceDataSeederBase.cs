using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using static TheTechIdea.Beep.SetUp.StepErrorHelpers;

namespace TheTechIdea.Beep.SetUp.Seeding
{
    /// <summary>
    /// Convenience base for seeders that insert a fixed, immutable list of reference records
    /// (status codes, role types, country codes, etc.) into a single entity/table.
    ///
    /// <para>
    /// Inserts run inside a transaction when the datasource supports one, so a partial failure
    /// leaves no rows behind. This matters because idempotency here is count-based
    /// (<see cref="IsAlreadySeeded"/>): a half-inserted batch that survived would either be
    /// re-inserted as duplicates or — worse — be mistaken for a complete seed.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The entity type whose records are inserted.</typeparam>
    public abstract class ReferenceDataSeederBase<T> : SeederBase
        where T : class, new()
    {
        /// <summary>Returns the full set of reference records to insert.</summary>
        protected abstract IReadOnlyList<T> GetRecords();

        /// <summary>
        /// When true (default) the insert batch is wrapped in a datasource transaction.
        /// Set false for datasources whose transaction support is unreliable — note that a
        /// partial failure then leaves committed rows requiring manual cleanup.
        /// </summary>
        protected virtual bool UseTransaction => true;

        /// <summary>
        /// Count-based idempotency: seeded only when the target already holds at least as many
        /// rows as this seeder would insert.
        /// <para>
        /// The inherited "any rows at all" check is unsafe for reference data — a batch that
        /// failed partway (49 of 100 rows) would report "already seeded" forever, silently
        /// leaving records 50-100 missing.
        /// </para>
        /// </summary>
        public override bool IsAlreadySeeded(IDataSource dataSource, IDMEEditor editor)
        {
            if (dataSource == null) return false;
            if (!dataSource.CheckEntityExist(TargetEntityName)) return false;

            var expected = GetRecords()?.Count ?? 0;
            if (expected == 0) return true;

            var rows = dataSource.GetEntity(TargetEntityName, null);
            var existing = rows?.Count() ?? 0;

            return existing >= expected;
        }

        /// <inheritdoc/>
        protected override IErrorsInfo SeedCore(IDataSource dataSource, IDMEEditor editor,
            IProgress<PassedArgs> progress)
        {
            var records = GetRecords();
            int total = records?.Count ?? 0;
            if (total == 0)
                return Ok($"No records to seed into '{TargetEntityName}'.");

            bool inTransaction = UseTransaction && TryBeginTransaction(dataSource, editor);

            try
            {
                for (int i = 0; i < total; i++)
                {
                    var result = dataSource.InsertEntity(TargetEntityName, records[i]);
                    if (result.Flag == Errors.Failed)
                    {
                        var detail = RollbackAndDescribe(dataSource, inTransaction, i);
                        return Fail(
                            $"Insert failed for record {i + 1}/{total} in '{TargetEntityName}': " +
                            $"{result.Message} {detail}",
                            result.Ex);
                    }

                    StepErrorHelpers.Report(progress,
                        (int)((i + 1) * 100.0 / total),
                        $"Seeding {SeederName}: {i + 1}/{total}");
                }

                if (inTransaction)
                {
                    var commit = dataSource.Commit(new PassedArgs());
                    if (commit.Flag != Errors.Ok)
                        return Fail(
                            $"Seeding '{TargetEntityName}' could not commit: {commit.Message}",
                            commit.Ex);
                }
            }
            catch (Exception)
            {
                // Roll back before the throw reaches SeederBase.Seed's catch-all, which would
                // otherwise return a failure while leaving the transaction open.
                RollbackAndDescribe(dataSource, inTransaction, -1);
                throw;
            }

            return Ok($"Seeded {total} records into '{TargetEntityName}'.");
        }

        /// <summary>
        /// Begins a transaction, tolerating datasources that don't support one. Returns false
        /// when the batch must proceed unprotected.
        /// </summary>
        private bool TryBeginTransaction(IDataSource dataSource, IDMEEditor editor)
        {
            try
            {
                var begin = dataSource.BeginTransaction(new PassedArgs());
                if (begin != null && begin.Flag == Errors.Ok) return true;

                editor?.AddLogMessage("Beep",
                    $"Seeder '{SeederId}': datasource '{dataSource.DatasourceName}' did not begin a " +
                    "transaction; a partial failure will leave rows behind.",
                    DateTime.Now, 0, null, Errors.Information);
                return false;
            }
            catch (NotImplementedException)
            {
                // Expected for datasources without transaction support (files, REST, some NoSQL).
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Rolls back when in a transaction and returns text describing what the caller is left
        /// with — an unprotected partial batch must be reported, never implied to be clean.
        /// </summary>
        private string RollbackAndDescribe(IDataSource dataSource, bool inTransaction, int failedIndex)
        {
            if (inTransaction)
            {
                // EndTransaction without Commit is this codebase's rollback (see UnitofWork.Commit).
                try { dataSource.EndTransaction(new PassedArgs()); }
                catch (Exception) { return "Rollback attempt failed; target may hold partial data."; }
                return "Transaction rolled back; no rows were committed.";
            }

            return failedIndex > 0
                ? $"WARNING: {failedIndex} record(s) were already committed and were NOT rolled back " +
                  $"(datasource has no transaction support). '{TargetEntityName}' needs manual cleanup " +
                  "before re-running."
                : "WARNING: partial data may remain (datasource has no transaction support).";
        }
    }
}
