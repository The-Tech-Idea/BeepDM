using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using TheTechIdea.Beep.CLI.Infrastructure;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools;

namespace TheTechIdea.Beep.CLI.Commands.Helpers
{
    /// <summary>
    /// Helper class for complex ClassCreator command operations
    /// </summary>
    public static class ClassCreatorCommandHelper
    {
        /// <summary>
        /// Generates POCO classes from datasource using entity names
        /// </summary>
        public static async Task GeneratePocoFromDataSource(
            string profile, 
            string datasourceName, 
            string className, 
            string[] entityNames, 
            string outputPath, 
            string namespaceString)
        {
            await AnsiConsole.Status()
                .StartAsync($"Generating POCO classes from datasource...", async ctx =>
                {
                    try
                    {
                        var services = new BeepServiceProvider(profile);
                        var editor = services.GetEditor();
                        var classCreator = new ClassCreator(editor);
                        
                        var ds = CliHelper.ValidateAndGetDataSource(editor, datasourceName);
                        if (ds == null) return;

                        var filePath = classCreator.CreatePOCOClass(
                            datasourceName,
                            className,
                            entityNames.ToList(),
                            "using System;\nusing System.Collections.Generic;",
                            "",
                            "",
                            outputPath,
                            namespaceString,
                            true
                        );

                        CliHelper.DisplaySuccess($"Generated POCO classes: {filePath}");
                        AnsiConsole.MarkupLine($"  [cyan]Class:[/] {className}");
                        AnsiConsole.MarkupLine($"  [cyan]Entities:[/] {string.Join(", ", entityNames)}");
                    }
                    catch (Exception ex)
                    {
                        CliHelper.DisplayError($"Error: {ex.Message}");
                    }
                });
        }

        /// <summary>
        /// Generates INotifyPropertyChanged classes from datasource using entity names
        /// </summary>
        public static async Task GenerateINotifyFromDataSource(
            string profile, 
            string datasourceName, 
            string[] entityNames, 
            string outputPath, 
            string namespaceString)
        {
            await AnsiConsole.Status()
                .StartAsync($"Generating INotifyPropertyChanged classes from datasource...", async ctx =>
                {
                    try
                    {
                        var services = new BeepServiceProvider(profile);
                        var editor = services.GetEditor();
                        var classCreator = new ClassCreator(editor);
                        
                        var ds = CliHelper.ValidateAndGetDataSource(editor, datasourceName);
                        if (ds == null) return;

                        var filePath = classCreator.CreateINotifyClass(
                            datasourceName,
                            entityNames.ToList(),
                            "using System;\nusing System.ComponentModel;",
                            "",
                            "",
                            outputPath,
                            namespaceString,
                            true
                        );

                        CliHelper.DisplaySuccess($"Generated INotifyPropertyChanged classes: {filePath}");
                        AnsiConsole.MarkupLine($"  [cyan]Entities:[/] {string.Join(", ", entityNames)}");
                    }
                    catch (Exception ex)
                    {
                        CliHelper.DisplayError($"Error: {ex.Message}");
                    }
                });
        }

        /// <summary>
        /// Generates Entity classes from datasource using entity names
        /// </summary>
        public static async Task GenerateEntityFromDataSource(
            string profile, 
            string datasourceName, 
            string[] entityNames, 
            string outputPath, 
            string namespaceString)
        {
            await AnsiConsole.Status()
                .StartAsync($"Generating entity classes from datasource...", async ctx =>
                {
                    try
                    {
                        var services = new BeepServiceProvider(profile);
                        var editor = services.GetEditor();
                        var classCreator = new ClassCreator(editor);
                        
                        var ds = CliHelper.ValidateAndGetDataSource(editor, datasourceName);
                        if (ds == null) return;

                        var filePath = classCreator.CreateEntityClass(
                            datasourceName,
                            entityNames.ToList(),
                            "using System;",
                            "",
                            outputPath,
                            namespaceString,
                            true
                        );

                        CliHelper.DisplaySuccess($"Generated entity classes: {filePath}");
                        AnsiConsole.MarkupLine($"  [cyan]Entities:[/] {string.Join(", ", entityNames)}");
                    }
                    catch (Exception ex)
                    {
                        CliHelper.DisplayError($"Error: {ex.Message}");
                    }
                });
        }
    }
}
