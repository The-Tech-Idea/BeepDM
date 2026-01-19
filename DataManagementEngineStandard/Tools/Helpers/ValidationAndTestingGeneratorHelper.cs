using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Tools.Helpers
{
    /// <summary>
    /// Helper class for generating unit tests and fluent validators for entities.
    /// </summary>
    public class ValidationAndTestingGeneratorHelper
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;

        /// <summary>
        /// Initializes a new instance of the ValidationAndTestingGeneratorHelper class.
        /// </summary>
        /// <param name="dmeEditor">The DMEEditor instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when dmeEditor is null.</exception>
        public ValidationAndTestingGeneratorHelper(IDMEEditor dmeEditor)
        {
            if (dmeEditor == null)
            {
                throw new ArgumentNullException(nameof(dmeEditor), "DMEEditor cannot be null.");
            }

            _dmeEditor = dmeEditor;
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        /// <summary>
        /// Generates a unit test class for the specified entity.
        /// </summary>
        /// <param name="entity">The entity structure to generate tests for.</param>
        /// <param name="outputPath">The output file path. If null, returns generated code as string.</param>
        /// <returns>The file path if outputPath is provided; otherwise, the generated code as a string.</returns>
        public string GenerateUnitTestClass(EntityStructure entity, string outputPath)
        {
            if (entity == null)
            {
                _helper.LogMessage("Validation", "Entity cannot be null.", Errors.Failed);
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(entity.EntityName))
            {
                _helper.LogMessage("Validation", "Entity name cannot be empty.", Errors.Failed);
                return string.Empty;
            }

            try
            {
                string className = $"{entity.EntityName}Tests";
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("using System;");
                sb.AppendLine("using Xunit;");
                sb.AppendLine("using FluentAssertions;");
                sb.AppendLine();
                sb.AppendLine("namespace TheTechIdea.Beep.Tests");
                sb.AppendLine("{");
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Unit tests for {entity.EntityName} entity.");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    public class {className}");
                sb.AppendLine("    {");
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Tests the creation of a new {entity.EntityName} instance.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        [Fact]");
                sb.AppendLine($"        public void Create_{entity.EntityName}_Should_Initialize_Properties()");
                sb.AppendLine("        {");
                sb.AppendLine($"            // Arrange & Act");
                sb.AppendLine($"            var entity = new {entity.EntityName}();");
                sb.AppendLine();
                sb.AppendLine($"            // Assert");
                sb.AppendLine($"            entity.Should().NotBeNull();");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Tests validation of {entity.EntityName} entity.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        [Fact]");
                sb.AppendLine($"        public void Validate_{entity.EntityName}_Should_Return_True_For_Valid_Data()");
                sb.AppendLine("        {");
                sb.AppendLine($"            // Arrange");
                sb.AppendLine($"            var entity = new {entity.EntityName}();");
                if (entity.Fields != null && entity.Fields.Count > 0)
                {
                    var firstField = entity.Fields.FirstOrDefault();
                    if (firstField != null)
                    {
                        sb.AppendLine($"            entity.{firstField.FieldName} = GetValidTestValue();");
                    }
                }
                sb.AppendLine();
                sb.AppendLine($"            // Act & Assert");
                sb.AppendLine($"            entity.Should().NotBeNull();");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Tests persistence of {entity.EntityName} entity.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        [Fact]");
                sb.AppendLine($"        public void Persist_{entity.EntityName}_Should_Save_Successfully()");
                sb.AppendLine("        {");
                sb.AppendLine($"            // Arrange");
                sb.AppendLine($"            var entity = new {entity.EntityName}();");
                sb.AppendLine();
                sb.AppendLine($"            // Act");
                sb.AppendLine($"            var result = entity != null;");
                sb.AppendLine();
                sb.AppendLine($"            // Assert");
                sb.AppendLine($"            result.Should().BeTrue();");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine($"        private object GetValidTestValue()");
                sb.AppendLine("        {");
                sb.AppendLine("            return \"TestValue\";");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");

                string generatedCode = sb.ToString();

                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    var targetDirectory = Path.GetDirectoryName(outputPath);
                    if (string.IsNullOrWhiteSpace(targetDirectory))
                    {
                        targetDirectory = outputPath;
                    }

                    _helper.EnsureOutputDirectory(targetDirectory);
                    var success = _helper.WriteToFile(outputPath, generatedCode, entity.EntityName);
                    if (success)
                    {
                        _helper.LogMessage("Validation", $"Unit test class generated: {outputPath}");
                        return outputPath;
                    }
                    return string.Empty;
                }

                return generatedCode;
            }
            catch (Exception ex)
            {
                _helper.LogMessage("Validation", $"Error generating unit test class: {ex.Message}", Errors.Failed);
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates fluent validators for the specified entity.
        /// </summary>
        /// <param name="entity">The entity structure to generate validators for.</param>
        /// <param name="outputPath">The output file path. If null, returns generated code as string.</param>
        /// <param name="namespaceName">The namespace for the validator class.</param>
        /// <returns>The file path if outputPath is provided; otherwise, the generated code as a string.</returns>
        public string GenerateFluentValidators(EntityStructure entity, string outputPath, string namespaceName = "TheTechIdea.Beep.Validators")
        {
            if (entity == null)
            {
                _helper.LogMessage("Validation", "Entity cannot be null.", Errors.Failed);
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(entity.EntityName))
            {
                _helper.LogMessage("Validation", "Entity name cannot be empty.", Errors.Failed);
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                namespaceName = "TheTechIdea.Beep.Validators";
            }

            try
            {
                string validatorClassName = $"{entity.EntityName}Validator";
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("using System;");
                sb.AppendLine("using FluentValidation;");
                sb.AppendLine();
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Fluent validator for {entity.EntityName} entity.");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    public class {validatorClassName} : AbstractValidator<{entity.EntityName}>");
                sb.AppendLine("    {");
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Initializes a new instance of the {validatorClassName} class.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public {validatorClassName}()");
                sb.AppendLine("        {");
                
                if (entity.Fields != null && entity.Fields.Count > 0)
                {
                    foreach (var field in entity.Fields)
                    {
                        if (field != null && !string.IsNullOrWhiteSpace(field.FieldName))
                        {
                            sb.AppendLine($"            RuleFor(x => x.{field.FieldName})");
                            
                            if (field.Fieldtype.ToLower().Contains("string"))
                            {
                                sb.AppendLine($"                .NotEmpty().WithMessage(\"{field.FieldName} is required\")");
                                sb.AppendLine($"                .MaximumLength(256).WithMessage(\"{field.FieldName} must not exceed 256 characters\");");
                            }
                            else if (field.Fieldtype.ToLower().Contains("int") || field.Fieldtype.ToLower().Contains("long"))
                            {
                                sb.AppendLine($"                .GreaterThanOrEqualTo(0).WithMessage(\"{field.FieldName} must be greater than or equal to 0\");");
                            }
                            else if (field.Fieldtype.ToLower().Contains("decimal") || field.Fieldtype.ToLower().Contains("double"))
                            {
                                sb.AppendLine($"                .GreaterThanOrEqualTo(0).WithMessage(\"{field.FieldName} must be greater than or equal to 0\");");
                            }
                            else if (field.Fieldtype.ToLower().Contains("date"))
                            {
                                sb.AppendLine($"                .LessThanOrEqualTo(DateTime.Now).WithMessage(\"{field.FieldName} cannot be in the future\");");
                            }
                            else
                            {
                                sb.AppendLine($"                .NotNull().WithMessage(\"{field.FieldName} is required\");");
                            }
                        }
                    }
                }
                else
                {
                    sb.AppendLine("            // Add validation rules for entity properties");
                }
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");

                string generatedCode = sb.ToString();

                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    var targetDirectory = Path.GetDirectoryName(outputPath);
                    if (string.IsNullOrWhiteSpace(targetDirectory))
                    {
                        targetDirectory = outputPath;
                    }

                    _helper.EnsureOutputDirectory(targetDirectory);
                    var success = _helper.WriteToFile(outputPath, generatedCode, entity.EntityName);
                    if (success)
                    {
                        _helper.LogMessage("Validation", $"Fluent validator generated: {outputPath}");
                        return outputPath;
                    }
                    return string.Empty;
                }

                return generatedCode;
            }
            catch (Exception ex)
            {
                _helper.LogMessage("Validation", $"Error generating fluent validators: {ex.Message}", Errors.Failed);
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates unit test classes for multiple entities.
        /// </summary>
        /// <param name="entities">The list of entity structures to generate tests for.</param>
        /// <param name="outputPath">The output directory path.</param>
        /// <returns>A list of generated file paths.</returns>
        public List<string> GenerateUnitTestClasses(List<EntityStructure> entities, string outputPath)
        {
            var generatedFiles = new List<string>();

            if (entities == null || entities.Count == 0)
            {
                _helper.LogMessage("Validation", "Entities list cannot be null or empty.", Errors.Failed);
                return generatedFiles;
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                _helper.LogMessage("Validation", "Output path cannot be empty.", Errors.Failed);
                return generatedFiles;
            }

            try
            {
                _helper.EnsureOutputDirectory(outputPath);

                foreach (var entity in entities)
                {
                    if (entity != null && !string.IsNullOrWhiteSpace(entity.EntityName))
                    {
                        string fileName = Path.Combine(outputPath, $"{entity.EntityName}Tests.cs");
                        string filePath = GenerateUnitTestClass(entity, fileName);
                        if (!string.IsNullOrWhiteSpace(filePath))
                        {
                            generatedFiles.Add(filePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _helper.LogMessage("Validation", $"Error generating unit test classes: {ex.Message}", Errors.Failed);
            }

            return generatedFiles;
        }

        /// <summary>
        /// Generates fluent validators for multiple entities.
        /// </summary>
        /// <param name="entities">The list of entity structures to generate validators for.</param>
        /// <param name="outputPath">The output directory path.</param>
        /// <param name="namespaceName">The namespace for the validator classes.</param>
        /// <returns>A list of generated file paths.</returns>
        public List<string> GenerateFluentValidatorsForEntities(List<EntityStructure> entities, string outputPath, string namespaceName = "TheTechIdea.Beep.Validators")
        {
            var generatedFiles = new List<string>();

            if (entities == null || entities.Count == 0)
            {
                _helper.LogMessage("Validation", "Entities list cannot be null or empty.", Errors.Failed);
                return generatedFiles;
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                _helper.LogMessage("Validation", "Output path cannot be empty.", Errors.Failed);
                return generatedFiles;
            }

            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                namespaceName = "TheTechIdea.Beep.Validators";
            }

            try
            {
                _helper.EnsureOutputDirectory(outputPath);

                foreach (var entity in entities)
                {
                    if (entity != null && !string.IsNullOrWhiteSpace(entity.EntityName))
                    {
                        string fileName = Path.Combine(outputPath, $"{entity.EntityName}Validator.cs");
                        string filePath = GenerateFluentValidators(entity, fileName, namespaceName);
                        if (!string.IsNullOrWhiteSpace(filePath))
                        {
                            generatedFiles.Add(filePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _helper.LogMessage("Validation", $"Error generating fluent validators for entities: {ex.Message}", Errors.Failed);
            }

            return generatedFiles;
        }
    }
}
