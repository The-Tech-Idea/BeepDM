using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools.Interfaces;
using TheTechIdea.Beep.Tools.Helpers;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Tools.Helpers
{
    /// <summary>
    /// Helper class for generating Web API controllers and related components
    /// </summary>
    public class WebApiGeneratorHelper : IWebApiGenerator
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;

        public WebApiGeneratorHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        /// <summary>
        /// Generates Web API controller classes for the provided entities
        /// </summary>
        public List<string> GenerateWebApiControllers(string dataSourceName, List<EntityStructure> entities, 
            string outputPath, string namespaceName = "TheTechIdea.ProjectControllers")
        {
            var generatedFiles = new List<string>();
            
            if (string.IsNullOrWhiteSpace(dataSourceName))
                throw new ArgumentException("Data source name cannot be null or empty", nameof(dataSourceName));
            
            if (entities == null || entities.Count == 0)
                throw new ArgumentException("Entities list cannot be null or empty", nameof(entities));

            var dataSource = _dmeEditor.GetDataSource(dataSourceName);
            if (dataSource == null)
            {
                _helper.LogMessage("ClassCreator", $"Data source {dataSourceName} not found.", Errors.Failed);
                return generatedFiles;
            }

            outputPath = _helper.EnsureOutputDirectory(outputPath);

            foreach (var entity in entities)
            {
                var className = $"{entity.EntityName}Controller";
                var filePath = Path.Combine(outputPath, $"{className}.cs");

                try
                {
                    var controllerCode = GenerateControllerCode(dataSourceName, entity, className, namespaceName);
                    if (_helper.WriteToFile(filePath, controllerCode, entity.EntityName))
                    {
                        generatedFiles.Add(filePath);
                    }
                }
                catch (Exception ex)
                {
                    _helper.LogMessage("ClassCreator", $"Error generating controller for {entity.EntityName}: {ex.Message}", Errors.Failed);
                }
            }

            return generatedFiles;
        }

        /// <summary>
        /// Generates a Web API controller class with data source and entity name as parameters
        /// </summary>
        public string GenerateWebApiControllerForEntityWithParams(string className, string outputPath,
            string namespaceName = "TheTechIdea.ProjectControllers")
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("Class name cannot be null or empty", nameof(className));

            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = Path.Combine(outputPath, $"{className}.cs");

            try
            {
                var controllerCode = GenerateControllerCodeWithParams(className, namespaceName);
                if (_helper.WriteToFile(filePath, controllerCode, className))
                {
                    return filePath;
                }
            }
            catch (Exception ex)
            {
                _helper.LogMessage("ClassCreator", $"Error generating controller: {ex.Message}", Errors.Failed);
            }

            return null;
        }

        /// <summary>
        /// Generates a minimal Web API for entities using .NET 8's Minimal API approach
        /// </summary>
        public string GenerateMinimalWebApi(string outputPath, string namespaceName = "TheTechIdea.ProjectMinimalAPI")
        {
            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = Path.Combine(outputPath, "Program.cs");

            try
            {
                var apiCode = GenerateMinimalApiCode(namespaceName);
                if (_helper.WriteToFile(filePath, apiCode, "MinimalAPI"))
                {
                    return filePath;
                }
            }
            catch (Exception ex)
            {
                _helper.LogMessage("ClassCreator", $"Error generating Minimal API: {ex.Message}", Errors.Failed);
            }

            return null;
        }

        #region Private Helper Methods

        /// <summary>
        /// Generates the code for a Web API controller for a specific entity
        /// </summary>
        private string GenerateControllerCode(string dataSourceName, EntityStructure entity, 
            string className, string namespaceName)
        {
            var sb = new StringBuilder();

            // Add using statements
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using TheTechIdea.Beep.DataBase;");
            sb.AppendLine("using TheTechIdea.Beep.Editor;");
            sb.AppendLine();

            // Namespace and class declaration
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// API Controller for {entity.EntityName} operations");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    [Route(\"api/[controller]\")]");
            sb.AppendLine("    [ApiController]");
            sb.AppendLine($"    public class {className} : ControllerBase");
            sb.AppendLine("    {");

            // Private fields
            sb.AppendLine($"        private readonly IDataSource _dataSource;");
            sb.AppendLine();

            // Constructor
            sb.AppendLine($"        public {className}(IDMEEditor dmeEditor)");
            sb.AppendLine("        {");
            sb.AppendLine($"            _dataSource = dmeEditor.GetDataSource(\"{dataSourceName}\") ?? ");
            sb.AppendLine($"                throw new System.Exception(\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate CRUD methods
            GenerateGetAllMethod(sb, entity);
            GenerateGetByIdMethod(sb, entity);
            GenerateCreateMethod(sb, entity);
            GenerateUpdateMethod(sb, entity);
            GenerateDeleteMethod(sb, entity);

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Generates controller code with parameters for dynamic entities
        /// </summary>
        private string GenerateControllerCodeWithParams(string className, string namespaceName)
        {
            var sb = new StringBuilder();

            // Add using statements
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using TheTechIdea.Beep.DataBase;");
            sb.AppendLine("using TheTechIdea.Beep.Editor;");
            sb.AppendLine();

            // Namespace and class declaration
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Generic API Controller for dynamic entity operations");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    [Route(\"api/[controller]\")]");
            sb.AppendLine("    [ApiController]");
            sb.AppendLine($"    public class {className} : ControllerBase");
            sb.AppendLine("    {");

            // Private fields
            sb.AppendLine($"        private readonly IDMEEditor _dmeEditor;");
            sb.AppendLine();

            // Constructor
            sb.AppendLine($"        public {className}(IDMEEditor dmeEditor)");
            sb.AppendLine("        {");
            sb.AppendLine($"            _dmeEditor = dmeEditor;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate parameterized CRUD methods
            GenerateParameterizedCrudMethods(sb);

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Generates minimal API code using .NET 8 patterns
        /// </summary>
        private string GenerateMinimalApiCode(string namespaceName)
        {
            var sb = new StringBuilder();

            // Add using statements
            sb.AppendLine("using Microsoft.AspNetCore.Builder;");
            sb.AppendLine("using Microsoft.AspNetCore.Http;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using TheTechIdea.Beep.DataBase;");
            sb.AppendLine("using TheTechIdea.Beep.Editor;");
            sb.AppendLine();

            // Namespace and Program class
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    public static class Program");
            sb.AppendLine("    {");
            sb.AppendLine("        public static async Task Main(string[] args)");
            sb.AppendLine("        {");
            sb.AppendLine("            var builder = WebApplication.CreateBuilder(args);");
            sb.AppendLine();

            // Service registration
            sb.AppendLine("            // Register services");
            sb.AppendLine("            builder.Services.AddSingleton<IDMEEditor>(provider => ");
            sb.AppendLine("                {");
            sb.AppendLine("                    // Initialize your DMEEditor here");
            sb.AppendLine("                    return new DMEEditor();");
            sb.AppendLine("                });");
            sb.AppendLine();

            sb.AppendLine("            var app = builder.Build();");
            sb.AppendLine();

            // Generate minimal API endpoints
            GenerateMinimalApiEndpoints(sb);

            sb.AppendLine("            await app.RunAsync();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Generates GetAll method for controller
        /// </summary>
        private void GenerateGetAllMethod(StringBuilder sb, EntityStructure entity)
        {
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Gets all {entity.EntityName} records");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        [HttpGet]");
            sb.AppendLine($"        public async Task<ActionResult<IEnumerable<object>>> GetAll()");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine($"                var result = await Task.Run(() => _dataSource.GetEntity(\"{entity.EntityName}\", null));");
            sb.AppendLine("                return Ok(result);");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (System.Exception ex)");
            sb.AppendLine("            {");
            sb.AppendLine("                return StatusCode(500, $\"Internal server error: {ex.Message}\");");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        /// <summary>
        /// Generates GetById method for controller
        /// </summary>
        private void GenerateGetByIdMethod(StringBuilder sb, EntityStructure entity)
        {
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Gets a {entity.EntityName} by ID");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        [HttpGet(\"{{id}}\")]");
            sb.AppendLine($"        public async Task<ActionResult<object>> GetById(int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                var filters = new List<AppFilter> { ");
            sb.AppendLine("                    new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() } ");
            sb.AppendLine("                };");
            sb.AppendLine($"                var result = await Task.Run(() => _dataSource.GetEntity(\"{entity.EntityName}\", filters));");
            sb.AppendLine("                if (result == null) return NotFound();");
            sb.AppendLine("                return Ok(result);");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (System.Exception ex)");
            sb.AppendLine("            {");
            sb.AppendLine("                return StatusCode(500, $\"Internal server error: {ex.Message}\");");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        /// <summary>
        /// Generates Create method for controller
        /// </summary>
        private void GenerateCreateMethod(StringBuilder sb, EntityStructure entity)
        {
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Creates a new {entity.EntityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine("        [HttpPost]");
            sb.AppendLine("        public async Task<ActionResult> Create([FromBody] object newItem)");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                if (newItem == null) return BadRequest(\"Item cannot be null\");");
            sb.AppendLine($"                await Task.Run(() => _dataSource.InsertEntity(\"{entity.EntityName}\", newItem));");
            sb.AppendLine($"                return CreatedAtAction(nameof(GetById), new {{ id = newItem.GetType().GetProperty(\"Id\")?.GetValue(newItem) }}, newItem);");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (System.Exception ex)");
            sb.AppendLine("            {");
            sb.AppendLine("                return StatusCode(500, $\"Internal server error: {ex.Message}\");");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        /// <summary>
        /// Generates Update method for controller
        /// </summary>
        private void GenerateUpdateMethod(StringBuilder sb, EntityStructure entity)
        {
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Updates an existing {entity.EntityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine("        [HttpPut(\"{id}\")]");
            sb.AppendLine("        public async Task<IActionResult> Update(int id, [FromBody] object updatedItem)");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                if (updatedItem == null) return BadRequest(\"Item cannot be null\");");
            sb.AppendLine("                var filters = new List<AppFilter> { ");
            sb.AppendLine("                    new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() } ");
            sb.AppendLine("                };");
            sb.AppendLine($"                var existingItem = await Task.Run(() => _dataSource.GetEntity(\"{entity.EntityName}\", filters));");
            sb.AppendLine("                if (existingItem == null) return NotFound();");
            sb.AppendLine($"                await Task.Run(() => _dataSource.UpdateEntity(\"{entity.EntityName}\", updatedItem));");
            sb.AppendLine("                return NoContent();");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (System.Exception ex)");
            sb.AppendLine("            {");
            sb.AppendLine("                return StatusCode(500, $\"Internal server error: {ex.Message}\");");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        /// <summary>
        /// Generates Delete method for controller
        /// </summary>
        private void GenerateDeleteMethod(StringBuilder sb, EntityStructure entity)
        {
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Deletes a {entity.EntityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine("        [HttpDelete(\"{id}\")]");
            sb.AppendLine("        public async Task<IActionResult> Delete(int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                var filters = new List<AppFilter> { ");
            sb.AppendLine("                    new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() } ");
            sb.AppendLine("                };");
            sb.AppendLine($"                var existingItem = await Task.Run(() => _dataSource.GetEntity(\"{entity.EntityName}\", filters));");
            sb.AppendLine("                if (existingItem == null) return NotFound();");
            sb.AppendLine($"                await Task.Run(() => _dataSource.DeleteEntity(\"{entity.EntityName}\", existingItem));");
            sb.AppendLine("                return NoContent();");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (System.Exception ex)");
            sb.AppendLine("            {");
            sb.AppendLine("                return StatusCode(500, $\"Internal server error: {ex.Message}\");");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        /// <summary>
        /// Generates parameterized CRUD methods
        /// </summary>
        private void GenerateParameterizedCrudMethods(StringBuilder sb)
        {
            // GetAll with parameters
            sb.AppendLine($"        [HttpGet(\"{{dataSourceName}}/{{entityName}}\")]");
            sb.AppendLine($"        public async Task<ActionResult<IEnumerable<object>>> GetAll(string dataSourceName, string entityName)");
            sb.AppendLine("        {");
            sb.AppendLine("            var dataSource = _dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("            if (dataSource == null) return NotFound($\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("            var result = await Task.Run(() => dataSource.GetEntity(entityName, null));");
            sb.AppendLine("            return Ok(result);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Additional parameterized methods would follow similar pattern...
        }

        /// <summary>
        /// Generates minimal API endpoints
        /// </summary>
        private void GenerateMinimalApiEndpoints(StringBuilder sb)
        {
            sb.AppendLine("            // GET all entities endpoint");
            sb.AppendLine("            app.MapGet(\"/api/{dataSourceName}/{entityName}\", ");
            sb.AppendLine("                async (string dataSourceName, string entityName, IDMEEditor dmeEditor) =>");
            sb.AppendLine("                {");
            sb.AppendLine("                    var dataSource = dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("                    if (dataSource == null)");
            sb.AppendLine("                        return Results.NotFound($\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("                    var result = await Task.Run(() => dataSource.GetEntity(entityName, null));");
            sb.AppendLine("                    return Results.Ok(result);");
            sb.AppendLine("                });");
            sb.AppendLine();

            // Additional minimal API endpoints would follow...
        }

        #endregion
    }
}