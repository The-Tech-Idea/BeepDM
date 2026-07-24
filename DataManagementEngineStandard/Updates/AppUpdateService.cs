using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// The <see cref="IAppUpdateService"/> façade — composes the feed client, delta planner,
    /// side-by-side applier and module updater into the app-facing surface. All collaborators are
    /// injectable so the whole flow is testable offline; the module service and directory link are
    /// optional (an app without a module channel simply gets app-level updates).
    /// </summary>
    public sealed class AppUpdateService : IAppUpdateService
    {
        private readonly UpdateFeedClient _feed;
        private readonly IModulePackageService? _modules;
        private readonly IDirectoryLink? _link;
        private readonly Action<string>? _recordVersion;

        public UpdateSettings Settings { get; }
        public event EventHandler<UpdateCheckResult>? UpdateAvailable;

        public AppUpdateService(
            UpdateSettings settings,
            UpdateFeedClient? feedClient = null,
            IModulePackageService? modulePackages = null,
            IDirectoryLink? link = null,
            Action<string>? recordVersion = null)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _feed = feedClient ?? new UpdateFeedClient();
            _modules = modulePackages;
            _link = link;
            _recordVersion = recordVersion;
        }

        private string InstallRoot => string.IsNullOrWhiteSpace(Settings.InstallRoot) ? AppContext.BaseDirectory : Settings.InstallRoot!;

        public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default)
        {
            if (Settings.Disabled)
                return UpdateCheckResult.Failed("Updates are disabled (BEEP_NO_UPDATE).", Settings.CurrentVersion);
            if (string.IsNullOrWhiteSpace(Settings.FeedUrl))
                return UpdateCheckResult.Failed("No update feed URL is configured.", Settings.CurrentVersion);

            UpdateFeed feed;
            try { feed = await _feed.FetchFeedAsync(Settings.FeedUrl, ct).ConfigureAwait(false); }
            catch (UpdateFeedException ex) { return UpdateCheckResult.Failed(ex.Message, Settings.CurrentVersion); }

            var result = new UpdateCheckResult
            {
                Succeeded = true,
                Feed = feed,
                CurrentVersion = Settings.CurrentVersion,
                LatestVersion = feed.Latest?.Version
            };

            if (feed.Latest != null && IsNewer(Settings.CurrentVersion, feed.Latest.Version))
            {
                result.AppUpdateAvailable = true;
                if (!string.IsNullOrWhiteSpace(feed.Latest.MinSupportedVersion)
                    && IsOlder(Settings.CurrentVersion, feed.Latest.MinSupportedVersion!))
                    result.RequiresFullInstall = true;
            }

            // Stale-module detection when a module channel is wired.
            if (_modules != null && feed.Modules.Count > 0)
            {
                var installed = await _modules.GetInstalledModulesAsync(Settings.ModuleDirectory, ct).ConfigureAwait(false);
                result.StaleModules = ModuleUpdater.ComputeStaleModules(feed.Modules, installed).ToList();
            }

            if (result.AnyUpdateAvailable)
                UpdateAvailable?.Invoke(this, result);

            return result;
        }

        public async Task<IErrorsInfo> ApplyAppUpdateAsync(UpdateCheckResult check, IProgress<PassedArgs>? progress = null, CancellationToken ct = default)
        {
            if (check?.Feed?.Latest is not { } release) return Fail("No release information to apply.");
            if (!check.AppUpdateAvailable) return Ok("Already up to date.");
            if (release.Delta is not { } delta)
                return Fail($"Release {release.Version} has no delta blob store; a full re-install via Setup.exe is required.");

            PayloadManifest remote;
            try { remote = await _feed.FetchManifestAsync(delta.ManifestUrl, ct).ConfigureAwait(false); }
            catch (UpdateFeedException ex) { return Fail(ex.Message, ex); }

            // Too old to delta from → materialize the whole version from blobs (local = null).
            var local = check.RequiresFullInstall ? null : ReadLocalManifest();
            var plan = DeltaPlanner.ComputePlan(remote, local);

            var applier = new SideBySideApplier(_link);
            var request = new SideBySideApplier.Request
            {
                InstallRoot = InstallRoot,
                NewVersion = release.Version,
                Plan = plan,
                CurrentVersion = string.IsNullOrWhiteSpace(Settings.CurrentVersion) ? null : Settings.CurrentVersion,
                FetchBlob = (hash, token) => _feed.FetchBlobAsync(CombineUrl(delta.BlobBaseUrl, hash), token),
                OnApplied = v => _recordVersion?.Invoke(v)
            };
            var applied = await applier.ApplyAsync(request, progress, ct).ConfigureAwait(false);

            var ok = applied.Flag == Errors.Ok;
            await ReportAsync(ok ? "apply-success" : "apply-failure", release.Version, ok, ok ? null : applied.Message, ct).ConfigureAwait(false);
            return applied;
        }

        public async Task<IErrorsInfo> ApplyModuleUpdatesAsync(UpdateCheckResult check, IProgress<PassedArgs>? progress = null, CancellationToken ct = default)
        {
            if (_modules == null) return Fail("No module package service is configured for this app.");
            if (check == null || check.StaleModules.Count == 0) return Ok("No modules to update.");

            var report = await new ModuleUpdater(_modules)
                .ApplyAsync(check.StaleModules, Settings.ModuleDirectory, progress, ct).ConfigureAwait(false);

            await ReportAsync(report.AllSucceeded ? "modules-success" : "modules-failure",
                check.LatestVersion ?? Settings.CurrentVersion, report.AllSucceeded,
                report.AllSucceeded ? null : string.Join("; ", report.Failed.Select(f => $"{f.Id}: {f.Error}")), ct).ConfigureAwait(false);

            return report.AllSucceeded
                ? Ok($"{report.Updated.Count} module(s) updated.")
                : Fail($"{report.Failed.Count} module update(s) failed: {string.Join("; ", report.Failed.Select(f => $"{f.Id}: {f.Error}"))}");
        }

        public Task<IErrorsInfo> RollbackAsync(CancellationToken ct = default)
            => Task.FromResult(new SideBySideApplier(_link).Rollback(InstallRoot));

        // ── helpers ──

        private PayloadManifest? ReadLocalManifest()
        {
            foreach (var candidate in new[]
            {
                Path.Combine(InstallRoot, SideBySideApplier.CurrentLinkName, "_payload-manifest.json"),
                Path.Combine(InstallRoot, "app-" + Settings.CurrentVersion, "_payload-manifest.json")
            })
            {
                if (!File.Exists(candidate)) continue;
                try { return JsonSerializer.Deserialize<PayloadManifest>(File.ReadAllText(candidate), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
                catch { /* fall through → treated as full install */ }
            }
            return null;
        }

        private static string CombineUrl(string baseUrl, string tail)
            => baseUrl.EndsWith('/') ? baseUrl + tail : baseUrl + "/" + tail;

        private static readonly HttpClient _telemetryHttp = new();

        /// <summary>
        /// Best-effort telemetry POST to the configured <see cref="UpdateSettings.TelemetryUrl"/>.
        /// A failure here is logged and swallowed — reporting must never affect the update itself.
        /// </summary>
        private async Task ReportAsync(string eventType, string toVersion, bool success, string? error, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(Settings.TelemetryUrl)) return;
            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    clientId = Settings.ClientId ?? "",
                    channel = Settings.Channel,
                    fromVersion = Settings.CurrentVersion,
                    toVersion,
                    success,
                    error,
                    eventType
                });
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                await _telemetryHttp.PostAsync(Settings.TelemetryUrl, content, ct).ConfigureAwait(false);
            }
            catch (Exception ex) { Debug.WriteLine($"[AppUpdateService] telemetry skipped: {ex.Message}"); }
        }

        internal static bool IsNewer(string current, string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate)) return false;
            if (string.IsNullOrWhiteSpace(current)) return true;
            if (NuGetVersion.TryParse(current, out var c) && NuGetVersion.TryParse(candidate, out var n)) return n > c;
            return !string.Equals(current, candidate, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsOlder(string current, string floor)
        {
            if (string.IsNullOrWhiteSpace(current)) return true;
            if (NuGetVersion.TryParse(current, out var c) && NuGetVersion.TryParse(floor, out var f)) return c < f;
            return false;
        }

        private static IErrorsInfo Ok(string msg) => new ErrorsInfo { Flag = Errors.Ok, Message = msg };
        private static IErrorsInfo Fail(string msg, Exception? ex = null) => new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };
    }
}
