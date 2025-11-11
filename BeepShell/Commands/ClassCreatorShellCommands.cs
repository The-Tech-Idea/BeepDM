using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using BeepShell.Infrastructure;
using Spectre.Console;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Commands
{
    /// <summary>
    /// Class creator commands for BeepShell
    /// Uses persistent DMEEditor for code generation
    /// </summary>
    public class ClassCreatorShellCommands : IShellCommand
    {
        private IDMEEditor _editor;

        public string CommandName => "class";
        public string Description => "Generate C# classes from database entities";
        public string Category => "Tools";
        public string Version => "1.0.0";
        public string Author => "BeepDM Team";
        public string[] Aliases => new[] { "codegen", "generate" };

        public void Initialize(IDMEEditor editor)
        {
            _editor = editor;
        }

        public Command BuildCommand()
        {
            var classCommand = new Command("class", Description);

            // class generate
            var generateCommand = new Command("generate", "Generate C# class from table");
            var dsArg = new Argument<string>("datasource", "Data source name");
            var tableArg = new Argument<string>("table", "Table name");
            var outputOption = new Option<string>("--output", "Output file path");
            var namespaceOption = new Option<string>("--namespace", () => "Generated", "Namespace for generated class");
            var publicOption = new Option<bool>("--public", () => true, "Generate public class");

            generateCommand.AddArgument(dsArg);
            generateCommand.AddArgument(tableArg);
            generateCommand.AddOption(outputOption);
            generateCommand.AddOption(namespaceOption);
            generateCommand.AddOption(publicOption);

            generateCommand.SetHandler((datasource, table, output, ns, isPublic) =>
            {
                GenerateClass(datasource, table, output, ns, isPublic);
            }, dsArg, tableArg, outputOption, namespaceOption, publicOption);

            classCommand.AddCommand(generateCommand);

            // class batch
            var batchCommand = new Command("batch", "Generate classes for all tables in a data source");
            var batchDsArg = new Argument<string>("datasource", "Data source name");
            var batchOutputOption = new Option<string>("--output", "Output directory path") { IsRequired = true };
            var batchNsOption = new Option<string>("--namespace", () => "Generated", "Namespace for generated classes");

            batchCommand.AddArgument(batchDsArg);
            batchCommand.AddOption(batchOutputOption);
            batchCommand.AddOption(batchNsOption);

            batchCommand.SetHandler((datasource, output, ns) =>
            {
                GenerateBatch(datasource, output, ns);
            }, batchDsArg, batchOutputOption, batchNsOption);

            classCommand.AddCommand(batchCommand);

            return classCommand;
        }

        private void GenerateClass(string datasourceName, string tableName, string outputPath, string namespaceName, bool isPublic)
        {
            try
            {
                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                    return;
                }

                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(datasourceName);
                }

                var structure = ds.GetEntityStructure(tableName, false);
                if (structure == null || structure.Fields == null || structure.Fields.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]Could not retrieve structure for '{tableName}'[/]");
                    return;
                }

                var className = SanitizeClassName(tableName);
                var code = GenerateClassCode(className, structure, namespaceName, isPublic);

                if (string.IsNullOrEmpty(outputPath))
                {
                    // Display to console
                    AnsiConsole.WriteLine();
                    var panel = new Panel(code);
                    panel.Header = new PanelHeader($"[green]{className}.cs[/]");
                    panel.Border = BoxBorder.Rounded;
                    AnsiConsole.Write(panel);
                }
                else
                {
                    // Write to file
                    File.WriteAllText(outputPath, code);
                    AnsiConsole.MarkupLine($"[green]✓[/] Class generated: {outputPath}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private void GenerateBatch(string datasourceName, string outputDir, string namespaceName)
        {
            try
            {
                var ds = _editor.GetDataSource(datasourceName);
                if (ds == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Data source '{datasourceName}' not found");
                    return;
                }

                if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
                {
                    _editor.OpenDataSource(datasourceName);
                }

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                    AnsiConsole.MarkupLine($"[cyan]Created directory: {outputDir}[/]");
                }

                var entities = ds.GetEntitesList()?.ToList();
                if (entities == null || entities.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No entities found[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"[cyan]Generating classes for {entities.Count} entities...[/]");

                var progress = AnsiConsole.Progress();
                progress.Start(ctx =>
                {
                    var task = ctx.AddTask("[cyan]Generating classes[/]", maxValue: entities.Count);

                    foreach (var entityName in entities)
                    {
                        try
                        {
                            task.Description = $"[cyan]Generating {entityName}[/]";

                            var structure = ds.GetEntityStructure(entityName, false);
                            if (structure?.Fields != null && structure.Fields.Count > 0)
                            {
                                var className = SanitizeClassName(entityName);
                                var code = GenerateClassCode(className, structure, namespaceName, true);
                                var filePath = Path.Combine(outputDir, $"{className}.cs");
                                
                                File.WriteAllText(filePath, code);
                            }

                            task.Increment(1);
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Error generating {entityName}: {ex.Message}");
                        }
                    }
                });

                AnsiConsole.MarkupLine($"[green]✓[/] Generated classes in: {outputDir}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Error: {ex.Message}");
            }
        }

        private string GenerateClassCode(string className, EntityStructure structure, string namespaceName, bool isPublic)
        {
            var sb = new StringBuilder();
            var accessibility = isPublic ? "public" : "internal";

            // Using statements
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine();

            // Namespace
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");

            // Class declaration
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Entity class for {structure.EntityName}");
            sb.AppendLine($"    /// Generated from {structure.DataSourceID ?? "database"}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    [Table(\"{structure.EntityName}\")]");
            sb.AppendLine($"    {accessibility} class {className}");
            sb.AppendLine("    {");

            // Properties
            foreach (var field in structure.Fields)
            {
                var propertyName = SanitizePropertyName(field.fieldname);
                var propertyType = MapDataType(field.fieldtype ?? "string", field.AllowDBNull);

                // Add attributes
                if (field.IsKey)
                {
                    sb.AppendLine("        [Key]");
                }
                if (field.IsAutoIncrement)
                {
                    sb.AppendLine("        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
                }
                if (!field.AllowDBNull && !IsNullableType(propertyType))
                {
                    sb.AppendLine("        [Required]");
                }
                if (field.Size1 > 0 && (field.fieldtype?.Contains("char", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    sb.AppendLine($"        [MaxLength({field.Size1})]");
                }

                sb.AppendLine($"        [Column(\"{field.fieldname}\")]");
                sb.AppendLine($"        public {propertyType} {propertyName} {{ get; set; }}");
                sb.AppendLine();
            }

            // Close class and namespace
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string SanitizeClassName(string name)
        {
            // Remove invalid characters and ensure valid C# identifier
            var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            if (char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;
            return sanitized;
        }

        private string SanitizePropertyName(string name)
        {
            return SanitizeClassName(name);
        }

        private string MapDataType(string dbType, bool allowNull)
        {
            if (string.IsNullOrEmpty(dbType))
                return "object";

            var type = dbType.ToLowerInvariant();
            var nullable = allowNull ? "?" : "";

            if (type.Contains("int"))
                return "int" + nullable;
            if (type.Contains("bigint"))
                return "long" + nullable;
            if (type.Contains("smallint"))
                return "short" + nullable;
            if (type.Contains("tinyint"))
                return "byte" + nullable;
            if (type.Contains("decimal") || type.Contains("numeric") || type.Contains("money"))
                return "decimal" + nullable;
            if (type.Contains("float") || type.Contains("real"))
                return "double" + nullable;
            if (type.Contains("bit") || type.Contains("bool"))
                return "bool" + nullable;
            if (type.Contains("date") || type.Contains("time"))
                return "DateTime" + nullable;
            if (type.Contains("guid") || type.Contains("uniqueidentifier"))
                return "Guid" + nullable;
            if (type.Contains("char") || type.Contains("text") || type.Contains("varchar"))
                return "string";
            if (type.Contains("binary") || type.Contains("image") || type.Contains("blob"))
                return "byte[]";

            return "object";
        }

        private bool IsNullableType(string type)
        {
            return type.EndsWith("?") || type == "string" || type == "byte[]" || type == "object";
        }

        public bool CanExecute() => _editor != null;

        public string[] GetExamples()
        {
            return new[]
            {
                "class generate mydb Users",
                "class generate mydb Products --output Product.cs --namespace MyApp.Models",
                "class batch mydb --output ./Models --namespace MyApp.Data"
            };
        }
    }
}
