using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// App self-update, registered by any Beep-based app via <c>AddBeepAppUpdates()</c> — check a
    /// static feed, apply hash-verified delta (or full) app updates side-by-side, roll back, and
    /// update feed-pinned NuGet modules. Lives in BeepDM (per decision D12) so update capability
    /// belongs to the deployed app, independent of how it was installed. Born cancellable:
    /// async-with-<see cref="CancellationToken"/> throughout, <see cref="IErrorsInfo"/> results,
    /// <see cref="PassedArgs"/> progress.
    /// </summary>
    public interface IAppUpdateService
    {
        /// <summary>Fetch + verify the feed and compare it against the installed state.</summary>
        Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default);

        /// <summary>Apply an app update (delta when possible, full otherwise), side-by-side.</summary>
        Task<IErrorsInfo> ApplyAppUpdateAsync(UpdateCheckResult check, IProgress<PassedArgs>? progress = null, CancellationToken ct = default);

        /// <summary>Update only the stale NuGet modules named in the check result.</summary>
        Task<IErrorsInfo> ApplyModuleUpdatesAsync(UpdateCheckResult check, IProgress<PassedArgs>? progress = null, CancellationToken ct = default);

        /// <summary>Flip back to the previous side-by-side version.</summary>
        Task<IErrorsInfo> RollbackAsync(CancellationToken ct = default);

        /// <summary>Feed URL, channel, mode and opt-out this service was configured with.</summary>
        UpdateSettings Settings { get; }

        /// <summary>Raised when a check finds an update available (drives the Optional-mode notification).</summary>
        event EventHandler<UpdateCheckResult>? UpdateAvailable;
    }
}
