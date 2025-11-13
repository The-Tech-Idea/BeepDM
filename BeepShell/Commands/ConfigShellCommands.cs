using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace BeepShell.Commands
{
    /// <summary>
    /// Configuration management commands for BeepShell - Core functionality
    /// Modular architecture using partial classes for better maintainability
    /// Leverages ConfigEditor specialized managers for proper folder structure
    /// </summary>
    public partial class ConfigShellCommands : IShellCommand
    {
        private IDMEEditor _editor = null!;

        public string CommandName => "config";
        public string Description => "Manage BeepDM configuration and profiles";
        public string Category => "Configuration";
        public string Version => "2.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "cfg", "configuration" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var configCommand = new Command("config", Description);

            // Add profile management commands
            AddProfileCommands(configCommand);
            
            // Add path management commands
            AddPathCommands(configCommand);
            
            // Add connection management commands
            AddConnectionCommands(configCommand);
            
            // Add driver management commands
            AddDriverCommands(configCommand);
            
            // Add general configuration commands
            AddGeneralCommands(configCommand);

            return configCommand;
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                // General
                "config show",
                "config save",
                "config reload",
                "config reset --confirm",
                "config export ./backup/config.json",
                "config import ./backup/config.json",
                
                // Profiles
                "config profile list",
                "config profile create production",
                "config profile create dev --template production",
                "config profile switch production",
                "config profile delete dev",
                
                // Paths
                "config path show",
                "config path create",
                
                // Connections
                "config connection list",
                "config connection add --name mydb --driver SqlServer --host localhost --database mydb --user sa --password pass",
                "config connection remove mydb",
                "config connection test mydb",
                
                // Drivers
                "config driver list",
                "config driver info SqlServer"
            };
        }
    }
}
