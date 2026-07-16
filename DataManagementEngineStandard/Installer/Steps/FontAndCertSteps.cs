using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.SetUp;

namespace TheTechIdea.Beep.Installer.Steps
{
    /// <summary>Installs/uninstalls fonts to the Windows Fonts directory.</summary>
    public class FontInstallStep : ISetupStep
    {
        private readonly bool _isUninstall;

        public string StepId => _isUninstall ? "installer.fonts.remove" : "installer.fonts.install";
        public string StepName => _isUninstall ? "Remove fonts" : "Install fonts";
        public string Description => _isUninstall ? "Removes installed fonts." : "Installs fonts to Windows.";
        public IReadOnlyList<string> DependsOn { get; }

        public FontInstallStep(bool isUninstall = false, string? dependsOn = null)
        {
            _isUninstall = isUninstall;
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
        {
            var fonts = context.TryGetProperty<List<string>>("FontFiles");
            return fonts == null || fonts.Count == 0;
        }

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var fontFiles = context.TryGetProperty<List<string>>("FontFiles");
            if (fontFiles == null || fontFiles.Count == 0)
                return StepErrorHelpers.Ok("No fonts configured.");

            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            var fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            int processed = 0;

            foreach (var fontRelPath in fontFiles)
            {
                var fontPath = Path.Combine(installPath, fontRelPath);
                if (!File.Exists(fontPath)) continue;

                var destPath = Path.Combine(fontsDir, Path.GetFileName(fontPath));
                progress?.Report(new PassedArgs { Messege = $"{(_isUninstall ? "Removing" : "Installing")} font: {Path.GetFileName(fontPath)}" });

                if (_isUninstall)
                {
                    // Best-effort removal: a locked/missing font file must not abort the loop — report and continue.
                    try { File.Delete(destPath); processed++; }
                    catch (Exception ex) { progress?.Report(new PassedArgs { Messege = $"FontInstallStep: failed to remove font '{destPath}': {ex.Message}" }); }
                    RemoveFontResource(destPath);
                }
                else
                {
                    File.Copy(fontPath, destPath, overwrite: true);
                    AddFontResource(destPath);
                    processed++;
                }
            }

            if (processed > 0) BroadcastFontChange();
            return StepErrorHelpers.Ok($"{processed} fonts {(_isUninstall ? "removed" : "installed")}.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));

        [DllImport("gdi32.dll")] private static extern int AddFontResource(string lpszFilename);
        [DllImport("gdi32.dll")] private static extern int RemoveFontResource(string lpszFilename);
        private static void BroadcastFontChange() => SendMessage(0xFFFF, 0x001D, IntPtr.Zero, IntPtr.Zero);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }

    /// <summary>Installs/uninstalls certificates to the Windows certificate store.</summary>
    public class CertificateInstallStep : ISetupStep
    {
        private readonly bool _isUninstall;

        public string StepId => _isUninstall ? "installer.certs.remove" : "installer.certs.install";
        public string StepName => _isUninstall ? "Remove certificates" : "Install certificates";
        public string Description => _isUninstall ? "Removes certificates from Windows store." : "Installs certificates to Windows store.";
        public IReadOnlyList<string> DependsOn { get; }

        public CertificateInstallStep(bool isUninstall = false, string? dependsOn = null)
        {
            _isUninstall = isUninstall;
            DependsOn = dependsOn != null ? new List<string> { dependsOn } : Array.Empty<string>();
        }

        public bool CanSkip(SetupContext context)
            => context.TryGetProperty<List<CertificateDef>>("Certificates")?.Count == 0;

        public IErrorsInfo Validate(SetupContext context) => StepErrorHelpers.Ok("Validated.");

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs>? progress = null)
        {
            var certs = context.TryGetProperty<List<CertificateDef>>("Certificates");
            if (certs == null || certs.Count == 0) return StepErrorHelpers.Ok("No certificates.");

            var installPath = context.TryGetProperty<string>("InstallPath") ?? "";
            int processed = 0;

            foreach (var cert in certs)
            {
                var certPath = Path.Combine(installPath, cert.Path);
                if (!File.Exists(certPath)) continue;

                progress?.Report(new PassedArgs { Messege = $"{(_isUninstall ? "Removing" : "Installing")} certificate: {cert.Name}" });

                var storeArg = cert.StoreLocation == CertStoreLocation.CurrentUser ? "CurrentUser" : "LocalMachine";
                var args = _isUninstall
                    ? $"certutil -delstore -{storeArg} \"{cert.StoreName}\" \"{cert.Thumbprint}\""
                    : $"certutil -addstore -{storeArg} \"{cert.StoreName}\" \"{certPath}\"";

                var p = Process.Start(new ProcessStartInfo("certutil", args)
                {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true
                });
                p?.WaitForExit(30000);
                if (p?.ExitCode == 0) processed++;
            }

            return StepErrorHelpers.Ok($"{processed} certificates {(_isUninstall ? "removed" : "installed")}.");
        }

        public Task<IErrorsInfo> ExecuteAsync(SetupContext context, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => Task.FromResult(Execute(context, progress));
    }

    public class CertificateDef
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";       // Relative to install path
        public string StoreName { get; set; } = "Root";
        public CertStoreLocation StoreLocation { get; set; } = CertStoreLocation.LocalMachine;
        public string Thumbprint { get; set; } = "";  // For uninstall
    }

    public enum CertStoreLocation { CurrentUser, LocalMachine }
}
