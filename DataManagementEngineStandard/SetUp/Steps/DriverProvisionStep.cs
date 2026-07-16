using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        /// <summary>
        /// Bare step id used when no package name is set. Retained so single-driver
        /// wizards and any existing <c>DependsOn</c> references keep resolving.
        /// </summary>
        public const string BaseStepId = "driver-provision";

        private readonly DriverProvisionStepOptions _opts;
        private readonly ILogger<DriverProvisionStep>? _logger;
        private readonly string _stepId;

        public DriverProvisionStep(DriverProvisionStepOptions opts, ILogger<DriverProvisionStep>? logger = null)
        {
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
            _logger = logger;
            // Default to the bare id: qualifying unconditionally would change the id of every
            // existing single-driver wizard and break their DependsOn references. Composers that
            // add more than one driver step assign qualified ids via BuildStepId.
            _stepId = string.IsNullOrWhiteSpace(opts.StepId) ? BaseStepId : opts.StepId;
        }

        /// <summary>
        /// Conventional qualified step id for a package (e.g. <c>"driver-provision:SQLite"</c>).
        /// Step ids must be unique within a wizard, so a wizard carrying one driver step per
        /// package must qualify them — an unqualified constant makes N&gt;1 drivers collide on
        /// <see cref="SetupWizardBuilder"/>'s step-id dictionary.
        /// </summary>
        public static string BuildStepId(string packageName)
            => string.IsNullOrWhiteSpace(packageName)
                ? BaseStepId
                : $"{BaseStepId}:{packageName}";

        /// <summary>
        /// True when any of <paramref name="assemblies"/> declares a type named
        /// <paramref name="classHandler"/>. Used to confirm a freshly downloaded driver really
        /// carries its IDataSource class before clearing <c>IsMissing</c>.
        /// </summary>
        private static bool ContainsDriverClass(IEnumerable<System.Reflection.Assembly> assemblies, string classHandler)
        {
            if (assemblies == null || string.IsNullOrWhiteSpace(classHandler)) return false;

            foreach (var asm in assemblies)
            {
                if (asm == null) continue;

                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    // Partially loadable assembly: inspect what did resolve rather than
                    // discarding the whole assembly.
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                if (types.Any(t => string.Equals(t.Name, classHandler, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Public accessor for the typed options. UI shells use this to read and write the
        /// option values directly (PackageName, Version, NuGetSources, AppInstallPath).
        /// </summary>
        public DriverProvisionStepOptions Options => _opts;

        // ── ISetupStep ───────────────────────────────────────────────────────

        public string StepId => _stepId;

        /// <summary>Bare type key — StepId may be qualified per package.</summary>
        public string TypeKey => BaseStepId;
        /// <inheritdoc/>
        public Security.SetupPermission RequiredPermission => Security.SetupPermission.ProvisionDriver;

        /// <inheritdoc/>
        public System.Text.Json.JsonElement? SerializeOptions()
            => System.Text.Json.JsonSerializer.SerializeToElement(_opts, Definition.SetupJson.Options);

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
                    context.Editor.ConfigEditor.SaveConfigValues();
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

            // Block: this method is called from a synchronous ISetupStep.Execute.
            //
            // Task.Run wraps the CALL, and that placement is the whole point. Two things that do
            // NOT work here:
            //  - ConfigureAwait(false) on the returned task: it only affects how an `await`
            //    resumes, and this code blocks with GetAwaiter().GetResult() rather than awaiting,
            //    so it changes nothing. It also cannot help, because the awaits that capture the
            //    context are the ones INSIDE LoadNuggetFromNuGetAsync.
            //  - Wrapping the already-created task: invoking an async method runs it synchronously
            //    to its first await, capturing the caller's SynchronizationContext right there.
            // Starting the method inside Task.Run means there is no context to capture at all, so
            // a UI caller blocked here cannot deadlock against its own continuation.
            var assemblies = Task.Run(() => context.Editor.assemblyHandler.LoadNuggetFromNuGetAsync(
                packageName: driver.PackageName,
                version: version,
                sources: sources,
                useSingleSharedContext: true,
                appInstallPath: _opts.AppInstallPath)).GetAwaiter().GetResult();

            if (assemblies == null || assemblies.Count == 0)
                return Fail($"NuGet download returned no assemblies for '{driver.PackageName}'.");

            StepErrorHelpers.Report(progress, 80, "Verifying driver registration…");

            // Verify via the canonical IAssemblyHandler.IsDriverClassLoaded — checks both
            // ConfigEditor.DataSourcesClasses (the registry populated by the
            // AssemblyScanningAssistant at startup) and LoadedAssemblies. This is the same
            // check the Blazor pattern uses in BeepSetupWizardRunner.StageExistingConnectionDriver.
            bool verified = context.Editor.assemblyHandler?.IsDriverClassLoaded(
                driver.classHandler, driver.dllname) ?? false;

            if (!verified)
            {
                // Fallback for scan lag: the assemblies loaded but the scan-driven registry
                // hasn't picked them up yet (NuGet install before the next LoadAllAssembly pass),
                // and IsDriverClassLoaded's dll-name check can miss a package whose DLL is named
                // differently from driver.dllname.
                //
                // Verify against the assemblies we just loaded rather than assuming: only clear
                // IsMissing when the class genuinely exists. Asserting it here would let a broken
                // driver through to ConnectionConfigStep, which then fails far from the cause.
                verified = ContainsDriverClass(assemblies, driver.classHandler);
                if (verified)
                    driver.IsMissing = false;
            }

            if (!verified)
                return Fail($"Driver '{driver.PackageName}' assemblies were loaded but the " +
                            $"IDataSource implementation '{driver.classHandler}' was not found. " +
                             "Ensure the driver DLL exposes an [AddinAttribute] class.");

            StepErrorHelpers.Report(progress, 90, "Persisting driver configuration…");
            context.Editor.ConfigEditor.SaveConnectionDriversConfigValues();
            // Persist the flipped IsMissing flag and any newly registered class defs.
            context.Editor.ConfigEditor.SaveConfigValues();
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
