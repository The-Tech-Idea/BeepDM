using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>Optional bounded detached stored-column read. Does not publish partial entities to Units.</summary>
    public interface IUnitofWorkStoredProjectionRead
    {
        bool SupportsStoredProjectionRead { get; }
        Task<StoredProjectionPage> ReadStoredProjectionPageAsync(BoundedPageRequest request, IEnumerable<string> fields,
            CancellationToken cancellationToken = default);
    }

    /// <summary>Frozen stored scalars. Binary values are copied on input and every access.</summary>
    public sealed class StoredProjectionRow : IReadOnlyDictionary<string, object>
    {
        private readonly Dictionary<string, object> _values;
        public StoredProjectionRow(IEnumerable<KeyValuePair<string, object>> values)
        {
            _values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values ?? throw new ArgumentNullException(nameof(values)))
            {
                if (string.IsNullOrWhiteSpace(value.Key) || _values.ContainsKey(value.Key)) throw new ArgumentException("Projection fields must be unique and nonempty.");
                var type=value.Value?.GetType();
                if(type is not null && !(type.IsPrimitive||type.IsEnum||type==typeof(string)||type==typeof(decimal)||type==typeof(Guid)||
                    type==typeof(DateTime)||type==typeof(DateTimeOffset)||type==typeof(TimeSpan)||type==typeof(byte[])))
                    throw new ArgumentException("Stored projections contain scalar or binary values only.");
                _values.Add(value.Key, Copy(value.Value));
            }
        }
        private static object Copy(object value) => value is byte[] bytes ? bytes.Clone() : value;
        public object this[string key] => Copy(_values[key]);
        public IEnumerable<string> Keys => _values.Keys;
        public IEnumerable<object> Values => _values.Values.Select(Copy);
        public int Count => _values.Count;
        public bool ContainsKey(string key) => _values.ContainsKey(key);
        public bool TryGetValue(string key, out object value)
        { var found=_values.TryGetValue(key,out var held);value=Copy(held);return found; }
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() =>
            _values.Select(value=>new KeyValuePair<string,object>(value.Key,Copy(value.Value))).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>Detached validated page observation; it is not an authorization or write capability.</summary>
    public sealed class StoredProjectionPage
    {
        public ProviderPageInfo Page { get; }
        public IReadOnlyList<StoredProjectionRow> Rows { get; }
        public StoredProjectionPage(ProviderPageInfo page, IEnumerable<StoredProjectionRow> rows)
        {
            Page=page??throw new ArgumentNullException(nameof(page));
            var copy=(rows??throw new ArgumentNullException(nameof(rows))).ToArray();
            if(copy.Length!=page.LoadedRecords||copy.Any(row=>row is null))throw new ArgumentException("Projection rows differ from the admitted page count.");
            Rows=Array.AsReadOnly(copy);
        }
    }
}
