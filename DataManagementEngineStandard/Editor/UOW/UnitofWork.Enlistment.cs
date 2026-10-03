using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW
{
    public partial class UnitofWork<T> : IEnlistedUnitofWork
    {
        private int _commitAdmission;
        public bool SupportsEnlistedCommit => !IsInListMode && DataSource != null;

        public async Task<IUnitofWorkCommitStage> PrepareCommitAsync(IDataSource transactionOwner,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref _commitAdmission, 1, 0) != 0)
                throw new InvalidOperationException("A commit or unresolved enlisted write is already active.");
            var stage = new EnlistedStage(this);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!SupportsEnlistedCommit || !ReferenceEquals(DataSource, transactionOwner))
                    throw new InvalidOperationException("The transaction owner must be this UoW's captured datasource.");
                var args = new UnitofWorkParams { EventAction = EventAction.PreCommit };
                PreCommit?.Invoke(this, args);
                if (args.Cancel)
                    throw new InvalidOperationException(args.Messege ?? "Commit cancelled before writing.");

                var keyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(PrimaryKey)) keyNames.Add(PrimaryKey);
                if (!string.IsNullOrWhiteSpace(GuidKey)) keyNames.Add(GuidKey);
                foreach (var field in EntityStructure?.Fields ?? new List<EntityField>())
                    if (field.IsAutoIncrement) keyNames.Add(field.FieldName);
                foreach (var item in stage.Units.GetPendingChanges().Added)
                    foreach (var property in typeof(T).GetProperties().Where(p => p.CanRead && p.CanWrite && keyNames.Contains(p.Name)))
                        stage.Keys.Add((item, property, property.GetValue(item)));

                stage.Pending = await CommitChangesToDataSource(null, cancellationToken, false, item =>
                {
                    foreach (var key in stage.Keys.Where(k => ReferenceEquals(k.Item, item)))
                        stage.Applied[(key.Item, key.Property)] = key.Property.GetValue(item);
                }).ConfigureAwait(false);
                foreach (var key in stage.Applied)
                    stage.Units.ConfirmGeneratedValue(stage.Pending, key.Key.Item, key.Key.Property.Name, key.Value);
                stage.Status.Flag = stage.Pending.AllSucceeded ? Errors.Ok : Errors.Failed;
                stage.Status.Message = stage.Pending.AllSucceeded ? "Changes prepared; transaction not committed" :
                    stage.Pending.FailedResults.FirstOrDefault()?.Errors?.Message ?? "Preparing changes failed";
            }
            catch (Exception ex)
            {
                stage.Status.Flag = Errors.Failed;
                stage.Status.Message = ex.Message;
                stage.Status.Ex = ex;
            }
            return stage;
        }

        private sealed class EnlistedStage : IUnitofWorkCommitStage
        {
            private readonly UnitofWork<T> _owner;
            private int _completed;
            private int _completing;
            private UnitofWorkCommitOutcome? _decision;
            internal readonly ObservableBindingList<T> Units;
            internal readonly List<(T Item, PropertyInfo Property, object Original)> Keys = new();
            internal readonly Dictionary<(T Item, PropertyInfo Property), object> Applied = new();
            internal CommitResult Pending;
            internal readonly ErrorsInfo Status = new ErrorsInfo { Flag = Errors.Ok };
            public IErrorsInfo Result => new ErrorsInfo { Flag = Status.Flag, Message = Status.Message,
                Ex = Status.Ex, Errors = new List<IErrorsInfo>(Status.Errors) };
            public bool IsResolved => Volatile.Read(ref _completed) != 0;

            internal EnlistedStage(UnitofWork<T> owner) { _owner = owner; Units = owner.Units; }

            public IErrorsInfo Complete(UnitofWorkCommitOutcome outcome)
            {
                if (Interlocked.CompareExchange(ref _completing, 1, 0) != 0)
                    throw new InvalidOperationException("The prepared write is already being resolved.");
                try
                {
                    if (IsResolved) throw new InvalidOperationException("The prepared write is already resolved.");
                    if (!Enum.IsDefined(typeof(UnitofWorkCommitOutcome), outcome))
                        throw new ArgumentOutOfRangeException(nameof(outcome));
                    if (_decision.HasValue && outcome != _decision.Value)
                        throw new InvalidOperationException("Cannot reverse a confirmed provider outcome during tracking repair.");
                    if (outcome == UnitofWorkCommitOutcome.Unknown)
                        return new ErrorsInfo { Flag = Errors.Warning, Message = "Provider outcome unknown; reconciliation required before another commit." };
                    if (outcome == UnitofWorkCommitOutcome.Committed && Status.Flag != Errors.Ok)
                        throw new InvalidOperationException("Cannot accept an unsuccessful prepared write.");
                    var result = new ErrorsInfo { Flag = Errors.Ok };
                    _decision = outcome;
                    try
                    {
                        if (outcome == UnitofWorkCommitOutcome.Committed)
                        {
                            if (Pending != null)
                            {
                                Units.AcceptCommit(Pending);
                                result.Errors.AddRange(Pending.NotificationErrors);
                            }
                            try { _owner.PostCommit?.Invoke(_owner, new UnitofWorkParams { EventAction = EventAction.PostCommit }); }
                            catch (Exception ex) { result.Errors.Add(new ErrorsInfo { Flag = Errors.Warning, Message = ex.Message, Ex = ex }); }
                        }
                        else
                        {
                            if (Pending != null) Units.DiscardCommit(Pending);
                            foreach (var key in Keys)
                                if (Applied.TryGetValue((key.Item, key.Property), out var applied) && Equals(key.Property.GetValue(key.Item), applied))
                                    key.Property.SetValue(key.Item, key.Original);
                        }
                        try { _owner.OnPropertyChanged(nameof(IsDirty)); }
                        catch (Exception ex) { result.Errors.Add(new ErrorsInfo { Flag = Errors.Warning, Message = ex.Message, Ex = ex }); }
                        Volatile.Write(ref _owner._commitAdmission, 0);
                        Volatile.Write(ref _completed, 1);
                    }
                    catch (Exception ex)
                    {
                        // Keep the lease closed until the caller explicitly repairs local tracking.
                        result.Flag = Errors.Failed;
                        result.Message = ex.Message;
                        result.Ex = ex;
                    }
                    return result;
                }
                finally { Volatile.Write(ref _completing, 0); }
            }
        }
    }
}
