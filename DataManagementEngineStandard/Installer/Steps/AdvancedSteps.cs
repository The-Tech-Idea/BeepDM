using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>Downloads components on-demand during installation with progress.</summary>
    public class DownloadOnDemandStep : ISetupStep
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromHours(2) };

        public string StepId => "installer.download";
        public string StepName => "Download components";
        public string Description => "Downloads components from the internet.";
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();

        public bool CanSkip(SetupContext context)
            => context.TryGetProperty<List<DownloadItem>>("Downloads")?.Count == 0;

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var items = context.TryGetProperty<List<DownloadItem>>("Downloads");
            if (items == null || items.Count == 0) return StepErrorHelpers.Ok("Nothing to download.");

            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            long totalBytes = 0;
            var errors = new List<string>();

            foreach (var item in items)
            {
                progress?.Report(new PassedArgs { Messege = $"Downloading {item.Name}..." });

                try
                {
                    var dest = Path.Combine(installPath, item.DestinationPath);
                    var dir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                    if (item.Sha256 != null && File.Exists(dest))
                    {
                        if (InstallHelpers.VerifyFileHash(dest, item.Sha256))
                        {
                            progress?.Report(new PassedArgs { Messege = $"{item.Name} already downloaded — verified." });
                            totalBytes += new FileInfo(dest).Length;
                            continue;
                        }
                    }

                    var response = _http.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();

                    using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                    using var fileStream = File.Create(dest);
                    var buffer = new byte[8192];
                    long downloaded = 0;
                    long total = response.Content.Headers.ContentLength ?? -1;
                    int read;

                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, read);
                        downloaded += read;
                        if (total > 0)
                            progress?.Report(new PassedArgs
                            {
                                Messege = $"{item.Name}: {downloaded * 100 / total}%",
                                ParameterInt1 = (int)(downloaded * 100 / total)
                            });
                    }

                    totalBytes += downloaded;

                    if (item.Sha256 != null && !InstallHelpers.VerifyFileHash(dest, item.Sha256))
                        errors.Add($"Hash mismatch for {item.Name}");
                }
                catch (Exception ex)
                {
                    errors.Add($"Download failed for {item.Name}: {ex.Message}");
                    if (item.Required) return StepErrorHelpers.Fail($"Required download failed: {item.Name}");
                }
            }

            context.Properties["DownloadedBytes"] = totalBytes;
            return errors.Count > 0
                ? StepErrorHelpers.Fail($"{errors.Count} download errors: {string.Join("; ", errors)}")
                : StepErrorHelpers.Ok($"Downloaded {FormatBytes(totalBytes)}.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));

        private static string FormatBytes(long b) => b switch
        {
            >= 1_073_741_824 => $"{b / 1_073_741_824.0:F1} GB",
            >= 1_048_576 => $"{b / 1_048_576.0:F1} MB",
            >= 1024 => $"{b / 1024.0:F1} KB",
            _ => $"{b} B"
        };
    }

    public class DownloadItem
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string DestinationPath { get; set; } = "";
        public string? Sha256 { get; set; }
        public bool Required { get; set; } = true;
    }

    /// <summary>Registers/unregisters COM components.</summary>
    public class ComRegistrationStep : ISetupStep
    {
        private readonly bool _isUninstall;

        public string StepId => _isUninstall ? "installer.com.unregister" : "installer.com.register";
        public string StepName => _isUninstall ? "Unregister COM" : "Register COM";
        public string Description => _isUninstall ? "Unregisters COM components." : "Registers COM components.";
        public IReadOnlyList<string> DependsOn { get; }

        public ComRegistrationStep(bool isUninstall = false, string? dependsOn = null)
        {
            _isUninstall = isUninstall;
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
            => context.TryGetProperty<List<string>>("ComComponents")?.Count == 0;

        public IErrorsInfo Validate(SetupContext context)
        {
            var components = context.TryGetProperty<List<string>>("ComComponents");
            if (components == null) return StepErrorHelpers.Ok("No COM components.");
            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            foreach (var c in components)
            {
                var path = Path.Combine(installPath, c);
                if (!File.Exists(path) && !_isUninstall)
                    return StepErrorHelpers.Fail($"COM component not found: {path}");
            }
            return StepErrorHelpers.Ok("Validated.");
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var components = context.TryGetProperty<List<string>>("ComComponents");
            if (components == null || components.Count == 0) return StepErrorHelpers.Ok("No COM components.");

            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            int processed = 0;
            var errors = new List<string>();

            foreach (var relPath in components)
            {
                var path = Path.Combine(installPath, relPath);
                progress?.Report(new PassedArgs { Messege = $"{(_isUninstall ? "Unregistering" : "Registering")}: {relPath}" });

                try
                {
                    var args = _isUninstall ? $"/u \"{path}\" /s" : $"\"{path}\" /s";
                    var p = Process.Start(new ProcessStartInfo("regsvr32", args)
                    {
                        UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true
                    });
                    p?.WaitForExit(30000);
                    if (p?.ExitCode == 0) processed++;
                    else errors.Add($"regsvr32 exit {p?.ExitCode}: {relPath}");
                }
                catch (Exception ex) { errors.Add($"{relPath}: {ex.Message}"); }
            }

            return errors.Count > 0
                ? StepErrorHelpers.Fail(errors.Count + " errors: " + string.Join("; ", errors))
                : StepErrorHelpers.Ok($"{processed} components {(_isUninstall ? "unregistered" : "registered")}.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }

    /// <summary>Creates/removes Windows Scheduled Tasks.</summary>
    public class ScheduledTaskStep : ISetupStep
    {
        private readonly bool _isUninstall;

        public string StepId => _isUninstall ? "installer.tasks.remove" : "installer.tasks.create";
        public string StepName => _isUninstall ? "Remove scheduled tasks" : "Create scheduled tasks";
        public string Description => _isUninstall ? "Removes Windows scheduled tasks." : "Creates Windows scheduled tasks.";
        public IReadOnlyList<string> DependsOn { get; }

        public ScheduledTaskStep(bool isUninstall = false, string? dependsOn = null)
        {
            _isUninstall = isUninstall;
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
            => context.TryGetProperty<List<ScheduledTaskDef>>("ScheduledTasks")?.Count == 0;

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var tasks = context.TryGetProperty<List<ScheduledTaskDef>>("ScheduledTasks");
            if (tasks == null || tasks.Count == 0) return StepErrorHelpers.Ok("No scheduled tasks.");

            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            int processed = 0;

            foreach (var task in tasks)
            {
                progress?.Report(new PassedArgs { Messege = $"{(_isUninstall ? "Removing" : "Creating")}: {task.Name}" });

                var exePath = Path.Combine(installPath, task.ProgramPath);
                var args = _isUninstall
                    ? $"/delete /tn \"{task.Name}\" /f"
                    : $"/create /tn \"{task.Name}\" /tr \"\\\"{exePath}\\\" {task.Arguments}\" /sc {task.ScheduleType.ToString().ToUpper()}";

                if (!_isUninstall)
                {
                    if (task.ScheduleType == TaskScheduleType.Daily)
                        args += $" /st {task.StartTime:HH:mm}";
                    else if (task.ScheduleType == TaskScheduleType.AtLogon)
                        args += " /sc onlogon";
                    else if (task.ScheduleType == TaskScheduleType.AtStartup)
                        args += " /sc onstart";
                    else if (task.ScheduleType == TaskScheduleType.Hourly)
                        args += $" /sc hourly /mo {task.Interval}";
                }

                var p = Process.Start(new ProcessStartInfo("schtasks", args)
                {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true
                });
                p?.WaitForExit(15000);
                if (p?.ExitCode == 0) processed++;
            }

            return StepErrorHelpers.Ok($"{processed} tasks {(_isUninstall ? "removed" : "created")}.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }

    public class ScheduledTaskDef
    {
        public string Name { get; set; } = "";
        public string ProgramPath { get; set; } = "";      // Relative to install path
        public string Arguments { get; set; } = "";
        public TaskScheduleType ScheduleType { get; set; } = TaskScheduleType.Daily;
        public TimeSpan StartTime { get; set; } = new(3, 0, 0); // 3 AM default
        public int Interval { get; set; } = 1;              // Hours for hourly schedule
    }

    public enum TaskScheduleType { Daily, Hourly, AtLogon, AtStartup }
}
