using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>Owned query authorization and restrictions captured at one policy revision.</summary>
    public sealed class QuerySecuritySnapshot
    {
        public long Revision { get; }
        public long ReadAuthorizationRevision { get; }
        public bool QueryAllowed { get; }
        public string RowFilterClause { get; }
        public IReadOnlyDictionary<string, object> RowFilterValues { get; }

        public QuerySecuritySnapshot(long revision, bool queryAllowed, string clause,
            IReadOnlyDictionary<string, object> values)
            : this(revision, queryAllowed, clause, values, revision) { }

        /// <summary>Read authorization changes include principal, roles and block/row policies, not field-only presentation rules.</summary>
        public QuerySecuritySnapshot(long revision, bool queryAllowed, string clause,
            IReadOnlyDictionary<string, object> values, long readAuthorizationRevision)
        {
            Revision = revision; QueryAllowed = queryAllowed; RowFilterClause = clause ?? string.Empty;
            ReadAuthorizationRevision = readAuthorizationRevision;
            RowFilterValues = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(
                values ?? new Dictionary<string, object>(), StringComparer.OrdinalIgnoreCase));
        }
    }
}

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>Optional coherent snapshot capability; every context/policy change advances the revision.</summary>
    public interface IQuerySecuritySnapshotProvider
    {
        long SecurityRevision { get; }
        Forms.Models.QuerySecuritySnapshot CaptureQuerySecurity(string blockName);
    }
}
