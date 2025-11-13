using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using BeepShell.Shared.Models;

namespace BeepShell.Shared.Interfaces
{
    /// <summary>
    /// Interface for shell command extensions that can be discovered and loaded dynamically.
    /// Implement this interface to create custom BeepShell commands.
    /// </summary>
    public interface IShellCommand
    {
        /// <summary>
        /// The command name (e.g., "export", "import", "analyze")
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// Short description of what the command does
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Category for organizing commands (e.g., "Data", "Admin", "Analysis")
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Command version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Command author/provider
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Command aliases (alternative names for the command)
        /// </summary>
        string[] Aliases { get; }

        /// <summary>
        /// Initialize the command with the DMEEditor instance.
        /// Called when the shell starts and loads extensions.
        /// </summary>
        /// <param name="editor">The persistent DMEEditor instance</param>
        void Initialize(IDMEEditor editor);

        /// <summary>
        /// Build the System.CommandLine Command object with options and arguments.
        /// </summary>
        /// <returns>Configured Command object</returns>
        Command BuildCommand();

        /// <summary>
        /// Optional: Validate if command can execute in current state
        /// </summary>
        /// <returns>True if command can execute, false otherwise</returns>
        bool CanExecute();

        /// <summary>
        /// Optional: Get usage examples for help display
        /// </summary>
        /// <returns>Array of example command strings</returns>
        string[] GetExamples();

        /// <summary>
        /// Optional: Called before command execution for setup/validation
        /// </summary>
        /// <returns>True to continue execution, false to abort</returns>
        bool OnBeforeExecute() => true;

        /// <summary>
        /// Optional: Called after command execution for cleanup
        /// </summary>
        void OnAfterExecute() { }
    }

    /// <summary>
    /// Interface for workflow extensions that orchestrate multiple operations.
    /// Workflows can execute complex multi-step processes with progress tracking.
    /// </summary>
    public interface IShellWorkflow
    {
        /// <summary>
        /// Workflow identifier
        /// </summary>
        string WorkflowName { get; }

        /// <summary>
        /// Workflow description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Category (e.g., "ETL", "Migration", "Backup")
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Initialize workflow with DMEEditor
        /// </summary>
        void Initialize(IDMEEditor editor);

        /// <summary>
        /// Execute the workflow with parameters
        /// </summary>
        /// <param name="parameters">Workflow parameters</param>
        /// <returns>Execution result</returns>
        Task<WorkflowResult> ExecuteAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// Validate workflow can run with given parameters
        /// </summary>
        bool ValidateParameters(Dictionary<string, object> parameters);

        /// <summary>
        /// Get list of required parameters
        /// </summary>
        List<WorkflowParameter> GetRequiredParameters();
    }

    /// <summary>
    /// Marker interface for shell extension providers.
    /// Implement this to create a container for multiple commands/workflows.
    /// </summary>
    public interface IShellExtension
    {
        /// <summary>
        /// Extension name/identifier
        /// </summary>
        string ExtensionName { get; }

        /// <summary>
        /// Extension version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Extension author/publisher
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Extension description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Extension dependencies (other extension names required)
        /// </summary>
        string[] Dependencies { get; }

        /// <summary>
        /// Get all commands provided by this extension
        /// </summary>
        IEnumerable<IShellCommand> GetCommands();

        /// <summary>
        /// Get all workflows provided by this extension
        /// </summary>
        IEnumerable<IShellWorkflow> GetWorkflows();

        /// <summary>
        /// Initialize the extension
        /// </summary>
        void Initialize(IDMEEditor editor);

        /// <summary>
        /// Called after successful initialization - use for final setup
        /// </summary>
        void OnLoad() { }

        /// <summary>
        /// Called when extension is being unloaded
        /// </summary>
        void OnUnload() { }

        /// <summary>
        /// Called when shell configuration changes
        /// </summary>
        void OnConfigurationChanged() { }

        /// <summary>
        /// Called when shell is shutting down
        /// </summary>
        void Cleanup();

        /// <summary>
        /// Get extension configuration if available
        /// </summary>
        IExtensionConfig? GetConfig() => null;
    }

    /// <summary>
    /// Extension configuration interface
    /// </summary>
    public interface IExtensionConfig
    {
        /// <summary>
        /// Configuration file path
        /// </summary>
        string ConfigPath { get; }

        /// <summary>
        /// Load configuration from file
        /// </summary>
        void Load();

        /// <summary>
        /// Save configuration to file
        /// </summary>
        void Save();

        /// <summary>
        /// Get configuration value
        /// </summary>
        T? GetValue<T>(string key, T? defaultValue = default);

        /// <summary>
        /// Set configuration value
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// Check if configuration has a key
        /// </summary>
        bool HasKey(string key);
    }
}
