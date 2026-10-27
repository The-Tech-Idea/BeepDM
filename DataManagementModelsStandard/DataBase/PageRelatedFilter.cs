using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional producer that applies copied related-row predicates before both count and page.</summary>
    public interface IBoundedRelatedPagedDataSource : IBoundedPagedDataSource { }
    /// <summary>Optional producer admitting declared same-type numeric parent/related field comparisons before count/page.</summary>
    public interface IBoundedComparedRelatedPagedDataSource : IBoundedRelatedPagedDataSource { }

    /// <summary>One immutable numeric comparison, never an SQL expression or authorization credential.</summary>
    public sealed class PageRelatedComparison
    {
        /// <summary>Declared numeric field on the parent entity.</summary>
        public string ParentField { get; }
        /// <summary>Declared same-type numeric field on the related entity.</summary>
        public string RelatedField { get; }
        /// <summary>One admitted comparison symbol; arbitrary expressions are refused.</summary>
        public string Operator { get; }
        /// <summary>Creates one immutable declared-field comparison.</summary>
        public PageRelatedComparison(string parentField, string comparison, string relatedField)
        {
            if (string.IsNullOrWhiteSpace(parentField) || string.IsNullOrWhiteSpace(relatedField) ||
                comparison is not ("=" or "<>" or "<" or "<=" or ">" or ">=")) throw new ArgumentException("Declared fields and a supported numeric comparison are required.");
            ParentField = parentField; RelatedField = relatedField; Operator = comparison;
        }
    }

    /// <summary>One immutable pair of declared fields that correlates a related row to its parent.</summary>
    public sealed class PageRelatedKey
    {
        /// <summary>Declared field on the parent entity.</summary>
        public string ParentField { get; }
        /// <summary>Declared field on the related entity with the same underlying type.</summary>
        public string RelatedField { get; }
        /// <summary>Creates a declared correlation; the provider validates metadata before execution.</summary>
        public PageRelatedKey(string parentField, string relatedField)
        {
            if (string.IsNullOrWhiteSpace(parentField) || string.IsNullOrWhiteSpace(relatedField))
                throw new ArgumentException("Declared correlation fields are required.");
            ParentField = parentField; RelatedField = relatedField;
        }
    }

    /// <summary>A positive EXISTS predicate over cached declared metadata; it adds no authorization.</summary>
    public sealed class PageRelatedFilter
    {
        private readonly PageRelatedKey[] _keys;
        private readonly AppFilter[] _filters, _anyFilters;
        private readonly PageRelatedComparison[] _comparisons;
        /// <summary>Copied positive comparisons evaluated in the same related EXISTS predicate.</summary>
        public IReadOnlyList<PageRelatedComparison> Comparisons => Array.AsReadOnly(_comparisons);
        /// <summary>Related entity name resolved through cached declared metadata.</summary>
        public string EntityName { get; }
        /// <summary>Copied one-to-four field correlations applied together.</summary>
        public IReadOnlyList<PageRelatedKey> Keys => Array.AsReadOnly(_keys);
        /// <summary>Returns independent copies of required related-row predicates.</summary>
        public List<AppFilter> CopyFilters() => _filters.Select(Copy).ToList();
        /// <summary>Returns independent copies of alternatives within one related-row OR group.</summary>
        public List<AppFilter> CopyAnyFilters() => _anyFilters.Select(Copy).ToList();
        /// <summary>Copies declared keys and predicates within the related query bounds.</summary>
        public PageRelatedFilter(string entityName, IEnumerable<PageRelatedKey> keys,
            IEnumerable<AppFilter> filters = null, IEnumerable<AppFilter> anyFilters = null)
            : this(entityName, keys, filters, anyFilters, null) { }
        /// <summary>Copies correlations, filters and optional exact numeric field comparisons within the related bounds.</summary>
        public PageRelatedFilter(string entityName, IEnumerable<PageRelatedKey> keys,
            IEnumerable<AppFilter> filters, IEnumerable<AppFilter> anyFilters, IEnumerable<PageRelatedComparison> comparisons)
        {
            if (string.IsNullOrWhiteSpace(entityName)) throw new ArgumentException("A declared related entity is required.");
            EntityName = entityName;
            _keys = (keys ?? throw new ArgumentNullException(nameof(keys))).Select(key => key ?? throw new ArgumentException("Null related key.")).ToArray();
            if (_keys.Length < 1 || _keys.Length > 4 || _keys.Select(key => key.RelatedField).Distinct(StringComparer.OrdinalIgnoreCase).Count() != _keys.Length)
                throw new ArgumentException("One to four distinct related correlation fields are required.");
            _filters = (filters ?? Array.Empty<AppFilter>()).Select(Copy).ToArray();
            _anyFilters = (anyFilters ?? Array.Empty<AppFilter>()).Select(Copy).ToArray();
            _comparisons = (comparisons ?? Array.Empty<PageRelatedComparison>()).Select(value => value ?? throw new ArgumentException("Null related comparison.")).ToArray();
            if (_comparisons.Length > 4) throw new ArgumentException("Too many related comparisons.");
            if (_filters.Length + _anyFilters.Length + _comparisons.Length > 16) throw new ArgumentException("Too many related filters.");
        }
        private static AppFilter Copy(AppFilter filter)
        {
            if (filter == null) throw new ArgumentException("Null related filter.");
            return new AppFilter { FieldName=filter.FieldName, Operator=filter.Operator, FieldType=filter.FieldType,
                FilterValue=filter.FilterValue, FilterValue1=filter.FilterValue1, valueType=filter.valueType, ID=filter.ID, GuidID=filter.GuidID };
        }
    }
}
