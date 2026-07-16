using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.SetUp.Rollback
{
    /// <summary>
    /// Answers whether a datasource backup is confirmed to exist before a schema change runs.
    /// </summary>
    /// <remarks>
    /// Replaces the inverted <c>CheckRollbackReadiness(backupConfirmed: !strict)</c>, which asserted
    /// a backup existed precisely when nobody had checked. The solo default returns false and warns
    /// (honest: no backup); strict mode requires a real true. Never claim a backup that wasn't verified.
    /// </remarks>
    public interface IBackupConfirmationProvider
    {
        Task<bool> IsBackupConfirmedAsync(SetupContext context, CancellationToken token = default);
    }
}
