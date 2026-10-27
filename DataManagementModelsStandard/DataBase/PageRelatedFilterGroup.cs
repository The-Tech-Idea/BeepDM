using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional producer admitting every required group of related-row alternatives before count and page.</summary>
    public interface IBoundedGroupedRelatedPagedDataSource : IBoundedRelatedPagedDataSource { }

    /// <summary>One immutable, nonempty OR group. Every required group must independently match.</summary>
    public sealed class PageRelatedFilterGroup
    {
        private readonly PageRelatedFilter[] _alternatives;
        /// <summary>Immutable alternatives; their keys, predicates and comparisons are already captured.</summary>
        public IReadOnlyList<PageRelatedFilter> Alternatives => Array.AsReadOnly(_alternatives);
        /// <summary>Captures one to eight related-row alternatives; arbitrary expressions are not accepted.</summary>
        public PageRelatedFilterGroup(IEnumerable<PageRelatedFilter> alternatives)
        {
            _alternatives = (alternatives ?? throw new ArgumentNullException(nameof(alternatives)))
                .Select(value => value ?? throw new ArgumentException("Null related alternative.")).ToArray();
            if (_alternatives.Length is < 1 or > 8) throw new ArgumentException("One to eight related alternatives are required.");
        }
    }
}
