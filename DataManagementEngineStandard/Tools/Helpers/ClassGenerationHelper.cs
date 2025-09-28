using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools.Helpers
{
    /// <summary>
    /// Helper class for common class generation utilities
    /// </summary>
    public class ClassGenerationHelper
    {
        private readonly IDMEEditor _dmeEditor;

        public ClassGenerationHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }

        /// <summary>
        /// Validates entity structure for class generation
        /// </summary>
        public List<string> ValidateEntityStructure(EntityStructure entity)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(entity.EntityName))
                errors.Add("Entity name cannot be null or empty.");

            if (entity.Fields == null || entity.Fields.Count == 0)
                errors.Add("Entity must have at least one field.");

            foreach (var field in entity.Fields)
            {
                if (string.IsNullOrEmpty(field.fieldname))
                    errors.Add($"Field name cannot be empty in entity {entity.EntityName}.");
                if (string.IsNullOrEmpty(field.fieldtype))
                    errors.Add($"Field type cannot be empty for field {field.fieldname} in entity {entity.EntityName}.");
            }

            return errors;
        }

        /// <summary>
        /// Generates a safe C# property name from a field name
        /// </summary>
        public string GenerateSafePropertyName(string fieldName, int index = 0)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return $"Field{index}";

            // Remove invalid characters and replace with underscore
            var safeName = Regex.Replace(fieldName, @"[^A-Za-z0-9_]", "_");

            // If it starts with a digit, prefix with underscore
            if (char.IsDigit(safeName[0]))
                safeName = "_" + safeName;

            // If empty after cleaning, provide default
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = $"Field{index}";

            return safeName;
        }

        /// <summary>
        /// Generates a backing field name from a property name
        /// </summary>
        public string GenerateBackingFieldName(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return "_value";

            return "_" + char.ToLowerInvariant(propertyName[0]) + 
                   (propertyName.Length > 1 ? propertyName.Substring(1) : "") + "Value";
        }

        /// <summary>
        /// Determines if a type is a reference type
        /// </summary>
        public bool IsReferenceType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return true;

            var lowerType = typeName.ToLower();
            return !(lowerType == "int" || lowerType == "long" || lowerType == "float" ||
                     lowerType == "double" || lowerType == "decimal" || lowerType == "bool" ||
                     lowerType == "byte" || lowerType == "sbyte" || lowerType == "char" ||
                     lowerType == "short" || lowerType == "ushort" || lowerType == "uint" ||
                     lowerType == "ulong" || lowerType == "int16" || lowerType == "int32" ||
                     lowerType == "int64" || lowerType == "uint16" || lowerType == "uint32" ||
                     lowerType == "uint64" || lowerType == "single" || lowerType == "boolean" ||
                     lowerType == "datetime" || lowerType == "timespan" || lowerType == "guid");
        }

        /// <summary>
        /// Determines if a type is numeric
        /// </summary>
        public bool IsNumericType(string fieldType)
        {
            var lowerType = fieldType.ToLower();
            return lowerType == "int" || lowerType == "int32" || lowerType == "int64" ||
                   lowerType == "long" || lowerType == "decimal" || lowerType == "double" ||
                   lowerType == "float" || lowerType == "short" || lowerType == "byte" ||
                   lowerType == "uint" || lowerType == "ulong" || lowerType == "ushort" ||
                   lowerType == "sbyte" || lowerType == "single";
        }

        /// <summary>
        /// Determines if a type is an integral type
        /// </summary>
        public bool IsIntegralType(string fieldType)
        {
            var lowerType = fieldType.ToLower();
            return lowerType == "int" || lowerType == "int32" || lowerType == "int64" ||
                   lowerType == "long" || lowerType == "short" || lowerType == "int16" ||
                   lowerType == "byte" || lowerType == "uint" || lowerType == "ulong" ||
                   lowerType == "ushort" || lowerType == "sbyte";
        }

        /// <summary>
        /// Maps C# field types to SQL Server types
        /// </summary>
        public string MapFieldTypeToSqlType(string fieldType)
        {
            var lowerType = fieldType.ToLower();
            return lowerType switch
            {
                "string" => "nvarchar(max)",
                "int" or "int32" => "int",
                "long" or "int64" => "bigint",
                "short" or "int16" => "smallint",
                "byte" => "tinyint",
                "bool" or "boolean" => "bit",
                "decimal" => "decimal(18, 2)",
                "double" => "float",
                "float" or "single" => "real",
                "datetime" => "datetime2",
                "guid" => "uniqueidentifier",
                "byte[]" => "varbinary(max)",
                _ => "nvarchar(max)"
            };
        }

        /// <summary>
        /// Maps C# types to protobuf types
        /// </summary>
        public string MapCSharpToProtoType(string csharpType)
        {
            var lowerType = csharpType.ToLower();
            return lowerType switch
            {
                "int" or "int32" => "int32",
                "long" or "int64" => "int64",
                "float" or "single" => "float",
                "double" => "double",
                "bool" or "boolean" => "bool",
                "string" => "string",
                "byte[]" => "bytes",
                "datetime" => "google.protobuf.Timestamp",
                "guid" => "string",
                "decimal" => "double", // No direct decimal in protobuf
                _ => "string" // Default to string for unknown types
            };
        }

        /// <summary>
        /// Converts a name to snake_case
        /// </summary>
        public string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var result = Regex.Replace(name, "([A-Z])", "_$1").ToLower();
            if (result.StartsWith("_"))
                result = result.Substring(1);

            return result;
        }

        /// <summary>
        /// Converts a name to PascalCase
        /// </summary>
        public string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Split by underscore and capitalize each part
            var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();

            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    result.Append(char.ToUpperInvariant(part[0]));
                    if (part.Length > 1)
                        result.Append(part.Substring(1).ToLowerInvariant());
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Converts a name to camelCase
        /// </summary>
        public string ToCamelCase(string name)
        {
            var pascalCase = ToPascalCase(name);
            if (string.IsNullOrEmpty(pascalCase))
                return pascalCase;

            return char.ToLowerInvariant(pascalCase[0]) + 
                   (pascalCase.Length > 1 ? pascalCase.Substring(1) : "");
        }

        /// <summary>
        /// Gets a user-friendly display name from a field name
        /// </summary>
        public string GetDisplayName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return string.Empty;

            // Insert a space before each capital letter and then capitalize the first letter
            var result = Regex.Replace(fieldName, "([A-Z])", " $1").Trim();
            return char.ToUpper(result[0]) + result.Substring(1);
        }

        /// <summary>
        /// Ensures output directory exists
        /// </summary>
        public string EnsureOutputDirectory(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = _dmeEditor.ConfigEditor.Config.ScriptsPath;
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            return outputPath;
        }

        /// <summary>
        /// Logs a message using the DMEEditor
        /// </summary>
        public void LogMessage(string category, string message, Errors errorType = Errors.Ok)
        {
            _dmeEditor.AddLogMessage(category, message, DateTime.Now, -1, null, errorType);
        }

        /// <summary>
        /// Generates standard using statements for class files
        /// </summary>
        public string GenerateStandardUsings(params string[] additionalUsings)
        {
            var usings = new List<string>
            {
                "using System;",
                "using System.Collections.Generic;",
                "using System.ComponentModel;",
                "using System.Runtime.CompilerServices;"
            };

            if (additionalUsings != null)
            {
                usings.AddRange(additionalUsings);
            }

            return string.Join(Environment.NewLine, usings.Distinct());
        }

        /// <summary>
        /// Generates a complete namespace wrapper around class content
        /// </summary>
        public string WrapInNamespace(string namespaceName, string classContent)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            
            // Indent the class content
            var lines = classContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    sb.AppendLine($"    {line}");
                else
                    sb.AppendLine();
            }
            
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Writes content to file with proper error handling
        /// </summary>
        public bool WriteToFile(string filePath, string content, string entityName = "")
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, content);
                LogMessage("ClassCreator", $"Successfully created file for {entityName}: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage("ClassCreator", $"Error writing file {filePath}: {ex.Message}", Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Generates a default constructor for a class
        /// </summary>
        public string GenerateDefaultConstructor(string className)
        {
            return $"public {className}() {{ }}";
        }

        /// <summary>
        /// Generates a parameterized constructor for a class
        /// </summary>
        public string GenerateParameterizedConstructor(string className, List<EntityField> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"public {className}(");

            // Add parameters
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                sb.Append($"    {MapFieldTypeToSqlType(field.fieldtype)} {GenerateSafePropertyName(field.fieldname)}{(i < fields.Count - 1 ? ", " : "")}");
            }

            sb.AppendLine(") {");

            // Assign parameters to properties
            foreach (var field in fields)
            {
                sb.AppendLine($"    this.{GenerateSafePropertyName(field.fieldname)} = {GenerateSafePropertyName(field.fieldname)};");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Generates a full property with getter and setter
        /// </summary>
        public string GenerateFullProperty(string propertyName, string fieldType)
        {
            var backingField = GenerateBackingFieldName(propertyName);
            var propertyType = MapFieldTypeToSqlType(fieldType);

            return $"private {propertyType} {backingField};\n" +
                   $"public {propertyType} {propertyName} {{ get => {backingField}; set => {backingField} = value; }}";
        }

        /// <summary>
        /// Checks if a file exists
        /// </summary>
        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        /// Reads content from a file
        /// </summary>
        public string ReadFileContent(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                LogMessage("FileReader", $"Error reading file {filePath}: {ex.Message}", Errors.Failed);
                return string.Empty;
            }
        }
    }
}