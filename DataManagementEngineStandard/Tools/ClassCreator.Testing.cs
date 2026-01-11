using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Tools.Interfaces;
using TheTechIdea.Beep.Tools.Helpers;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Partial class for testing and validation functionality
    /// </summary>
    public partial class ClassCreator
    {
        #region Private Fields

        private ValidationAndTestingGeneratorHelper _validationTestingHelper;

        #endregion

        #region Properties

        /// <summary>Gets the validation and testing generator helper (lazy-loaded)</summary>
        protected ValidationAndTestingGeneratorHelper ValidationTestingGenerator
        {
            get
            {
                if (_validationTestingHelper == null)
                {
                    _validationTestingHelper = new ValidationAndTestingGeneratorHelper(DMEEditor);
                }
                return _validationTestingHelper;
            }
        }

        #endregion

        #region Testing and Validation Methods

        /// <summary>
        /// Generates a unit test class template for an entity
        /// </summary>
        /// <param name="entity">The EntityStructure to generate the test class for</param>
        /// <param name="outputPath">The output path to save the test class file</param>
        /// <returns>The path to the generated test class file</returns>
        public string GenerateUnitTestClass(EntityStructure entity, string outputPath)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                var validationErrors = ValidateEntityStructure(entity);
                if (validationErrors.Count > 0)
                {
                    var errorMessage = $"Entity validation failed: {string.Join(", ", validationErrors)}";
                    throw new ArgumentException(errorMessage, nameof(entity));
                }
                
                return ValidationTestingGenerator.GenerateUnitTestClass(entity, outputPath);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating unit test class for {entity?.EntityName}: {ex.Message}", 
                    Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Generates FluentValidation validators for entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <returns>The validator class code</returns>
        public string GenerateFluentValidators(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectValidators")
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                var validationErrors = ValidateEntityStructure(entity);
                if (validationErrors.Count > 0)
                {
                    var errorMessage = $"Entity validation failed: {string.Join(", ", validationErrors)}";
                    throw new ArgumentException(errorMessage, nameof(entity));
                }
                
                if (string.IsNullOrWhiteSpace(namespaceName))
                    throw new ArgumentException("Namespace name cannot be null or empty", nameof(namespaceName));

                return ValidationTestingGenerator.GenerateFluentValidators(entity, outputPath, namespaceName);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating FluentValidation validators for {entity?.EntityName}: {ex.Message}", 
                    Errors.Failed);
                throw;
            }
        }

        #endregion
    }

    /// <summary>
    /// Helper class for generating testing and validation classes
    /// </summary>
    public class ValidationAndTestingGeneratorHelper
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;

        public ValidationAndTestingGeneratorHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        /// <summary>
        /// Validates the given EntityStructure to ensure it meets class generation requirements
        /// </summary>
        public List<string> ValidateEntityStructure(EntityStructure entity)
        {
            return _helper.ValidateEntityStructure(entity);
        }

        /// <summary>
        /// Generates a unit test class template for an entity
        /// </summary>
        public string GenerateUnitTestClass(EntityStructure entity, string outputPath)
        {
            var className = $"{entity.EntityName}Tests";
            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = Path.Combine(outputPath, $"{className}.cs");

            var sb = new StringBuilder();
            
            // Add using statements
            sb.AppendLine("using Xunit;");
            sb.AppendLine("using Moq;");
            sb.AppendLine(_helper.GenerateStandardUsings(
                "using TheTechIdea.Beep.DataBase;",
                "using TheTechIdea.Beep.Editor;"
            ));
            sb.AppendLine();

            sb.AppendLine($"namespace {entity.EntityName}.Tests");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Unit tests for {entity.EntityName}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly Mock<IDataSource> _mockDataSource;");
            sb.AppendLine("        private readonly Mock<IDMEEditor> _mockDMEEditor;");
            sb.AppendLine();

            // Constructor
            sb.AppendLine($"        public {className}()");
            sb.AppendLine("        {");
            sb.AppendLine("            _mockDataSource = new Mock<IDataSource>();");
            sb.AppendLine("            _mockDMEEditor = new Mock<IDMEEditor>();");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Test methods
            GenerateTestMethods(sb, entity);

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var result = sb.ToString();
            _helper.WriteToFile(filePath, result, entity.EntityName);
            return filePath;
        }

        /// <summary>
        /// Generates FluentValidation validators for entity
        /// </summary>
        public string GenerateFluentValidators(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectValidators")
        {
            var className = $"{entity.EntityName}Validator";
            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = Path.Combine(outputPath, $"{className}.cs");

            var sb = new StringBuilder();
            
            // Add using statements
            sb.AppendLine("using FluentValidation;");
            sb.AppendLine(_helper.GenerateStandardUsings());
            sb.AppendLine();

            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// FluentValidation validator for {entity.EntityName}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {className} : AbstractValidator<{entity.EntityName}>");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {className}()");
            sb.AppendLine("        {");

            // Generate validation rules for each field
            foreach (var field in entity.Fields)
            {
                var safePropertyName = _helper.GenerateSafePropertyName(field.fieldname);
                sb.AppendLine($"            RuleFor(x => x.{safePropertyName})");

                // Apply rules based on field properties
                if (field.IsRequired)
                {
                    if (_helper.IsReferenceType(field.fieldtype))
                    {
                        sb.AppendLine($"                .NotEmpty().WithMessage(\"{safePropertyName} is required.\")");
                    }
                    else
                    {
                        sb.AppendLine($"                .NotNull().WithMessage(\"{safePropertyName} is required.\")");
                    }
                }

                // String-specific validations
                if (field.fieldtype.ToLower().Contains("string"))
                {
                    if (field.Size > 0)
                    {
                        sb.AppendLine($"                .MaximumLength({field.Size}).WithMessage(\"{safePropertyName} must not exceed {field.Size} characters.\")");
                    }
                }

                // Numeric validations
                if (_helper.IsNumericType(field.fieldtype))
                {
                    // Use the ValueMin and ValueMax properties from EntityField
                    if (field.ValueMin != 0)
                    {
                        sb.AppendLine($"                .GreaterThanOrEqualTo({field.ValueMin}).WithMessage(\"{safePropertyName} must be greater than or equal to {field.ValueMin}.\")");
                    }

                    if (field.ValueMax != 0)
                    {
                        sb.AppendLine($"                .LessThanOrEqualTo({field.ValueMax}).WithMessage(\"{safePropertyName} must be less than or equal to {field.ValueMax}.\")");
                    }
                }

                // Email validation
                if (safePropertyName.ToLower().Contains("email"))
                {
                    sb.AppendLine($"                .EmailAddress().WithMessage(\"{safePropertyName} must be a valid email address.\")");
                }

                sb.AppendLine("                ;");
                sb.AppendLine();
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var result = sb.ToString();
            _helper.WriteToFile(filePath, result, entity.EntityName);
            return filePath;
        }

        /// <summary>
        /// Generates test methods for the entity
        /// </summary>
        private void GenerateTestMethods(StringBuilder sb, EntityStructure entity)
        {
            // Test creation
            sb.AppendLine("        [Fact]");
            sb.AppendLine($"        public void Create{entity.EntityName}_ShouldSucceed()");
            sb.AppendLine("        {");
            sb.AppendLine("            // Arrange");
            sb.AppendLine($"            var entity = new {entity.EntityName}();");
            sb.AppendLine();
            sb.AppendLine("            // Act & Assert");
            sb.AppendLine("            Assert.NotNull(entity);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Test property setting
            foreach (var field in entity.Fields.Take(3)) // Limit to first 3 fields for brevity
            {
                var safePropertyName = _helper.GenerateSafePropertyName(field.fieldname);
                var testValue = GetTestValue(field.fieldtype);
                
                sb.AppendLine("        [Fact]");
                sb.AppendLine($"        public void Set{safePropertyName}_ShouldSucceed()");
                sb.AppendLine("        {");
                sb.AppendLine("            // Arrange");
                sb.AppendLine($"            var entity = new {entity.EntityName}();");
                sb.AppendLine($"            var testValue = {testValue};");
                sb.AppendLine();
                sb.AppendLine("            // Act");
                sb.AppendLine($"            entity.{safePropertyName} = testValue;");
                sb.AppendLine();
                sb.AppendLine("            // Assert");
                sb.AppendLine($"            Assert.Equal(testValue, entity.{safePropertyName});");
                sb.AppendLine("        }");
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Gets a test value for a given field type
        /// </summary>
        private string GetTestValue(string fieldType)
        {
            var lowerType = fieldType.ToLower();
            return lowerType switch
            {
                "string" => "\"Test Value\"",
                "int" or "int32" => "42",
                "long" or "int64" => "42L",
                "decimal" => "42.5m",
                "double" => "42.5d",
                "float" or "single" => "42.5f",
                "bool" or "boolean" => "true",
                "datetime" => "new DateTime(2023, 1, 1)",
                "guid" => "Guid.NewGuid()",
                _ => "null"
            };
        }
    }
}