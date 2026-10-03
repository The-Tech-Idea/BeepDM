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
        Task<BoundedPageResponse> ReadPageAsync(string entityName, BoundedPageRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed class PageOrder
    {
        public string FieldName { get; }
        public bool Descending { get; }
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
        public const int MaximumPageSize = 5000;
        public const int MaximumPayloadBytes = 16 * 1024 * 1024;
        private readonly AppFilter[] _filters;
        private readonly PageOrder[] _order;
        public Guid RequestId { get; }
        public long PageNumber { get; }
        public int PageSize { get; }
        public int MaxPayloadBytes { get; }
        public long Offset { get; }
        public IReadOnlyList<PageOrder> Order => Array.AsReadOnly(_order);
        public List<AppFilter> CopyFilters() => _filters.Select(CopyFilter).ToList();

        public BoundedPageRequest(long pageNumber, int pageSize, int maxPayloadBytes,
            IEnumerable<PageOrder> order = null, IEnumerable<AppFilter> filters = null)
            : this(Guid.NewGuid(), pageNumber, pageSize, maxPayloadBytes, order, filters) { }

        private BoundedPageRequest(Guid requestId, long pageNumber, int pageSize, int maxPayloadBytes,
            IEnumerable<PageOrder> order, IEnumerable<AppFilter> filters)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
            if (pageSize < 1 || pageSize > MaximumPageSize) throw new ArgumentOutOfRangeException(nameof(pageSize));
            if (maxPayloadBytes < 2 || maxPayloadBytes > MaximumPayloadBytes) throw new ArgumentOutOfRangeException(nameof(maxPayloadBytes));
            Offset = checked((pageNumber - 1) * pageSize);
            RequestId = requestId; PageNumber = pageNumber; PageSize = pageSize; MaxPayloadBytes = maxPayloadBytes;
            _order = (order ?? Array.Empty<PageOrder>()).Select(item => item ?? throw new ArgumentException("Null order field.")).ToArray();
            _filters = (filters ?? Array.Empty<AppFilter>()).Select(CopyFilter).ToArray();
        }

        internal BoundedPageRequest WithValues(IEnumerable<PageOrder> order, IEnumerable<AppFilter> filters) =>
            new(RequestId, PageNumber, PageSize, MaxPayloadBytes, order, filters);

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
        public Guid RequestId { get; }
        public long PageNumber { get; }
        public int PageSize { get; }
        public long TotalRecords { get; }
        public int PayloadBytes => _payload.Length;
        internal ReadOnlyMemory<byte> Payload => _payload;
        public byte[] CopyPayload() => (byte[])_payload.Clone();

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
        public Guid RequestId { get; }
        public long PageNumber { get; }
        public int PageSize { get; }
        public long TotalRecords { get; }
        public long TotalPages => TotalRecords / PageSize + (TotalRecords % PageSize == 0 ? 0 : 1);
        public int LoadedRecords { get; }
        public int PayloadBytes { get; }
        public bool IsOutOfRange => PageNumber > 1 && (TotalRecords == 0 || PageNumber > TotalPages);
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
