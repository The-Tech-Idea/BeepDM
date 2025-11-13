using System;
using System.CommandLine;
using Spectre.Console;

namespace BeepShell.Commands
{
    /// <summary>
    /// Testing and validation generation commands for ClassCreatorShellCommands
    /// </summary>
    public partial class ClassCreatorShellCommands
    {
        /// <summary>
        /// Builds testing-related generation commands
        /// </summary>
        private void AddTestingCommands(Command classCommand)
        {
            // class test - Generate unit test class
            var testCommand = new Command("test", "Generate unit test class for entity");
            var testDsArg = new Argument<string>("datasource", "Data source name");
            var testTableArg = new Argument<string>("table", "Table name");
            var testOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };

            testCommand.AddArgument(testDsArg);
            testCommand.AddArgument(testTableArg);
            testCommand.AddOption(testOutputOption);

            testCommand.SetHandler((datasource, table, output) =>
            {
                GenerateUnitTest(datasource, table, output);
            }, testDsArg, testTableArg, testOutputOption);

            classCommand.AddCommand(testCommand);

            // class validator - Generate FluentValidation validators
            var validatorCommand = new Command("validator", "Generate FluentValidation validator for entity");
            var valDsArg = new Argument<string>("datasource", "Data source name");
            var valTableArg = new Argument<string>("table", "Table name");
            var valOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };
            var valNsOption = new Option<string>("--namespace", () => "TheTechIdea.ProjectValidators", "Namespace");

            validatorCommand.AddArgument(valDsArg);
            validatorCommand.AddArgument(valTableArg);
            validatorCommand.AddOption(valOutputOption);
            validatorCommand.AddOption(valNsOption);

            validatorCommand.SetHandler((datasource, table, output, ns) =>
            {
                GenerateValidator(datasource, table, output, ns);
            }, valDsArg, valTableArg, valOutputOption, valNsOption);

            classCommand.AddCommand(validatorCommand);
        }

        /// <summary>
        /// Generates unit test class
        /// </summary>
        private void GenerateUnitTest(string datasourceName, string tableName, string outputDir)
        {
            try
            {
                var structure = GetEntityStructure(datasourceName, tableName);
                if (structure == null) return;

                AnsiConsole.Status()
                    .Start($"Generating unit test for {tableName}...", ctx =>
                    {
                        var result = _classCreator.GenerateUnitTestClass(structure, outputDir);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Unit test class generated: {result}");
                            AnsiConsole.MarkupLine("[dim]Test framework: xUnit with Moq[/]");
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates FluentValidation validator
        /// </summary>
        private void GenerateValidator(string datasourceName, string tableName, string outputDir, string namespaceName)
        {
            try
            {
                var structure = GetEntityStructure(datasourceName, tableName);
                if (structure == null) return;

                AnsiConsole.Status()
                    .Start($"Generating FluentValidation validator for {tableName}...", ctx =>
                    {
                        var result = _classCreator.GenerateFluentValidators(structure, outputDir, namespaceName);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Validator generated: {result}");
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
