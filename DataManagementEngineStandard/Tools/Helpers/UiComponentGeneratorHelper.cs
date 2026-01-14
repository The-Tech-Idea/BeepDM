using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Tools.Helpers
{
    /// <summary>
    /// Helper class for generating UI components (Blazor, GraphQL, gRPC) from entity structures.
    /// </summary>
    public class UiComponentGeneratorHelper
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;

        /// <summary>
        /// Initializes a new instance of the UiComponentGeneratorHelper class.
        /// </summary>
        /// <param name="dmeEditor">The DMEEditor instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when dmeEditor is null.</exception>
        public UiComponentGeneratorHelper(IDMEEditor dmeEditor)
        {
            if (dmeEditor == null)
            {
                throw new ArgumentNullException(nameof(dmeEditor), "DMEEditor cannot be null.");
            }

            _dmeEditor = dmeEditor;
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        /// <summary>
        /// Generates GraphQL schema for entities.
        /// </summary>
        /// <param name="entities">The entity structures to generate GraphQL for.</param>
        /// <param name="outputPath">The output file path.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <returns>The generated GraphQL schema code.</returns>
        public string GenerateGraphQLSchema(List<EntityStructure> entities, string outputPath, string namespaceName)
        {
            if (entities == null || entities.Count == 0)
            {
                _helper.LogMessage("UI", "Entities list cannot be null or empty.", Errors.Failed);
                return string.Empty;
            }

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# GraphQL Schema Generated from Entities");
                sb.AppendLine();

                foreach (var entity in entities)
                {
                    if (entity != null && !string.IsNullOrWhiteSpace(entity.EntityName))
                    {
                        sb.AppendLine($"type {entity.EntityName} {{");
                        
                        if (entity.Fields != null)
                        {
                            foreach (var field in entity.Fields)
                            {
                                if (field != null && !string.IsNullOrWhiteSpace(field.fieldname))
                                {
                                    string graphqlType = MapToGraphQLType(field.fieldtype);
                                    sb.AppendLine($"  {field.fieldname}: {graphqlType}");
                                }
                            }
                        }

                        sb.AppendLine("}");
                        sb.AppendLine();
                    }
                }

                string generatedCode = sb.ToString();

                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    var targetDir = Path.GetDirectoryName(outputPath);
                    if (string.IsNullOrWhiteSpace(targetDir))
                    {
                        targetDir = outputPath;
                        outputPath = Path.Combine(outputPath, "schema.graphql");
                    }

                    _helper.EnsureOutputDirectory(targetDir);
                    var success = _helper.WriteToFile(outputPath, generatedCode, "GraphQL Schema");
                    if (success)
                    {
                        _helper.LogMessage("UI", $"GraphQL schema generated: {outputPath}");
                        return outputPath;
                    }
                    return string.Empty;
                }

                return generatedCode;
            }
            catch (Exception ex)
            {
                _helper.LogMessage("UI", $"Error generating GraphQL schema: {ex.Message}", Errors.Failed);
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates a Blazor component for the specified entity.
        /// </summary>
        /// <param name="entity">The entity structure to generate a component for.</param>
        /// <param name="outputPath">The output file path.</param>
        /// <param name="namespaceName">The namespace for the component.</param>
        /// <returns>The file path if outputPath is provided; otherwise, the generated component code as a string.</returns>
        public string GenerateBlazorComponent(EntityStructure entity, string outputPath, string namespaceName = "TheTechIdea.Beep.Components")
        {
            if (entity == null)
            {
                _helper.LogMessage("UI", "Entity cannot be null.", Errors.Failed);
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(entity.EntityName))
            {
                _helper.LogMessage("UI", "Entity name cannot be empty.", Errors.Failed);
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                namespaceName = "TheTechIdea.Beep.Components";
            }

            try
            {
                string componentName = $"{entity.EntityName}Form";
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("@page \"/form\"");
                sb.AppendLine($"@namespace {namespaceName}");
                sb.AppendLine("@inject IBeepService BeepService");
                sb.AppendLine("@inject NavigationManager Navigation");
                sb.AppendLine();
                sb.AppendLine($"<EditForm Model=\"entity\" OnValidSubmit=\"@Save\">");
                sb.AppendLine($"    <DataAnnotationsValidator />");
                sb.AppendLine($"    <ValidationSummary />");
                sb.AppendLine();
                sb.AppendLine($"    <div class=\"container mt-4\">");
                sb.AppendLine($"        <h2>Manage {entity.EntityName}</h2>");
                sb.AppendLine();
                sb.AppendLine($"        <div class=\"card\">");
                sb.AppendLine($"            <div class=\"card-body\">");

                if (entity.Fields != null && entity.Fields.Count > 0)
                {
                    foreach (var field in entity.Fields)
                    {
                        if (field != null && !string.IsNullOrWhiteSpace(field.fieldname))
                        {
                            sb.AppendLine($"                <div class=\"form-group mb-3\">");
                            sb.AppendLine($"                    <label for=\"{field.fieldname}\" class=\"form-label\">{field.fieldname}</label>");
                            
                            if (field.fieldtype.ToLower().Contains("bool"))
                            {
                                sb.AppendLine($"                    <InputCheckbox id=\"{field.fieldname}\" class=\"form-check-input\" @bind-Value=\"entity.{field.fieldname}\" />");
                            }
                            else if (field.fieldtype.ToLower().Contains("int") || field.fieldtype.ToLower().Contains("long") || 
                                     field.fieldtype.ToLower().Contains("decimal") || field.fieldtype.ToLower().Contains("double"))
                            {
                                sb.AppendLine($"                    <InputNumber id=\"{field.fieldname}\" class=\"form-control\" @bind-Value=\"entity.{field.fieldname}\" />");
                            }
                            else if (field.fieldtype.ToLower().Contains("date"))
                            {
                                sb.AppendLine($"                    <InputDate id=\"{field.fieldname}\" class=\"form-control\" @bind-Value=\"entity.{field.fieldname}\" />");
                            }
                            else
                            {
                                sb.AppendLine($"                    <InputText id=\"{field.fieldname}\" class=\"form-control\" @bind-Value=\"entity.{field.fieldname}\" />");
                            }
                            
                            sb.AppendLine($"                </div>");
                        }
                    }
                }

                sb.AppendLine($"            </div>");
                sb.AppendLine($"        </div>");
                sb.AppendLine();
                sb.AppendLine($"        <div class=\"mt-3\">");
                sb.AppendLine($"            <button type=\"submit\" class=\"btn btn-primary\">Save</button>");
                sb.AppendLine($"            <button type=\"button\" class=\"btn btn-secondary\" @onclick=\"Cancel\">Cancel</button>");
                sb.AppendLine($"        </div>");
                sb.AppendLine($"    </div>");
                sb.AppendLine($"</EditForm>");
                sb.AppendLine();
                sb.AppendLine("@code {");
                sb.AppendLine($"    private {entity.EntityName} entity = new();");
                sb.AppendLine($"    private bool isSaving = false;");
                sb.AppendLine();
                sb.AppendLine($"    protected override async Task OnInitializedAsync()");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        entity = new {entity.EntityName}();");
                sb.AppendLine($"    }}");
                sb.AppendLine();
                sb.AppendLine($"    private async Task Save()");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        try");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            isSaving = true;");
                sb.AppendLine($"            var uow = BeepService.DMEEditor.CreateUnitOfWork<{entity.EntityName}>();");
                sb.AppendLine($"            if (entity != null)");
                sb.AppendLine($"            {{");
                sb.AppendLine($"                uow.AddNew(entity);");
                sb.AppendLine($"                uow.Commit();");
                sb.AppendLine($"            }}");
                sb.AppendLine($"            Navigation.NavigateTo(\"/\");");
                sb.AppendLine($"        }}");
                sb.AppendLine($"        catch (Exception ex)");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            // Log error or show user message");
                sb.AppendLine($"            System.Diagnostics.Debug.WriteLine($\"Error saving {entity.EntityName}: {{ex.Message}}\");");
                sb.AppendLine($"        }}");
                sb.AppendLine($"        finally");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            isSaving = false;");
                sb.AppendLine($"        }}");
                sb.AppendLine($"    }}");
                sb.AppendLine();
                sb.AppendLine($"    private void Cancel()");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        Navigation.NavigateTo(\"/\");");
                sb.AppendLine($"    }}");
                sb.AppendLine($"}}");

                string generatedCode = sb.ToString();

                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    var targetDir = Path.GetDirectoryName(outputPath);
                    var filePath = outputPath;
                    if (string.IsNullOrWhiteSpace(targetDir))
                    {
                        targetDir = outputPath;
                        filePath = Path.Combine(outputPath, $"{componentName}.razor");
                    }

                    _helper.EnsureOutputDirectory(targetDir);
                    var success = _helper.WriteToFile(filePath, generatedCode, entity.EntityName);
                    if (success)
                    {
                        _helper.LogMessage("UI", $"Blazor component generated: {filePath}");
                        return filePath;
                    }
                    return string.Empty;
                }

                return generatedCode;
            }
            catch (Exception ex)
            {
                _helper.LogMessage("UI", $"Error generating Blazor component: {ex.Message}", Errors.Failed);
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates Blazor components for multiple entities.
        /// </summary>
        /// <param name="entities">The list of entity structures to generate components for.</param>
        /// <param name="outputPath">The output directory path.</param>
        /// <param name="namespaceName">The namespace for the components.</param>
        /// <returns>A list of generated file paths.</returns>
        public List<string> GenerateBlazorComponents(List<EntityStructure> entities, string outputPath, string namespaceName = "TheTechIdea.Beep.Components")
        {
            var generatedFiles = new List<string>();

            if (entities == null || entities.Count == 0)
            {
                _helper.LogMessage("UI", "Entities list cannot be null or empty.", Errors.Failed);
                return generatedFiles;
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                _helper.LogMessage("UI", "Output path cannot be empty.", Errors.Failed);
                return generatedFiles;
            }

            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                namespaceName = "TheTechIdea.Beep.Components";
            }

            try
            {
                _helper.EnsureOutputDirectory(outputPath);

                foreach (var entity in entities)
                {
                    if (entity != null && !string.IsNullOrWhiteSpace(entity.EntityName))
                    {
                        string fileName = Path.Combine(outputPath, $"{entity.EntityName}Form.razor");
                        string filePath = GenerateBlazorComponent(entity, fileName, namespaceName);
                        if (!string.IsNullOrWhiteSpace(filePath))
                        {
                            generatedFiles.Add(filePath);
                        }
                    }
                }

                _helper.LogMessage("UI", $"Generated Blazor components for {generatedFiles.Count} entities.");
            }
            catch (Exception ex)
            {
                _helper.LogMessage("UI", $"Error generating Blazor components: {ex.Message}", Errors.Failed);
            }

            return generatedFiles;
        }

        /// <summary>
        /// Maps database field type to GraphQL type.
        /// </summary>
        private string MapToGraphQLType(string fieldType)
        {
            if (string.IsNullOrEmpty(fieldType))
                return "String";

            string lowerType = fieldType.ToLower();

            if (lowerType.Contains("int"))
                return "Int";
            if (lowerType.Contains("decimal") || lowerType.Contains("double") || lowerType.Contains("float"))
                return "Float";
            if (lowerType.Contains("bool"))
                return "Boolean";
            if (lowerType.Contains("date") || lowerType.Contains("time"))
                return "DateTime";
            if (lowerType.Contains("guid"))
                return "ID";

            return "String";
        }
    }
}
