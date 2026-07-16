using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.SetUp.Rollback;

namespace TheTechIdea.Beep.SetUp.Rollback
{
    /// <summary>
    /// Solo default: reports no confirmed backup, and warns. Honest — there is no backup — rather
    /// than the old inverted logic that claimed one existed whenever strict mode was off.
    /// </summary>
    public sealed class NoBackupConfirmationProvider : IBackupConfirmationProvider
    {
        private readonly ILogger _logger;

        public NoBackupConfirmationProvider(ILogger logger = null) => _logger = logger;

        public Task<bool> IsBackupConfirmedAsync(SetupContext context, CancellationToken token = default)
        {
            _logger?.LogWarning(
                "No backup confirmation provider configured; treating the datasource as NOT backed up. " +
                "Register an IBackupConfirmationProvider for environments where rollback safety matters.");
            return Task.FromResult(false);
        }
    }
}
