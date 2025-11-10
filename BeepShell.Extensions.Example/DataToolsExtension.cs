using BeepShell.Infrastructure;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Extensions.Example
{
    /// <summary>
    /// Example extension provider that packages multiple commands and workflows together
    /// Demonstrates lifecycle hooks, configuration, and event handling
    /// </summary>
    [ShellExtension(
        Name = "Data Tools Extension",
        Version = "1.0.0",
        Author = "BeepDM Community",
        Description = "Example extension demonstrating BeepShell extensibility features",
        ConfigFileName = "datatools.config.json"
    )]
    public class DataToolsExtension : IShellExtension
    {
        private IDMEEditor _editor;
        private readonly List<IShellCommand> _commands = new();
        private readonly List<IShellWorkflow> _workflows = new();
        private IExtensionConfig _config;

        public string ExtensionName => "Data Tools Extension";
        public string Version => "1.0.0";
        public string Author => "BeepDM Community";
        public string Description => "Example extension with export, import, and sync capabilities";
        public string[] Dependencies => Array.Empty<string>();

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;

            // Load configuration
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TheTechIdea",
                "BeepShell",
                "Extensions",
                "datatools.config.json"
            );
            _config = new ExtensionConfig(configPath);
            
            try
            {
                _config.Load();
            }
            catch
            {
                // Use defaults if config doesn't exist
                _config.SetValue("maxBatchSize", 1000);
                _config.SetValue("defaultFormat", "csv");
                _config.Save();
            }

            // Register commands
            var exportCmd = new ExportCommand();
            exportCmd.Initialize(editor);
            _commands.Add(exportCmd);

            var importCmd = new ImportCommand();
            importCmd.Initialize(editor);
            _commands.Add(importCmd);

            // Register workflows
            var syncWorkflow = new DataSyncWorkflow();
            syncWorkflow.Initialize(editor);
            _workflows.Add(syncWorkflow);

            _editor.Logger?.WriteLog("Data Tools Extension initialized");
        }

        public void OnLoad()
        {
            // Called after successful initialization
            _editor.Logger?.WriteLog("Data Tools Extension loaded successfully");
            
            // Set default config values if not present
            if (!_config.HasKey("enabled"))
            {
                _config.SetValue("enabled", true);
                _config.Save();
            }
        }

        public void OnUnload()
        {
            // Called when extension is being unloaded
            _editor.Logger?.WriteLog("Data Tools Extension unloading...");
            
            // Save any pending changes
            _config.Save();
        }

        public void OnConfigurationChanged()
        {
            // Called when shell configuration changes
            _editor.Logger?.WriteLog("Configuration changed - reloading extension settings");
            
            // Reload extension configuration
            _config.Load();
        }

        public IEnumerable<IShellCommand> GetCommands()
        {
            return _commands;
        }

        public IEnumerable<IShellWorkflow> GetWorkflows()
        {
            return _workflows;
        }

        public void Cleanup()
        {
            // Cleanup resources if needed
            _config?.Save();
            _commands.Clear();
            _workflows.Clear();
            
            _editor.Logger?.WriteLog("Data Tools Extension cleaned up");
        }

        public IExtensionConfig GetConfig()
        {
            return _config;
        }
    }
}
