using System;
using System.CommandLine;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Spectre.Console;
using TheTechIdea.Beep.Addin;

namespace BeepShell.Commands
{
    /// <summary>
    /// DLL creation and compilation commands for ClassCreatorShellCommands
    /// </summary>
    public partial class ClassCreatorShellCommands
    {
        /// <summary>
        /// Builds DLL creation commands
        /// </summary>
        private void AddDllCommands(Command classCommand)
        {
            // class dll - Create DLL from entities
            var dllCommand = new Command("dll", "Create a DLL from entities");
            var dllNameArg = new Argument<string>("dllname", "Name of the DLL to create");
            var dllDsArg = new Argument<string>("datasource", "Data source name");
            var dllOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };
            var dllNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace");
            var dllAllOption = new Option<bool>("--all", () => false, "Include all tables");
            var dllTablesOption = new Option<string[]>("--tables", "Specific tables to include");
            var dllGenCsOption = new Option<bool>("--generate-cs", () => true, "Generate C# source files");

            dllCommand.AddArgument(dllNameArg);
            dllCommand.AddArgument(dllDsArg);
            dllCommand.AddOption(dllOutputOption);
            dllCommand.AddOption(dllNsOption);
            dllCommand.AddOption(dllAllOption);
            dllCommand.AddOption(dllTablesOption);
            dllCommand.AddOption(dllGenCsOption);

            dllCommand.SetHandler((dllName, datasource, output, ns, all, tables, generateCs) =>
            {
                CreateDll(dllName, datasource, output, ns, all, tables, generateCs);
            }, dllNameArg, dllDsArg, dllOutputOption, dllNsOption, dllAllOption, dllTablesOption, dllGenCsOption);

            classCommand.AddCommand(dllCommand);

            // class dll-from-path - Create DLL from C# files in directory
            var dllPathCommand = new Command("dll-from-path", "Create DLL from existing C# files");
            var dllPathNameArg = new Argument<string>("dllname", "Name of the DLL to create");
            var dllPathInputArg = new Argument<string>("inputpath", "Directory containing C# files");
            var dllPathOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };
            var dllPathNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectClasses", "Namespace");

            dllPathCommand.AddArgument(dllPathNameArg);
            dllPathCommand.AddArgument(dllPathInputArg);
            dllPathCommand.AddOption(dllPathOutputOption);
            dllPathCommand.AddOption(dllPathNsOption);

            dllPathCommand.SetHandler((dllName, inputPath, output, ns) =>
            {
                CreateDllFromPath(dllName, inputPath, output, ns);
            }, dllPathNameArg, dllPathInputArg, dllPathOutputOption, dllPathNsOption);

            classCommand.AddCommand(dllPathCommand);
        }

        /// <summary>
        /// Creates a DLL from entities
        /// </summary>
        private void CreateDll(string dllName, string datasourceName, string outputDir, string namespaceName, 
            bool generateAll, string[] tables, bool generateCsFiles)
        {
            try
            {
                var entities = GetEntityStructures(datasourceName, generateAll, tables);
                if (entities.Count == 0) return;

                AnsiConsole.MarkupLine($"[cyan]Creating DLL from {entities.Count} entities...[/]");

                var progress = new Progress<PassedArgs>(args =>
                {
                    if (!string.IsNullOrEmpty(args.ParameterString1))
                    {
                        var percentage = args.ParameterInt2 > 0 
                            ? (args.ParameterInt1 * 100 / args.ParameterInt2) 
                            : 0;
                        
                        if (args.EventType == "Progress")
                        {
                            AnsiConsole.MarkupLine($"[dim]→[/] {args.ParameterString1} ({percentage}%)");
                        }
                        else if (args.EventType == "Error")
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] {args.ParameterString1}");
                        }
                    }
                });

                using var cts = new CancellationTokenSource();
                
                AnsiConsole.Status()
                    .Start("Compiling DLL...", ctx =>
                    {
                        var result = _classCreator.CreateDLL(dllName, entities, outputDir, progress, 
                            cts.Token, namespaceName, generateCsFiles);
                        
                        if (!string.IsNullOrEmpty(result) && !result.Contains("Error") && !result.Contains("Failed"))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] DLL created successfully: {dllName}.dll");
                            AnsiConsole.MarkupLine($"[dim]Location: {outputDir}[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] DLL creation failed: {result}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a DLL from existing C# files in a directory
        /// </summary>
        private void CreateDllFromPath(string dllName, string inputPath, string outputDir, string namespaceName)
        {
            try
            {
                if (!System.IO.Directory.Exists(inputPath))
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Input directory not found: {inputPath}");
                    return;
                }

                var csFiles = System.IO.Directory.GetFiles(inputPath, "*.cs", System.IO.SearchOption.AllDirectories);
                AnsiConsole.MarkupLine($"[cyan]Found {csFiles.Length} C# files in {inputPath}[/]");

                if (csFiles.Length == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No C# files found to compile[/]");
                    return;
                }

                var progress = new Progress<PassedArgs>(args =>
                {
                    if (!string.IsNullOrEmpty(args.ParameterString1))
                    {
                        var percentage = args.ParameterInt2 > 0 
                            ? (args.ParameterInt1 * 100 / args.ParameterInt2) 
                            : 0;
                        
                        if (args.EventType == "Progress")
                        {
                            AnsiConsole.MarkupLine($"[dim]→[/] {args.ParameterString1} ({percentage}%)");
                        }
                        else if (args.EventType == "Error")
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] {args.ParameterString1}");
                        }
                    }
                });

                using var cts = new CancellationTokenSource();
                
                AnsiConsole.Status()
                    .Start("Compiling DLL from files...", ctx =>
                    {
                        var result = _classCreator.CreateDLLFromFilesPath(dllName, inputPath, outputDir, 
                            progress, cts.Token, namespaceName);
                        
                        if (!string.IsNullOrEmpty(result) && !result.Contains("Error") && !result.Contains("Failed"))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] DLL created successfully: {dllName}.dll");
                            AnsiConsole.MarkupLine($"[dim]Location: {outputDir}[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] DLL creation failed: {result}");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }
    }
}
