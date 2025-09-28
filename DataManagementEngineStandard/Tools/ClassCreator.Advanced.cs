using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools.Interfaces;
using TheTechIdea.Beep.Tools.Helpers;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Partial class for documentation, utilities, and advanced features
    /// </summary>
    public partial class ClassCreator : IDocumentationGenerator, IUiComponentGenerator, IServerlessGenerator
    {
        #region Private Fields

        private DocumentationGeneratorHelper _documentationHelper;
        private UiComponentGeneratorHelper _uiComponentHelper;
        private ServerlessGeneratorHelper _serverlessHelper;

        #endregion

        #region Properties

        /// <summary>Gets the documentation generator helper (lazy-loaded)</summary>
        protected IDocumentationGenerator DocumentationGenerator
        {
            get
            {
                if (_documentationHelper == null)
                {
                    _documentationHelper = new DocumentationGeneratorHelper(DMEEditor);
                }
                return _documentationHelper;
            }
        }

        /// <summary>Gets the UI component generator helper (lazy-loaded)</summary>
        protected IUiComponentGenerator UiComponentGenerator
        {
            get
            {
                if (_uiComponentHelper == null)
                {
                    _uiComponentHelper = new UiComponentGeneratorHelper(DMEEditor);
                }
                return _uiComponentHelper;
            }
        }

        /// <summary>Gets the serverless generator helper (lazy-loaded)</summary>
        protected IServerlessGenerator ServerlessGenerator
        {
            get
            {
                if (_serverlessHelper == null)
                {
                    _serverlessHelper = new ServerlessGeneratorHelper(DMEEditor);
                }
                return _serverlessHelper;
            }
        }

        #endregion

        #region Documentation Methods

        /// <summary>
        /// Generates XML documentation from entity structure
        /// </summary>
        public string GenerateEntityDocumentation(EntityStructure entity, string outputPath)
        {
            try
            {
                ValidateEntityForAdvancedGeneration(entity);
                return DocumentationGenerator.GenerateEntityDocumentation(entity, outputPath);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating entity documentation for {entity?.EntityName}: {ex.Message}",
                    Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Generates a difference report between two versions of an entity
        /// </summary>
        public string GenerateEntityDiffReport(EntityStructure originalEntity, EntityStructure newEntity)
        {
            try
            {
                if (originalEntity == null)
                    throw new ArgumentNullException(nameof(originalEntity));
                if (newEntity == null)
                    throw new ArgumentNullException(nameof(newEntity));

                return DocumentationGenerator.GenerateEntityDiffReport(originalEntity, newEntity);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating entity diff report: {ex.Message}", Errors.Failed);
                throw;
            }
        }

        #endregion

        #region UI Component Generation Methods

        /// <summary>
        /// Generates GraphQL type definitions from entity structures
        /// </summary>
        public string GenerateGraphQLSchema(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectGraphQL")
        {
            try
            {
                if (entities == null || entities.Count == 0)
                    throw new ArgumentException("Entities list cannot be null or empty", nameof(entities));

                foreach (var entity in entities)
                {
                    ValidateEntityForAdvancedGeneration(entity);
                }

                return UiComponentGenerator.GenerateGraphQLSchema(entities, outputPath, namespaceName);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating GraphQL schema: {ex.Message}", Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Generates Blazor component for displaying and editing entity
        /// </summary>
        public string GenerateBlazorComponent(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectComponents")
        {
            try
            {
                ValidateEntityForAdvancedGeneration(entity);

                if (string.IsNullOrWhiteSpace(namespaceName))
                    throw new ArgumentException("Namespace name cannot be null or empty", nameof(namespaceName));

                return UiComponentGenerator.GenerateBlazorComponent(entity, outputPath, namespaceName);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating Blazor component for {entity?.EntityName}: {ex.Message}",
                    Errors.Failed);
                throw;
            }
        }

        #endregion

        #region Serverless Generation Methods

   

        /// <summary>
        /// Generates gRPC service definitions for entity
        /// </summary>
        public (string ProtoFile, string ServiceImplementation) GenerateGrpcService(EntityStructure entity,
            string outputPath, string namespaceName)
        {
            try
            {
                ValidateEntityForAdvancedGeneration(entity);

                if (string.IsNullOrWhiteSpace(namespaceName))
                    throw new ArgumentException("Namespace name cannot be null or empty", nameof(namespaceName));

                return ServerlessGenerator.GenerateGrpcService(entity, outputPath, namespaceName);
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating gRPC service for {entity?.EntityName}: {ex.Message}",
                    Errors.Failed);
                throw;
            }
        }

        #endregion

        #region Validation Helper

        /// <summary>
        /// Validates an entity structure before advanced generation
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        private void ValidateEntityForAdvancedGeneration(EntityStructure entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var validationErrors = ValidateEntityStructure(entity);
            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Entity validation failed: {string.Join(", ", validationErrors)}";
                throw new ArgumentException(errorMessage, nameof(entity));
            }
        }
     
        #endregion
    }

    /// <summary>
    /// Helper class for generating documentation
    /// </summary>
    public class DocumentationGeneratorHelper : IDocumentationGenerator
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;

        public DocumentationGeneratorHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        /// <summary>
        /// Generates XML documentation from entity structure
        /// </summary>
        public string GenerateEntityDocumentation(EntityStructure entity, string outputPath)
        {
            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = Path.Combine(outputPath, $"{entity.EntityName}Documentation.xml");

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<doc>");
            sb.AppendLine("    <assembly>");
            sb.AppendLine($"        <name>{entity.EntityName}</name>");
            sb.AppendLine("    </assembly>");
            sb.AppendLine("    <members>");

            // Entity documentation
            sb.AppendLine($"        <member name=\"T:{entity.EntityName}\">");
            sb.AppendLine($"            <summary>");
            sb.AppendLine($"            {(!string.IsNullOrEmpty(entity.Description) ? entity.Description : $"Represents the {entity.EntityName} entity")}");
            sb.AppendLine($"            </summary>");
            if (!string.IsNullOrEmpty(entity.Caption))
                sb.AppendLine($"            <remarks>Caption: {entity.Caption}</remarks>");
            sb.AppendLine("        </member>");

            // Document each field
            foreach (var field in entity.Fields)
            {
                var safePropertyName = _helper.GenerateSafePropertyName(field.fieldname);
                sb.AppendLine($"        <member name=\"P:{entity.EntityName}.{safePropertyName}\">");
                sb.AppendLine("            <summary>");
                sb.AppendLine($"            {(!string.IsNullOrEmpty(field.Description) ? field.Description : $"The {safePropertyName} property")}");
                sb.AppendLine("            </summary>");
                sb.AppendLine($"            <value>A {field.fieldtype} value representing {safePropertyName}</value>");

                if (!string.IsNullOrEmpty(field.DefaultValue))
                    sb.AppendLine($"            <remarks>Default value: {field.DefaultValue}</remarks>");
                if (field.IsRequired)
                    sb.AppendLine("            <remarks>This field is required</remarks>");

                sb.AppendLine("        </member>");
            }

            sb.AppendLine("    </members>");
            sb.AppendLine("</doc>");

            var result = sb.ToString();
            _helper.WriteToFile(filePath, result, entity.EntityName);
            return result;
        }

        /// <summary>
        /// Generates a difference report between two versions of an entity
        /// </summary>
        public string GenerateEntityDiffReport(EntityStructure originalEntity, EntityStructure newEntity)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# Entity Difference Report");
            sb.AppendLine($"Date: {DateTime.Now}");
            sb.AppendLine($"Original Entity: {originalEntity.EntityName}");
            sb.AppendLine($"New Entity: {newEntity.EntityName}");
            sb.AppendLine();

            // Basic entity property changes
            if (originalEntity.EntityName != newEntity.EntityName)
            {
                sb.AppendLine($"## Name Change");
                sb.AppendLine($"- Original: {originalEntity.EntityName}");
                sb.AppendLine($"- New: {newEntity.EntityName}");
                sb.AppendLine();
            }

            // Field differences
            sb.AppendLine("## Field Changes");

            var originalFields = originalEntity.Fields.ToDictionary(f => f.fieldname);
            var newFields = newEntity.Fields.ToDictionary(f => f.fieldname);

            // Fields removed
            var removedFields = originalFields.Keys.Except(newFields.Keys).ToList();
            if (removedFields.Any())
            {
                sb.AppendLine("### Removed Fields");
                foreach (var fieldName in removedFields)
                {
                    var field = originalFields[fieldName];
                    sb.AppendLine($"- {field.fieldname} ({field.fieldtype})");
                }
                sb.AppendLine();
            }

            // Fields added
            var addedFields = newFields.Keys.Except(originalFields.Keys).ToList();
            if (addedFields.Any())
            {
                sb.AppendLine("### Added Fields");
                foreach (var fieldName in addedFields)
                {
                    var field = newFields[fieldName];
                    sb.AppendLine($"- {field.fieldname} ({field.fieldtype})");
                }
                sb.AppendLine();
            }

            // Summary
            sb.AppendLine("## Summary");
            sb.AppendLine($"- Fields Removed: {removedFields.Count}");
            sb.AppendLine($"- Fields Added: {addedFields.Count}");
            sb.AppendLine($"- Total Fields in Original: {originalEntity.Fields.Count}");
            sb.AppendLine($"- Total Fields in New: {newEntity.Fields.Count}");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Helper class for generating UI components
    /// </summary>
    public class UiComponentGeneratorHelper : IUiComponentGenerator
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;

        public UiComponentGeneratorHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        /// <summary>
        /// Generates GraphQL schema from entities
        /// </summary>
        public string GenerateGraphQLSchema(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectGraphQL")
        {
            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = Path.Combine(outputPath, "GraphQLSchema.cs");

            var sb = new StringBuilder();
            sb.AppendLine("using HotChocolate.Types;");
            sb.AppendLine("using HotChocolate;");
            sb.AppendLine(_helper.GenerateStandardUsings());
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");

            // Generate type classes for each entity
            foreach (var entity in entities)
            {
                var typeName = entity.EntityName + "Type";
                sb.AppendLine($"    public class {typeName} : ObjectType<{entity.EntityName}>");
                sb.AppendLine("    {");
                sb.AppendLine("        protected override void Configure(IObjectTypeDescriptor<" + entity.EntityName + "> descriptor)");
                sb.AppendLine("        {");
                sb.AppendLine("            descriptor.Description(\"" + (string.IsNullOrEmpty(entity.Description) ?
                                                                     $"Represents a {entity.EntityName} entity" :
                                                                     entity.Description) + "\");");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("}");

            var result = sb.ToString();
            _helper.WriteToFile(filePath, result, "GraphQLSchema");
            return result;
        }

        /// <summary>
        /// Generates Blazor component for entity
        /// </summary>
        public string GenerateBlazorComponent(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectComponents")
        {
            var componentName = $"{entity.EntityName}Component";
            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = Path.Combine(outputPath, $"{componentName}.razor");

            var sb = new StringBuilder();
            sb.AppendLine("@namespace " + namespaceName);
            sb.AppendLine("@using TheTechIdea.Beep.DataBase");
            sb.AppendLine();
            sb.AppendLine("<div class=\"card\">");
            sb.AppendLine("    <div class=\"card-header\">");
            sb.AppendLine($"        <h3>{entity.Caption ?? entity.EntityName}</h3>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div class=\"card-body\">");
            sb.AppendLine("        <form>");

            // Generate form fields
            foreach (var field in entity.Fields.Take(5)) // Limit for brevity
            {
                var safePropertyName = _helper.GenerateSafePropertyName(field.fieldname);
                sb.AppendLine("            <div class=\"form-group\">");
                sb.AppendLine($"                <label for=\"{safePropertyName}\">{_helper.GetDisplayName(safePropertyName)}</label>");
                sb.AppendLine($"                <input type=\"text\" class=\"form-control\" id=\"{safePropertyName}\" />");
                sb.AppendLine("            </div>");
            }

            sb.AppendLine("            <button type=\"submit\" class=\"btn btn-primary\">Save</button>");
            sb.AppendLine("        </form>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</div>");

            var result = sb.ToString();
            _helper.WriteToFile(filePath, result, entity.EntityName);
            return result;
        }
    }

    /// <summary>
    /// Helper class for generating serverless functions
    /// </summary>
    public class ServerlessGeneratorHelper : IServerlessGenerator
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;

        public ServerlessGeneratorHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        /// <summary>
        /// Generates serverless functions for entity operations
        /// </summary>
        public string GenerateServerlessFunctions(EntityStructure entity, string outputPath,
            CloudProviderType cloudProvider = CloudProviderType.Azure)
        {
            var className = $"{entity.EntityName}Functions";
            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = Path.Combine(outputPath, $"{className}.cs");

            var sb = new StringBuilder();

            switch (cloudProvider)
            {
                case CloudProviderType.Azure:
                    GenerateAzureFunctions(sb, entity, className);
                    break;
                case CloudProviderType.AWS:
                    GenerateAWSLambdaFunctions(sb, entity, className);
                    break;
                default:
                    throw new NotSupportedException($"Cloud provider {cloudProvider} is not supported");
            }

            var result = sb.ToString();
            _helper.WriteToFile(filePath, result, entity.EntityName);
            return result;
        }

        /// <summary>
        /// Generates gRPC service definitions
        /// </summary>
        public (string ProtoFile, string ServiceImplementation) GenerateGrpcService(EntityStructure entity,
            string outputPath, string namespaceName)
        {
            outputPath = _helper.EnsureOutputDirectory(outputPath);

            // Generate proto file
            var protoSb = new StringBuilder();
            protoSb.AppendLine("syntax = \"proto3\";");
            protoSb.AppendLine();
            protoSb.AppendLine($"option csharp_namespace = \"{namespaceName}.Protos\";");
            protoSb.AppendLine();
            protoSb.AppendLine($"service {entity.EntityName}Service {{");
            protoSb.AppendLine($"  rpc GetAll (GetAllRequest) returns (stream {entity.EntityName});");
            protoSb.AppendLine($"  rpc GetById (GetByIdRequest) returns ({entity.EntityName});");
            protoSb.AppendLine("}");
            protoSb.AppendLine();
            protoSb.AppendLine($"message {entity.EntityName} {{");

            int fieldNumber = 1;
            foreach (var field in entity.Fields)
            {
                var protoType = _helper.MapCSharpToProtoType(field.fieldtype);
                var snakeCaseName = _helper.ToSnakeCase(field.fieldname);
                protoSb.AppendLine($"  {protoType} {snakeCaseName} = {fieldNumber++};");
            }

            protoSb.AppendLine("}");
            protoSb.AppendLine();
            protoSb.AppendLine("message GetAllRequest {}");
            protoSb.AppendLine("message GetByIdRequest { int32 id = 1; }");

            var protoResult = protoSb.ToString();
            var protoFilePath = Path.Combine(outputPath, $"{entity.EntityName.ToLower()}.proto");
            _helper.WriteToFile(protoFilePath, protoResult, $"{entity.EntityName} Proto");

            // Generate service implementation (simplified)
            var serviceSb = new StringBuilder();
            serviceSb.AppendLine("using Grpc.Core;");
            serviceSb.AppendLine(_helper.GenerateStandardUsings());
            serviceSb.AppendLine();
            serviceSb.AppendLine($"namespace {namespaceName}.Services");
            serviceSb.AppendLine("{");
            serviceSb.AppendLine($"    public class {entity.EntityName}ServiceImpl : {entity.EntityName}Service.{entity.EntityName}ServiceBase");
            serviceSb.AppendLine("    {");
            serviceSb.AppendLine("        // Implementation would go here");
            serviceSb.AppendLine("    }");
            serviceSb.AppendLine("}");

            var serviceResult = serviceSb.ToString();
            var serviceFilePath = Path.Combine(outputPath, $"{entity.EntityName}Service.cs");
            _helper.WriteToFile(serviceFilePath, serviceResult, $"{entity.EntityName} Service");

            return (protoResult, serviceResult);
        }

        private void GenerateAzureFunctions(StringBuilder sb, EntityStructure entity, string className)
        {
            sb.AppendLine("using Microsoft.Azure.WebJobs;");
            sb.AppendLine("using Microsoft.Azure.WebJobs.Extensions.Http;");
            sb.AppendLine("using Microsoft.AspNetCore.Http;");
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine(_helper.GenerateStandardUsings());
            sb.AppendLine();
            sb.AppendLine($"namespace TheTechIdea.Functions");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");
            sb.AppendLine("        [FunctionName(\"Get" + entity.EntityName + "s\")]");
            sb.AppendLine("        public static async Task<IActionResult> GetAll(");
            sb.AppendLine("            [HttpTrigger(AuthorizationLevel.Function, \"get\", Route = \"" + entity.EntityName.ToLower() + "s\")] HttpRequest req)");
            sb.AppendLine("        {");
            sb.AppendLine("            // Implementation would go here");
            sb.AppendLine("            return new OkResult();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }

        private void GenerateAWSLambdaFunctions(StringBuilder sb, EntityStructure entity, string className)
        {
            sb.AppendLine("using Amazon.Lambda.Core;");
            sb.AppendLine("using Amazon.Lambda.APIGatewayEvents;");
            sb.AppendLine(_helper.GenerateStandardUsings());
            sb.AppendLine();
            sb.AppendLine($"namespace TheTechIdea.Lambda");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");
            sb.AppendLine("        public APIGatewayProxyResponse GetAll(APIGatewayProxyRequest request, ILambdaContext context)");
            sb.AppendLine("        {");
            sb.AppendLine("            // Implementation would go here");
            sb.AppendLine("            return new APIGatewayProxyResponse { StatusCode = 200 };");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
    }
}
