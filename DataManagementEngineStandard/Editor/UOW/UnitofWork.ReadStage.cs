using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW
{
    public partial class UnitofWork<T> : IStagedUnitofWorkRead, IStagedUnitofWorkPageRead, IUnitofWorkRecordRevision, IUnitofWorkReadBufferIdentity
    {
        private readonly object _readPublicationGate = new();
        private int _readAdmission;
        private long _readTargetRevision;
        private long _recordTargetRevision;
        private object _readBufferIdentity = new();

        public bool SupportsReadBufferIdentity => true;
        public bool RequiresReadAuthorization => IsTenantScoped && TenantFiltersReads;
        public bool TryGetReadBufferIdentity(out object identity)
        {
            identity = Volatile.Read(ref _readBufferIdentity);
            return !disposedValue && !IsFilterOn;
        }

        private void InvalidateReadBufferIdentity() => Volatile.Write(ref _readBufferIdentity, new object());

        public bool SupportsRecordRevision => true;

        public bool TryGetRecordRevision(out long revision)
        {
            revision = Volatile.Read(ref _recordTargetRevision);
            return !disposedValue;
        }

        // An inherited capability must not silently bypass an adapter's overridden Get semantics.
        public virtual bool SupportsStagedRead => !disposedValue && !IsInListMode && DataSource != null &&
            GetType().GetMethod(nameof(Get), Type.EmptyTypes)?.DeclaringType == typeof(UnitofWork<T>) &&
            GetType().GetMethod(nameof(Get), new[] { typeof(List<AppFilter>) })?.DeclaringType == typeof(UnitofWork<T>);

        public virtual Task<IUnitofWorkReadStage> PrepareReadAsync(List<AppFilter> filters,
            CancellationToken cancellationToken = default) => PrepareReadCoreAsync(filters, null, cancellationToken);

        public virtual bool SupportsStagedPageRead => SupportsStagedRead && DataSource is IBoundedPagedDataSource &&
            GetType().GetMethod(nameof(PrepareReadAsync), new[] { typeof(List<AppFilter>), typeof(CancellationToken) })?.DeclaringType == typeof(UnitofWork<T>);

        public virtual async Task<IUnitofWorkPageReadStage> PreparePageReadAsync(BoundedPageRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return (IUnitofWorkPageReadStage)await PrepareReadCoreAsync(request.CopyFilters(), request, cancellationToken).ConfigureAwait(false);
        }

        private async Task<IUnitofWorkReadStage> PrepareReadCoreAsync(List<AppFilter> filters,
            BoundedPageRequest page, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Interlocked.CompareExchange(ref _commitAdmission, 1, 0) != 0)
                throw new InvalidOperationException("A commit or staged read is already active.");
            Volatile.Write(ref _readAdmission, 1);
            ReadStage stage = null;
            try
            {
                if (!SupportsStagedRead) throw new NotSupportedException("This UoW does not support staged datasource reads.");
                if (page != null && !SupportsStagedPageRead) throw new NotSupportedException("This UoW/provider does not support bounded staged pages.");
                stage = new ReadStage(this, cancellationToken, page != null);
                stage.CheckTarget();
                if (page != null) page = QualifyPageRequest(page);
                var args = new UnitofWorkParams { EventAction = EventAction.PreQuery };
                PreQuery?.Invoke(this, args);
                if (args.Cancel) throw new InvalidOperationException(args.Messege ?? "Read cancelled before provider execution.");
                stage.CheckTarget();
                stage.Filters = (filters ?? new List<AppFilter>()).Select(CopyReadFilter).ToList();
                // The authoritative tenant boundary is independent of a caller's same-field predicate.
                if (IsTenantScoped && TenantFiltersReads)
                    stage.Filters.Add(new AppFilter { FieldName = TenantFieldName, Operator = "=", FilterValue = TenantId });
                var providerFilters = stage.Filters.Select(CopyReadFilter).ToList();
                var timer = Stopwatch.StartNew();
                // IDataSource has no token overload. Await physical acknowledgement, then reject cancellation;
                // abandoning this task would allow close/drain to finish while provider work is still active.
                List<T> candidateRows;
                if (page != null)
                {
                    page = page.WithValues(page.Order, providerFilters);
                    var response = await ((IBoundedPagedDataSource)stage.Source).ReadPageAsync(stage.EntityName, page, cancellationToken).ConfigureAwait(false);
                    stage.CheckTarget();
                    candidateRows = DecodePageResponse(page, response, cancellationToken, out var info);
                    stage.Page = info;
                }
                else
                {
                    var rows = await stage.Source.GetEntityAsync(stage.EntityName, providerFilters).ConfigureAwait(false);
                    stage.CheckTarget();
                    if (rows == null) throw new InvalidOperationException("Provider returned no read result; null is not an empty row set.");
                    candidateRows = new List<T>();
                    foreach (var row in rows)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        candidateRows.Add(ConvertReadRow(row));
                    }
                }
                stage.Candidate = new ObservableBindingList<T>(candidateRows);
                if (_validationHelper != null)
                {
                    var validator = _validationHelper;
                    stage.Candidate.CustomValidator = item =>
                    {
                        var result = new ValidationResult();
                        var validation = validator.ValidateEntity(item);
                        if (validation.Flag == Errors.Failed)
                            result.Errors.Add(new ValidationError(null, validation.Message, ValidationSeverity.Error));
                        return result;
                    };
                }
                AttachHandlers(stage.Candidate);
                stage.Duration = timer.Elapsed;
                stage.CheckTarget();
                return stage;
            }
            catch
            {
                if (stage != null) stage.Dispose();
                else ReleaseReadAdmission();
                throw;
            }
        }

        private static AppFilter CopyReadFilter(AppFilter filter)
        {
            if (filter == null) throw new ArgumentException("A read filter cannot be null.");
            return new AppFilter { FieldName = filter.FieldName, Operator = filter.Operator,
                FieldType = filter.FieldType, FilterValue = filter.FilterValue, FilterValue1 = filter.FilterValue1,
                valueType = filter.valueType, ID = filter.ID, GuidID = filter.GuidID };
        }

        private static T ConvertReadRow(object source)
        {
            if (source == null) throw new InvalidOperationException("Provider returned a null row.");
            // Copy the row shell without copying its observers. Nested mutable values remain a documented
            // provider/record ownership limitation, not a claimed transitive metadata or object snapshot.
            if (source is T typed) return (T)typed.CopyWithoutObservers();
            var target = new T();
            var map = source as IDictionary<string, object>;
            var mapped = false;
            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanWrite || property.GetIndexParameters().Length != 0) continue;
                object value;
                if (map != null)
                {
                    var entry = map.FirstOrDefault(pair => string.Equals(pair.Key, property.Name, StringComparison.OrdinalIgnoreCase));
                    if (entry.Key == null) continue;
                    value = entry.Value;
                }
                else
                {
                    var origin = source.GetType().GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (origin?.CanRead != true || origin.GetIndexParameters().Length != 0) continue;
                    value = origin.GetValue(source);
                }
                mapped = true;
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (value == null || value is DBNull)
                {
                    if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                        throw new InvalidOperationException($"Null read value cannot be assigned to '{property.Name}'.");
                    property.SetValue(target, null);
                }
                else
                {
                    if (!type.IsInstanceOfType(value))
                        value = type == typeof(Guid) ? Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)) :
                            type.IsEnum ? Enum.Parse(type, Convert.ToString(value, CultureInfo.InvariantCulture), true) :
                            Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
                    property.SetValue(target, value);
                }
            }
            if (!mapped) throw new InvalidOperationException("Provider row has no readable fields matching the record type.");
            return target;
        }

        private void RejectMutationDuringRead()
        {
            if (Volatile.Read(ref _readAdmission) != 0)
                throw new InvalidOperationException("Cannot replace or clear records while a staged read is active.");
        }

        private void ReleaseReadAdmission()
        {
            Volatile.Write(ref _readAdmission, 0);
            Volatile.Write(ref _commitAdmission, 0);
        }

        private sealed class ReadStage : IUnitofWorkPageReadStage
        {
            private readonly UnitofWork<T> _owner;
            private readonly CancellationToken _token;
            private readonly ObservableBindingList<T> _prior, _priorFiltered;
            private readonly EntityStructure _entity;
            private readonly bool _wasFiltered;
            private readonly string _tenantField, _tenantId;
            private readonly bool _tenantReads;
            private readonly long _revision;
            private readonly object _priorBufferIdentity;
            private readonly string _pageSchema;
            private readonly List<Exception> _failures = new();
            private int _busy, _disposed, _published;
            private int _publicationThread;
            private bool _publicationWindow;
            internal readonly IDataSource Source;
            internal readonly string EntityName;
            internal ObservableBindingList<T> Candidate;
            internal List<AppFilter> Filters;
            internal TimeSpan Duration;
            public ProviderPageInfo Page { get; internal set; }
            public object PreparedBufferIdentity { get; } = new object();
            public bool IsPublished => Volatile.Read(ref _published) != 0;
            public IReadOnlyList<Exception> NotificationFailures => _failures.ToArray();

            internal ReadStage(UnitofWork<T> owner, CancellationToken token, bool pageRead)
            {
                _owner = owner; _token = token;
                Source = owner.DataSource; EntityName = owner.EntityName; _entity = owner.EntityStructure;
                _prior = owner._units; _priorFiltered = owner._filteredunits; _wasFiltered = owner.IsFilterOn;
                _tenantField = owner.TenantFieldName; _tenantId = owner.TenantId; _tenantReads = owner.TenantFiltersReads;
                _revision = Volatile.Read(ref owner._readTargetRevision);
                _priorBufferIdentity = Volatile.Read(ref owner._readBufferIdentity);
                _pageSchema = pageRead ? PageSchemaSignature(owner.EntityStructure) : null;
            }

            internal void CheckTarget()
            {
                _token.ThrowIfCancellationRequested();
                if (_owner.disposedValue) throw new ObjectDisposedException(nameof(UnitofWork<T>));
                if (!ReferenceEquals(Source, _owner.DataSource) || EntityName != _owner.EntityName ||
                    !ReferenceEquals(_entity, _owner.EntityStructure) || _owner.IsInListMode ||
                    _tenantField != _owner.TenantFieldName || _tenantId != _owner.TenantId || _tenantReads != _owner.TenantFiltersReads ||
                    !ReferenceEquals(_prior, _owner._units) || !ReferenceEquals(_priorFiltered, _owner._filteredunits) ||
                    _wasFiltered != _owner.IsFilterOn || _revision != Volatile.Read(ref _owner._readTargetRevision) ||
                    !ReferenceEquals(_priorBufferIdentity, Volatile.Read(ref _owner._readBufferIdentity)))
                    throw new InvalidOperationException("Read target changed during preparation.");
                if (_pageSchema != null && _pageSchema != PageSchemaSignature(_owner.EntityStructure))
                    throw new InvalidOperationException("Page fields or primary keys changed during preparation.");
                if (_owner.IsDirty || (_owner.DeletedUnits?.Count ?? 0) != 0)
                    throw new InvalidOperationException("Staged reads cannot replace unsaved records.");
            }

            public void Publish(Action<Action> authorizePublication)
            {
                ArgumentNullException.ThrowIfNull(authorizePublication);
                if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
                    throw new InvalidOperationException("Read stage publication/disposal is already active.");
                try
                {
                    ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
                    if (IsPublished) throw new InvalidOperationException("Read stage has already published.");
                    if (Page?.IsOutOfRange == true) throw new InvalidOperationException("An out-of-range page cannot replace the live buffer.");
                    CheckTarget();
                    try
                    {
                        _publicationWindow = true;
                        _publicationThread = Environment.CurrentManagedThreadId;
                        authorizePublication(PublishOwnedState);
                        if (!IsPublished) throw new InvalidOperationException("Read publication was not authorized.");
                    }
                    catch (Exception ex) when (IsPublished) { _failures.Add(ex); }
                    finally { _publicationWindow = false; }
                    if (IsPublished)
                    {
                        Observe(() => _owner.DetachHandlers(_prior));
                        if (!ReferenceEquals(_prior, _priorFiltered)) Observe(() => _owner.DetachHandlers(_priorFiltered));
                        Observe(() => _owner.RecordQueryHistory(Filters, Duration, true));
                        foreach (PropertyChangedEventHandler observer in _owner.PropertyChanged?.GetInvocationList() ?? Array.Empty<Delegate>())
                            Observe(() => observer(_owner, new PropertyChangedEventArgs(nameof(Units))));
                        var args = new UnitofWorkParams { EventAction = EventAction.PostQuery };
                        foreach (EventHandler<UnitofWorkParams> observer in _owner.PostQuery?.GetInvocationList() ?? Array.Empty<Delegate>())
                            Observe(() => observer(Candidate, args));
                        if (args.Cancel) _failures.Add(new InvalidOperationException("PostQuery cannot undo published records."));
                    }
                }
                finally { Volatile.Write(ref _busy, 0); }
            }

            private void PublishOwnedState()
            {
                lock (_owner._readPublicationGate)
                {
                    if (!_publicationWindow || _publicationThread != Environment.CurrentManagedThreadId ||
                        Volatile.Read(ref _disposed) != 0 || IsPublished)
                        throw new InvalidOperationException("Read publication action is no longer valid.");
                    _token.ThrowIfCancellationRequested();
                    if (_owner.disposedValue || _revision != Volatile.Read(ref _owner._readTargetRevision) ||
                        !ReferenceEquals(_priorBufferIdentity, Volatile.Read(ref _owner._readBufferIdentity)) ||
                        !ReferenceEquals(_prior, _owner._units) || !ReferenceEquals(_priorFiltered, _owner._filteredunits) ||
                        _wasFiltered != _owner.IsFilterOn ||
                        !ReferenceEquals(Source, _owner._dataSource) || EntityName != _owner.EntityName ||
                        !ReferenceEquals(_entity, _owner.EntityStructure) || _owner.IsInListMode ||
                        _tenantField != _owner.TenantFieldName || _tenantId != _owner.TenantId || _tenantReads != _owner.TenantFiltersReads)
                        throw new InvalidOperationException("Read publication lost its captured target.");
                    Interlocked.Increment(ref _owner._recordTargetRevision);
                    _owner._units = Candidate;
                    _owner._filteredunits = null;
                    _owner.IsFilterOn = false;
                    Volatile.Write(ref _owner._readBufferIdentity, PreparedBufferIdentity);
                    Volatile.Write(ref _published, 1);
                }
            }

            private void Observe(Action action)
            {
                try { action(); }
                catch (Exception ex) { _failures.Add(ex); }
            }

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
                    throw new InvalidOperationException("Cannot dispose a read stage during its publication.");
                try
                {
                    if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                    try
                    {
                        if (!IsPublished && Candidate != null)
                        {
                            try { _owner.DetachHandlers(Candidate); }
                            finally { Candidate.Dispose(); }
                        }
                    }
                    finally { _owner.ReleaseReadAdmission(); }
                }
                finally { Volatile.Write(ref _busy, 0); }
            }
        }
    }
}
