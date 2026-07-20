using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>Creates Windows shortcuts (.lnk files) for desktop, start menu, and startup.</summary>
    public class ShortcutCreateStep : ISetupStep
    {
        public string StepId => "installer.shortcuts.create";
        public string StepName => "Create shortcuts";
        public string Description => "Creates application shortcuts on the desktop and start menu.";
        public IReadOnlyList<string> DependsOn { get; }

        public ShortcutCreateStep(string? dependsOn = null)
        {
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context) => false;

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context.TryGetProperty<InstallConfig>("InstallConfig") == null)
                return StepErrorHelpers.Fail("InstallConfig not found.");
            if (string.IsNullOrWhiteSpace(context.TryGetProperty<string>("InstallPath")))
                return StepErrorHelpers.Fail("InstallPath not set.");
            return StepErrorHelpers.Ok("Validated.");
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var config = context.TryGetProperty<InstallConfig>("InstallConfig")!;
            var installPath = context.TryGetProperty<string>("InstallPath")!;

            var allShortcuts = new List<ShortcutDefinition>(config.Shortcuts ?? Enumerable.Empty<ShortcutDefinition>());
            foreach (var comp in config.Components.Where(c => c.Selected || c.Required))
                allShortcuts.AddRange(comp.Shortcuts ?? Enumerable.Empty<ShortcutDefinition>());

            if (allShortcuts.Count == 0)
                return StepErrorHelpers.Ok("No shortcuts configured.");

            if (context.Options?.DryRun == true)
                return StepErrorHelpers.Ok($"Dry run: {allShortcuts.Count} shortcut(s) would be created. Nothing was written.");

            var created = new List<ShortcutDefinition>();
            foreach (var sc in allShortcuts)
            {
                var targetPath = Path.Combine(installPath, sc.TargetPath);
                if (!File.Exists(targetPath)) continue;

                var linkPath = GetShortcutPath(sc, config, InstallScope.IsPerUser(context));
                if (string.IsNullOrEmpty(linkPath)) continue;

                var dir = Path.GetDirectoryName(linkPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                CreateShortcut(linkPath, targetPath, sc.Arguments, sc.WorkingDirectory, sc.IconPath);
                created.Add(sc);
                progress?.Report(new PassedArgs { Messege = $"Created: {sc.Name}" });
            }

            context.Properties["ShortcutsCreated"] = created;
            return StepErrorHelpers.Ok($"{created.Count} shortcuts created.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));

        // ── Path helpers ──────────────────────────────────────────────

        private static string GetShortcutPath(ShortcutDefinition sc, InstallConfig config, bool perUser)
            => ShortcutPathResolver.Resolve(sc, config, perUser);

        // ── COM-based .lnk creator ────────────────────────────────────

        private static void CreateShortcut(string linkPath, string targetPath, string args, string workDir, string iconPath)
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(linkPath);
            shortcut.TargetPath = targetPath;
            if (!string.IsNullOrEmpty(args)) shortcut.Arguments = args;
            if (!string.IsNullOrEmpty(workDir)) shortcut.WorkingDirectory = workDir;
            if (!string.IsNullOrEmpty(iconPath)) shortcut.IconLocation = iconPath;
            shortcut.Save();
            Marshal.FinalReleaseComObject(shortcut);
            Marshal.FinalReleaseComObject(shell);
        }
    }
}
