using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// File-based <see cref="IFirstRunDetector"/> that records setup completion in a marker
    /// file under the configuration directory.
    /// </summary>
    public class FileBasedFirstRunDetector : IFirstRunDetector
    {
        private readonly IDMEEditor _editor;
        private readonly string _markerFileName;
        private readonly ILogger<FileBasedFirstRunDetector>? _logger;
        private string? _markerFilePath;

        public FileBasedFirstRunDetector(IDMEEditor editor, string? markerFileName = null,
            ILogger<FileBasedFirstRunDetector>? logger = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _markerFileName = string.IsNullOrWhiteSpace(markerFileName) ? ".setup_complete" : markerFileName.Trim();
            _logger = logger;
        }

        public bool WasSetupCompleted => _markerFilePath != null && File.Exists(_markerFilePath);

        private string GetMarkerFilePath()
        {
            if (_markerFilePath != null)
                return _markerFilePath;

            var configPath = _editor.ConfigEditor?.ConfigPath;
            if (string.IsNullOrWhiteSpace(configPath))
            {
                configPath = Path.Combine(AppContext.BaseDirectory, "Config");
            }

            try
            {
                Directory.CreateDirectory(configPath);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not create config directory '{Path}'", configPath);
                System.Diagnostics.Debug.WriteLine($"[FileBasedFirstRunDetector] Could not create config directory '{configPath}': {ex.Message}");
                configPath = AppContext.BaseDirectory;
            }

            _markerFilePath = Path.Combine(configPath, _markerFileName);
            return _markerFilePath;
        }

        public Task<bool> IsFirstRunAsync()
        {
            try
            {
                var path = GetMarkerFilePath();
                return Task.FromResult(!File.Exists(path));
            }
            catch
            {
                return Task.FromResult(true);
            }
        }

        public Task MarkSetupCompleteAsync()
        {
            try
            {
                var path = GetMarkerFilePath();
                File.WriteAllText(path, DateTimeOffset.UtcNow.ToString("O"));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not mark setup complete");
                System.Diagnostics.Debug.WriteLine($"[FileBasedFirstRunDetector] Could not mark setup complete: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task ClearSetupFlagAsync()
        {
            try
            {
                var path = GetMarkerFilePath();
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not clear setup flag");
                System.Diagnostics.Debug.WriteLine($"[FileBasedFirstRunDetector] Could not clear setup flag: {ex.Message}");
            }
            return Task.CompletedTask;
        }
    }
}
