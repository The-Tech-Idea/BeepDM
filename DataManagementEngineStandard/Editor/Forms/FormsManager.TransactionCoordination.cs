using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Real transaction coordination for <c>CommitFormAsync</c>. Groups every
    /// dirty block in commit scope by its owning <see cref="IDataSource"/> —
    /// not by form, since one form's blocks can span several datasources and
    /// several forms can share one — and, for whichever datasources actually
    /// implement the <see cref="IDataSource"/> transaction triple
    /// (<c>BeginTransaction</c>/<c>Commit</c>/<c>EndTransaction</c>), wraps
    /// every block commit on that datasource in one real transaction so they
    /// all become durable together or not at all.
    /// </summary>
    public partial class FormsManager
    {
        #region G0.24 — Two-Phase Commit Coordination

        /// <summary>
        /// Attempts a commit across every form/block in scope, opening a real
        /// transaction on each transaction-capable datasource first.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Before this pass, this method's own doc comment claimed it
        /// "optionally wraps [the commit] in a single source-level
        /// transaction if every participating form's data source supports
        /// transactions" and "rolls back all committed forms" on failure —
        /// neither was true. No transaction was ever opened on any
        /// datasource; <c>UnitOfWork.Commit()</c> persisted each block's
        /// writes immediately and independently, so a failure partway
        /// through a multi-block commit left the already-saved blocks
        /// permanently committed. The "rollback" call on those already-saved
        /// blocks discarded in-memory dirty state that no longer existed —
        /// it never touched the database rows that had already been written.
        /// The single-form case (the more common one) had no coordination at
        /// all: several dirty blocks in one form, on one or more
        /// datasources, committed one at a time with nothing tying them
        /// together, so this fix removes that special case entirely and runs
        /// every commit — single- or cross-form — through the same real
        /// coordination. (2026-08-22)
        /// </para>
        /// <para>
        /// <b>Prepare / commit, not full ACID 2PC.</b> Phase 1 (prepare) opens
        /// a transaction per datasource and runs every form's normal
        /// ON-INSERT + <c>SaveDirtyBlocksAsync</c> path inside it — nothing is
        /// durable yet on a transactional datasource, so a prepare failure is
        /// a true, clean abort (<c>EndTransaction</c> on every opened
        /// transaction). Phase 2 (commit) calls <c>Commit</c> on every opened
        /// transaction. A failure <em>during</em> phase 2 — after at least one
        /// datasource has already durably committed — is the one outcome no
        /// software-only coordinator across independent database engines can
        /// fully prevent without a real distributed transaction coordinator
        /// (MS DTC or equivalent); this method does not pretend otherwise. It
        /// logs exactly which datasources committed and which did not rather
        /// than reporting success or attempting to "roll back" a write that
        /// is already durable.
        /// </para>
        /// <para>
        /// A datasource whose provider does not implement the transaction
        /// triple (<c>JsonDataSource</c>, <c>CSVDataSource</c>, and other
        /// file-backed sources throw <see cref="NotImplementedException"/>)
        /// has no ACID mechanism to open — its blocks keep today's
        /// best-effort, immediately-durable commit behaviour, same as before
        /// this fix. True atomicity across a transactional and a
        /// non-transactional datasource together is not achievable without
        /// one of them changing engines.
        /// </para>
        /// <para>
        /// This is deliberately narrower than
        /// <c>IDistributedTransactionCoordinator</c> (see
        /// <c>DistributedDatasource/Distributed</c>), which already
        /// implements full 2PC/saga coordination for datasources that are
        /// shards under one <see cref="TheTechIdea.Beep.Distributed.DistributedDataSource"/>.
        /// This method does not duplicate that — it closes the much more
        /// common gap of several independent, ordinary transaction-capable
        /// datasources (e.g. two SQL Server connections) being committed
        /// together from one form or call-stack of forms.
        /// </para>
        /// </remarks>
        private async Task<bool> TryCrossFormTransactionCommitAsync(
            List<FormsManager> formsToCommit,
            List<string> orderedBlocks)
        {
            var formOwnedBlocks = new Dictionary<FormsManager, List<string>>();
            foreach (var fm in formsToCommit)
            {
                var owned = fm.GetDirtyBlocks().Where(b => orderedBlocks.Contains(b)).ToList();
                if (owned.Count > 0)
                    formOwnedBlocks[fm] = owned;
            }

            if (formOwnedBlocks.Count == 0) return true;

            var dataSources = new HashSet<IDataSource>();
            foreach (var pair in formOwnedBlocks)
                foreach (var blockName in pair.Value)
                {
                    var ds = pair.Key.GetBlock(blockName)?.UnitOfWork?.DataSource;
                    if (ds != null) dataSources.Add(ds);
                }

            var txArgs = new PassedArgs { Messege = "FormsManager.CommitFormAsync" };
            var openedTransactions = new List<IDataSource>();
            var nonTransactional = new HashSet<IDataSource>();

            foreach (var ds in dataSources)
            {
                IErrorsInfo beginResult;
                try
                {
                    beginResult = ds.BeginTransaction(txArgs);
                }
                catch (NotImplementedException)
                {
                    nonTransactional.Add(ds);
                    continue;
                }

                if (beginResult == null || beginResult.Flag != Errors.Ok)
                {
                    // Nothing has been written anywhere yet — a clean,
                    // complete abort of whatever did open.
                    AbortOpenedTransactions(openedTransactions, txArgs);
                    LogError($"Could not begin a commit transaction: {beginResult?.Message ?? "no result"}");
                    return false;
                }

                openedTransactions.Add(ds);
            }

            var committedForms = new List<(FormsManager Fm, List<string> Blocks)>();
            try
            {
                foreach (var pair in formOwnedBlocks)
                {
                    var fm = pair.Key;
                    var blocks = pair.Value;

                    if (!await fm.FireOnInsertForDirtyBlocksAsync(blocks).ConfigureAwait(false))
                        throw new InvalidOperationException(
                            $"Commit cancelled by ON-INSERT trigger for form '{fm._currentFormName}'");

                    var success = await fm._dirtyStateManager.SaveDirtyBlocksAsync(blocks).ConfigureAwait(false);
                    if (!success)
                        throw new InvalidOperationException($"Commit failed for form '{fm._currentFormName}'");

                    committedForms.Add((fm, blocks));
                }
            }
            catch (Exception ex)
            {
                // Prepare-phase failure. Every opened transaction is a true,
                // clean abort — none of its writes are durable yet.
                AbortOpenedTransactions(openedTransactions, txArgs);

                // A block already saved to a NON-transactional datasource
                // before this failure has no ACID mechanism to undo it
                // through. That is an inherent limit of that provider, not
                // something this fix introduces — but it must be reported
                // loudly rather than folded into "commit failed" as though
                // everything rolled back.
                var strandedBlocks = committedForms
                    .SelectMany(cf => cf.Blocks.Where(b =>
                        nonTransactional.Contains(cf.Fm.GetBlock(b)?.UnitOfWork?.DataSource)))
                    .ToList();

                if (strandedBlocks.Count > 0)
                {
                    LogError(
                        "PARTIAL COMMIT — manual reconciliation required. These blocks " +
                        "committed to a non-transactional datasource before a later failure " +
                        "aborted the rest of this commit, and could NOT be rolled back: " +
                        $"{string.Join(", ", strandedBlocks)}. Reason: {ex.Message}", ex);
                }

                LogError("Cross-form commit failed during prepare", ex);
                return false;
            }

            // Every form's batch committed. For transaction-capable
            // datasources this all ran inside an open transaction — nothing
            // is durable there yet. Make it so.
            var committedCount = 0;
            foreach (var ds in openedTransactions)
            {
                IErrorsInfo commitResult;
                try
                {
                    commitResult = ds.Commit(txArgs);
                }
                catch (Exception ex)
                {
                    commitResult = new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
                }

                if (commitResult == null || commitResult.Flag != Errors.Ok)
                {
                    LogError(
                        $"PARTIAL COMMIT — manual reconciliation required. {committedCount} of " +
                        $"{openedTransactions.Count} datasource transactions committed before " +
                        $"this one failed: {commitResult?.Message ?? "no result"}",
                        commitResult?.Ex);
                    return false;
                }

                committedCount++;
            }

            return true;
        }

        private void AbortOpenedTransactions(List<IDataSource> openedTransactions, PassedArgs txArgs)
        {
            foreach (var ds in openedTransactions)
            {
                try { ds.EndTransaction(txArgs); }
                catch (Exception ex) { LogError("EndTransaction failed while aborting a prepare-phase transaction", ex); }
            }
        }

        #endregion
    }
}
