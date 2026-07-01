using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>Installs/uninstalls Windows Services.</summary>
    public class ServiceInstallStep : ISetupStep
    {
        private readonly bool _isUninstall;

        public string StepId => _isUninstall ? "installer.service.remove" : "installer.service.install";
        public string StepName => _isUninstall ? "Remove services" : "Install services";
        public string Description => _isUninstall ? "Removes Windows Services." : "Installs and starts Windows Services.";
        public IReadOnlyList<string> DependsOn { get; }

        public ServiceInstallStep(bool isUninstall = false, string? dependsOn = null)
        {
            _isUninstall = isUninstall;
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
            => context.TryGetProperty<List<ServiceDefinition>>("Services")?.Count == 0;

        public IErrorsInfo Validate(SetupContext context)
        {
            var services = context.TryGetProperty<List<ServiceDefinition>>("Services");
            if (services == null) return StepErrorHelpers.Ok("No services.");
            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            foreach (var svc in services.Where(s => !_isUninstall))
            {
                var path = Path.Combine(installPath, svc.BinaryPath);
                if (!File.Exists(path))
                    return StepErrorHelpers.Fail($"Service binary not found: {path}");
            }
            return StepErrorHelpers.Ok("Validated.");
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var services = context.TryGetProperty<List<ServiceDefinition>>("Services");
            if (services == null || services.Count == 0)
                return StepErrorHelpers.Ok("No services configured.");

            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            int processed = 0;
            var errors = new List<string>();

            foreach (var svc in services)
            {
                progress?.Report(new PassedArgs { Messege = $"{(_isUninstall ? "Removing" : "Installing")} service: {svc.Name}" });

                if (_isUninstall)
                {
                    // Stop then delete
                    RunSc($"stop \"{svc.Name}\"");
                    var result = RunSc($"delete \"{svc.Name}\"");
                    if (result == 0) processed++;
                }
                else
                {
                    var binPath = Path.Combine(installPath, svc.BinaryPath);
                    var args = svc.Arguments ?? "";
                    var createArgs = $"create \"{svc.Name}\" binPath= \"\\\"{binPath}\\\" {args}\"" +
                                     (svc.StartType != ServiceStartType.Automatic ? "" : " start= auto") +
                                     (string.IsNullOrEmpty(svc.DisplayName) ? "" : $" DisplayName= \"{svc.DisplayName}\"");

                    var result = RunSc(createArgs);
                    if (result == 0)
                    {
                        if (svc.StartAfterInstall)
                            RunSc($"start \"{svc.Name}\"");

                        if (!string.IsNullOrEmpty(svc.Description))
                            RunSc($"description \"{svc.Name}\" \"{svc.Description}\"");

                        processed++;
                    }
                    else
                    {
                        errors.Add($"Failed to create service '{svc.Name}' (exit code {result})");
                    }
                }
            }

            return errors.Count > 0
                ? StepErrorHelpers.Fail(errors.Count + " errors: " + string.Join("; ", errors))
                : StepErrorHelpers.Ok($"{processed} services {(_isUninstall ? "removed" : "installed")}.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));

        private static int RunSc(string arguments)
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo("sc", arguments)
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true
                });
                p?.WaitForExit(15000);
                return p?.ExitCode ?? -1;
            }
            catch { return -1; }
        }
    }

    public class ServiceDefinition
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string BinaryPath { get; set; } = "";     // Relative to install path
        public string Arguments { get; set; } = "";
        public ServiceStartType StartType { get; set; } = ServiceStartType.Automatic;
        public bool StartAfterInstall { get; set; } = true;
    }

    public enum ServiceStartType { Automatic, Manual, Disabled }
}
