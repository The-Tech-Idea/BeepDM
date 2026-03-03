using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Importing.Interfaces;

namespace TheTechIdea.Beep.Editor.Importing.History
{
    /// <summary>
    /// Contract for persisting and querying import run history records.
    /// </summary>
    public interface IImportRunHistoryStore
    {
        /// <summary>Persists a completed run record.</summary>
        Task SaveRunAsync(ImportRunRecord record, CancellationToken token = default);

        /// <summary>Returns all run records for the given context key, newest first.</summary>
        Task<IReadOnlyList<ImportRunRecord>> GetRunsAsync(string contextKey, CancellationToken token = default);

        /// <summary>
        /// Returns the most recent run record for <paramref name="contextKey"/> whose
        /// <see cref="ImportRunRecord.FinalState"/> is <see cref="Interfaces.ImportState.Completed"/>.
        /// Returns <c>null</c> when no such record exists.
        /// </summary>
        Task<ImportRunRecord?> GetLastSuccessfulRunAsync(string contextKey, CancellationToken token = default);

        /// <summary>Deletes all run records for the given context key.</summary>
        Task ClearAsync(string contextKey, CancellationToken token = default);
    }
}
