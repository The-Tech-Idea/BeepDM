using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional acknowledged, physically drained entity read. It must preserve provider
    /// hooks and transaction-bound reads, and never fall back after owned acquisition fails.</summary>
    public interface IOwnedEntityReadDataSource
    {
        bool SupportsOwnedEntityReads { get; }
        Task<EntityReadResult> ReadEntityAsync(string entityName, List<AppFilter> filters,
            CancellationToken cancellationToken = default);
    }

    public sealed class EntityReadResult
    {
        public IErrorsInfo Outcome { get; }
        public IReadOnlyList<object> Rows { get; }
        public EntityReadResult(IErrorsInfo outcome, IEnumerable<object> rows)
        {
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));
            Outcome = new ErrorsInfo { Flag = outcome.Flag, Message = outcome.Message, Ex = outcome.Ex,
                Errors = outcome.Errors?.ToList() ?? new List<IErrorsInfo>() };
            Rows = Array.AsReadOnly(outcome.Flag == Errors.Ok
                ? (rows ?? throw new ArgumentNullException(nameof(rows))).ToArray() : Array.Empty<object>());
        }
    }
}
