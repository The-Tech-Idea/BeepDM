using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>Registers/unregisters file extension associations.</summary>
    public class FileAssociationStep : ISetupStep
    {
        public string StepId => "installer.fileassoc";
        public string StepName => "Register file associations";
        public string Description => "Associates file extensions with the application.";
        public IReadOnlyList<string> DependsOn { get; }

        private readonly bool _isUninstall;

        public FileAssociationStep(bool isUninstall = false, string? dependsOn = null)
        {
            _isUninstall = isUninstall;
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
        {
            return context.TryGetProperty<List<FileAssociation>>("FileAssociations")?.Count == 0;
        }

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var associations = context.TryGetProperty<List<FileAssociation>>("FileAssociations");
            if (associations == null || associations.Count == 0)
                return StepErrorHelpers.Ok("No file associations configured.");

            var installPath = context.TryGetProperty<string>("InstallPath")!;
            int count = 0;

            if (context.Options?.DryRun == true)
                return StepErrorHelpers.Ok($"Dry run: {associations.Count} file association(s) would be " +
                                           $"{(_isUninstall ? "removed" : "registered")}. Nothing was changed.");

            // Associations must land in the same hive the install targets, otherwise a
            // per-machine install registers types only the installing user can see.
            var perUser = InstallScope.IsPerUser(context);

            foreach (var assoc in associations)
            {
                progress?.Report(new PassedArgs { Messege = $"{(_isUninstall ? "Removing" : "Registering")}: {assoc.Extension}" });

                if (_isUninstall)
                {
                    InstallHelpers.UnregisterFileAssociation(assoc.Extension, assoc.ProgId, perUser);
                }
                else
                {
                    var exePath = Path.Combine(installPath, assoc.TargetPath);
                    var iconPath = string.IsNullOrEmpty(assoc.IconPath)
                        ? exePath
                        : Path.Combine(installPath, assoc.IconPath);
                    InstallHelpers.RegisterFileAssociation(
                        assoc.Extension, assoc.ProgId, assoc.Description,
                        iconPath, $"\"{exePath}\" \"%1\"", perUser);
                }
                count++;
            }

            return StepErrorHelpers.Ok($"{(_isUninstall ? "Removed" : "Registered")} {count} file associations.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }

    public class FileAssociation
    {
        public string Extension { get; set; } = "";     // e.g., ".beep"
        public string ProgId { get; set; } = "";        // e.g., "Beep.Project"
        public string Description { get; set; } = "";   // e.g., "Beep Project File"
        public string TargetPath { get; set; } = "";    // Relative to install path
        public string IconPath { get; set; } = "";       // Relative to install path
    }
}
