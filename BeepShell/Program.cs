using System;
using System.IO;
using TheTechIdea.Beep.Shell.Infrastructure;
using BeepShell.Commands;
using Spectre.Console;

namespace TheTechIdea.Beep.Shell
{
    /// <summary>
    /// BeepShell - Interactive REPL for BeepDM
    /// Maintains persistent state and connections across commands
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                // Execute any pending driver cleanup BEFORE loading any assemblies
                var appPath = AppDomain.CurrentDomain.BaseDirectory;
                DriverShellCommands.ExecutePendingCleanup(appPath);

                // Display welcome banner
                DisplayWelcomeBanner();

                // Determine profile (from args or default)
                string profile = GetProfileFromArgs(args);

                // Create and run the interactive shell
                var shell = new InteractiveShell(profile);
                return shell.Run();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }

        private static void DisplayWelcomeBanner()
        {
            var banner = new FigletText("BeepShell")
                .Centered()
                .Color(Color.Cyan1);

            AnsiConsole.Write(banner);
            
            AnsiConsole.MarkupLine("[dim]BeepDM Interactive Shell - Persistent Connections & State[/]");
            AnsiConsole.MarkupLine("[dim]Version 1.0.0 | Type 'help' for commands | Type 'exit' to quit[/]");
            AnsiConsole.WriteLine();
        }

        private static string GetProfileFromArgs(string[] args)
        {
            // Check for --profile argument
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--profile" || args[i] == "-p")
                {
                    return args[i + 1];
                }
            }

            // Check environment variable
            var envProfile = Environment.GetEnvironmentVariable("BEEP_PROFILE");
            if (!string.IsNullOrEmpty(envProfile))
            {
                return envProfile;
            }

            return "default";
        }
    }
}
