using System;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheTechIdea.Beep.CLI.Commands;
using TheTechIdea.Beep.CLI.Infrastructure;

namespace TheTechIdea.Beep.CLI
{
    /// <summary>
    /// Main entry point for BeepDM CLI
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                // Build the root command with all subcommands
                var rootCommand = BuildRootCommand();
                
                // Parse and execute
                return await rootCommand.InvokeAsync(args);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static RootCommand BuildRootCommand()
        {
            var rootCommand = new RootCommand("BeepDM - Data Management Platform CLI")
            {
                // Add all command groups
                ProfileCommands.Build(),
                ConfigCommands.Build(),
                DriverCommands.Build(),
                DataSourceCommands.Build(),
                ETLCommands.Build(),
                MappingCommands.Build(),
                SyncCommands.Build(),
                ImportCommands.Build()
            };

            return rootCommand;
        }
    }
}
