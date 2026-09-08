using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>
    /// Registers COM in-process servers (Track A3.3). For every selected component's
    /// <see cref="ComRegistration"/>, writes the HKCR\CLSID\{Clsid}\InprocServer32 tree and the
    /// ProgId alias. Scope-aware: per-user → HKCU\Software\Classes, per-machine → HKLM\Software\Classes.
    /// Runs after <see cref="FileCopyStep"/>. Reversed by <see cref="UninstallStep"/>.
    /// </summary>
    public class ComServerRegistrationStep : ISetupStep
    {
        public string StepId => "installer.com.register";
        public string StepName => "Register COM servers";
        public string Description => "Writes HKCR\\CLSID entries for COM DLLs.";
        public IReadOnlyList<string> DependsOn { get; }

        public ComServerRegistrationStep(string? dependsOn = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            return config?.Components
                .Where(c => c.Selected || c.Required)
                .All(c => (c.ComRegistrations == null || c.ComRegistrations.Count == 0)) ?? true;
        }

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            if (config == null) return StepErrorHelpers.Ok("No configuration — nothing to register.");

            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            var written = new List<ComRegistration>();

            using var classes = InstallScope.OpenBaseKey(context, config)
                .CreateSubKey(@"Software\Classes", writable: true);
            if (classes == null)
                return StepErrorHelpers.Fail("Could not open Software\\Classes for COM registration.");

            foreach (var comp in config.Components.Where(c => c.Selected || c.Required))
            {
                foreach (var com in comp.ComRegistrations ?? Enumerable.Empty<ComRegistration>())
                {
                    if (string.IsNullOrWhiteSpace(com.Clsid)) continue;
                    var dll = Expand(com.DllPath, installPath);

                    // HKCR\CLSID\{Clsid}
                    using var clsid = classes.CreateSubKey($@"CLSID\{com.Clsid}");
                    if (!string.IsNullOrEmpty(com.Description)) clsid?.SetValue(null, com.Description);
                    if (!string.IsNullOrEmpty(com.ProgId)) clsid?.SetValue("ProgId", com.ProgId);
                    using var inproc = clsid?.CreateSubKey("InprocServer32");
                    inproc?.SetValue(null, dll);
                    if (!string.IsNullOrEmpty(com.ThreadingModel))
                        inproc?.SetValue("ThreadingModel", com.ThreadingModel);

                    // HKCR\{ProgId} → CLSID alias
                    if (!string.IsNullOrEmpty(com.ProgId))
                    {
                        using var progId = classes.CreateSubKey(com.ProgId);
                        progId?.SetValue(null, !string.IsNullOrEmpty(com.Description) ? com.Description : com.ProgId);
                        using var progClsid = progId?.CreateSubKey("CLSID");
                        progClsid?.SetValue(null, com.Clsid);
                    }

                    written.Add(com);
                }
            }

            context.Properties["ComRegistrationsWritten"] = written;
            return written.Count == 0
                ? StepErrorHelpers.Ok("No COM servers to register.")
                : StepErrorHelpers.Ok($"Registered {written.Count} COM server(s).");
        }

        public bool SupportsRollback => true;

        /// <summary>
        /// Removes the CLSID and ProgId trees this step wrote, through the same scope-aware base key
        /// it registered under — the reverse of Execute, and the same shape UninstallStep uses.
        /// </summary>
        public Task<IErrorsInfo> RollbackAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            var written = context.TryGetProperty<List<ComRegistration>>("ComRegistrationsWritten");
            if (written == null || written.Count == 0)
                return Task.FromResult(StepErrorHelpers.Ok("No COM registrations to undo."));

            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            var removed = 0;
            try
            {
                using var classes = InstallScope.OpenBaseKey(context, config)
                    .OpenSubKey(@"Software\Classes", writable: true);
                if (classes == null)
                    return Task.FromResult(StepErrorHelpers.Ok("The Software classes key is not writable; nothing undone."));

                foreach (var com in written)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(com.Clsid))
                            classes.DeleteSubKeyTree($@"CLSID\{com.Clsid}", throwOnMissingSubKey: false);
                        if (!string.IsNullOrWhiteSpace(com.ProgId))
                            classes.DeleteSubKeyTree(com.ProgId, throwOnMissingSubKey: false);
                        removed++;
                    }
                    catch (Exception ex)
                    {
                        progress?.Report(new PassedArgs { Messege = $"Could not unregister {com.Clsid}: {ex.Message}" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(StepErrorHelpers.Ok($"COM rollback skipped: {ex.Message}"));
            }

            context.Properties["ComRegistrationsWritten"] = new List<ComRegistration>();
            return Task.FromResult(StepErrorHelpers.Ok($"{removed} COM server registration(s) removed."));
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));

        internal static string Expand(string path, string installPath)
            => (path ?? "").Replace("{InstallPath}", installPath);
    }

    /// <summary>
    /// Installs assemblies into the GAC via <c>gacutil.exe</c> (Track A3.3). Best-effort: if
    /// gacutil isn't on PATH / the Windows SDK, the step reports OK with a note rather than
    /// failing the install (modern .NET rarely uses the GAC). Reversed by <see cref="UninstallStep"/>.
    /// </summary>
    public class GacInstallStep : ISetupStep
    {
        public string StepId => "installer.gac.install";
        public string StepName => "Install assemblies to GAC";
        public string Description => "Adds assemblies to the Global Assembly Cache via gacutil.";
        public IReadOnlyList<string> DependsOn { get; }

        public GacInstallStep(string? dependsOn = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            return config?.Components
                .Where(c => c.Selected || c.Required)
                .All(c => (c.GacAssemblies == null || c.GacAssemblies.Count == 0)) ?? true;
        }

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            if (config == null) return StepErrorHelpers.Ok("No configuration — nothing to GAC.");

            var assemblies = config.Components
                .Where(c => c.Selected || c.Required)
                .SelectMany(c => c.GacAssemblies ?? Enumerable.Empty<GacAssembly>())
                .ToList();
            if (assemblies.Count == 0)
                return StepErrorHelpers.Ok("No assemblies to GAC.");

            var gacutil = FindGacUtil();
            if (gacutil == null)
                return StepErrorHelpers.Ok("GAC install skipped — gacutil.exe not found (modern .NET: this is expected).");

            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            var installed = new List<GacAssembly>();
            foreach (var asm in assemblies)
            {
                var path = ComServerRegistrationStep.Expand(asm.Path, installPath);
                if (!File.Exists(path)) continue;
                if (RunGacUtil(gacutil, $"/i \"{path}\"", out var err))
                    installed.Add(asm);
                else
                    progress?.Report(new PassedArgs { Messege = $"GAC install failed for {path}: {err}" });
            }

            context.Properties["GacAssembliesInstalled"] = installed;
            return StepErrorHelpers.Ok(installed.Count == 0
                ? "No assemblies were GAC-installed."
                : $"GAC-installed {installed.Count} assembly/ies.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));

        internal static bool RunGacUtil(string exe, string args, out string error)
        {
            try
            {
                // stdout was redirected and never drained here, so a chatty gacutil could block
                // on a full pipe until the wait expired.
                var run = InstallHelpers.RunProcess(new ProcessStartInfo
                {
                    FileName = exe, Arguments = args,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                }, 30_000);

                error = run.Started && !run.TimedOut ? run.StandardError.Trim() : run.Error;
                return run.Succeeded;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        internal static string? FindGacUtil()
        {
            foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "")
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                // A bad PATH entry must not stop the scan — report and continue to the next dir.
                try { var c = Path.Combine(dir, "gacutil.exe"); if (File.Exists(c)) return c; }
                catch (Exception ex) { Debug.WriteLine($"GacInstallStep.FindGacUtil: probing '{dir}' ignored: {ex}"); }
            }
            return null;
        }
    }
}
