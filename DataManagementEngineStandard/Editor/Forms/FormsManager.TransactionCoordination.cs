using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager : IFormsCommitOutcomes
    {
        public async Task<FormCommitResult> CommitFormWithOutcomeAsync()
            => (FormCommitResult)await CommitFormAsync().ConfigureAwait(false);

        private readonly Guid _commitFormInstanceId = Guid.NewGuid();
        private readonly ConcurrentDictionary<IUnitofWork, FormBlockCommitResult> _unresolvedFormCommits =
            new(ReferenceEqualityComparer.Instance);
        private readonly ConcurrentDictionary<IDataSource, List<IUnitofWorkCommitStage>> _unresolvedFormTransactions =
            new(ReferenceEqualityComparer.Instance);

        private sealed class CommitTarget
        {
            public FormsManager Form;
            public DataBlockInfo Block;
            public IUnitofWork Unit;
            public IDataSource Source;
            public IUnitofWorkCommitStage Stage;
            public FormBlockCommitResult Outcome;
        }

        private sealed class CommitTransactionGroup
        {
            public IDataSource Source;
            public List<CommitTarget> Targets;
            public bool Opened;
            public bool CommitAttempted;
            public bool Committed;
            public FormDataSourceCommitResult Outcome;
            public FormDataSourceAdmission Admission;
        }

        private List<CommitTarget> CaptureCommitTargets(Dictionary<FormsManager, List<string>> blocksByForm)
        {
            var targets = new List<CommitTarget>();
            foreach (var pair in blocksByForm)
            {
                var names = new HashSet<string>(pair.Value, StringComparer.OrdinalIgnoreCase);
                var order = pair.Key.BuildCommitOrder().Where(names.Contains)
                    .Concat(pair.Value).Distinct(StringComparer.OrdinalIgnoreCase);
                foreach (var name in order)
                {
                    var block = pair.Key.GetBlock(name);
                    var unit = block?.UnitOfWork;
                    if (unit == null) throw new InvalidOperationException($"Block '{name}' has no unit of work.");
                    targets.Add(new CommitTarget
                    {
                        Form = pair.Key, Block = block, Unit = unit, Source = unit.DataSource,
                        Outcome = new FormBlockCommitResult
                        {
                            FormInstanceId = pair.Key._commitFormInstanceId,
                            FormName = pair.Key._currentFormName, BlockName = name,
                            DataSourceName = unit.DataSource?.DatasourceName,
                            State = FormBlockCommitState.Unattempted
                        }
                    });
                }
            }
            return targets;
        }

        private static void VerifyCommitTarget(CommitTarget target)
        {
            if (target.Form._unresolvedFormCommits.TryGetValue(target.Unit, out var previous))
            {
                if (previous.Reconciliation?.IsResolved == true)
                    target.Form._unresolvedFormCommits.TryRemove(target.Unit, out _);
                else
                {
                    target.Outcome.State = FormBlockCommitState.Unknown;
                    target.Outcome.Reconciliation = previous.Reconciliation;
                    throw new InvalidOperationException("Previous write requires reconciliation; automatic replay denied.");
                }
            }
            if (target.Source != null && target.Form._unresolvedFormTransactions.TryGetValue(target.Source, out var stages))
            {
                if (stages.Count > 0 && stages.All(s => s.IsResolved))
                    target.Form._unresolvedFormTransactions.TryRemove(target.Source, out _);
                else
                {
                    target.Outcome.State = FormBlockCommitState.Unknown;
                    throw new InvalidOperationException("Provider transaction requires reconciliation or a verified fresh connection before reuse.");
                }
            }
            if (target.Form._disposed ||
                !ReferenceEquals(target.Form.GetBlock(target.Outcome.BlockName), target.Block) ||
                !ReferenceEquals(target.Block.UnitOfWork, target.Unit) ||
                !ReferenceEquals(target.Unit.DataSource, target.Source))
                throw new InvalidOperationException("The captured commit target changed before writing.");
        }

        private async Task<bool> TryCrossFormTransactionCommitAsync(
            List<CommitTarget> targets, FormCommitResult result)
        {
            var groups = targets.GroupBy(t => t.Source, ReferenceEqualityComparer.Instance)
                .Select(g => new CommitTransactionGroup { Source = (IDataSource)g.Key, Targets = g.ToList(),
                    Outcome = new FormDataSourceCommitResult { InstanceId = Guid.NewGuid(),
                        DataSourceName = ((IDataSource)g.Key)?.DatasourceName } })
                .ToList();
            result.DataSources = groups.Select(g => g.Outcome).ToList().AsReadOnly();
            var args = new PassedArgs { Messege = "FormsManager.CommitFormAsync" };
            try
            {
                foreach (var target in targets) VerifyCommitTarget(target);
                if (targets.Any(t => t.Form._dirtyStateManager is not ICoordinatedBlockSave))
                    throw new NotSupportedException("Dirty-state helper must expose coordinated per-block save results.");
                foreach (var group in groups.Where(g => g.Source != null))
                    group.Admission = FormDataSourceAdmission.Enter(group.Source);

                foreach (var group in groups)
                {
                    bool canEnlist = group.Source != null &&
                        DataSourceCapabilityMatrix.Supports(group.Source.DatasourceType, CapabilityType.SupportsTransactions) &&
                        group.Targets.All(t => t.Unit is IEnlistedUnitofWork e && e.SupportsEnlistedCommit);
                    if (!canEnlist) { result.UsesIndependentCommits = true; group.Outcome.State = FormDataSourceCommitState.Independent; continue; }
                    IErrorsInfo begin;
                    group.Opened = true;
                    group.Outcome.State = FormDataSourceCommitState.Unknown;
                    try { begin = group.Source.BeginTransaction(args); }
                    catch (NotImplementedException) { group.Opened = false; result.UsesIndependentCommits = true;
                        group.Outcome.State = FormDataSourceCommitState.Independent; continue; }
                    // A failed or thrown begin may still have allocated provider resources.
                    if (begin?.Flag != Errors.Ok)
                        throw new InvalidOperationException($"Begin transaction failed: {begin?.Message ?? "no result"}");
                    group.Outcome.State = FormDataSourceCommitState.Opened;
                }
                if (result.UsesIndependentCommits)
                    result.Errors.Add(new ErrorsInfo { Flag = Errors.Warning,
                        Message = "Independent block commits are not atomic across this form scope." });

                foreach (var formTargets in targets.GroupBy(t => t.Form))
                {
                    var fm = formTargets.Key;
                    var namedTargets = formTargets.ToDictionary(t => t.Outcome.BlockName, StringComparer.OrdinalIgnoreCase);
                    var names = namedTargets.Keys.ToList();
                    if (!await fm.FireOnInsertForDirtyBlocksAsync(names).ConfigureAwait(false))
                        throw new InvalidOperationException("Commit cancelled by ON-INSERT.");
                    var saves = await ((ICoordinatedBlockSave)fm._dirtyStateManager)
                        .SaveDirtyBlocksWithResultsAsync(names, async block =>
                        {
                            var target = namedTargets[block.BlockName];
                            try { VerifyCommitTarget(target); }
                            catch (Exception ex)
                            {
                                target.Outcome.State = FormBlockCommitState.FailedBeforeWrite;
                                return new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
                            }
                            var group = groups.First(g => ReferenceEquals(g.Source, target.Source));
                            target.Outcome.State = FormBlockCommitState.Unknown;
                            IErrorsInfo write;
                            if (group.Opened)
                            {
                                target.Stage = await ((IEnlistedUnitofWork)target.Unit)
                                    .PrepareCommitAsync(target.Source).ConfigureAwait(false);
                                write = target.Stage?.Result;
                                if (write?.Flag == Errors.Ok) target.Outcome.State = FormBlockCommitState.Prepared;
                            }
                            else
                            {
                                write = await target.Unit.Commit().ConfigureAwait(false);
                                if (write?.Flag == Errors.Ok) target.Outcome.State = FormBlockCommitState.Committed;
                            }
                            target.Outcome.Message = write?.Message ?? "Write returned no result";
                            if (write?.Errors != null) result.Errors.AddRange(write.Errors);
                            return write;
                        }).ConfigureAwait(false);
                    foreach (var save in saves.Where(s => !s.Success))
                    {
                        var target = namedTargets[save.BlockName];
                        target.Outcome.Message = save.ErrorMessage;
                        if (target.Outcome.State == FormBlockCommitState.Unattempted)
                            target.Outcome.State = FormBlockCommitState.FailedBeforeWrite;
                    }
                    if (saves.Count != names.Count || saves.Any(s => !s.Success))
                        throw new InvalidOperationException(saves.FirstOrDefault(s => !s.Success)?.ErrorMessage ?? "Incomplete block save outcomes.");
                }

                foreach (var target in targets) VerifyCommitTarget(target);
                foreach (var group in groups.Where(g => g.Opened))
                {
                    group.CommitAttempted = true;
                    group.Outcome.State = FormDataSourceCommitState.Unknown;
                    var commit = group.Source.Commit(args);
                    if (commit?.Flag != Errors.Ok)
                        throw new InvalidOperationException($"Provider commit outcome unknown: {commit?.Message ?? "no result"}");
                    group.Committed = true;
                    group.Outcome.State = FormDataSourceCommitState.Committed;
                    foreach (var target in group.Targets)
                    {
                        target.Outcome.State = FormBlockCommitState.Committed;
                        ResolveStage(target, UnitofWorkCommitOutcome.Committed, result);
                    }
                }
                return result.AllWritesCommitted;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex });
                result.Message = ex.Message;
                return false;
            }
            finally
            {
                foreach (var group in groups.Where(g => g.Opened && !g.Committed))
                {
                    bool rolledBack = false;
                    try
                    {
                        var abort = group.Source.EndTransaction(args);
                        rolledBack = abort?.Flag == Errors.Ok;
                        if (!rolledBack) result.Errors.Add(new ErrorsInfo { Flag = Errors.Failed,
                            Message = $"Transaction cleanup failed: {abort?.Message ?? "no result"}" });
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new ErrorsInfo { Flag = Errors.Failed, Message = $"Transaction cleanup failed: {ex.Message}", Ex = ex });
                    }
                    foreach (var target in group.Targets.Where(t => t.Stage != null || t.Outcome.State == FormBlockCommitState.Unknown))
                    {
                        // EndTransaction after a failed Commit cannot establish whether writes were durable.
                        var knownRollback = rolledBack && !group.CommitAttempted;
                        target.Outcome.State = knownRollback ? FormBlockCommitState.RolledBack : FormBlockCommitState.Unknown;
                        ResolveStage(target, knownRollback ? UnitofWorkCommitOutcome.RolledBack : UnitofWorkCommitOutcome.Unknown, result);
                    }
                    group.Outcome.State = rolledBack && !group.CommitAttempted ? FormDataSourceCommitState.RolledBack : FormDataSourceCommitState.Unknown;
                }
                foreach (var target in targets.Where(t => t.Outcome.State == FormBlockCommitState.Unknown || t.Outcome.Reconciliation != null))
                    target.Form._unresolvedFormCommits[target.Unit] = target.Outcome;
                foreach (var group in groups.Where(g => g.Source != null))
                {
                    if (group.Outcome.State == FormDataSourceCommitState.Independent &&
                        group.Targets.Any(t => t.Outcome.State == FormBlockCommitState.Unknown))
                        group.Outcome.State = FormDataSourceCommitState.Unknown;
                    if (group.Outcome.State != FormDataSourceCommitState.Unknown) continue;
                    var stages = group.Targets.Where(t => t.Stage != null).Select(t => t.Stage).ToList();
                    foreach (var form in group.Targets.Select(t => t.Form).Distinct())
                        form._unresolvedFormTransactions[group.Source] = stages;
                    group.Admission?.HoldUntil(() => stages.Count > 0 && stages.All(s => s.IsResolved));
                }
                foreach (var group in groups) group.Admission?.Dispose();
            }
        }

        private static void ResolveStage(CommitTarget target, UnitofWorkCommitOutcome outcome, FormCommitResult result)
        {
            if (target.Stage == null) return;
            try
            {
                var completion = target.Stage.Complete(outcome);
                if (completion?.Errors != null) result.Errors.AddRange(completion.Errors);
                if (completion?.Flag != Errors.Ok)
                    result.Errors.Add(completion ?? new ErrorsInfo { Flag = Errors.Failed, Message = "Tracking completion returned no result." });
                if (outcome == UnitofWorkCommitOutcome.Unknown || completion?.Flag != Errors.Ok)
                    target.Outcome.Reconciliation = target.Stage;
            }
            catch (Exception ex)
            {
                target.Outcome.Reconciliation = target.Stage;
                result.Errors.Add(new ErrorsInfo { Flag = Errors.Warning, Message = $"Tracking completion requires repair: {ex.Message}", Ex = ex });
            }
        }
    }
}
