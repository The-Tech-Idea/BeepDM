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
    /// <summary>
    /// Executes custom actions (scripts, executables) at defined install points.
    /// Actions are read from context.Properties["CustomActions"] filtered by timing.
    /// </summary>
    public class CustomActionStep : ISetupStep
    {
        private readonly CustomActionTiming _timing;

        public string StepId => $"installer.custom.{_timing.ToString().ToLower()}";
        public string StepName => _timing switch
        {
            CustomActionTiming.BeforeInstall => "Run pre-install actions",
            CustomActionTiming.AfterInstall => "Run post-install actions",
            CustomActionTiming.BeforeUninstall => "Run pre-uninstall actions",
            CustomActionTiming.AfterUninstall => "Run post-uninstall actions",
            _ => "Run custom actions"
        };
        public string Description => $"Executes custom scripts for {_timing} phase.";
        public IReadOnlyList<string> DependsOn { get; }

        public CustomActionStep(CustomActionTiming timing, string? dependsOn = null)
        {
            _timing = timing;
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
        {
            var actions = GetActions(context);
            return actions.Count == 0;
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            var actions = GetActions(context);
            foreach (var action in actions)
            {
                var expandedPath = ExpandPath(action.Path, context);
                if (action.Required && !File.Exists(expandedPath))
                    return StepErrorHelpers.Fail($"Required action file not found: {expandedPath}");
            }
            return StepErrorHelpers.Ok("Validated.");
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var actions = GetActions(context);
            if (actions.Count == 0)
                return StepErrorHelpers.Ok($"No {_timing} actions.");

            var errors = new List<string>();
            int executed = 0;

            foreach (var action in actions.OrderBy(a => a.Order))
            {
                progress?.Report(new PassedArgs { Messege = $"Running: {action.Description ?? action.Path}" });

                try
                {
                    var expandedPath = ExpandPath(action.Path, context);
                    var expandedArgs = ExpandPath(action.Arguments ?? "", context);
                    var expandedWorkDir = ExpandPath(action.WorkingDirectory ?? "", context);

                    if (!File.Exists(expandedPath))
                    {
                        if (action.Required)
                        {
                            errors.Add($"Required action not found: {expandedPath}");
                            continue;
                        }
                        continue;
                    }

                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = expandedPath,
                            Arguments = expandedArgs,
                            WorkingDirectory = string.IsNullOrEmpty(expandedWorkDir)
                                ? Path.GetDirectoryName(expandedPath) ?? ""
                                : expandedWorkDir,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    if (!process.WaitForExit(action.TimeoutMs > 0 ? action.TimeoutMs : 300_000))
                    {
                        process.Kill();
                        errors.Add($"Action timed out: {action.Description}");
                        continue;
                    }

                    if (process.ExitCode != 0 && action.FailOnError)
                    {
                        errors.Add($"Action failed (exit {process.ExitCode}): {action.Description}\n{error}");
                        continue;
                    }

                    context.Properties[$"ActionOutput_{action.Description}"] = output;
                    executed++;
                }
                catch (Exception ex)
                {
                    if (action.FailOnError)
                        errors.Add($"Action error: {action.Description} — {ex.Message}");
                }
            }

            return errors.Count > 0
                ? StepErrorHelpers.Fail($"{executed} actions ran, {errors.Count} failed: {string.Join("; ", errors)}")
                : StepErrorHelpers.Ok($"{executed} custom actions executed.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));

        private List<CustomAction> GetActions(SetupContext context)
        {
            var all = context.TryGetProperty<List<CustomAction>>("CustomActions");
            return all?.Where(a => a.Timing == _timing).ToList() ?? new List<CustomAction>();
        }

        private static string ExpandPath(string path, SetupContext context)
        {
            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            var expanded = path
                .Replace("{InstallPath}", installPath)
                .Replace("{ProductName}", config?.ProductName ?? "")
                .Replace("{Version}", config?.ProductVersion ?? "")
                .Replace("{Publisher}", config?.Publisher ?? "")
                .Replace("{Temp}", Path.GetTempPath())
                .Replace("{Windows}", Environment.GetFolderPath(Environment.SpecialFolder.Windows))
                .Replace("{System}", Environment.SystemDirectory)
                .Replace("{ProgramFiles}", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
                .Replace("{Desktop}", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));

            // {Custom:fieldId} — values collected from custom wizard pages (Track A1.3).
            if (context.Properties.TryGetValue("CustomValues", out var cv) && cv is Dictionary<string, string> custom)
            {
                foreach (var kv in custom)
                    expanded = expanded.Replace("{Custom:" + kv.Key + "}", kv.Value ?? "");
            }
            return expanded;
        }
    }

    public class CustomAction
    {
        public string Path { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";
        public string? Description { get; set; }
        public CustomActionTiming Timing { get; set; } = CustomActionTiming.AfterInstall;
        public int Order { get; set; }
        public bool Required { get; set; } = true;
        public bool FailOnError { get; set; } = true;
        public int TimeoutMs { get; set; }
    }

    public enum CustomActionTiming
    {
        BeforeInstall,
        AfterInstall,
        BeforeUninstall,
        AfterUninstall
    }
}
