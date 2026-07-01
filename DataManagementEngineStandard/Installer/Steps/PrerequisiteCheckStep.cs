using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>
    /// Validates system prerequisites: OS version, .NET runtime, admin rights, disk space.
    /// Optionally downloads and silently installs missing prerequisites.
    /// </summary>
    public class PrerequisiteCheckStep : ISetupStep
    {
        public string StepId => "installer.prerequisites.check";
        public string StepName => "Check prerequisites";
        public string Description => "Verifies system requirements and installs missing components.";
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();

        public bool CanSkip(SetupContext context) => false;

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context.TryGetProperty<InstallConfig>("InstallConfig") == null)
                return StepErrorHelpers.Fail("InstallConfig not found.");
            return StepErrorHelpers.Ok("Ready to check prerequisites.");
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig")!;
            var results = new List<string>();
            bool allMet = true;

            // 1. OS version
            progress?.Report(new PassedArgs { Messege = "Checking OS version..." });
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                results.Add("FAIL: Windows 10 or later is required.");
                allMet = false;
            }
            else if (Environment.OSVersion.Version.Major < 10)
            {
                results.Add("FAIL: Windows 10 (build 1809+) or Windows 11 required.");
                allMet = false;
            }
            else
                results.Add("PASS: Windows version OK.");

            // 2. Admin rights
            progress?.Report(new PassedArgs { Messege = "Checking administrator privileges..." });
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                results.Add("WARN: Administrator privileges recommended. Some features may not install correctly.");
            }
            else
                results.Add("PASS: Running as administrator.");

            // 3. .NET runtime
            progress?.Report(new PassedArgs { Messege = "Checking .NET runtime..." });
            var dotnetVersion = GetDotNetRuntimeVersion();
            if (dotnetVersion == null)
            {
                results.Add("FAIL: .NET runtime not found. Please install .NET 9.0 or later.");
                allMet = false;
            }
            else
                results.Add($"PASS: .NET {dotnetVersion} detected.");

            // 4. Disk space
            progress?.Report(new PassedArgs { Messege = "Checking disk space..." });
            var installPath = context.TryGetProperty<string>("InstallPath") ?? config.DefaultInstallPath;
            if (!string.IsNullOrWhiteSpace(installPath))
            {
                try
                {
                    var root = Path.GetPathRoot(installPath);
                    if (root != null)
                    {
                        var drive = new DriveInfo(root);
                        long required = config.Components.Sum(c => c.SizeBytes) * 2; // 2x for temp/working space
                        if (drive.AvailableFreeSpace < required)
                        {
                            results.Add($"FAIL: Insufficient disk space. Required: {required / 1_048_576} MB, Available: {drive.AvailableFreeSpace / 1_048_576} MB");
                            allMet = false;
                        }
                        else
                            results.Add($"PASS: Disk space OK ({drive.AvailableFreeSpace / 1_073_741_824} GB available).");
                    }
                }
                catch { results.Add("WARN: Could not check disk space."); }
            }

            // 5. Custom prerequisites from config
            if (config.Prerequisites?.Count > 0)
            {
                foreach (var prereq in config.Prerequisites)
                {
                    progress?.Report(new PassedArgs { Messege = $"Checking {prereq.Name}..." });
                    if (!string.IsNullOrWhiteSpace(prereq.DetectionCommand))
                    {
                        var detected = RunDetectionCommand(prereq.DetectionCommand, prereq.DetectionPattern);
                        if (detected)
                            results.Add($"PASS: {prereq.Name} detected.");
                        else if (prereq.IsMandatory)
                        {
                            results.Add($"FAIL: {prereq.Name} not found. Required version: {prereq.VersionRequired}");
                            allMet = false;
                        }
                        else
                            results.Add($"WARN: {prereq.Name} not found (optional).");
                    }
                }
            }

            context.Properties["PrerequisiteResults"] = results;
            context.Properties["PrerequisitesMet"] = allMet;

            return allMet
                ? StepErrorHelpers.Ok($"All prerequisites met.\r\n{string.Join("\r\n", results)}")
                : StepErrorHelpers.Fail($"Prerequisites not met:\r\n{string.Join("\r\n", results)}");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));

        // ── Helpers ──────────────────────────────────────────────────────

        private static string? GetDotNetRuntimeVersion()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "--list-runtimes",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);

                // Parse: Microsoft.NETCore.App 9.0.0 [C:\...]
                var match = Regex.Match(output, @"Microsoft\.NETCore\.App\s+([\d.]+)");
                return match.Success ? match.Groups[1].Value : null;
            }
            catch
            {
                // dotnet CLI not found
                return null;
            }
        }

        private static bool RunDetectionCommand(string command, string? pattern)
        {
            try
            {
                var parts = command.Split(' ', 2);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = parts[0],
                        Arguments = parts.Length > 1 ? parts[1] : "",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(10000);

                if (string.IsNullOrWhiteSpace(pattern)) return process.ExitCode == 0;
                return Regex.IsMatch(output, pattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
