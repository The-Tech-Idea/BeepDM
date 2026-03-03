using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.Importing.Sync
{
    /// <summary>
    /// Persists the high-watermark value between incremental import runs so that each run
    /// fetches only records newer than the last successfully imported record.
    /// </summary>
    public interface IWatermarkStore
    {
        /// <summary>
        /// Persists the latest watermark value for the given pipeline context key.
        /// </summary>
        Task SaveWatermarkAsync(string contextKey, object value, CancellationToken token = default);

        /// <summary>
        /// Returns the last saved watermark value, or <c>null</c> when no run has completed yet.
        /// </summary>
        Task<object?> LoadWatermarkAsync(string contextKey, CancellationToken token = default);

        /// <summary>Removes the stored watermark (forces a full-refresh on the next run).</summary>
        Task ClearWatermarkAsync(string contextKey, CancellationToken token = default);
    }
}
