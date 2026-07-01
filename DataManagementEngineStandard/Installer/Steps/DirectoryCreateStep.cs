using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>
    /// Creates the installation directory structure. Reads paths from context properties.
    /// </summary>
    public class DirectoryCreateStep : ISetupStep
    {
        public string StepId => "installer.directory.create";
        public string StepName => "Create directories";
        public string Description => "Creates the installation folder structure.";
        public IReadOnlyList<string> DependsOn { get; }

        public DirectoryCreateStep(string? dependsOn = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context) => false;

        public IErrorsInfo Validate(SetupContext context)
        {
            var path = context.TryGetProperty<string>("InstallPath");
            if (string.IsNullOrWhiteSpace(path))
                return StepErrorHelpers.Fail("InstallPath not set in context.");
            return StepErrorHelpers.Ok("Validated.");
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var path = context.TryGetProperty<string>("InstallPath");
            if (string.IsNullOrWhiteSpace(path))
                return StepErrorHelpers.Fail("InstallPath not set.");

            var dirs = new List<string> { path };
            // Add component-specific subdirectories
            if (context.TryGetProperty<List<string>>("ComponentDirs") is { } compDirs)
                dirs.AddRange(compDirs);

            foreach (var dir in dirs)
            {
                progress?.Report(new PassedArgs { Messege = $"Creating {dir}..." });
                Directory.CreateDirectory(dir);
            }

            // Track created dirs for uninstall manifest
            context.Properties["CreatedDirectories"] = dirs;
            return StepErrorHelpers.Ok($"{dirs.Count} directories created.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }
}
