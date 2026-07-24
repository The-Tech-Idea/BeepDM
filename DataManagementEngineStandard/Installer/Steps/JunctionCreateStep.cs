using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;
using TheTechIdea.Beep.Updates;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>
    /// Side-by-side layout: points <c>&lt;base&gt;\current</c> at the just-installed
    /// <c>&lt;base&gt;\app-&lt;version&gt;</c> (a junction, via the shared <see cref="IDirectoryLink"/>),
    /// so shortcuts that target <c>current</c> transparently launch this version — and a later
    /// delta update flips the junction to a new version beside this one without touching the
    /// running files. Also stamps the runtime install root into the shipped
    /// <c>update-settings.json</c> so the app's self-updater knows where to lay new versions down.
    ///
    /// A no-op for a flat install (<see cref="CanSkip"/> returns true), so it can sit in the graph
    /// unconditionally.
    /// </summary>
    public class JunctionCreateStep : ISetupStep
    {
        private readonly IDirectoryLink _link;

        public JunctionCreateStep(string? dependsOn = null, IDirectoryLink? link = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
            _link = link ?? new JunctionLink();
        }

        public string StepId => "installer.junction.create";
        public string StepName => "Link current version";
        public string Description => "Points <base>\\current at the installed version (side-by-side layout).";
        public IReadOnlyList<string> DependsOn { get; }

        public bool CanSkip(SetupContext context) => !IsSideBySide(context);

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            if (!IsSideBySide(context)) return StepErrorHelpers.Ok("Flat install — no junction needed.");

            var baseDir = context.TryGetProperty<string>("InstallBaseDir");
            var installPath = context.TryGetProperty<string>("InstallPath");
            var launchPath = context.TryGetProperty<string>("LaunchPath");
            if (string.IsNullOrWhiteSpace(baseDir) || string.IsNullOrWhiteSpace(installPath) || string.IsNullOrWhiteSpace(launchPath))
                return StepErrorHelpers.Fail("Side-by-side layout paths are missing from the context.");

            if (context.Options?.DryRun == true)
                return StepErrorHelpers.Ok($"Dry run: would link {launchPath} → {installPath}.");

            try { _link.Point(launchPath!, installPath!); }
            catch (Exception ex)
            {
                return StepErrorHelpers.Fail($"Could not link '{launchPath}' → '{installPath}': {ex.Message}", ex);
            }

            PatchUpdateSettings(installPath!, baseDir!, context);
            progress?.Report(new PassedArgs { Messege = $"Linked current → {Path.GetFileName(installPath)}", ParameterInt1 = 100 });
            return StepErrorHelpers.Ok($"'current' now points at {installPath}.");
        }

        /// <summary>
        /// Adds the runtime install root (and version) to the shipped update-settings.json so the
        /// self-updater materializes new versions beside this one under the base, rather than
        /// nesting them inside it. Best-effort — a missing/unreadable settings file just means the
        /// app wasn't provisioned for self-update.
        /// </summary>
        private static void PatchUpdateSettings(string installPath, string baseDir, SetupContext context)
        {
            var file = Path.Combine(installPath, "update-settings.json");
            if (!File.Exists(file)) return;
            try
            {
                var read = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
                var settings = JsonSerializer.Deserialize<UpdateSettings>(File.ReadAllText(file), read) ?? new UpdateSettings();
                settings.InstallRoot = baseDir;
                if (string.IsNullOrWhiteSpace(settings.CurrentVersion)
                    && context.TryGetProperty<InstallConfig>("InstallConfig") is { } config)
                    settings.CurrentVersion = config.ProductVersion;

                var write = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
                File.WriteAllText(file, JsonSerializer.Serialize(settings, write));
            }
            catch { /* self-update provisioning must never fail the install */ }
        }

        private static bool IsSideBySide(SetupContext context)
            => context.Properties.TryGetValue("SideBySide", out var v) && v is bool b && b;

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }
}
