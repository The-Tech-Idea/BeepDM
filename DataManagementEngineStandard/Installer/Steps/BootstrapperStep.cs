using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>
    /// Downloads and silently installs prerequisite packages in sequence.
    /// Each bootstrapper item has a detection command, download URL, and silent install args.
    /// </summary>
    public class BootstrapperStep : ISetupStep
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(10) };

        public string StepId => "installer.bootstrapper";
        public string StepName => "Install prerequisites";
        public string Description => "Downloads and installs required components.";
        public IReadOnlyList<string> DependsOn { get; }

        public BootstrapperStep(string? dependsOn = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
            => context.TryGetProperty<List<BootstrapperItem>>("Bootstrapper")?.Count == 0;

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var items = context.TryGetProperty<List<BootstrapperItem>>("Bootstrapper");
            if (items == null || items.Count == 0) return StepErrorHelpers.Ok("No prerequisites.");

            var errors = new List<string>();
            int installed = 0;

            foreach (var item in items)
            {
                progress?.Report(new PassedArgs { Messege = $"Checking {item.Name}..." });

                // Skip if already installed
                if (!string.IsNullOrWhiteSpace(item.DetectionCommand) && RunDetection(item.DetectionCommand))
                {
                    progress?.Report(new PassedArgs { Messege = $"{item.Name} already installed — skipping." });
                    installed++;
                    continue;
                }

                var installerPath = item.LocalPath;

                // Download if URL provided
                if (!string.IsNullOrWhiteSpace(item.DownloadUrl))
                {
                    installerPath = Path.Combine(Path.GetTempPath(), $"beep_prereq_{item.Id}.exe");
                    progress?.Report(new PassedArgs { Messege = $"Downloading {item.Name}..." });
                    try
                    {
                        var data = _http.GetByteArrayAsync(item.DownloadUrl).GetAwaiter().GetResult();
                        File.WriteAllBytes(installerPath, data);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Download failed for {item.Name}: {ex.Message}");
                        if (item.Required) continue;
                    }
                }

                if (!File.Exists(installerPath))
                {
                    errors.Add($"Installer not found: {installerPath}");
                    if (item.Required) continue;
                }

                // Run silent install
                progress?.Report(new PassedArgs { Messege = $"Installing {item.Name}..." });
                try
                {
                    var args = item.SilentArgs ?? "/quiet /norestart";
                    var p = Process.Start(new ProcessStartInfo(installerPath!, args)
                    {
                        UseShellExecute = true,
                        Verb = "runas" // Elevate for prerequisites
                    });
                    p?.WaitForExit(item.TimeoutMs > 0 ? item.TimeoutMs : 600_000);

                    if (p?.ExitCode == 0 || p?.ExitCode == 3010) // 3010 = reboot required
                    {
                        installed++;
                        progress?.Report(new PassedArgs { Messege = $"{item.Name} installed." });
                    }
                    else
                    {
                        errors.Add($"{item.Name} installer exit code: {p?.ExitCode}");
                        if (item.Required) return StepErrorHelpers.Fail($"Required prerequisite failed: {item.Name}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{item.Name} install failed: {ex.Message}");
                    if (item.Required) return StepErrorHelpers.Fail($"Required prerequisite failed: {item.Name}");
                }
            }

            return errors.Count > 0
                ? StepErrorHelpers.Fail($"{installed} installed, {errors.Count} errors: {string.Join("; ", errors)}")
                : StepErrorHelpers.Ok($"{installed} prerequisites installed.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));

        private static bool RunDetection(string command)
        {
            try
            {
                var parts = command.Split(' ', 2);
                var p = Process.Start(new ProcessStartInfo(parts[0], parts.Length > 1 ? parts[1] : "")
                {
                    RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
                });
                p?.WaitForExit(10000);
                return p?.ExitCode == 0;
            }
            catch { return false; }
        }
    }

    public class BootstrapperItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string DetectionCommand { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string LocalPath { get; set; } = "";
        public string SilentArgs { get; set; } = "/quiet /norestart";
        public bool Required { get; set; } = true;
        public int TimeoutMs { get; set; }
    }
}
