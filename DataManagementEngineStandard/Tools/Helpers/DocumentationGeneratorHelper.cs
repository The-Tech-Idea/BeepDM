using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using System.Linq;

namespace TheTechIdea.Beep.Tools.Helpers
{
    /// <summary>
    /// Helper class for generating XML documentation and diff reports for entities.
    /// </summary>
    public class DocumentationGeneratorHelper
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;

        /// <summary>
        /// Initializes a new instance of the DocumentationGeneratorHelper class.
        /// </summary>
        /// <param name="dmeEditor">The DMEEditor instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when dmeEditor is null.</exception>
        public DocumentationGeneratorHelper(IDMEEditor dmeEditor)
        {
            if (dmeEditor == null)
            {
                throw new ArgumentNullException(nameof(dmeEditor), "DMEEditor cannot be null.");
            }

            _dmeEditor = dmeEditor;
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        /// <summary>
        /// Generates XML documentation for the specified entity.
        /// </summary>
        /// <param name="entity">The entity structure to generate documentation for.</param>
        /// <param name="outputPath">The output file path. If null, returns generated documentation as string.</param>
        /// <returns>The file path if outputPath is provided; otherwise, the generated documentation as a string.</returns>
        public string GenerateEntityDocumentation(EntityStructure entity, string outputPath)
        {
            if (entity == null)
            {
                _helper.LogMessage("DocGen", "Entity cannot be null.", Errors.Failed);
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(entity.EntityName))
            {
                _helper.LogMessage("DocGen", "Entity name cannot be empty.", Errors.Failed);
                return string.Empty;
            }

            try
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sb.AppendLine("<doc>");
                sb.AppendLine("  <assembly>");
                sb.AppendLine($"    <name>TheTechIdea.Beep</name>");
                sb.AppendLine("  </assembly>");
                sb.AppendLine("  <members>");
                sb.AppendLine($"    <member name=\"T:{entity.EntityName}\">");
                sb.AppendLine($"      <summary>");
                sb.AppendLine($"      {entity.EntityName} entity documentation.");
                sb.AppendLine($"      </summary>");
                sb.AppendLine($"      <remarks>");
                sb.AppendLine($"      TODO: Add detailed description of the {entity.EntityName} entity.");
                sb.AppendLine($"      </remarks>");
                sb.AppendLine($"    </member>");

                // Add documentation for properties
                if (entity.Fields != null && entity.Fields.Count > 0)
                {
                    foreach (var field in entity.Fields)
                    {
                        if (field != null && !string.IsNullOrWhiteSpace(field.fieldname))
                        {
                            sb.AppendLine($"    <member name=\"P:{entity.EntityName}.{field.fieldname}\">");
                            sb.AppendLine($"      <summary>");
                            sb.AppendLine($"      Gets or sets the {field.fieldname} property.");
                            sb.AppendLine($"      </summary>");
                            sb.AppendLine($"      <remarks>");
                            sb.AppendLine($"      TODO: Add detailed description for {field.fieldname}.");
                            sb.AppendLine($"      </remarks>");
                            sb.AppendLine($"      <value>Type: {field.fieldtype}</value>");
                            sb.AppendLine($"    </member>");
                        }
                    }
                }

                sb.AppendLine("  </members>");
                sb.AppendLine("</doc>");

                string generatedCode = sb.ToString();

                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    _helper.EnsureOutputDirectory(outputPath);
                    var success = _helper.WriteToFile(outputPath, generatedCode, entity.EntityName);
                    if (success)
                    {
                        _helper.LogMessage("DocGen", $"Entity documentation generated: {outputPath}");
                        return outputPath;
                    }
                    return string.Empty;
                }

                return generatedCode;
            }
            catch (Exception ex)
            {
                _helper.LogMessage("DocGen", $"Error generating entity documentation: {ex.Message}", Errors.Failed);
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates a diff report between two versions of an entity.
        /// </summary>
        /// <param name="originalEntity">The original entity structure.</param>
        /// <param name="newEntity">The new entity structure.</param>
        /// <returns>The diff report as a string.</returns>
        public string GenerateEntityDiffReport(EntityStructure originalEntity, EntityStructure newEntity)
        {
            if (originalEntity == null || newEntity == null)
            {
                _helper.LogMessage("DocGen", "Both entities cannot be null.", Errors.Failed);
                return string.Empty;
            }

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Entity Diff Report: {originalEntity.EntityName}");
                sb.AppendLine(new string('=', 50));
                sb.AppendLine();
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                // Compare entity names
                if (originalEntity.EntityName != newEntity.EntityName)
                {
                    sb.AppendLine($"CHANGED: Entity Name");
                    sb.AppendLine($"  Old: {originalEntity.EntityName}");
                    sb.AppendLine($"  New: {newEntity.EntityName}");
                    sb.AppendLine();
                }

                // Compare fields
                sb.AppendLine("Field Analysis:");
                sb.AppendLine(new string('-', 50));

                var originalFields = originalEntity.Fields ?? new List<EntityField>();
                var newFields = newEntity.Fields ?? new List<EntityField>();

                // Find removed fields
                var removedFields = originalFields.Where(f => !newFields.Any(nf => nf.fieldname == f.fieldname)).ToList();
                if (removedFields.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("REMOVED FIELDS:");
                    foreach (var field in removedFields)
                    {
                        sb.AppendLine($"  - {field.fieldname} ({field.fieldtype})");
                    }
                }

                // Find added fields
                var addedFields = newFields.Where(f => !originalFields.Any(of => of.fieldname == f.fieldname)).ToList();
                if (addedFields.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("ADDED FIELDS:");
                    foreach (var field in addedFields)
                    {
                        sb.AppendLine($"  + {field.fieldname} ({field.fieldtype})");
                    }
                }

                // Find modified fields
                var modifiedFields = newFields.Where(f => 
                    originalFields.Any(of => of.fieldname == f.fieldname && of.fieldtype != f.fieldtype)).ToList();
                if (modifiedFields.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("MODIFIED FIELDS:");
                    foreach (var field in modifiedFields)
                    {
                        var originalField = originalFields.FirstOrDefault(f => f.fieldname == field.fieldname);
                        if (originalField != null)
                        {
                            sb.AppendLine($"  ~ {field.fieldname}");
                            sb.AppendLine($"    Type changed from {originalField.fieldtype} to {field.fieldtype}");
                        }
                    }
                }

                // Summary
                sb.AppendLine();
                sb.AppendLine("Summary:");
                sb.AppendLine($"  Total Fields (Original): {originalFields.Count}");
                sb.AppendLine($"  Total Fields (New): {newFields.Count}");
                sb.AppendLine($"  Added: {addedFields.Count}");
                sb.AppendLine($"  Removed: {removedFields.Count}");
                sb.AppendLine($"  Modified: {modifiedFields.Count}");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _helper.LogMessage("DocGen", $"Error generating entity diff report: {ex.Message}", Errors.Failed);
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates XML documentation for multiple entities.
        /// </summary>
        /// <param name="entities">The list of entity structures to generate documentation for.</param>
        /// <param name="outputPath">The output directory path.</param>
        /// <returns>A list of generated file paths.</returns>
        public List<string> GenerateEntityDocumentations(List<EntityStructure> entities, string outputPath)
        {
            var generatedFiles = new List<string>();

            if (entities == null || entities.Count == 0)
            {
                _helper.LogMessage("DocGen", "Entities list cannot be null or empty.", Errors.Failed);
                return generatedFiles;
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                _helper.LogMessage("DocGen", "Output path cannot be empty.", Errors.Failed);
                return generatedFiles;
            }

            try
            {
                _helper.EnsureOutputDirectory(outputPath);

                foreach (var entity in entities)
                {
                    if (entity != null && !string.IsNullOrWhiteSpace(entity.EntityName))
                    {
                        string fileName = Path.Combine(outputPath, $"{entity.EntityName}Documentation.xml");
                        string filePath = GenerateEntityDocumentation(entity, fileName);
                        if (!string.IsNullOrWhiteSpace(filePath))
                        {
                            generatedFiles.Add(filePath);
                        }
                    }
                }

                _helper.LogMessage("DocGen", $"Generated documentation for {generatedFiles.Count} entities.");
            }
            catch (Exception ex)
            {
                _helper.LogMessage("DocGen", $"Error generating entity documentations: {ex.Message}", Errors.Failed);
            }

            return generatedFiles;
        }
    }
}
