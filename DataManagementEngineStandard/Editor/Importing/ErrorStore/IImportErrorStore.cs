using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.Importing.ErrorStore
{
    /// <summary>
    /// Abstraction for persisting, loading, and managing <see cref="ImportErrorRecord"/> items
    /// produced during import runs.
    /// </summary>
    public interface IImportErrorStore
    {
        /// <summary>Persists a single failed record.</summary>
        Task SaveAsync(ImportErrorRecord record, CancellationToken token = default);

        /// <summary>Returns all error records for the given pipeline context key.</summary>
        Task<IReadOnlyList<ImportErrorRecord>> LoadAsync(string contextKey, CancellationToken token = default);

        /// <summary>
        /// Returns only records for <paramref name="contextKey"/> that have not yet been replayed.
        /// </summary>
        Task<IReadOnlyList<ImportErrorRecord>> LoadPendingAsync(string contextKey, CancellationToken token = default);

        /// <summary>Marks a specific record as replayed (by batch + record index).</summary>
        Task MarkReplayedAsync(string contextKey, int batchNumber, int recordIndex, CancellationToken token = default);

        /// <summary>Removes all error records for the given context key.</summary>
        Task ClearAsync(string contextKey, CancellationToken token = default);
    }
}
