using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Complete counted stored-column observation. Logical digest, never an actor or write grant.</summary>
    public sealed class ConditionalStoredProjectionGuard
    {
        /// <summary>Maximum complete reference rows per cohort; never a truncation allowance.</summary>
        public const int MaximumRows = 5000;
        /// <summary>Cached declared reference entity.</summary>
        public string EntityName { get; }
        /// <summary>Copied immutable bounded selector, including all related predicates.</summary>
        public BoundedPageRequest Request { get; }
        /// <summary>Selected stored fields; includes the complete primary key.</summary>
        public IReadOnlyList<string> Fields { get; }
        /// <summary>Complete observed key; the producer verifies it against metadata.</summary>
        public IReadOnlyList<string> KeyFields { get; }
        /// <summary>Count of the entire selected cohort.</summary>
        public int ExpectedCount { get; }
        /// <summary>Versioned logical content digest; not a signature or authority grant.</summary>
        public string ExpectedDigest { get; }
        /// <summary>Actual canonical bytes counted while preparing this digest.</summary>
        public long CanonicalBytes { get; }

        /// <summary>Capture one complete acknowledged first page; partial observations refuse.</summary>
        public ConditionalStoredProjectionGuard(string entityName, BoundedPageRequest request, IEnumerable<string> fields,
            IEnumerable<string> keyFields, StoredProjectionPage observation)
        {
            if (string.IsNullOrWhiteSpace(entityName) || entityName.Length > 256 || entityName.Any(char.IsControl)) throw new ArgumentException("An exact declared entity is required.");
            ArgumentNullException.ThrowIfNull(request); ArgumentNullException.ThrowIfNull(observation);
            if (request.PageNumber != 1 || request.PageSize > MaximumRows || request.ValidateReadOnlyScalars ||
                request.CopyFilters().Count == 0 || observation.Page.RequestId != request.RequestId ||
                observation.Page.PageNumber != 1 || observation.Page.PageSize != request.PageSize ||
                observation.Page.PayloadBytes > request.MaxPayloadBytes ||
                observation.Page.TotalRecords != observation.Rows.Count || observation.Rows.Count > MaximumRows)
                throw new ArgumentException("A complete bounded first-page stored observation with required predicates is required.");
            EntityName = entityName; Request = request;
            Fields = Array.AsReadOnly(Names(fields, 128)); KeyFields = Array.AsReadOnly(Names(keyFields, 128));
            if (request.Order.Count > Fields.Count || request.Order.Select(order=>order.FieldName).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=request.Order.Count ||
                KeyFields.Any(key => !Fields.Contains(key, StringComparer.OrdinalIgnoreCase)) ||
                request.Order.Any(order => !Fields.Contains(order.FieldName, StringComparer.OrdinalIgnoreCase)))
                throw new ArgumentException("Select the complete key and every order field.");
            var digest = ComputeDigest(Fields, KeyFields, observation.Rows);
            ExpectedCount = observation.Rows.Count; ExpectedDigest = digest.Digest; CanonicalBytes = digest.Bytes;
        }
        private static string[] Names(IEnumerable<string> names, int maximum)
        {
            var copy = names?.ToArray() ?? throw new ArgumentNullException(nameof(names));
            if (copy.Length < 1 || copy.Length > maximum || copy.Any(name => string.IsNullOrWhiteSpace(name) || name.Length > 128 || name.Any(char.IsControl)) ||
                copy.Distinct(StringComparer.OrdinalIgnoreCase).Count() != copy.Length) throw new ArgumentException("Projection fields must be bounded unique declared identifiers.");
            return copy.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        /// <summary>Order-independent rows; exact string/binary values, numeric value and stored date ticks/kind. Bounded canonical encoding.</summary>
        public static (string Digest, int Count, long Bytes) ComputeDigest(IEnumerable<string> fields, IEnumerable<string> keys, IEnumerable<StoredProjectionRow> rows)
        {
            var columns = Names(fields, 128); var keyColumns = Names(keys, 128);
            if (keyColumns.Any(key => !columns.Contains(key, StringComparer.OrdinalIgnoreCase))) throw new ArgumentException("Projection keys must be selected.");
            ArgumentNullException.ThrowIfNull(rows);
            var rowHashes = new List<string>(); var observedKeys = new HashSet<string>(StringComparer.Ordinal); long bytes = 0;
            string Hash(StoredProjectionRow row, string[] selected)
            {
                using var stream = new DigestStream(BoundedPageRequest.MaximumPayloadBytes - bytes);
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartArray();
                    foreach (var field in selected)
                    {
                        if (!row.TryGetValue(field, out var value)) throw new ArgumentException("The stored observation omitted a selected field.");
                        writer.WriteStringValue(field.ToUpperInvariant()); WriteScalar(writer, value); writer.Flush();
                    }
                    writer.WriteEndArray(); writer.Flush();
                }
                bytes += stream.Length; return stream.Finish();
            }
            foreach (var row in rows)
            {
                if (row is null || row.Count != columns.Length || rowHashes.Count >= MaximumRows ||
                    keyColumns.Any(key => !row.TryGetValue(key, out var value) || value is null))
                    throw new ArgumentException("A complete bounded projection with nonnull keys is required.");
                if (!observedKeys.Add(Hash(row, keyColumns))) throw new ArgumentException("The projection contains duplicate primary keys.");
                rowHashes.Add(Hash(row, columns));
            }
            using var result = new DigestStream(BoundedPageRequest.MaximumPayloadBytes - bytes);
            using (var writer = new Utf8JsonWriter(result))
            {
                writer.WriteStartArray(); writer.WriteStringValue("stored-projection-content-v3");
                foreach (var field in columns) writer.WriteStringValue(field.ToUpperInvariant());
                writer.WriteStringValue("keys"); foreach (var key in keyColumns) writer.WriteStringValue(key.ToUpperInvariant());
                writer.WriteNumberValue(rowHashes.Count);
                foreach (var hash in rowHashes.Order(StringComparer.Ordinal)) writer.WriteStringValue(hash);
                writer.WriteEndArray(); writer.Flush();
            }
            return (result.Finish(), rowHashes.Count, bytes + result.Length);
        }
        private static void WriteScalar(Utf8JsonWriter writer, object value)
        {
            if (value is null || value == DBNull.Value) { writer.WriteNullValue(); return; }
            var type = value.GetType();
            if (type.IsEnum) { writer.WriteStringValue("integer"); writer.WriteStringValue(Enum.Format(type, value, "D")); return; }
            switch (value)
            {
                case string text: writer.WriteStringValue("text"); writer.WriteStringValue(text); return;
                case char character: writer.WriteStringValue("char"); writer.WriteStringValue(character.ToString()); return;
                case bool flag: writer.WriteBooleanValue(flag); return;
                case byte[] binary: writer.WriteStringValue("bytes"); writer.WriteBase64StringValue(binary); return;
                case DateTime date: writer.WriteStringValue("date"); writer.WriteNumberValue(date.Ticks); writer.WriteNumberValue((int)date.Kind); return;
                case DateTimeOffset offset: writer.WriteStringValue("offset"); writer.WriteStringValue(offset.ToString("O", CultureInfo.InvariantCulture)); return;
                case TimeSpan duration: writer.WriteStringValue("duration"); writer.WriteNumberValue(duration.Ticks); return;
                case Guid id: writer.WriteStringValue("guid"); writer.WriteStringValue(id.ToString("N")); return;
                case decimal amount: writer.WriteStringValue("decimal"); writer.WriteStringValue(amount.ToString("G29", CultureInfo.InvariantCulture)); return;
                case double number when !double.IsFinite(number): throw new ArgumentException("Nonfinite stored numbers are unsupported.");
                case float number when !float.IsFinite(number): throw new ArgumentException("Nonfinite stored numbers are unsupported.");
                case double number: writer.WriteStringValue("binary64"); writer.WriteStringValue(number.ToString("R", CultureInfo.InvariantCulture)); return;
                case float number: writer.WriteStringValue("binary32"); writer.WriteStringValue(number.ToString("R", CultureInfo.InvariantCulture)); return;
                case byte or sbyte or short or ushort or int or uint or long or ulong:
                    writer.WriteStringValue("integer"); writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture)); return;
                default: throw new ArgumentException("Only stored scalar and binary values are supported.");
            }
        }
        private sealed class DigestStream(long limit) : Stream
        {
            private readonly IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            private long length;
            public string Finish() => Convert.ToHexString(hash.GetHashAndReset());
            public override bool CanRead => false; public override bool CanSeek => false; public override bool CanWrite => true;
            public override long Length => length; public override long Position { get => length; set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
            public override void Write(ReadOnlySpan<byte> buffer)
            { if (buffer.Length > limit - length) throw new ArgumentException("The complete projection exceeds its canonical byte budget."); hash.AppendData(buffer); length += buffer.Length; }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            protected override void Dispose(bool disposing) { if (disposing) hash.Dispose(); base.Dispose(disposing); }
        }
    }
}
