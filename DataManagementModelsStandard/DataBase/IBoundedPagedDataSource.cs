using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Optional bounded page producer. Apply every filter before count/page, use the complete order,
    /// and count/read from one coherent snapshot. Bound rows and encoded bytes at the producer;
    /// do not implement by loading an unbounded GetEntity result and truncating it.
    /// Await physical completion even when cancellation is requested. No background work may outlive the task.
    /// </summary>
    public interface IBoundedPagedDataSource
    {
        /// <summary>Reads an admitted bounded page and its snapshot count; physical work completes before the task.</summary>
        Task<BoundedPageResponse> ReadPageAsync(string entityName, BoundedPageRequest request,
            CancellationToken cancellationToken = default);
    }

    /// <summary>One declared field in a deterministic page order.</summary>
    public sealed class PageOrder
    {
        /// <summary>Declared metadata field used for ordering.</summary>
        public string FieldName { get; }
        /// <summary>Whether this field is ordered from greatest to least.</summary>
        public bool Descending { get; }
        /// <summary>Creates one declared order field.</summary>
        public PageOrder(string fieldName, bool descending = false)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("An order field is required.", nameof(fieldName));
            FieldName = fieldName;
            Descending = descending;
        }
    }

    /// <summary>Immutable admission bounds and copied filter/order values, not a security credential.</summary>
    public sealed class BoundedPageRequest
    {
        /// <summary>Absolute producer row limit; callers may request a smaller limit.</summary>
        public const int MaximumPageSize = 5000;
        /// <summary>Absolute encoded UTF-8 payload limit in bytes.</summary>
        public const int MaximumPayloadBytes = 16 * 1024 * 1024;
        private readonly AppFilter[] _filters;
        private readonly AppFilter[] _anyFilters;
        private readonly PageOrder[] _order;
        private readonly PageRelatedFilter[] _anyRelated;
        private readonly PageRelatedFilterGroup[] _requiredRelatedGroups;
        /// <summary>Every immutable related group must match before count/page; alternatives within a group are OR.</summary>
        public IReadOnlyList<PageRelatedFilterGroup> RequiredRelatedGroups => Array.AsReadOnly(_requiredRelatedGroups);
        /// <summary>At least one related predicate must match together with every required parent predicate.</summary>
        public IReadOnlyList<PageRelatedFilter> AnyRelatedFilters => Array.AsReadOnly(_anyRelated);
        /// <summary>Explicit client-side validation of every transported read-only scalar against its getter after writable cells are loaded.</summary>
        public bool ValidateReadOnlyScalars { get; }
        /// <summary>Correlation identifier preserved through UnitofWork admission.</summary>
        public Guid RequestId { get; }
        /// <summary>Requested one-based page number.</summary>
        public long PageNumber { get; }
        /// <summary>Maximum rows on this page.</summary>
        public int PageSize { get; }
        /// <summary>Maximum encoded bytes admitted for this response.</summary>
        public int MaxPayloadBytes { get; }
        /// <summary>Checked zero-based row offset.</summary>
        public long Offset { get; }
        /// <summary>Copied deterministic order; admission adds the complete primary key.</summary>
        public IReadOnlyList<PageOrder> Order => Array.AsReadOnly(_order);
        /// <summary>Returns independent copies of every required parent predicate.</summary>
        public List<AppFilter> CopyFilters() => _filters.Select(CopyFilter).ToList();
        /// <summary>Alternatives in a single OR group; required filters remain AND predicates.</summary>
        public List<AppFilter> CopyAnyFilters() => _anyFilters.Select(CopyFilter).ToList();

        /// <summary>Creates a bounded request with copied required predicates and ordering.</summary>
        public BoundedPageRequest(long pageNumber, int pageSize, int maxPayloadBytes,
            IEnumerable<PageOrder> order = null, IEnumerable<AppFilter> filters = null)
            : this(Guid.NewGuid(), pageNumber, pageSize, maxPayloadBytes, order, filters, null, null) { }

        /// <summary>Adds a copied any-of group without changing the original constructor signature.</summary>
        public BoundedPageRequest(long pageNumber, int pageSize, int maxPayloadBytes,
            IEnumerable<PageOrder> order, IEnumerable<AppFilter> filters, IEnumerable<AppFilter> anyFilters)
            : this(Guid.NewGuid(), pageNumber, pageSize, maxPayloadBytes, order, filters, anyFilters, null) { }

        /// <summary>Requires the optional related-page producer; legacy producers must refuse this request.</summary>
        public BoundedPageRequest(long pageNumber, int pageSize, int maxPayloadBytes,
            IEnumerable<PageOrder> order, IEnumerable<AppFilter> filters, IEnumerable<AppFilter> anyFilters,
            IEnumerable<PageRelatedFilter> anyRelatedFilters)
            : this(Guid.NewGuid(), pageNumber, pageSize, maxPayloadBytes, order, filters, anyFilters, anyRelatedFilters) { }

        /// <summary>Requires the optional grouped related-page producer; older producers must refuse rather than ignore groups.</summary>
        public BoundedPageRequest(long pageNumber, int pageSize, int maxPayloadBytes,
            IEnumerable<PageOrder> order, IEnumerable<AppFilter> filters, IEnumerable<AppFilter> anyFilters,
            IEnumerable<PageRelatedFilter> anyRelatedFilters, IEnumerable<PageRelatedFilterGroup> requiredRelatedGroups)
            : this(Guid.NewGuid(), pageNumber, pageSize, maxPayloadBytes, order, filters, anyFilters, anyRelatedFilters, false, requiredRelatedGroups) { }

        private BoundedPageRequest(Guid requestId, long pageNumber, int pageSize, int maxPayloadBytes,
            IEnumerable<PageOrder> order, IEnumerable<AppFilter> filters, IEnumerable<AppFilter> anyFilters,
            IEnumerable<PageRelatedFilter> anyRelatedFilters, bool validateReadOnlyScalars = false, IEnumerable<PageRelatedFilterGroup> requiredRelatedGroups = null)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
            if (pageSize < 1 || pageSize > MaximumPageSize) throw new ArgumentOutOfRangeException(nameof(pageSize));
            if (maxPayloadBytes < 2 || maxPayloadBytes > MaximumPayloadBytes) throw new ArgumentOutOfRangeException(nameof(maxPayloadBytes));
            Offset = checked((pageNumber - 1) * pageSize);
            RequestId = requestId; PageNumber = pageNumber; PageSize = pageSize; MaxPayloadBytes = maxPayloadBytes;
            ValidateReadOnlyScalars = validateReadOnlyScalars;
            _order = (order ?? Array.Empty<PageOrder>()).Select(item => item ?? throw new ArgumentException("Null order field.")).ToArray();
            _filters = (filters ?? Array.Empty<AppFilter>()).Select(CopyFilter).ToArray();
            _anyFilters = (anyFilters ?? Array.Empty<AppFilter>()).Select(CopyFilter).ToArray();
            _anyRelated = (anyRelatedFilters ?? Array.Empty<PageRelatedFilter>()).Select(item => item ?? throw new ArgumentException("Null related predicate.")).ToArray();
            _requiredRelatedGroups = (requiredRelatedGroups ?? Array.Empty<PageRelatedFilterGroup>()).Select(value => value ?? throw new ArgumentException("Null required related group.")).ToArray();
            if (_requiredRelatedGroups.Length > 8 || _anyRelated.Length + _requiredRelatedGroups.Sum(group => group.Alternatives.Count) > 16) throw new ArgumentException("Too many grouped related predicates.");
            if (_anyRelated.Length > 8) throw new ArgumentException("Too many related predicates.");
            if (_filters.Length + _anyFilters.Length + _anyRelated.Concat(_requiredRelatedGroups.SelectMany(group => group.Alternatives)).Sum(item => item.CopyFilters().Count + item.CopyAnyFilters().Count + item.Comparisons.Count) > 64) throw new ArgumentException("Too many page filters.");
        }

        internal BoundedPageRequest WithValues(IEnumerable<PageOrder> order, IEnumerable<AppFilter> filters) =>
            new(RequestId, PageNumber, PageSize, MaxPayloadBytes, order, filters, _anyFilters, _anyRelated, ValidateReadOnlyScalars, _requiredRelatedGroups);
        /// <summary>Preserves the request identity/bounds while requiring computed scalar transport validation; no fields are ignored.</summary>
        public BoundedPageRequest WithReadOnlyScalarValidation() =>
            new(RequestId, PageNumber, PageSize, MaxPayloadBytes, _order, _filters, _anyFilters, _anyRelated, true, _requiredRelatedGroups);

        /// <summary>Preserves the traversal identity, copied predicates and bounds for a numbered batch.</summary>
        public BoundedPageRequest ForPage(long pageNumber) =>
            new(RequestId, pageNumber, PageSize, MaxPayloadBytes, _order, _filters, _anyFilters, _anyRelated, ValidateReadOnlyScalars, _requiredRelatedGroups);

        private static AppFilter CopyFilter(AppFilter filter)
        {
            if (filter == null) throw new ArgumentException("Null page filter.");
            return new AppFilter { FieldName = filter.FieldName, Operator = filter.Operator,
                FieldType = filter.FieldType, FilterValue = filter.FilterValue, FilterValue1 = filter.FilterValue1,
                valueType = filter.valueType, ID = filter.ID, GuidID = filter.GuidID };
        }
    }

    /// <summary>
    /// Copied UTF-8 JSON array of scalar-valued row objects (binary fields use base64 strings).
    /// Envelope evidence is validated by the consumer, not trusted merely because it matches a request.
    /// No record objects or enumerable row producers cross this boundary.
    /// </summary>
    public sealed class BoundedPageResponse
    {
        private readonly byte[] _payload;
        /// <summary>Request correlation observed by the producer.</summary>
        public Guid RequestId { get; }
        /// <summary>One-based page number observed by the producer.</summary>
        public long PageNumber { get; }
        /// <summary>Row limit observed by the producer.</summary>
        public int PageSize { get; }
        /// <summary>Snapshot count after applying every predicate.</summary>
        public long TotalRecords { get; }
        /// <summary>Encoded size of the copied JSON payload.</summary>
        public int PayloadBytes => _payload.Length;
        internal ReadOnlyMemory<byte> Payload => _payload;
        /// <summary>Returns an independent copy of the encoded rows.</summary>
        public byte[] CopyPayload() => (byte[])_payload.Clone();

        /// <summary>Copies the producer envelope and payload; consumer admission still validates them.</summary>
        public BoundedPageResponse(Guid requestId, long pageNumber, int pageSize, long totalRecords, ReadOnlySpan<byte> payload)
        {
            if (payload.Length > BoundedPageRequest.MaximumPayloadBytes) throw new ArgumentOutOfRangeException(nameof(payload));
            RequestId = requestId; PageNumber = pageNumber; PageSize = pageSize; TotalRecords = totalRecords;
            _payload = payload.ToArray();
        }
    }

    /// <summary>Validated page/count observation; publication acknowledgement is reported separately.</summary>
    public sealed class ProviderPageInfo
    {
        /// <summary>Correlation identifier of the admitted request.</summary>
        public Guid RequestId { get; }
        /// <summary>Requested one-based page number.</summary>
        public long PageNumber { get; }
        /// <summary>Admitted row limit.</summary>
        public int PageSize { get; }
        /// <summary>Validated filtered snapshot count.</summary>
        public long TotalRecords { get; }
        /// <summary>Number of pages implied by the count and admitted row limit.</summary>
        public long TotalPages => TotalRecords / PageSize + (TotalRecords % PageSize == 0 ? 0 : 1);
        /// <summary>Validated number of decoded rows.</summary>
        public int LoadedRecords { get; }
        /// <summary>Validated encoded response size.</summary>
        public int PayloadBytes { get; }
        /// <summary>Whether a page beyond the first lies outside the observed count.</summary>
        public bool IsOutOfRange => PageNumber > 1 && (TotalRecords == 0 || PageNumber > TotalPages);
        /// <summary>Validates exact row cardinality and byte limits against the admitted request.</summary>
        public ProviderPageInfo(BoundedPageRequest request, long totalRecords, int loadedRecords, int payloadBytes)
        {
            ArgumentNullException.ThrowIfNull(request);
            var expected = request.Offset >= totalRecords ? 0 : (int)Math.Min(request.PageSize, totalRecords - request.Offset);
            if (totalRecords < 0 || loadedRecords != expected || payloadBytes < 2 || payloadBytes > request.MaxPayloadBytes)
                throw new ArgumentException("Page observation does not fit its request bounds and snapshot count.");
            RequestId = request.RequestId;
            PageNumber = request.PageNumber; PageSize = request.PageSize; TotalRecords = totalRecords;
            LoadedRecords = loadedRecords; PayloadBytes = payloadBytes;
        }
    }
}
