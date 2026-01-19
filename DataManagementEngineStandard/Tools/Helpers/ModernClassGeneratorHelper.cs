using System;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Tools.Helpers;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Tools.Helpers
{
    /// <summary>
    /// Helper class for generating modern C# class patterns
    /// </summary>
    public class ModernClassGeneratorHelper 
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;

        public ModernClassGeneratorHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        /// <summary>
        /// Creates a C# record class for immutable data models
        /// </summary>
        public string CreateRecordClass(string recordName, EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectClasses", bool generateFile = true)
        {
            var sb = new StringBuilder();

            try
            {
                // Add using directives
                sb.AppendLine(_helper.GenerateStandardUsings());
                sb.AppendLine();
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Immutable record representing {entity.EntityName}");
                sb.AppendLine($"    /// </summary>");

                // Create the record declaration
                sb.AppendLine($"    public record {recordName}(");

                // Add parameters for each field
                for (int i = 0; i < entity.Fields.Count; i++)
                {
                    var field = entity.Fields[i];
                    var safePropertyName = _helper.GenerateSafePropertyName(field.FieldName, i);
                    var nullableSuffix = _helper.IsReferenceType(field.Fieldtype) ? "?" : "?";

                    sb.Append($"        {field.Fieldtype}{nullableSuffix} {safePropertyName}");

                    // Add comma if not the last field
                    if (i < entity.Fields.Count - 1)
                    {
                        sb.AppendLine(",");
                    }
                    else
                    {
                        sb.AppendLine(");");
                    }
                }

                sb.AppendLine("}");

                var result = sb.ToString();

                // Write to file if requested
                if (generateFile)
                {
                    outputPath = _helper.EnsureOutputDirectory(outputPath);
                    var filePath = Path.Combine(outputPath, $"{recordName}.cs");
                    _helper.WriteToFile(filePath, result, recordName);
                }

                return result;
            }
            catch (Exception ex)
            {
                _helper.LogMessage("ClassCreator", $"Error creating record class: {ex.Message}", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Creates a class with support for nullable reference types
        /// </summary>
        public string CreateNullableAwareClass(string className, EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectClasses", bool generateNullableAnnotations = true)
        {
            var sb = new StringBuilder();

            try
            {
                // Add using directives
                sb.AppendLine(_helper.GenerateStandardUsings());

                if (generateNullableAnnotations)
                {
                    sb.AppendLine("#nullable enable");
                }

                sb.AppendLine();
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Nullable-aware class for {entity.EntityName}");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    public class {className}");
                sb.AppendLine("    {");

                // Constructor
                sb.AppendLine($"        public {className}()");
                sb.AppendLine("        {");
                sb.AppendLine("        }");
                sb.AppendLine();

                // Properties with nullable annotations
                foreach (var field in entity.Fields)
                {
                    var safePropertyName = _helper.GenerateSafePropertyName(field.FieldName);

                    if (generateNullableAnnotations)
                    {
                        var isReferenceType = _helper.IsReferenceType(field.Fieldtype);
                        var nullableAnnotation = isReferenceType ? "?" : "";

                        sb.AppendLine($"        /// <summary>");
                        sb.AppendLine($"        /// Gets or sets the {safePropertyName}");
                        sb.AppendLine($"        /// </summary>");
                        sb.AppendLine($"        public {field.Fieldtype}{nullableAnnotation} {safePropertyName} {{ get; set; }}");
                    }
                    else
                    {
                        sb.AppendLine($"        /// <summary>");
                        sb.AppendLine($"        /// Gets or sets the {safePropertyName}");
                        sb.AppendLine($"        /// </summary>");
                        sb.AppendLine($"        public {field.Fieldtype} {safePropertyName} {{ get; set; }}");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");

                if (generateNullableAnnotations)
                {
                    sb.AppendLine();
                    sb.AppendLine("#nullable restore");
                }

                var result = sb.ToString();

                // Write to file
                outputPath = _helper.EnsureOutputDirectory(outputPath);
                var filePath = Path.Combine(outputPath, $"{className}.cs");
                _helper.WriteToFile(filePath, result, className);

                return result;
            }
            catch (Exception ex)
            {
                _helper.LogMessage("ClassCreator", $"Error creating nullable aware class: {ex.Message}", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Creates a domain-driven design style aggregate root class
        /// </summary>
        public string CreateDDDAggregateRoot(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectDomain")
        {
            var className = $"{entity.EntityName}Aggregate";
            var sb = new StringBuilder();

            try
            {
                // Add using directives
                sb.AppendLine(_helper.GenerateStandardUsings());
                sb.AppendLine();
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");

                // Interface for the aggregate root
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// Marker interface for aggregate roots in the domain");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    public interface IAggregateRoot { }");
                sb.AppendLine();

                // Create the aggregate root class
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// Aggregate root for {entity.EntityName}");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public class {className} : IAggregateRoot");
                sb.AppendLine("    {");

                // Private fields
                foreach (var field in entity.Fields)
                {
                    var safePropertyName = _helper.GenerateSafePropertyName(field.FieldName);
                    sb.AppendLine($"        private readonly {field.Fieldtype}? _{safePropertyName.ToLower()};");
                }
                sb.AppendLine();

                // Add ID field if not present
                if (!entity.Fields.Any(f => f.FieldName.Equals("Id", StringComparison.OrdinalIgnoreCase)))
                {
                    sb.AppendLine("        private readonly Guid _id;");
                    sb.AppendLine();
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine("        /// Unique identifier for this aggregate");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine("        public Guid Id => _id;");
                    sb.AppendLine();
                }

                // Constructor
                sb.AppendLine($"        public {className}(");
                for (int i = 0; i < entity.Fields.Count; i++)
                {
                    var field = entity.Fields[i];
                    var safePropertyName = _helper.GenerateSafePropertyName(field.FieldName);
                    sb.Append($"            {field.Fieldtype}? {safePropertyName.ToLower()}");

                    if (i < entity.Fields.Count - 1)
                    {
                        sb.AppendLine(",");
                    }
                    else
                    {
                        sb.AppendLine(")");
                    }
                }

                sb.AppendLine("        {");

                // Initialize ID if not in the fields
                if (!entity.Fields.Any(f => f.FieldName.Equals("Id", StringComparison.OrdinalIgnoreCase)))
                {
                    sb.AppendLine("            _id = Guid.NewGuid();");
                }

                // Set fields from parameters
                foreach (var field in entity.Fields)
                {
                    var safePropertyName = _helper.GenerateSafePropertyName(field.FieldName);
                    sb.AppendLine($"            _{safePropertyName.ToLower()} = {safePropertyName.ToLower()};");
                }

                sb.AppendLine("        }");
                sb.AppendLine();

                // Properties (getters only for immutability)
                foreach (var field in entity.Fields)
                {
                    var safePropertyName = _helper.GenerateSafePropertyName(field.FieldName);
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// Gets the {safePropertyName}");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine($"        public {field.Fieldtype}? {safePropertyName} => _{safePropertyName.ToLower()};");
                    sb.AppendLine();
                }

                // Factory method
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Factory method to create a new {entity.EntityName}");
                sb.AppendLine("        /// </summary>");
                sb.Append($"        public static {className} Create(");

                // Factory method parameters (exclude ID fields)
                var nonIdFields = entity.Fields.Where(f => 
                    !f.FieldName.Equals("Id", StringComparison.OrdinalIgnoreCase)).ToList();
                
                for (int i = 0; i < nonIdFields.Count; i++)
                {
                    var field = nonIdFields[i];
                    var safePropertyName = _helper.GenerateSafePropertyName(field.FieldName);
                    sb.Append($"{field.Fieldtype}? {safePropertyName.ToLower()}");

                    if (i < nonIdFields.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.AppendLine(")");
                sb.AppendLine("        {");

                // Create and return the aggregate
                sb.Append($"            return new {className}(");
                for (int i = 0; i < entity.Fields.Count; i++)
                {
                    var field = entity.Fields[i];
                    var safePropertyName = _helper.GenerateSafePropertyName(field.FieldName);
                    sb.Append($"{safePropertyName.ToLower()}");

                    if (i < entity.Fields.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.AppendLine(");");
                sb.AppendLine("        }");

                sb.AppendLine("    }");
                sb.AppendLine("}");

                var result = sb.ToString();

                // Write to file
                outputPath = _helper.EnsureOutputDirectory(outputPath);
                var filePath = Path.Combine(outputPath, $"{className}.cs");
                _helper.WriteToFile(filePath, result, className);

                return result;
            }
            catch (Exception ex)
            {
                _helper.LogMessage("ClassCreator", $"Error creating DDD aggregate root: {ex.Message}", Errors.Failed);
                return null;
            }
        }
    }
}