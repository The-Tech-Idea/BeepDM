using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using static TheTechIdea.Beep.SetUp.StepErrorHelpers;

namespace TheTechIdea.Beep.SetUp.Steps
{
    /// <summary>
    /// Ensures that the connection driver for a specific package is available in the
    /// current process before a connection is opened.
    ///
    /// Three-state resolution model
    /// ─────────────────────────────
    ///  State 1  !IsMissing           → already loaded in-process → no-op / CanSkip
    ///  State 2  IsMissing &amp;&amp; !NuggetMissing → DLL cached on disk, not yet loaded →
    ///                                   assemblyHandler.LoadDriverFromLocalPackage
    ///                                   (falls through to State 3 on failure)
    ///  State 3  NuggetMissing        → package not downloaded →
    ///                                   assemblyHandler.LoadNuggetFromNuGetAsync
    ///
    /// After any State 2/3 work: ConfigEditor.SaveConnectionDriversConfigValues()
    /// </summary>
    public class DriverProvisionStep : IDriverProvisionStep
    {
        private readonly DriverProvisionStepOptions _opts;
        private readonly ILogger<DriverProvisionStep>? _logger;

        public DriverProvisionStep(DriverProvisionStepOptions opts, ILogger<DriverProvisionStep>? logger = null)
        {
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
            _logger = logger;
        }

        // ── ISetupStep ───────────────────────────────────────────────────────

        public string StepId => "driver-provision";
        public string StepName => "Driver Provisioning";
        public string Description => $"Ensure driver '{_opts.PackageName}' is loaded in the current process.";
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();

        public bool CanSkip(SetupContext context)
        {
            if (context?.Options?.DryRun == true) return true;
            // State 1: driver already loaded → nothing to do
            var driver = FindDriver(context);
            return driver != null && !driver.IsMissing;
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context?.Editor == null)
                return Fail("SetupContext.Editor is required.");

            if (string.IsNullOrWhiteSpace(_opts.PackageName))
                return Fail("DriverProvisionStepOptions.PackageName must be set.");

            return Ok();
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            var driver = FindDriver(context);
            if (driver == null)
                return Fail($"No driver configuration found with PackageName='{_opts.PackageName}'. " +
                             "Ensure the driver is registered in DataDriversClasses.");

            // State 1 — already loaded (safety check; CanSkip should catch this first)
            if (!driver.IsMissing)
                return Ok("Driver already loaded.");

            // State 2 — DLL cached on disk but not yet loaded into this process
            if (!driver.NuggetMissing)
            {
                StepErrorHelpers.Report(progress, 20, $"Loading driver from local package: {driver.PackageName}");

                if (context.Editor.assemblyHandler == null)
                    return Fail("IDMEEditor.assemblyHandler is null. Ensure AssemblyHandler was initialised before running DriverProvisionStep.");

                bool loaded = context.Editor.assemblyHandler
                    .LoadDriverFromLocalPackage(driver, out _);

                if (loaded && !driver.IsMissing)
                {
                    StepErrorHelpers.Report(progress, 90, "Driver loaded from local package.");
                    context.Editor.ConfigEditor.SaveConnectionDriversConfigValues();
                    _logger?.LogInformation("Driver '{PackageName}' loaded from local package", driver.PackageName);
                    return Ok($"Driver '{driver.PackageName}' loaded from local cache.");
                }

                // Fall through: local load failed — treat the package as missing and download
                driver.NuggetMissing = true;
            }

            // State 3 — package not on disk; download from NuGet
            StepErrorHelpers.Report(progress, 30, $"Downloading NuGet package: {driver.PackageName}");

            if (context.Editor.assemblyHandler == null)
                return Fail("IDMEEditor.assemblyHandler is null. Ensure AssemblyHandler was initialised before running DriverProvisionStep.");

            var version = _opts.Version
                          ?? (string.IsNullOrWhiteSpace(driver.NuggetVersion) ? null : driver.NuggetVersion);

            var sources = BuildSourceList(driver.NuggetSource);

            var downloadTask = context.Editor.assemblyHandler.LoadNuggetFromNuGetAsync(
                packageName: driver.PackageName,
                version: version,
                sources: sources,
                useSingleSharedContext: true,
                appInstallPath: _opts.AppInstallPath);

            // Block: this method is called from a synchronous ISetupStep.Execute.
            // Use ConfigureAwait(false).GetAwaiter().GetResult() to avoid deadlocks
            // if a SynchronizationContext is captured by the caller.
            var assemblies = downloadTask.ConfigureAwait(false).GetAwaiter().GetResult();

            if (assemblies == null || assemblies.Count == 0)
                return Fail($"NuGet download returned no assemblies for '{driver.PackageName}'.");

            StepErrorHelpers.Report(progress, 80, "Verifying [AddinAttribute] registration…");

            // Verify the driver registered itself through [AddinAttribute] detection
            bool verified = context.Editor.ConfigEditor.DataDriversClasses
                .Any(d => d.PackageName == driver.PackageName && !d.IsMissing);

            if (!verified)
                return Fail($"Driver '{driver.PackageName}' assemblies were loaded but no " +
                             "[AddinAttribute] type was found in DataDriversClasses. " +
                             "Ensure the driver DLL exposes an [AddinAttribute] class.");

            StepErrorHelpers.Report(progress, 90, "Persisting driver configuration…");
            context.Editor.ConfigEditor.SaveConnectionDriversConfigValues();
            _logger?.LogInformation("Driver '{PackageName}' v{Version} downloaded and loaded from NuGet",
                driver.PackageName, version ?? "(latest)");

            return Ok($"Driver '{driver.PackageName}' downloaded and loaded from NuGet.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private ConnectionDriversConfig FindDriver(SetupContext context) =>
            context?.Editor?.ConfigEditor?.DataDriversClasses
                ?.FirstOrDefault(d => string.Equals(
                    d.PackageName, _opts.PackageName, StringComparison.OrdinalIgnoreCase));

        private IList<string> BuildSourceList(string storedSource)
        {
            var result = new List<string>();
            if (!string.IsNullOrWhiteSpace(storedSource))
                result.Add(storedSource);

            if (_opts.NuGetSources != null)
                result.AddRange(_opts.NuGetSources.Where(s => !string.IsNullOrWhiteSpace(s)));

            return result.Count > 0 ? result : null;
        }
    }
}
