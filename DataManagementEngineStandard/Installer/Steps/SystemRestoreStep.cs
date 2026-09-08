using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>Creates a Windows System Restore point before installation begins.</summary>
    public class SystemRestoreStep : ISetupStep
    {
        public string StepId => "installer.restorepoint";
        public string StepName => "Create restore point";
        public string Description => "Creates a system restore point before installation.";
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();

        /// <summary>
        /// Skipped unless the project asked for a restore point. It used to return false
        /// unconditionally, which is one reason this step was never wired into a graph:
        /// adding it would have taken a restore point on every install regardless.
        /// </summary>
        public bool CanSkip(SetupContext context)
            => context.TryGetProperty<InstallConfig>("InstallConfig")?.CreateRestorePoint != true;

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig");
            var desc = $"Beep Installer — {config?.ProductName} v{config?.ProductVersion}";
            var success = InstallHelpers.CreateSystemRestorePoint(desc);
            progress?.Report(new PassedArgs { Messege = success ? "Restore point created." : "Restore point skipped (may require admin)." });
            return success ? StepErrorHelpers.Ok("Restore point created.") : StepErrorHelpers.Ok("Restore point skipped.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }
}
