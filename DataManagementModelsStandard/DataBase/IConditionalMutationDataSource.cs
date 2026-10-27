using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional atomic single-row mutation. No read/ordinary-update fallback is permitted.</summary>
    public interface IConditionalMutationDataSource
    {
        /// <summary>Awaits physical completion of one guarded mutation.</summary>
        Task<ConditionalMutationResponse> MutateConditionallyAsync(ConditionalMutationRequest request,
            CancellationToken cancellationToken = default);
    }

    /// <summary>The supported single-row operation.</summary>
    public enum ConditionalMutationKind
    {
        /// <summary>Create a new guarded row.</summary>
        Insert,
        /// <summary>Replace fields only if the expected predicates still match.</summary>
        Update,
        /// <summary>Remove only the expected guarded row.</summary>
        Delete
    }
    /// <summary>Physical acknowledgement, distinct from admission or uncertain dispatch.</summary>
    public enum ConditionalMutationStatus
    {
        /// <summary>Exactly one row changed and commit was acknowledged.</summary>
        Applied,
        /// <summary>No row matched and rollback was acknowledged.</summary>
        NoMatch,
        /// <summary>The operation was refused before dispatch.</summary>
        NotApplied,
        /// <summary>The operation may have reached the provider; reload before another mutation.</summary>
        Unconfirmed
    }

    /// <summary>One row that must exist. Fields are equality predicates; null means IS NULL.</summary>
    public sealed class ConditionalRowGuard
    {
        /// <summary>Declared entity identifier.</summary>
        public string EntityName { get; }
        /// <summary>Copied expected equality predicates.</summary>
        public IReadOnlyDictionary<string, object> Match { get; }
        /// <summary>Optional declared UTC expiry column; null values allow indefinite validity.</summary>
        public string NotExpiredUtcField { get; }
        /// <summary>Optional civil-date lower endpoint, inclusive against the provider UTC calendar day. Null values are open.</summary>
        public string CalendarFromField { get; }
        /// <summary>Optional civil-date upper endpoint, inclusive against the provider UTC calendar day. Null values are open.</summary>
        public string CalendarThroughField { get; }
        /// <summary>Requires explicit calendar capability; providers recheck these groups immediately before commit.</summary>
        public bool HasCalendarWindow => CalendarFromField != null || CalendarThroughField != null;
        /// <summary>Constructs a bounded foreign-row guard.</summary>
        public ConditionalRowGuard(string entityName, IReadOnlyDictionary<string, object> match) : this(entityName, match, null) { }
        /// <summary>Constructs a guard with a declared UTC expiry column.</summary>
        public ConditionalRowGuard(string entityName, IReadOnlyDictionary<string, object> match, string notExpiredUtcField) : this(entityName, match, notExpiredUtcField, null, null) { }
        /// <summary>Constructs independent UTC-instant and inclusive civil-date windows.
        /// Calendar columns must preserve their recorded day and use date/date-time fields without an offset.</summary>
        public ConditionalRowGuard(string entityName, IReadOnlyDictionary<string, object> match, string notExpiredUtcField,
            string calendarFromField, string calendarThroughField)
        {
            if (string.IsNullOrWhiteSpace(entityName)) throw new ArgumentException("A guard entity is required.");
            EntityName = entityName;
            if (notExpiredUtcField != null && (string.IsNullOrWhiteSpace(notExpiredUtcField) || notExpiredUtcField.Length > 128))
                throw new ArgumentException("An expiry column must be a bounded declared field.");
            foreach (var field in new[] { calendarFromField, calendarThroughField })
                if (field != null && (string.IsNullOrWhiteSpace(field) || field.Length > 128 || field.Any(char.IsControl)))
                    throw new ArgumentException("A calendar endpoint must be a bounded declared field.");
            NotExpiredUtcField = notExpiredUtcField;
            CalendarFromField = calendarFromField; CalendarThroughField = calendarThroughField;
            Match = ConditionalMutationRequest.CopyValues(match);
            if (Match.Count == 0) throw new ArgumentException("A guard must contain predicates.");
        }
    }

    /// <summary>At least one alternative must match. Every guard group in a mutation is required.</summary>
    public sealed class ConditionalGuardGroup
    {
        /// <summary>Any matching foreign row satisfies this group.</summary>
        public IReadOnlyList<ConditionalRowGuard> Alternatives { get; }
        /// <summary>Copies a bounded set of alternatives.</summary>
        public ConditionalGuardGroup(IEnumerable<ConditionalRowGuard> alternatives)
        {
            var copy = alternatives?.ToArray() ?? throw new ArgumentNullException(nameof(alternatives));
            if (copy.Length == 0 || copy.Length > 16 || copy.Any(item => item == null))
                throw new ArgumentException("A bounded nonempty guard group is required.");
            Alternatives = Array.AsReadOnly(copy);
        }
    }

    /// <summary>Copied scalar values, expected row predicates and required foreign-row guards.
    /// This is data, not authorization: the application owns who may construct it.</summary>
    public sealed class ConditionalMutationRequest
    {
        /// <summary>Mutation correlation identifier.</summary>
        public Guid RequestId { get; } = Guid.NewGuid();
        /// <summary>Declared entity identifier.</summary>
        public string EntityName { get; }
        /// <summary>Requested operation.</summary>
        public ConditionalMutationKind Kind { get; }
        /// <summary>Copied replacement scalar values.</summary>
        public IReadOnlyDictionary<string, object> Values { get; }
        /// <summary>Copied expected equality predicates.</summary>
        public IReadOnlyDictionary<string, object> Match { get; }
        /// <summary>All required foreign-row guard groups.</summary>
        public IReadOnlyList<ConditionalGuardGroup> Guards { get; }
        /// <summary>Constructs an immutable guarded mutation.</summary>
        public ConditionalMutationRequest(string entityName, ConditionalMutationKind kind,
            IReadOnlyDictionary<string, object> values, IReadOnlyDictionary<string, object> match,
            IEnumerable<ConditionalGuardGroup> guards)
        {
            if (string.IsNullOrWhiteSpace(entityName)) throw new ArgumentException("An entity is required.");
            if (!Enum.IsDefined(typeof(ConditionalMutationKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            EntityName = entityName; Kind = kind;
            Values = CopyValues(values); Match = CopyValues(match);
            var copy = guards?.ToArray() ?? throw new ArgumentNullException(nameof(guards));
            if (kind != ConditionalMutationKind.Delete && Values.Count == 0 || copy.Length == 0 || copy.Length > 16 || copy.Any(item => item == null))
                throw new ArgumentException("Values and bounded required guards are required.");
            if (kind != ConditionalMutationKind.Insert && Match.Count == 0)
                throw new ArgumentException("An update requires expected row predicates.");
            if (kind == ConditionalMutationKind.Insert && Match.Count != 0)
                throw new ArgumentException("An insert does not accept update predicates.");
            if (kind == ConditionalMutationKind.Delete && Values.Count != 0)
                throw new ArgumentException("A delete does not accept replacement values.");
            Guards = Array.AsReadOnly(copy);
        }

        internal static IReadOnlyDictionary<string, object> CopyValues(IReadOnlyDictionary<string, object> values)
        {
            if (values == null || values.Count > 128) throw new ArgumentException("A bounded value map is required.");
            var copy = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in values)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || !copy.TryAdd(pair.Key, pair.Value))
                    throw new ArgumentException("Field names must be unique and nonempty.");
                if (pair.Value != null && pair.Value != DBNull.Value && pair.Value is not string &&
                    pair.Value is not bool && pair.Value is not byte && pair.Value is not short && pair.Value is not int &&
                    pair.Value is not long && pair.Value is not decimal && pair.Value is not double && pair.Value is not float &&
                    pair.Value is not Guid && pair.Value is not DateTime && pair.Value is not DateTimeOffset)
                    throw new ArgumentException("Only immutable scalar values are accepted.");
            }
            return new ReadOnlyDictionary<string, object>(copy);
        }
    }

    /// <summary>Applied means exactly one row and an acknowledged commit. Unconfirmed is never retry permission.</summary>
    public sealed class ConditionalMutationResponse
    {
        /// <summary>Mutation correlation identifier.</summary>
        public Guid RequestId { get; }
        /// <summary>Physical acknowledgement outcome.</summary>
        public ConditionalMutationStatus Status { get; }
        /// <summary>Full provider diagnostic; never present its text to a user.</summary>
        public Exception Failure { get; }
        /// <summary>Returns the acknowledgement and optional diagnostic.</summary>
        public ConditionalMutationResponse(Guid requestId, ConditionalMutationStatus status, Exception failure = null)
        {
            if (!Enum.IsDefined(typeof(ConditionalMutationStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            RequestId = requestId; Status = status; Failure = failure;
        }
    }
}
