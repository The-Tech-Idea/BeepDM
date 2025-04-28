using System;

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TheTechIdea.Beep.DataBase;

using System.Threading;
using TheTechIdea.Beep.Utilities;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Roslyn;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Helpers;

namespace TheTechIdea.Beep.Tools
{
    public class ClassCreator : IClassCreator
    {
        public string outputFileName { get; set; }
        public string outputpath { get; set; }

        public IDMEEditor DMEEditor { get; set; }

       public ClassCreator(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
        }
        public void CompileClassFromText(string SourceString, string output)
        {
            //List<string> retval = CompileCode(provider, new List<string>() { SourceString }, output);
            //if (retval.Count > 0)
            //{
            //    for (int i = 0; i < retval.Count - 1; i++)
            //    {
            //        DMEEditor.AddLogMessage("Beep Class Creator", retval[i], DateTime.Now, i, null, TheTechIdea.Util.Errors.Failed);
            //    }

            //}
            if (!RoslynCompiler.CompileClassFromStringToDLL(SourceString, output))
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Compiling Code ", DateTime.Now, -1, null, Errors.Failed);
            }

        }
        public string CreateDLL(string dllname, List<EntityStructure> entities, string outputpath, IProgress<PassedArgs> progress, CancellationToken token, string NameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true)
        {
            List<string> listofpaths = new List<string>();
            int i = 1;
            int total = entities.Count;
            try
            {
                foreach (EntityStructure item in entities)
                {

                    try
                    {
                        listofpaths.Add(Path.Combine(outputpath, item.EntityName + ".cs"));
                        CreateClass(item.EntityName, item.Fields, outputpath, NameSpacestring);
                        if (progress != null)
                        {
                            PassedArgs ps = new PassedArgs { ParameterString1 = $"Created class {item.EntityName}", EventType = "Update", ParameterInt1 = i, ParameterInt2 = total };
                            progress.Report(ps);

                        }
                    }
                    catch (Exception ex)
                    {

                        if (progress != null)
                        {
                            PassedArgs ps = new PassedArgs { ParameterString1 = $"Error in Creating class for {item.EntityName}", EventType = "Error", ParameterInt1 = i, ParameterInt2 = total, Messege = ex.Message };
                            progress.Report(ps);

                        }
                    }

                    i++;
                }
                outputFileName = dllname + ".dll";
                if (outputpath == null)
                {
                    outputpath = Assembly.GetEntryAssembly().Location + "\\";
                }
                if (progress != null)
                {
                    PassedArgs ps = new PassedArgs { ParameterString1 = "Creating DLL", EventType = "Update", ParameterInt1 = i, ParameterInt2 = total };
                    progress.Report(ps);

                }
                string ret = "ok";
                //List<string> retval = CompileCode(provider, listofpaths, Path.Combine(outputpath, dllname + ".dll"));
                if (!RoslynCompiler.CompileCodeToDLL(listofpaths, Path.Combine(outputpath, dllname + ".dll")))
                {
                    DMEEditor.AddLogMessage("Beep", $"Error in Compiling Code ", DateTime.Now, -1, null, Errors.Failed);
                }
                return ret;
            }
            catch (Exception ex)
            {

                return ex.Message;
            }

        }
        public string CreateDLLFromFilesPath(string dllname, string filepath, string outputpath, IProgress<PassedArgs> progress, CancellationToken token, string NameSpacestring = "TheTechIdea.ProjectClasses")
        {

            List<string> listofpaths = new List<string>();
         //   options.BracingStyle = "C";
            int i = 1;
            int total = Directory.GetFiles(filepath, "*.cs").Count();
            try
            {
                foreach (string item in Directory.GetFiles(filepath, "*.cs"))
                {

                    try
                    {
                        listofpaths.Add(Path.Combine(outputpath, item));

                        if (progress != null)
                        {
                            PassedArgs ps = new PassedArgs { ParameterString1 = $"Created class {item}", EventType = "Update", ParameterInt1 = i, ParameterInt2 = total };
                            progress.Report(ps);

                        }
                    }
                    catch (Exception ex)
                    {

                        if (progress != null)
                        {
                            PassedArgs ps = new PassedArgs { ParameterString1 = $"Error in Creating class for {item}", EventType = "Error", ParameterInt1 = i, ParameterInt2 = total, Messege = ex.Message };
                            progress.Report(ps);

                        }
                    }

                    i++;
                }
                outputFileName = dllname + ".dll";
                if (outputpath == null)
                {
                    outputpath = Assembly.GetEntryAssembly().Location + "\\";
                }
                if (progress != null)
                {
                    PassedArgs ps = new PassedArgs { ParameterString1 = "Creating DLL", EventType = "Update", ParameterInt1 = i, ParameterInt2 = total };
                    progress.Report(ps);

                }
                string ret = "ok";
                if (!RoslynCompiler.CompileCodeToDLL(listofpaths, Path.Combine(outputpath, dllname + ".dll")))
                {
                    DMEEditor.AddLogMessage("Beep", $"Error in Compiling Code ", DateTime.Now, -1, null, Errors.Failed);
                }
                return ret;
            }
            catch (Exception ex)
            {

                return ex.Message;
            }

        }
        public string CreateClass(string classname, List<EntityField> flds, string poutputpath, string NameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true)
        {
            EntityStructure entity = new EntityStructure();
            entity.EntityName = flds.ElementAt(0).EntityName;
            entity.Fields = flds;

              return CreatePOCOClass(classname, entity, null, null, null, null, NameSpacestring, GenerateCSharpCodeFiles);


        }
        public string CreateClass(string classname, EntityStructure entity, string poutputpath, string NameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true)
        {
            return CreatePOCOClass(classname,entity, null, null, null, null, NameSpacestring, GenerateCSharpCodeFiles); 
        }
        public void GenerateCSharpCode(string fileName)
        {

            if (!RoslynCompiler.CompileFile(fileName))
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Compiling Code ", DateTime.Now, -1, null, Errors.Failed);
            }
        }
        private string GenerateFieldTemplate(EntityField field, string template)
        {
            string extractedTemplate = template.Replace(":FIELDNAME", field.fieldname)
                                                .Replace(":FIELDTYPE", field.fieldtype + "?");
            return extractedTemplate;
        }

        #region "Create Classes"
        public string CreatePOCOClass(string classname,EntityStructure entity, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true)
        {
            return CreateClassFromTemplate(classname,entity, "public  :FIELDTYPE :FIELDNAME {get;set;}", usingheader, implementations, extracode, outputpath, nameSpacestring, GenerateCSharpCodeFiles);
        }
        public string CreateINotifyClass(EntityStructure entity, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true)
        {
            string implementations2 = "";
            if (string.IsNullOrEmpty(implementations))
            {
                implementations2 = " INotifyPropertyChanged ";
            }
            else
            {
                implementations2 = implementations+"," + " INotifyPropertyChanged ";
            }
            
            string extracode2 = Environment.NewLine + " public event PropertyChangedEventHandler PropertyChanged;\r\n\r\n    // This method is called by the Set accessor of each property.\r\n    // The CallerMemberName attribute that is applied to the optional propertyName\r\n    // parameter causes the property name of the caller to be substituted as an argument.\r\n    private void NotifyPropertyChanged([CallerMemberName] string propertyName = \"\")\r\n    {\r\n        if (PropertyChanged != null)\r\n        {\r\n            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));\r\n        }\r\n    }";
            string template2 = Environment.NewLine + " private :FIELDTYPE :FIELDNAMEValue ;";
            template2 += Environment.NewLine + " public :FIELDTYPE :FIELDNAME\r\n    {\r\n        get\r\n        {\r\n            return this.:FIELDNAMEValue;\r\n        }\r\n\r\n        set\r\n        {\r\n    this.:FIELDNAMEValue = value;\r\n                NotifyPropertyChanged();\r\n           \n        }\r\n    }";
            return CreateClassFromTemplate(entity.EntityName,entity, template2, usingheader, implementations2, extracode2, outputpath, nameSpacestring, GenerateCSharpCodeFiles);
        }
        public string CreatEntityClass(
               EntityStructure entity,
               string usingHeader,
               string extraCode,            // ignored; we’ll use our own notification boilerplate
               string outputPath,
               string namespaceString = "TheTechIdea.ProjectClasses",
               bool generateFiles = true
           )
        {
            // inherit both your base Entity and INotifyPropertyChanged
            const string implementations = "Entity, INotifyPropertyChanged";

            // always inject full INotifyPropertyChanged boilerplate:
            var notificationCode = @"
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
";

            // per-field template (we ignore the old template2 completely)
            var fieldTemplate = @"
private :FIELDTYPE _ :FIELDNAMEValue;

public :FIELDTYPE :FIELDNAME
{
    get => _ :FIELDNAMEValue;
    set => SetProperty(ref _ :FIELDNAMEValue, value);
}
";

            return CreateClassFromTemplate(
                classname: entity.EntityName,
                entity: entity,
                template: fieldTemplate,
                usingheader: usingHeader,
                implementations: implementations,
                extracode: notificationCode,
                outputpath: outputPath,
                nameSpacestring: namespaceString,
                GenerateCSharpCodeFiles: generateFiles
            );
        }
        public string CreateClassFromTemplate(
          string classname,
          EntityStructure entity,
          string template,
          string usingheader,
          string implementations,
          string extracode,
          string outputpath,
          string nameSpacestring = "TheTechIdea.ProjectClasses",
          bool GenerateCSharpCodeFiles = true
      )
        {
            // choose class name
            var cls = string.IsNullOrWhiteSpace(classname)
                ? entity.EntityName
                : classname;

            var sb = new StringBuilder();

            sb.AppendLine(usingheader);
            sb.AppendLine();
            sb.AppendLine($"namespace {nameSpacestring}");
            sb.AppendLine("{");

            var inherit = string.IsNullOrWhiteSpace(implementations)
                ? ""
                : $" : {implementations}";
            sb.AppendLine($"    public class {cls}{inherit}");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {cls}() {{ }}");
            sb.AppendLine();

            for (int i = 0; i < entity.Fields.Count; i++)
            {
                var fld = entity.Fields[i];

                // 1) make a safe name (alphanumerics + underscore)
                var raw = fld.fieldname ?? "";
                var safe = Regex.Replace(raw, @"[^A-Za-z0-9_]", "_");

                // 2) if it's now empty, give it a default
                if (string.IsNullOrWhiteSpace(safe))
                    safe = "Field" + i;

                // 3) if it starts with a digit, prefix underscore
                if (char.IsDigit(safe[0]))
                    safe = "_" + safe;

                // 4) build a lower-camel backing field
                var prop = safe;
                var back = "_" +
                    char.ToLowerInvariant(prop[0]) +
                    (prop.Length > 1 ? prop.Substring(1) : "") +
                    "Value";

                // emit backing field
                sb.AppendLine($"        private {fld.fieldtype}? {back};");
                sb.AppendLine();

                // emit property
                sb.AppendLine($"        public {fld.fieldtype}? {prop}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => {back};");
                sb.AppendLine($"            set => SetProperty(ref {back}, value);");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            // inject INotifyPropertyChanged boilerplate if provided
            if (!string.IsNullOrWhiteSpace(extracode))
            {
                foreach (var line in extracode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                    sb.AppendLine("        " + line.TrimEnd());
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var result = sb.ToString();

            if (GenerateCSharpCodeFiles)
            {
                var file = string.IsNullOrWhiteSpace(outputpath)
                    ? Path.Combine(this.DMEEditor.ConfigEditor.Config.ScriptsPath, $"{cls}.cs")
                    : Path.Combine(outputpath, $"{cls}.cs");
                File.WriteAllText(file, result);
            }

            return result;
        }


        public Assembly CreateAssemblyFromCode(string code)
        {
            Assembly assembly = null;
            try
            {
                assembly = RoslynCompiler.CreateAssembly(DMEEditor, code);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep ML.NET", $" Error Compiling Code {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return assembly;
        }
        public Type CreateTypeFromCode(string code, string outputtypename)
        {
            Type OutputType = null;
            Assembly assembly = null;
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                assembly = CreateAssemblyFromCode(code);
                OutputType = assembly.GetType(outputtypename);

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep ML.NET", $" Error Compiling Code {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return OutputType;
        }
        #endregion
        private void LogMessage(string category, string message, Errors errorType = Errors.Ok)
        {
            DMEEditor.AddLogMessage(category, message, DateTime.Now, -1, null, errorType);
        }
        #region "WebApi Generator"
        /// <summary>
        /// Generates Web API controller classes for the provided entities in a specified data source.
        /// </summary>
        /// <param name="dataSourceName">The name of the data source.</param>
        /// <param name="entities">List of entity structures to generate controllers for.</param>
        /// <param name="outputPath">The directory to save the generated controller files.</param>
        /// <param name="namespaceName">The namespace for the generated controllers.</param>
        /// <returns>A list of paths to the generated controller files.</returns>
        public List<string> GenerateWebApiControllers(string dataSourceName, List<EntityStructure> entities, string outputPath, string namespaceName = "TheTechIdea.ProjectControllers")
        {
            List<string> generatedFiles = new List<string>();
            IDMEEditor editor = DMEEditor;
            IDataSource dataSource = editor.GetDataSource(dataSourceName);

            if (dataSource == null)
            {
                LogMessage("ClassCreator", $"Data source {dataSourceName} not found.", Errors.Failed);
                return generatedFiles;
            }

            foreach (var entity in entities)
            {
                string className = $"{entity.EntityName}Controller";
                string filePath = Path.Combine(outputPath, $"{className}.cs");

                try
                {
                    var controllerCode = GenerateControllerCode(dataSourceName, entity, className, namespaceName);
                    File.WriteAllText(filePath, controllerCode);
                    generatedFiles.Add(filePath);
                    LogMessage("ClassCreator", $"Successfully created Web API controller: {className}", Errors.Ok);
                }
                catch (Exception ex)
                {
                    LogMessage("ClassCreator", $"Error generating controller for {entity.EntityName}: {ex.Message}", Errors.Failed);
                }
            }

            return generatedFiles;
        }

        /// <summary>
        /// Generates the code for a Web API controller for the specified entity.
        /// </summary>
        /// <param name="dataSourceName">The name of the data source.</param>
        /// <param name="entity">The entity structure to generate a controller for.</param>
        /// <param name="className">The name of the controller class.</param>
        /// <param name="namespaceName">The namespace for the controller class.</param>
        /// <returns>The generated C# code for the controller.</returns>
        private string GenerateControllerCode(string dataSourceName, EntityStructure entity, string className, string namespaceName)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using TheTechIdea.Beep.DataBase;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    [Route(\"api/[controller]\")]");
            sb.AppendLine("    [ApiController]");
            sb.AppendLine($"    public class {className} : ControllerBase");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly IDataSource _dataSource;");
            sb.AppendLine("");
            sb.AppendLine($"        public {className}(IDMEEditor dmeEditor)");
            sb.AppendLine("        {");
            sb.AppendLine($"            _dataSource = dmeEditor.GetDataSource(\"{dataSourceName}\") ?? throw new System.Exception(\"Data source not found.\");");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine($"        [HttpGet]");
            sb.AppendLine($"        public async Task<ActionResult<IEnumerable<object>>> GetAll()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var result = await Task.Run(() => _dataSource.GetEntity(\"{entity.EntityName}\", null));");
            sb.AppendLine("            return Ok(result);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine($"        [HttpGet(\"{{id}}\")]");
            sb.AppendLine($"        public async Task<ActionResult<object>> GetById(int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            var filters = new List<AppFilter> { new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() } };");
            sb.AppendLine($"            var result = await Task.Run(() => _dataSource.GetEntity(\"{entity.EntityName}\", filters));");
            sb.AppendLine("            if (result == null)");
            sb.AppendLine("                return NotFound();");
            sb.AppendLine("            return Ok(result);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        [HttpPost]");
            sb.AppendLine("        public async Task<ActionResult> Create([FromBody] object newItem)");
            sb.AppendLine("        {");
            sb.AppendLine($"            await Task.Run(() => _dataSource.InsertEntity(\"{entity.EntityName}\", newItem));");
            sb.AppendLine($"            return CreatedAtAction(nameof(GetById), new {{ id = newItem.GetType().GetProperty(\"Id\")?.GetValue(newItem) }}, newItem);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        [HttpPut(\"{id}\")]");
            sb.AppendLine("        public async Task<IActionResult> Update(int id, [FromBody] object updatedItem)");
            sb.AppendLine("        {");
            sb.AppendLine("            var filters = new List<AppFilter> { new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() } };");
            sb.AppendLine($"            var existingItem = await Task.Run(() => _dataSource.GetEntity(\"{entity.EntityName}\", filters));");
            sb.AppendLine("            if (existingItem == null)");
            sb.AppendLine("                return NotFound();");
            sb.AppendLine($"            await Task.Run(() => _dataSource.UpdateEntity(\"{entity.EntityName}\", updatedItem));");
            sb.AppendLine("            return NoContent();");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        [HttpDelete(\"{id}\")]");
            sb.AppendLine("        public async Task<IActionResult> Delete(int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            var filters = new List<AppFilter> { new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() } };");
            sb.AppendLine($"            var existingItem = await Task.Run(() => _dataSource.GetEntity(\"{entity.EntityName}\", filters));");
            sb.AppendLine("            if (existingItem == null)");
            sb.AppendLine("                return NotFound();");
            sb.AppendLine($"            await Task.Run(() => _dataSource.DeleteEntity(\"{entity.EntityName}\", existingItem));");
            sb.AppendLine("            return NoContent();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
        /// <summary>
        /// Generates a Web API controller class for a single entity, with data source and entity name as parameters in API methods.
        /// </summary>
        /// <param name="className">The name of the controller class to be generated.</param>
        /// <param name="outputPath">The directory to save the generated controller file.</param>
        /// <param name="namespaceName">The namespace for the generated controller.</param>
        /// <returns>The path to the generated controller file.</returns>
        public string GenerateWebApiControllerForEntityWithParams(
            string className,
            string outputPath,
            string namespaceName = "TheTechIdea.ProjectControllers")
        {
            string filePath = Path.Combine(outputPath, $"{className}.cs");

            try
            {
                var controllerCode = GenerateControllerCodeWithParams(className, namespaceName);
                File.WriteAllText(filePath, controllerCode);
                LogMessage("ClassCreator", $"Successfully created Web API controller: {className}", Errors.Ok);
                return filePath;
            }
            catch (Exception ex)
            {
                LogMessage("ClassCreator", $"Error generating controller: {ex.Message}", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Generates the code for a Web API controller with data source and entity name as parameters.
        /// </summary>
        /// <param name="className">The name of the controller class.</param>
        /// <param name="namespaceName">The namespace for the controller class.</param>
        /// <returns>The generated C# code for the controller.</returns>
        private string GenerateControllerCodeWithParams(string className, string namespaceName)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using TheTechIdea.Beep.DataBase;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    [Route(\"api/[controller]\")]");
            sb.AppendLine("    [ApiController]");
            sb.AppendLine($"    public class {className} : ControllerBase");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly IDMEEditor _dmeEditor;");
            sb.AppendLine("");
            sb.AppendLine($"        public {className}(IDMEEditor dmeEditor)");
            sb.AppendLine("        {");
            sb.AppendLine($"            _dmeEditor = dmeEditor;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine($"        [HttpGet(\"{{dataSourceName}}/{{entityName}}\")]");
            sb.AppendLine($"        public async Task<ActionResult<IEnumerable<object>>> GetAll(string dataSourceName, string entityName)");
            sb.AppendLine("        {");
            sb.AppendLine("            var dataSource = _dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("            if (dataSource == null)");
            sb.AppendLine("                return NotFound($\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("");
            sb.AppendLine("            var result = await Task.Run(() => dataSource.GetEntity(entityName, null));");
            sb.AppendLine("            return Ok(result);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine($"        [HttpGet(\"{{dataSourceName}}/{{entityName}}/{{id}}\")]");
            sb.AppendLine($"        public async Task<ActionResult<object>> GetById(string dataSourceName, string entityName, int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            var dataSource = _dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("            if (dataSource == null)");
            sb.AppendLine("                return NotFound($\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("");
            sb.AppendLine("            var filters = new List<AppFilter> { new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() } };");
            sb.AppendLine("            var result = await Task.Run(() => dataSource.GetEntity(entityName, filters));");
            sb.AppendLine("            if (result == null)");
            sb.AppendLine("                return NotFound();");
            sb.AppendLine("            return Ok(result);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        [HttpPost(\"{dataSourceName}/{entityName}\")]");
            sb.AppendLine("        public async Task<ActionResult> Create(string dataSourceName, string entityName, [FromBody] object newItem)");
            sb.AppendLine("        {");
            sb.AppendLine("            var dataSource = _dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("            if (dataSource == null)");
            sb.AppendLine("                return NotFound($\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("");
            sb.AppendLine("            await Task.Run(() => dataSource.InsertEntity(entityName, newItem));");
            sb.AppendLine("            return CreatedAtAction(nameof(GetById), new { dataSourceName, entityName, id = newItem.GetType().GetProperty(\"Id\")?.GetValue(newItem) }, newItem);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        [HttpPut(\"{dataSourceName}/{entityName}/{id}\")]");
            sb.AppendLine("        public async Task<IActionResult> Update(string dataSourceName, string entityName, int id, [FromBody] object updatedItem)");
            sb.AppendLine("        {");
            sb.AppendLine("            var dataSource = _dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("            if (dataSource == null)");
            sb.AppendLine("                return NotFound($\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("");
            sb.AppendLine("            var filters = new List<AppFilter> { new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() } };");
            sb.AppendLine("            var existingItem = await Task.Run(() => dataSource.GetEntity(entityName, filters));");
            sb.AppendLine("            if (existingItem == null)");
            sb.AppendLine("                return NotFound();");
            sb.AppendLine($"            await Task.Run(() => dataSource.UpdateEntity(entityName, updatedItem));");
            sb.AppendLine("            return NoContent();");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        [HttpDelete(\"{dataSourceName}/{entityName}/{id}\")]");
            sb.AppendLine("        public async Task<IActionResult> Delete(string dataSourceName, string entityName, int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            var dataSource = _dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("            if (dataSource == null)");
            sb.AppendLine("                return NotFound($\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("");
            sb.AppendLine("            var filters = new List<AppFilter> { new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() } };");
            sb.AppendLine($"            await Task.Run(() => dataSource.DeleteEntity(entityName, filters));");
            sb.AppendLine("            return NoContent();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine("");

            return sb.ToString();
        }
        /// <summary>
        /// Generates a minimal Web API for an entity using .NET 8's Minimal API approach.
        /// </summary>
        /// <param name="outputPath">The directory to save the generated API file.</param>
        /// <param name="namespaceName">The namespace for the generated API.</param>
        /// <returns>The path to the generated API file.</returns>
        public string GenerateMinimalWebApi(string outputPath, string namespaceName = "TheTechIdea.ProjectMinimalAPI")
        {
            string filePath = Path.Combine(outputPath, "Program.cs");

            try
            {
                var apiCode = GenerateMinimalApiCode(namespaceName);
                File.WriteAllText(filePath, apiCode);
                LogMessage("ClassCreator", $"Successfully created Minimal API in {filePath}", Errors.Ok);
                return filePath;
            }
            catch (Exception ex)
            {
                LogMessage("ClassCreator", $"Error generating Minimal API: {ex.Message}", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Generates the code for a minimal API using .NET 8.
        /// </summary>
        /// <param name="namespaceName">The namespace for the API.</param>
        /// <returns>The generated C# code for the API.</returns>
        private string GenerateMinimalApiCode(string namespaceName)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using Microsoft.AspNetCore.Builder;");
            sb.AppendLine("using Microsoft.AspNetCore.Http;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using TheTechIdea.Beep.DataBase;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    public static class Program");
            sb.AppendLine("    {");
            sb.AppendLine("        public static async Task Main(string[] args)");
            sb.AppendLine("        {");
            sb.AppendLine("            var builder = WebApplication.CreateBuilder(args);");
            sb.AppendLine("            builder.Services.AddSingleton<IDMEEditor, DMEEditor>(); // Assuming DMEEditor is registered here");
            sb.AppendLine("");
            sb.AppendLine("            var app = builder.Build();");
            sb.AppendLine("");
            sb.AppendLine("            app.MapGet(\"/api/{dataSourceName}/{entityName}\", async (string dataSourceName, string entityName, [FromServices] IDMEEditor dmeEditor) =>");
            sb.AppendLine("            {");
            sb.AppendLine("                var dataSource = dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("                if (dataSource == null)");
            sb.AppendLine("                    return Results.NotFound($\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("");
            sb.AppendLine("                var result = await Task.Run(() => dataSource.GetEntity(entityName, null));");
            sb.AppendLine("                return Results.Ok(result);");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            app.MapGet(\"/api/{dataSourceName}/{entityName}/{id}\", async (string dataSourceName, string entityName, int id, [FromServices] IDMEEditor dmeEditor) =>");
            sb.AppendLine("            {");
            sb.AppendLine("                var dataSource = dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("                if (dataSource == null)");
            sb.AppendLine("                    return Results.NotFound($\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("");
            sb.AppendLine("                var filters = new List<AppFilter> { new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() } };");
            sb.AppendLine("                var result = await Task.Run(() => dataSource.GetEntity(entityName, filters));");
            sb.AppendLine("                if (result == null)");
            sb.AppendLine("                    return Results.NotFound();");
            sb.AppendLine("");
            sb.AppendLine("                return Results.Ok(result);");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            app.MapPost(\"/api/{dataSourceName}/{entityName}\", async (string dataSourceName, string entityName, [FromBody] object newItem, [FromServices] IDMEEditor dmeEditor) =>");
            sb.AppendLine("            {");
            sb.AppendLine("                var dataSource = dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("                if (dataSource == null)");
            sb.AppendLine("                    return Results.NotFound($\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("");
            sb.AppendLine("                await Task.Run(() => dataSource.InsertEntity(entityName, newItem));");
            sb.AppendLine("                return Results.Created($\"/api/{dataSourceName}/{entityName}/{newItem.GetType().GetProperty(\"Id\")?.GetValue(newItem)}\", newItem);");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            app.MapPut(\"/api/{dataSourceName}/{entityName}/{id}\", async (string dataSourceName, string entityName, int id, [FromBody] object updatedItem, [FromServices] IDMEEditor dmeEditor) =>");
            sb.AppendLine("            {");
            sb.AppendLine("                var dataSource = dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("                if (dataSource == null)");
            sb.AppendLine("                    return Results.NotFound($\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("");
            sb.AppendLine("                var filters = new List<AppFilter> { new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() } };");
            sb.AppendLine("                var existingItem = await Task.Run(() => dataSource.GetEntity(entityName, filters));");
            sb.AppendLine("                if (existingItem == null)");
            sb.AppendLine("                    return Results.NotFound();");
            sb.AppendLine("");
            sb.AppendLine("                await Task.Run(() => dataSource.UpdateEntity(entityName, updatedItem));");
            sb.AppendLine("                return Results.NoContent();");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            app.MapDelete(\"/api/{dataSourceName}/{entityName}/{id}\", async (string dataSourceName, string entityName, int id, [FromServices] IDMEEditor dmeEditor) =>");
            sb.AppendLine("            {");
            sb.AppendLine("                var dataSource = dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("                if (dataSource == null)");
            sb.AppendLine("                    return Results.NotFound($\"Data source '{dataSourceName}' not found.\");");
            sb.AppendLine("");
            sb.AppendLine("                var filters = new List<AppFilter> { new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() } };");
            sb.AppendLine("                await Task.Run(() => dataSource.DeleteEntity(entityName, filters));");
            sb.AppendLine("                return Results.NoContent();");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            await app.RunAsync();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        #endregion "WebApi Generator"
        /// <summary>
        /// Validates the given EntityStructure to ensure it meets class generation requirements.
        /// </summary>
        /// <param name="entity">The EntityStructure to validate.</param>
        /// <returns>A list of validation errors. If empty, the entity is valid.</returns>
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
        /// Generates a data access layer class for an entity.
        /// </summary>
        /// <param name="entity">The EntityStructure to generate the DAL class for.</param>
        /// <param name="outputPath">The output path to save the class file.</param>
        /// <returns>The path to the generated DAL class file.</returns>
        public string GenerateDataAccessLayer(EntityStructure entity, string outputPath)
        {
            var className = $"{entity.EntityName}Repository";
            var filePath = Path.Combine(outputPath, $"{className}.cs");

            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using TheTechIdea.Beep.DataBase;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {entity.EntityName}.DataAccess");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly IDataSource _dataSource;");
            sb.AppendLine("");
            sb.AppendLine($"        public {className}(IDataSource dataSource)");
            sb.AppendLine("        {");
            sb.AppendLine("            _dataSource = dataSource;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public IEnumerable<object> GetAll()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return _dataSource.GetEntity(\"{entity.EntityName}\", null);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public object GetById(int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            var filters = new List<AppFilter>");
            sb.AppendLine("            {");
            sb.AppendLine("                new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() }");
            sb.AppendLine("            };");
            sb.AppendLine($"            return _dataSource.GetEntity(\"{entity.EntityName}\", filters);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public void Add(object entity)");
            sb.AppendLine("        {");
            sb.AppendLine($"            _dataSource.InsertEntity(\"{entity.EntityName}\", entity);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public void Update(object entity)");
            sb.AppendLine("        {");
            sb.AppendLine($"            _dataSource.UpdateEntity(\"{entity.EntityName}\", entity);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public void Delete(int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            var filters = new List<AppFilter>");
            sb.AppendLine("            {");
            sb.AppendLine("                new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() }");
            sb.AppendLine("            };");
            sb.AppendLine($"            _dataSource.DeleteEntity(\"{entity.EntityName}\", filters);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            LogMessage("ClassCreator", $"Generated DAL for {entity.EntityName} at {filePath}", Errors.Ok);
            return filePath;
        }
        /// <summary>
        /// Generates a unit test class template for an entity.
        /// </summary>
        /// <param name="entity">The EntityStructure to generate the test class for.</param>
        /// <param name="outputPath">The output path to save the test class file.</param>
        /// <returns>The path to the generated test class file.</returns>
        public string GenerateUnitTestClass(EntityStructure entity, string outputPath)
        {
            var className = $"{entity.EntityName}Tests";
            var filePath = Path.Combine(outputPath, $"{className}.cs");

            var sb = new StringBuilder();
            sb.AppendLine("using Xunit;");
            sb.AppendLine("using TheTechIdea.Beep.DataBase;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {entity.EntityName}.Tests");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly IDataSource _mockDataSource;");
            sb.AppendLine("");
            sb.AppendLine($"        public {className}()");
            sb.AppendLine("        {");
            sb.AppendLine("            // Setup mock data source");
            sb.AppendLine("            _mockDataSource = new Mock<IDataSource>().Object;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        [Fact]");
            sb.AppendLine($"        public void Test_GetAll_{entity.EntityName}()");
            sb.AppendLine("        {");
            sb.AppendLine("            // Arrange");
            sb.AppendLine($"            var repository = new {entity.EntityName}Repository(_mockDataSource);");
            sb.AppendLine("");
            sb.AppendLine("            // Act");
            sb.AppendLine("            var result = repository.GetAll();");
            sb.AppendLine("");
            sb.AppendLine("            // Assert");
            sb.AppendLine("            Assert.NotNull(result);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            LogMessage("ClassCreator", $"Generated Unit Test Template for {entity.EntityName} at {filePath}", Errors.Ok);
            return filePath;
        }
        /// <summary>
        /// Generates an EF DbContext class for the given list of entities.
        /// </summary>
        /// <param name="entities">The list of EntityStructures.</param>
        /// <param name="namespaceString">The namespace for the DbContext class.</param>
        /// <param name="outputPath">The output path for the generated DbContext file.</param>
        /// <returns>Path to the generated DbContext file.</returns>
        public string GenerateDbContext(List<EntityStructure> entities, string namespaceString, string outputPath)
        {
            var className = "ApplicationDbContext";
            var filePath = Path.Combine(outputPath, $"{className}.cs");

            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {namespaceString}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className} : DbContext");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {className}(DbContextOptions<{className}> options) : base(options) {{ }}");
            sb.AppendLine("");

            foreach (var entity in entities)
            {
                sb.AppendLine($"        public DbSet<{entity.EntityName}> {entity.EntityName}s {{ get; set; }}");
            }

            sb.AppendLine("");
            sb.AppendLine("        protected override void OnModelCreating(ModelBuilder modelBuilder)");
            sb.AppendLine("        {");
            foreach (var entity in entities)
            {
                sb.AppendLine($"            modelBuilder.Entity<{entity.EntityName}>();");
            }
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            LogMessage("ClassCreator", $"Generated DbContext at {filePath}", Errors.Ok);
            return filePath;
        }
        /// <summary>
        /// Generates EF Core configuration classes for the given entity.
        /// </summary>
        /// <param name="entity">The EntityStructure to generate configuration for.</param>
        /// <param name="namespaceString">The namespace for the configuration class.</param>
        /// <param name="outputPath">The output path for the generated configuration file.</param>
        /// <returns>Path to the generated configuration file.</returns>
        public string GenerateEntityConfiguration(EntityStructure entity, string namespaceString, string outputPath)
        {
            var className = $"{entity.EntityName}Configuration";
            var filePath = Path.Combine(outputPath, $"{className}.cs");

            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {namespaceString}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className} : IEntityTypeConfiguration<{entity.EntityName}>");
            sb.AppendLine("    {");
            sb.AppendLine($"        public void Configure(EntityTypeBuilder<{entity.EntityName}> builder)");
            sb.AppendLine("        {");
            foreach (var field in entity.Fields)
            {
                sb.AppendLine($"            builder.Property(e => e.{field.fieldname}).HasColumnName(\"{field.fieldname}\");");
            }
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            LogMessage("ClassCreator", $"Generated EF Configuration for {entity.EntityName} at {filePath}", Errors.Ok);
            return filePath;
        }
        /// <summary>
        /// Generates a class with C# record type for immutable data models
        /// </summary>
        /// <param name="recordName">Name of the record to create</param>
        /// <param name="entity">Entity structure to base the record on</param>
        /// <param name="outputPath">Output file path</param>
        /// <param name="namespaceName">Namespace to use</param>
        /// <param name="generateFile">Whether to generate physical file</param>
        /// <returns>The generated code as string</returns>
        public string CreateRecordClass(string recordName, EntityStructure entity, string outputPath,
                                      string namespaceName = "TheTechIdea.ProjectClasses",
                                      bool generateFile = true)
        {
            string filepath = Path.Combine(outputPath, $"{recordName}.cs");
            StringBuilder sb = new StringBuilder();

            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;

                // Add using directives
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("");
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");

                // Create the record declaration
                sb.AppendLine($"    public record {recordName}(");

                // Add parameters for each field
                for (int i = 0; i < entity.Fields.Count; i++)
                {
                    EntityField field = entity.Fields[i];
                    string nullableSuffix = field.fieldtype.Contains("string") ? "" : "?";

                    sb.Append($"        {field.fieldtype}{nullableSuffix} {field.fieldname}");

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

                // Write to file if requested
                if (generateFile)
                {
                    File.WriteAllText(filepath, sb.ToString());
                    DMEEditor.AddLogMessage("ClassCreator", $"Generated record class {recordName} at {filepath}", DateTime.Now, 0, null, Errors.Ok);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ClassCreator", $"Error creating record class: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Creates a class with support for nullable reference types
        /// </summary>
        /// <param name="className">Name of the class</param>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace name</param>
        /// <param name="generateNullableAnnotations">Whether to add nullable annotations</param>
        /// <returns>The generated code</returns>
        public string CreateNullableAwareClass(string className, EntityStructure entity, string outputPath,
                                             string namespaceName = "TheTechIdea.ProjectClasses",
                                             bool generateNullableAnnotations = true)
        {
            string filepath = Path.Combine(outputPath, $"{className}.cs");
            StringBuilder sb = new StringBuilder();

            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;

                // Add using directives
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");

                if (generateNullableAnnotations)
                {
                    // Enable nullable reference types 
                    sb.AppendLine("#nullable enable");
                    sb.AppendLine("");
                }

                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
                sb.AppendLine($"    public class {className}");
                sb.AppendLine("    {");

                // Constructor
                sb.AppendLine($"        public {className}()");
                sb.AppendLine("        {");
                sb.AppendLine("        }");
                sb.AppendLine("");

                // Properties with nullable annotations
                foreach (EntityField field in entity.Fields)
                {
                    if (generateNullableAnnotations)
                    {
                        bool isReferenceType = IsReferenceType(field.fieldtype);
                        string nullableAnnotation = isReferenceType ? "?" : "";

                        sb.AppendLine($"        public {field.fieldtype}{nullableAnnotation} {field.fieldname} {{ get; set; }}");
                    }
                    else
                    {
                        sb.AppendLine($"        public {field.fieldtype} {field.fieldname} {{ get; set; }}");
                    }
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");

                if (generateNullableAnnotations)
                {
                    sb.AppendLine("");
                    sb.AppendLine("#nullable restore");
                }

                // Write to file
                File.WriteAllText(filepath, sb.ToString());
                DMEEditor.AddLogMessage("ClassCreator", $"Generated nullable aware class {className} at {filepath}", DateTime.Now, 0, null, Errors.Ok);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ClassCreator", $"Error creating nullable aware class: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Determines if a type is a reference type
        /// </summary>
        /// <param name="typeName">The name of the type</param>
        /// <returns>True if it's a reference type, false otherwise</returns>
        private bool IsReferenceType(string typeName)
        {
            typeName = typeName.ToLower();
            return !(typeName == "int" || typeName == "long" || typeName == "float" ||
                     typeName == "double" || typeName == "decimal" || typeName == "bool" ||
                     typeName == "byte" || typeName == "sbyte" || typeName == "char" ||
                     typeName == "short" || typeName == "ushort" || typeName == "uint" ||
                     typeName == "ulong" || typeName == "int16" || typeName == "int32" ||
                     typeName == "int64" || typeName == "uint16" || typeName == "uint32" ||
                     typeName == "uint64" || typeName == "single" || typeName == "boolean" ||
                     typeName == "datetime" || typeName == "timespan" || typeName == "guid");
        }

        /// <summary>
        /// Creates a domain-driven design style aggregate root class from entity
        /// </summary>
        /// <param name="entity">The entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <returns>Generated code as string</returns>
        public string CreateDDDAggregateRoot(EntityStructure entity, string outputPath,
                                           string namespaceName = "TheTechIdea.ProjectDomain")
        {
            string className = $"{entity.EntityName}Aggregate";
            string filepath = Path.Combine(outputPath, $"{className}.cs");

            StringBuilder sb = new StringBuilder();

            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;

                // Add using directives
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("");
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");

                // Interface for the aggregate root
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// Marker interface for aggregate roots in the domain");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    public interface IAggregateRoot { }");
                sb.AppendLine("");

                // Create the aggregate root class
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// Aggregate root for {entity.EntityName}");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public class {className} : IAggregateRoot");
                sb.AppendLine("    {");

                // Private fields
                foreach (EntityField field in entity.Fields)
                {
                    sb.AppendLine($"        private {field.fieldtype} _{field.fieldname.ToLower()};");
                }
                sb.AppendLine("");

                // Add ID field if not present
                if (!entity.Fields.Any(f => f.fieldname.Equals("Id", StringComparison.OrdinalIgnoreCase)))
                {
                    sb.AppendLine("        private Guid _id;");
                    sb.AppendLine("");
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine("        /// Unique identifier for this aggregate");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine("        public Guid Id => _id;");
                    sb.AppendLine("");
                }

                // Constructor
                sb.AppendLine($"        public {className}(");

                // Constructor parameters
                for (int i = 0; i < entity.Fields.Count; i++)
                {
                    EntityField field = entity.Fields[i];
                    sb.Append($"            {field.fieldtype} {field.fieldname.ToLower()}");

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
                if (!entity.Fields.Any(f => f.fieldname.Equals("Id", StringComparison.OrdinalIgnoreCase)))
                {
                    sb.AppendLine("            _id = Guid.NewGuid();");
                }

                // Set fields from parameters
                foreach (EntityField field in entity.Fields)
                {
                    sb.AppendLine($"            _{field.fieldname.ToLower()} = {field.fieldname.ToLower()};");
                }

                sb.AppendLine("        }");
                sb.AppendLine("");

                // Properties (getters only for immutability)
                foreach (EntityField field in entity.Fields)
                {
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// Gets the {field.fieldname}");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine($"        public {field.fieldtype} {field.fieldname} => _{field.fieldname.ToLower()};");
                    sb.AppendLine("");
                }

                // Domain methods (placeholder)
                sb.AppendLine("        // Domain behavior methods would go here");
                sb.AppendLine("");

                // Factory method
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Factory method to create a new {entity.EntityName}");
                sb.AppendLine("        /// </summary>");
                sb.Append("        public static ");
                sb.Append($"{className} Create(");

                // Factory method parameters
                for (int i = 0; i < entity.Fields.Count; i++)
                {
                    EntityField field = entity.Fields[i];
                    if (!field.fieldname.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append($"{field.fieldtype} {field.fieldname.ToLower()}");

                        if (i < entity.Fields.Count - 1 &&
                            !entity.Fields[i + 1].fieldname.Equals("Id", StringComparison.OrdinalIgnoreCase))
                        {
                            sb.Append(", ");
                        }
                    }
                }
                sb.AppendLine(")");
                sb.AppendLine("        {");

                // Create and return the aggregate
                sb.Append("            return new ");
                sb.Append($"{className}(");

                // Pass constructor parameters
                for (int i = 0; i < entity.Fields.Count; i++)
                {
                    EntityField field = entity.Fields[i];
                    sb.Append($"{field.fieldname.ToLower()}");

                    if (i < entity.Fields.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.AppendLine(");");
                sb.AppendLine("        }");

                sb.AppendLine("    }");
                sb.AppendLine("}");

                // Write to file
                File.WriteAllText(filepath, sb.ToString());
                DMEEditor.AddLogMessage("ClassCreator", $"Generated DDD aggregate root {className} at {filepath}", DateTime.Now, 0, null, Errors.Ok);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ClassCreator", $"Error creating DDD aggregate root: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Generates GraphQL type definitions from entity structures
        /// </summary>
        /// <param name="entities">The entity structures to convert</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <returns>The generated GraphQL schema</returns>
        public string GenerateGraphQLSchema(List<EntityStructure> entities, string outputPath,
                                          string namespaceName = "TheTechIdea.ProjectGraphQL")
        {
            string filePath = Path.Combine(outputPath, "GraphQLSchema.cs");
            StringBuilder sb = new StringBuilder();

            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;

                // Add using directives
                sb.AppendLine("using System;");
                sb.AppendLine("using HotChocolate.Types;");
                sb.AppendLine("using HotChocolate;");
                sb.AppendLine("");
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");

                // Generate GraphQL type classes for each entity
                foreach (EntityStructure entity in entities)
                {
                    string typeName = entity.EntityName + "Type";

                    sb.AppendLine($"    public class {typeName} : ObjectType<{entity.EntityName}>");
                    sb.AppendLine("    {");
                    sb.AppendLine("        protected override void Configure(IObjectTypeDescriptor<" + entity.EntityName + "> descriptor)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            descriptor.Description(\"" + (string.IsNullOrEmpty(entity.Description) ?
                                                                             $"Represents a {entity.EntityName} entity" :
                                                                             entity.Description) + "\");");

                    // Configure fields
                    foreach (EntityField field in entity.Fields)
                    {
                        sb.AppendLine("");
                        sb.AppendLine($"            descriptor.Field(t => t.{field.fieldname})");

                        if (!string.IsNullOrEmpty(field.Description))
                        {
                            sb.AppendLine($"                .Description(\"{field.Description}\")");
                        }

                        // Mark as ID field if it's named Id or has fieldindex 0
                        if (field.fieldname.Equals("Id", StringComparison.OrdinalIgnoreCase) || field.FieldIndex == 0)
                        {
                            sb.AppendLine("                .Type<IdType>()");
                        }

                        sb.AppendLine("                .Name(\"" + field.fieldname.ToCamelCase() + "\");");
                    }

                    sb.AppendLine("        }");
                    sb.AppendLine("    }");
                    sb.AppendLine();
                }

                // Generate Query type
                sb.AppendLine("    public class Query");
                sb.AppendLine("    {");

                foreach (EntityStructure entity in entities)
                {
                    string pluralName = entity.EntityName + "s"; // Simple pluralization

                    sb.AppendLine($"        [GraphQLName(\"{entity.EntityName.ToCamelCase()}\")]");
                    sb.AppendLine($"        public {entity.EntityName} Get{entity.EntityName}(int id)  ;");
                    sb.AppendLine();
                    sb.AppendLine($"        [GraphQLName(\"{pluralName.ToCamelCase()}\")]");
                    sb.AppendLine($"        public IQueryable<{entity.EntityName}> Get{pluralName}()  ;");
                    sb.AppendLine();
                }

                sb.AppendLine("    }");
                sb.AppendLine();

                // Generate Mutation type
                sb.AppendLine("    public class Mutation");
                sb.AppendLine("    {");

                foreach (EntityStructure entity in entities)
                {
                    sb.AppendLine($"        [GraphQLName(\"create{entity.EntityName}\")]");
                    sb.AppendLine($"        public {entity.EntityName} Create{entity.EntityName}({entity.EntityName} input)  ;");
                    sb.AppendLine();
                    sb.AppendLine($"        [GraphQLName(\"update{entity.EntityName}\")]");
                    sb.AppendLine($"        public {entity.EntityName} Update{entity.EntityName}(int id, {entity.EntityName} input)  ;");
                    sb.AppendLine();
                    sb.AppendLine($"        [GraphQLName(\"delete{entity.EntityName}\")]");
                    sb.AppendLine($"        public bool Delete{entity.EntityName}(int id)  ;");
                    sb.AppendLine();
                }

                sb.AppendLine("    }");
                sb.AppendLine();

                // Add extension methods for string conversion to camelCase
                sb.AppendLine("    internal static class StringExtensions");
                sb.AppendLine("    {");
                sb.AppendLine("        public static string ToCamelCase(this string str)");
                sb.AppendLine("        {");
                sb.AppendLine("            if (string.IsNullOrEmpty(str)) return str;");
                sb.AppendLine("            if (str.Length == 1) return str.ToLowerInvariant();");
                sb.AppendLine("            return char.ToLowerInvariant(str[0]) + str.Substring(1);");
                sb.AppendLine("        }");
                sb.AppendLine("    }");

                sb.AppendLine("}");

                // Write to file
                File.WriteAllText(filePath, sb.ToString());
                DMEEditor.AddLogMessage("ClassCreator", $"Generated GraphQL schema at {filePath}", DateTime.Now, 0, null, Errors.Ok);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ClassCreator", $"Error generating GraphQL schema: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Generates repository pattern implementation for an entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <param name="interfaceOnly">Whether to generate interface only</param>
        /// <returns>The generated repository code</returns>
        public string GenerateRepositoryImplementation(EntityStructure entity, string outputPath,
                                                    string namespaceName = "TheTechIdea.ProjectRepositories",
                                                    bool interfaceOnly = false)
        {
            StringBuilder sb = new StringBuilder();
            string repoInterfaceName = $"I{entity.EntityName}Repository";
            string repoClassName = $"{entity.EntityName}Repository";
            string filePath;

            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;

                // Generate interface
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Threading.Tasks;");
                sb.AppendLine("");
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");

                // Interface declaration
                sb.AppendLine($"    public interface {repoInterfaceName}");
                sb.AppendLine("    {");
                sb.AppendLine($"        Task<IEnumerable<{entity.EntityName}>> GetAllAsync();");
                sb.AppendLine($"        Task<{entity.EntityName}> GetByIdAsync(int id);");
                sb.AppendLine($"        Task<{entity.EntityName}> AddAsync({entity.EntityName} entity);");
                sb.AppendLine($"        Task<bool> UpdateAsync({entity.EntityName} entity);");
                sb.AppendLine("        Task<bool> DeleteAsync(int id);");
                sb.AppendLine("    }");

                if (!interfaceOnly)
                {
                    // Repository implementation
                    sb.AppendLine("");
                    sb.AppendLine($"    public class {repoClassName} : {repoInterfaceName}");
                    sb.AppendLine("    {");
                    sb.AppendLine("        private readonly IDataSource _dataSource;");
                    sb.AppendLine("");

                    // Constructor
                    sb.AppendLine($"        public {repoClassName}(IDataSource dataSource)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            _dataSource = dataSource;");
                    sb.AppendLine("        }");
                    sb.AppendLine("");

                    // GetAllAsync
                    sb.AppendLine($"        public async Task<IEnumerable<{entity.EntityName}>> GetAllAsync()");
                    sb.AppendLine("        {");
                    sb.AppendLine("            return await Task.Run(() => {");
                    sb.AppendLine($"                var results = _dataSource.GetEntity(\"{entity.EntityName}\", null);");
                    sb.AppendLine($"                return results as IEnumerable<{entity.EntityName}>;");
                    sb.AppendLine("            });");
                    sb.AppendLine("        }");
                    sb.AppendLine("");

                    // GetByIdAsync
                    sb.AppendLine($"        public async Task<{entity.EntityName}> GetByIdAsync(int id)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            return await Task.Run(() => {");
                    sb.AppendLine("                var filters = new List<AppFilter> {");
                    sb.AppendLine("                    new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() }");
                    sb.AppendLine("                };");
                    sb.AppendLine($"                var result = _dataSource.GetEntity(\"{entity.EntityName}\", filters);");
                    sb.AppendLine($"                return result as {entity.EntityName};");
                    sb.AppendLine("            });");
                    sb.AppendLine("        }");
                    sb.AppendLine("");

                    // AddAsync
                    sb.AppendLine($"        public async Task<{entity.EntityName}> AddAsync({entity.EntityName} entity)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            return await Task.Run(() => {");
                    sb.AppendLine($"                _dataSource.InsertEntity(\"{entity.EntityName}\", entity);");
                    sb.AppendLine("                return entity;");
                    sb.AppendLine("            });");
                    sb.AppendLine("        }");
                    sb.AppendLine("");

                    // UpdateAsync
                    sb.AppendLine($"        public async Task<bool> UpdateAsync({entity.EntityName} entity)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            return await Task.Run(() => {");
                    sb.AppendLine("                try");
                    sb.AppendLine("                {");
                    sb.AppendLine($"                    _dataSource.UpdateEntity(\"{entity.EntityName}\", entity);");
                    sb.AppendLine("                    return true;");
                    sb.AppendLine("                }");
                    sb.AppendLine("                catch");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    return false;");
                    sb.AppendLine("                }");
                    sb.AppendLine("            });");
                    sb.AppendLine("        }");
                    sb.AppendLine("");

                    // DeleteAsync
                    sb.AppendLine("        public async Task<bool> DeleteAsync(int id)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            return await Task.Run(() => {");
                    sb.AppendLine("                try");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    var filters = new List<AppFilter> {");
                    sb.AppendLine("                        new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() }");
                    sb.AppendLine("                    };");
                    sb.AppendLine($"                    _dataSource.DeleteEntity(\"{entity.EntityName}\", filters);");
                    sb.AppendLine("                    return true;");
                    sb.AppendLine("                }");
                    sb.AppendLine("                catch");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    return false;");
                    sb.AppendLine("                }");
                    sb.AppendLine("            });");
                    sb.AppendLine("        }");
                    sb.AppendLine("    }");
                }

                sb.AppendLine("}");

                // Determine file path based on whether we're generating interface only
                if (interfaceOnly)
                {
                    filePath = Path.Combine(outputPath, $"{repoInterfaceName}.cs");
                }
                else
                {
                    filePath = Path.Combine(outputPath, $"{repoClassName}.cs");
                }

                // Write to file
                File.WriteAllText(filePath, sb.ToString());
                DMEEditor.AddLogMessage("ClassCreator", $"Generated repository for {entity.EntityName} at {filePath}", DateTime.Now, 0, null, Errors.Ok);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ClassCreator", $"Error generating repository: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Generates serverless function code (Azure Functions/AWS Lambda) for entity operations
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="cloudProvider">Cloud provider type (Azure, AWS, etc)</param>
        /// <returns>The serverless function code</returns>
        public string GenerateServerlessFunctions(EntityStructure entity, string outputPath,
                                                CloudProviderType cloudProvider = CloudProviderType.Azure)
        {
            string className = $"{entity.EntityName}Functions";
            string filePath = Path.Combine(outputPath, $"{className}.cs");
            StringBuilder sb = new StringBuilder();

            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;

                switch (cloudProvider)
                {
                    case CloudProviderType.Azure:
                        GenerateAzureFunctions(entity, sb);
                        break;
                    case CloudProviderType.AWS:
                        GenerateAWSLambdaFunctions(entity, sb);
                        break;
                    default:
                        DMEEditor.AddLogMessage("ClassCreator", $"Unsupported cloud provider type: {cloudProvider}", DateTime.Now, 0, null, Errors.Failed);
                        return null;
                }

                // Write to file
                File.WriteAllText(filePath, sb.ToString());
                DMEEditor.AddLogMessage("ClassCreator", $"Generated serverless functions for {entity.EntityName} at {filePath}", DateTime.Now, 0, null, Errors.Ok);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ClassCreator", $"Error generating serverless functions: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        private void GenerateAzureFunctions(EntityStructure entity, StringBuilder sb)
        {
            sb.AppendLine("using System;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using Microsoft.Azure.WebJobs;");
            sb.AppendLine("using Microsoft.Azure.WebJobs.Extensions.Http;");
            sb.AppendLine("using Microsoft.AspNetCore.Http;");
            sb.AppendLine("using Microsoft.Extensions.Logging;");
            sb.AppendLine("using Newtonsoft.Json;");
            sb.AppendLine("using TheTechIdea.Beep.DataBase;");
            sb.AppendLine("");
            sb.AppendLine($"namespace TheTechIdea.Functions");
            sb.AppendLine("{");

            // Class declaration
            sb.AppendLine($"    public class {entity.EntityName}Functions");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly IDMEEditor _dmeEditor;");
            sb.AppendLine("");

            // Constructor
            sb.AppendLine($"        public {entity.EntityName}Functions(IDMEEditor dmeEditor)");
            sb.AppendLine("        {");
            sb.AppendLine("            _dmeEditor = dmeEditor;");
            sb.AppendLine("        }");
            sb.AppendLine("");

            // GetAll function
            sb.AppendLine("        [FunctionName(\"Get" + entity.EntityName + "s\")]");
            sb.AppendLine("        public async Task<IActionResult> GetAll(");
            sb.AppendLine("            [HttpTrigger(AuthorizationLevel.Function, \"get\", Route = \"" + entity.EntityName.ToLower() + "s\")] HttpRequest req,");
            sb.AppendLine("            ILogger log)");
            sb.AppendLine("        {");
            sb.AppendLine("            log.LogInformation($\"Getting all " + entity.EntityName + "s\");");
            sb.AppendLine("");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                string dataSourceName = req.Query[\"dataSource\"];");
            sb.AppendLine("                if (string.IsNullOrEmpty(dataSourceName))");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new BadRequestObjectResult(\"Please provide a dataSource query parameter\");");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                var dataSource = _dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("                if (dataSource == null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new NotFoundObjectResult($\"Data source '{dataSourceName}' not found\");");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine($"                var result = await Task.Run(() => dataSource.GetEntity(\"{entity.EntityName}\", null));");
            sb.AppendLine("                return new OkObjectResult(result);");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception ex)");
            sb.AppendLine("            {");
            sb.AppendLine("                log.LogError(ex, $\"Error getting " + entity.EntityName + "s\");");
            sb.AppendLine("                return new StatusCodeResult(StatusCodes.Status500InternalServerError);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");

            // GetById function
            sb.AppendLine("        [FunctionName(\"Get" + entity.EntityName + "ById\")]");
            sb.AppendLine("        public async Task<IActionResult> GetById(");
            sb.AppendLine("            [HttpTrigger(AuthorizationLevel.Function, \"get\", Route = \"" + entity.EntityName.ToLower() + "s/{id}\")] HttpRequest req,");
            sb.AppendLine("            string id,");
            sb.AppendLine("            ILogger log)");
            sb.AppendLine("        {");
            sb.AppendLine("            log.LogInformation($\"Getting " + entity.EntityName + " by id: {id}\");");
            sb.AppendLine("");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                string dataSourceName = req.Query[\"dataSource\"];");
            sb.AppendLine("                if (string.IsNullOrEmpty(dataSourceName))");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new BadRequestObjectResult(\"Please provide a dataSource query parameter\");");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                var dataSource = _dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("                if (dataSource == null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new NotFoundObjectResult($\"Data source '{dataSourceName}' not found\");");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                var filters = new List<AppFilter> { new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id } };");
            sb.AppendLine($"                var result = await Task.Run(() => dataSource.GetEntity(\"{entity.EntityName}\", filters));");
            sb.AppendLine("");
            sb.AppendLine("                if (result == null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new NotFoundResult();");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                return new OkObjectResult(result);");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception ex)");
            sb.AppendLine("            {");
            sb.AppendLine("                log.LogError(ex, $\"Error getting " + entity.EntityName + " by id: {id}\");");
            sb.AppendLine("                return new StatusCodeResult(StatusCodes.Status500InternalServerError);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");

            // Create function
            sb.AppendLine("        [FunctionName(\"Create" + entity.EntityName + "\")]");
            sb.AppendLine("        public async Task<IActionResult> Create(");
            sb.AppendLine("            [HttpTrigger(AuthorizationLevel.Function, \"post\", Route = \"" + entity.EntityName.ToLower() + "s\")] HttpRequest req,");
            sb.AppendLine("            ILogger log)");
            sb.AppendLine("        {");
            sb.AppendLine("            log.LogInformation(\"Creating a new " + entity.EntityName + "\");");
            sb.AppendLine("");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                string dataSourceName = req.Query[\"dataSource\"];");
            sb.AppendLine("                if (string.IsNullOrEmpty(dataSourceName))");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new BadRequestObjectResult(\"Please provide a dataSource query parameter\");");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                var dataSource = _dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("                if (dataSource == null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new NotFoundObjectResult($\"Data source '{dataSourceName}' not found\");");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();");
            sb.AppendLine($"                var item = JsonConvert.DeserializeObject<{entity.EntityName}>(requestBody);");
            sb.AppendLine("");
            sb.AppendLine($"                await Task.Run(() => dataSource.InsertEntity(\"{entity.EntityName}\", item));");
            sb.AppendLine("");
            sb.AppendLine("                return new CreatedResult(\"\", item);");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception ex)");
            sb.AppendLine("            {");
            sb.AppendLine("                log.LogError(ex, \"Error creating " + entity.EntityName + "\");");
            sb.AppendLine("                return new StatusCodeResult(StatusCodes.Status500InternalServerError);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");

            // Update function
            sb.AppendLine("        [FunctionName(\"Update" + entity.EntityName + "\")]");
            sb.AppendLine("        public async Task<IActionResult> Update(");
            sb.AppendLine("            [HttpTrigger(AuthorizationLevel.Function, \"put\", Route = \"" + entity.EntityName.ToLower() + "s/{id}\")] HttpRequest req,");
            sb.AppendLine("            string id,");
            sb.AppendLine("            ILogger log)");
            sb.AppendLine("        {");
            sb.AppendLine("            log.LogInformation($\"Updating " + entity.EntityName + " with id: {id}\");");
            sb.AppendLine("");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                string dataSourceName = req.Query[\"dataSource\"];");
            sb.AppendLine("                if (string.IsNullOrEmpty(dataSourceName))");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new BadRequestObjectResult(\"Please provide a dataSource query parameter\");");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                var dataSource = _dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("                if (dataSource == null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new NotFoundObjectResult($\"Data source '{dataSourceName}' not found\");");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();");
            sb.AppendLine($"                var item = JsonConvert.DeserializeObject<{entity.EntityName}>(requestBody);");
            sb.AppendLine("");
            sb.AppendLine($"                await Task.Run(() => dataSource.UpdateEntity(\"{entity.EntityName}\", item));");
            sb.AppendLine("");
            sb.AppendLine("                return new OkObjectResult(item);");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception ex)");
            sb.AppendLine("            {");
            sb.AppendLine("                log.LogError(ex, $\"Error updating " + entity.EntityName + " with id: {id}\");");
            sb.AppendLine("                return new StatusCodeResult(StatusCodes.Status500InternalServerError);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");

            // Delete function
            sb.AppendLine("        [FunctionName(\"Delete" + entity.EntityName + "\")]");
            sb.AppendLine("        public async Task<IActionResult> Delete(");
            sb.AppendLine("            [HttpTrigger(AuthorizationLevel.Function, \"delete\", Route = \"" + entity.EntityName.ToLower() + "s/{id}\")] HttpRequest req,");
            sb.AppendLine("            string id,");
            sb.AppendLine("            ILogger log)");
            sb.AppendLine("        {");
            sb.AppendLine("            log.LogInformation($\"Deleting " + entity.EntityName + " with id: {id}\");");
            sb.AppendLine("");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                string dataSourceName = req.Query[\"dataSource\"];");
            sb.AppendLine("                if (string.IsNullOrEmpty(dataSourceName))");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new BadRequestObjectResult(\"Please provide a dataSource query parameter\");");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                var dataSource = _dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("                if (dataSource == null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new NotFoundObjectResult($\"Data source '{dataSourceName}' not found\");");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                var filters = new List<AppFilter> { new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id } };");
            sb.AppendLine($"                await Task.Run(() => dataSource.DeleteEntity(\"{entity.EntityName}\", filters));");
            sb.AppendLine("");
            sb.AppendLine("                return new NoContentResult();");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception ex)");
            sb.AppendLine("            {");
            sb.AppendLine("                log.LogError(ex, $\"Error deleting " + entity.EntityName + " with id: {id}\");");
            sb.AppendLine("                return new StatusCodeResult(StatusCodes.Status500InternalServerError);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");
        }

        private void GenerateAWSLambdaFunctions(EntityStructure entity, StringBuilder sb)
        {
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Amazon.Lambda.Core;");
            sb.AppendLine("using Amazon.Lambda.APIGatewayEvents;");
            sb.AppendLine("using Newtonsoft.Json;");
            sb.AppendLine("using TheTechIdea.Beep.DataBase;");
            sb.AppendLine("");
            sb.AppendLine("[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]");
            sb.AppendLine("");
            sb.AppendLine($"namespace TheTechIdea.Lambda");
            sb.AppendLine("{");

            // Class declaration
            sb.AppendLine($"    public class {entity.EntityName}Functions");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly IDMEEditor _dmeEditor;");
            sb.AppendLine("");

            // Constructor
            sb.AppendLine($"        public {entity.EntityName}Functions()");
            sb.AppendLine("        {");
            sb.AppendLine("            // Initialize DMEEditor");
            sb.AppendLine("            // This would typically be done through DI in a real application");
            sb.AppendLine("            _dmeEditor = new DMEEditor();");
            sb.AppendLine("        }");
            sb.AppendLine("");

            // GetAll function
            sb.AppendLine("        public async Task<APIGatewayProxyResponse> GetAll(APIGatewayProxyRequest request, ILambdaContext context)");
            sb.AppendLine("        {");
            sb.AppendLine("            context.Logger.LogLine($\"Getting all " + entity.EntityName + "s\");");
            sb.AppendLine("");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                if (!request.QueryStringParameters.TryGetValue(\"dataSource\", out string dataSourceName))");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new APIGatewayProxyResponse");
            sb.AppendLine("                    {");
            sb.AppendLine("                        StatusCode = 400,");
            sb.AppendLine("                        Body = JsonConvert.SerializeObject(new { message = \"Please provide a dataSource query parameter\" })");
            sb.AppendLine("                    };");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                var dataSource = _dmeEditor.GetDataSource(dataSourceName);");
            sb.AppendLine("                if (dataSource == null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    return new APIGatewayProxyResponse");
            sb.AppendLine("                    {");
            sb.AppendLine("                        StatusCode = 404,");
            sb.AppendLine("                        Body = JsonConvert.SerializeObject(new { message = $\"Data source '{dataSourceName}' not found\" })");
            sb.AppendLine("                    };");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine($"                var result = await Task.Run(() => dataSource.GetEntity(\"{entity.EntityName}\", null));");
            sb.AppendLine("");
            sb.AppendLine("                return new APIGatewayProxyResponse");
            sb.AppendLine("                {");
            sb.AppendLine("                    StatusCode = 200,");
            sb.AppendLine("                    Body = JsonConvert.SerializeObject(result)");
            sb.AppendLine("                };");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception ex)");
            sb.AppendLine("            {");
            sb.AppendLine("                context.Logger.LogLine($\"Error getting " + entity.EntityName + "s: {ex.Message}\");");
            sb.AppendLine("                return new APIGatewayProxyResponse");
            sb.AppendLine("                {");
            sb.AppendLine("                    StatusCode = 500,");
            sb.AppendLine("                    Body = JsonConvert.SerializeObject(new { message = \"Internal server error\" })");
            sb.AppendLine("                };");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");

            // Add other functions (GetById, Create, Update, Delete) following similar pattern
            // but using AWS Lambda API Gateway patterns

            sb.AppendLine("    }");
            sb.AppendLine("}");
        }

        /// <summary>
        /// Generates XML documentation from entity structure
        /// </summary>
        /// <param name="entity">Entity structure to document</param>
        /// <param name="outputPath">Output path</param>
        /// <returns>The XML documentation string</returns>
        public string GenerateEntityDocumentation(EntityStructure entity, string outputPath)
        {
            string filePath = Path.Combine(outputPath, $"{entity.EntityName}Documentation.xml");
            StringBuilder sb = new StringBuilder();

            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;

                // XML header
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

                if (entity.Category != null)
                    sb.AppendLine($"            <remarks>Category: {entity.Category}</remarks>");

                sb.AppendLine("        </member>");

                // Document each field
                foreach (var field in entity.Fields)
                {
                    sb.AppendLine($"        <member name=\"P:{entity.EntityName}.{field.fieldname}\">");
                    sb.AppendLine("            <summary>");
                    sb.AppendLine($"            {(!string.IsNullOrEmpty(field.Description) ? field.Description : $"The {field.fieldname} property")}");
                    sb.AppendLine("            </summary>");
                    sb.AppendLine($"            <value>A {field.fieldtype} value representing {field.fieldname}</value>");

                    // Add validation info if available
                    if (!string.IsNullOrEmpty(field.DefaultValue))
                        sb.AppendLine($"            <remarks>Default value: {field.DefaultValue}</remarks>");

                    if (field.IsRequired)
                        sb.AppendLine("            <remarks>This field is required</remarks>");

                    // Add additional metadata information
                    sb.AppendLine("        </member>");
                }

                // Close XML
                sb.AppendLine("    </members>");
                sb.AppendLine("</doc>");

                // Write to file
                File.WriteAllText(filePath, sb.ToString());
                DMEEditor.AddLogMessage("ClassCreator", $"Generated XML documentation for {entity.EntityName} at {filePath}", DateTime.Now, 0, null, Errors.Ok);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ClassCreator", $"Error generating XML documentation: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
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
            string className = $"{entity.EntityName}Validator";
            string filePath = Path.Combine(outputPath, $"{className}.cs");
            StringBuilder sb = new StringBuilder();

            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;

                // Add using directives
                sb.AppendLine("using FluentValidation;");
                sb.AppendLine("using System;");
                sb.AppendLine("");
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");

                // Class declaration
                sb.AppendLine($"    public class {className} : AbstractValidator<{entity.EntityName}>");
                sb.AppendLine("    {");
                sb.AppendLine($"        public {className}()");
                sb.AppendLine("        {");

                // Add validation rules for each field
                foreach (var field in entity.Fields)
                {
                    sb.AppendLine($"            RuleFor(x => x.{field.fieldname})");

                    // Apply rules based on field type and properties
                    if (field.IsRequired)
                    {
                        if (field.fieldtype.ToLower().Contains("string"))
                        {
                            sb.AppendLine("                .NotEmpty().WithMessage($\"{nameof(" + field.fieldname + ")} is required.\")");
                        }
                        else
                        {
                            sb.AppendLine("                .NotNull().WithMessage($\"{nameof(" + field.fieldname + ")} is required.\")");
                        }
                    }

                    // String-specific validations
                    if (field.fieldtype.ToLower().Contains("string"))
                    {
                        if (field.Size > 0)
                        {
                            sb.AppendLine($"                .MaximumLength({field.Size}).WithMessage($\"{{nameof({field.fieldname})}} must not exceed {field.Size} characters.\")");
                        }
                    }

                    // Numeric validations
                    if (IsNumericType(field.fieldtype))
                    {
                        if (field.ValueMin != null && !string.IsNullOrEmpty(field.ValueMin.ToString()))
                        {
                            sb.AppendLine($"                .GreaterThanOrEqualTo({field.ValueMin}).WithMessage($\"{{nameof({field.fieldname})}} must be greater than or equal to {field.ValueMin}.\")");
                        }

                        if (field.ValueMax != null && !string.IsNullOrEmpty(field.ValueMax.ToString()))
                        {
                            sb.AppendLine($"                .LessThanOrEqualTo({field.ValueMax}).WithMessage($\"{{nameof({field.fieldname})}} must be less than or equal to {field.ValueMax}.\")");
                        }
                    }

                    // Email validation
                    if (field.fieldname.ToLower().Contains("email"))
                    {
                        sb.AppendLine("                .EmailAddress().WithMessage($\"{nameof(" + field.fieldname + ")} must be a valid email address.\")");
                    }

                    sb.AppendLine("                ;"); // End rule
                    sb.AppendLine("");
                }

                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");

                // Write to file
                File.WriteAllText(filePath, sb.ToString());
                DMEEditor.AddLogMessage("ClassCreator", $"Generated FluentValidator for {entity.EntityName} at {filePath}", DateTime.Now, 0, null, Errors.Ok);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ClassCreator", $"Error generating FluentValidation class: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        private bool IsNumericType(string fieldType)
        {
            fieldType = fieldType.ToLower();
            return fieldType == "int" || fieldType == "int32" || fieldType == "int64" ||
                   fieldType == "long" || fieldType == "decimal" || fieldType == "double" ||
                   fieldType == "float" || fieldType == "short" || fieldType == "byte" ||
                   fieldType == "uint" || fieldType == "ulong" || fieldType == "ushort" ||
                   fieldType == "sbyte" || fieldType == "single";
        }

        /// <summary>
        /// Generates Entity Framework Core migration code for entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <returns>The migration code</returns>
        public string GenerateEFCoreMigration(EntityStructure entity, string outputPath,
                                            string namespaceName = "TheTechIdea.ProjectMigrations")
        {
            string migrationName = $"Create{entity.EntityName}Table";
            string className = $"{migrationName}";
            string filePath = Path.Combine(outputPath, $"{className}.cs");
            StringBuilder sb = new StringBuilder();

            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;

                // Add using directives
                sb.AppendLine("using Microsoft.EntityFrameworkCore.Migrations;");
                sb.AppendLine("using Microsoft.EntityFrameworkCore.Metadata;");
                sb.AppendLine("using System;");
                sb.AppendLine("");
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");

                // Class declaration
                sb.AppendLine($"    public partial class {className} : Migration");
                sb.AppendLine("    {");

                // Up method - create tables
                sb.AppendLine("        protected override void Up(MigrationBuilder migrationBuilder)");
                sb.AppendLine("        {");

                // Create table
                sb.AppendLine($"            migrationBuilder.CreateTable(");
                sb.AppendLine($"                name: \"{entity.EntityName}\",");
                sb.AppendLine($"                columns: table => new");
                sb.AppendLine($"                {{");

                // Generate columns based on entity fields
                bool hasPrimaryKey = false;

                foreach (var field in entity.Fields)
                {
                    string columnType = MapFieldTypeToSqlType(field.fieldtype);

                    // Identify primary key field
                    bool isPrimaryKey = field.fieldname.ToLower() == "id" || field.IsKey;

                    if (isPrimaryKey)
                    {
                        hasPrimaryKey = true;

                        sb.Append($"                    table.Column<{field.fieldtype}>(name: \"{field.fieldname}\", nullable: false)");

                        // Add auto-increment for Id column
                        if (field.fieldname.ToLower() == "id" && IsIntegralType(field.fieldtype))
                        {
                            sb.Append($".Annotation(\"SqlServer:ValueGenerationStrategy\", SqlServerValueGenerationStrategy.IdentityColumn)");
                        }

                        sb.AppendLine(",");
                    }
                    else
                    {
                        bool isNullable = !field.IsRequired;
                        sb.AppendLine($"                    table.Column<{field.fieldtype}>(name: \"{field.fieldname}\", nullable: {isNullable.ToString().ToLower()}),");
                    }
                }

                sb.AppendLine($"                }},");

                // Create primary key constraint
                sb.Append($"                constraints: table =>");
                sb.AppendLine($"                {{");

                if (hasPrimaryKey)
                {
                    var pkField = entity.Fields.FirstOrDefault(f => f.fieldname.ToLower() == "id" || f.IsKey);
                    if (pkField != null)
                    {
                        sb.AppendLine($"                    table.PrimaryKey(\"PK_{entity.EntityName}\", x => x.{pkField.fieldname});");
                    }
                }

                sb.AppendLine($"                }});");

                // Create indexes if needed
                var indexableFields = entity.Fields.Where(f => f.IsUnique || f.IsIndexed).ToList();
                if (indexableFields.Any())
                {
                    sb.AppendLine();
                    foreach (var field in indexableFields)
                    {
                        string indexType = field.IsUnique ? "Unique" : "";
                        sb.AppendLine($"            migrationBuilder.CreateIndex(");
                        sb.AppendLine($"                name: \"IX_{entity.EntityName}_{field.fieldname}\",");
                        sb.AppendLine($"                table: \"{entity.EntityName}\",");
                        sb.AppendLine($"                column: \"{field.fieldname}\",");
                        sb.AppendLine($"                unique: {field.IsUnique.ToString().ToLower()});");
                    }
                }

                sb.AppendLine("        }");
                sb.AppendLine();

                // Down method - drop tables
                sb.AppendLine("        protected override void Down(MigrationBuilder migrationBuilder)");
                sb.AppendLine("        {");
                sb.AppendLine($"            migrationBuilder.DropTable(");
                sb.AppendLine($"                name: \"{entity.EntityName}\");");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");

                // Write to file
                File.WriteAllText(filePath, sb.ToString());
                DMEEditor.AddLogMessage("ClassCreator", $"Generated EF Core Migration for {entity.EntityName} at {filePath}", DateTime.Now, 0, null, Errors.Ok);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ClassCreator", $"Error generating EF Core Migration: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        private string MapFieldTypeToSqlType(string fieldType)
        {
            fieldType = fieldType.ToLower();
            return fieldType switch
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

        private bool IsIntegralType(string fieldType)
        {
            fieldType = fieldType.ToLower();
            return fieldType == "int" || fieldType == "int32" || fieldType == "int64" ||
                   fieldType == "long" || fieldType == "short" || fieldType == "int16" ||
                   fieldType == "byte" || fieldType == "uint" || fieldType == "ulong" ||
                   fieldType == "ushort" || fieldType == "sbyte";
        }

        /// <summary>
        /// Generates a difference report between two versions of an entity
        /// </summary>
        /// <param name="originalEntity">Original entity</param>
        /// <param name="newEntity">New entity</param>
        /// <returns>Difference report as string</returns>
        public string GenerateEntityDiffReport(EntityStructure originalEntity, EntityStructure newEntity)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;

                // Header
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

                if (originalEntity.Description != newEntity.Description)
                {
                    sb.AppendLine($"## Description Change");
                    sb.AppendLine($"- Original: {originalEntity.Description ?? "[None]"}");
                    sb.AppendLine($"- New: {newEntity.Description ?? "[None]"}");
                    sb.AppendLine();
                }

                if (originalEntity.Category != newEntity.Category)
                {
                    sb.AppendLine($"## Category Change");
                    sb.AppendLine($"- Original: {originalEntity.Category ?? "[None]"}");
                    sb.AppendLine($"- New: {newEntity.Category ?? "[None]"}");
                    sb.AppendLine();
                }

                // Field differences
                sb.AppendLine("## Field Changes");

                // Get lists of fields
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

                // Fields modified
                var commonFields = originalFields.Keys.Intersect(newFields.Keys).ToList();
                var modifiedFields = new List<string>();

                foreach (var fieldName in commonFields)
                {
                    var originalField = originalFields[fieldName];
                    var newField = newFields[fieldName];

                    bool isModified = false;
                    StringBuilder fieldChanges = new StringBuilder();

                    if (originalField.fieldtype != newField.fieldtype)
                    {
                        isModified = true;
                        fieldChanges.AppendLine($"  - Type: {originalField.fieldtype} -> {newField.fieldtype}");
                    }

                    if (originalField.IsRequired != newField.IsRequired)
                    {
                        isModified = true;
                        fieldChanges.AppendLine($"  - Required: {originalField.IsRequired} -> {newField.IsRequired}");
                    }

                    if (originalField.IsKey != newField.IsKey)
                    {
                        isModified = true;
                        fieldChanges.AppendLine($"  - Primary Key: {originalField.IsKey} -> {newField.IsKey}");
                    }

                    if (originalField.IsUnique != newField.IsUnique)
                    {
                        isModified = true;
                        fieldChanges.AppendLine($"  - Unique: {originalField.IsUnique} -> {newField.IsUnique}");
                    }

                    if (originalField.Size != newField.Size)
                    {
                        isModified = true;
                        fieldChanges.AppendLine($"  - Size: {originalField.Size} -> {newField.Size}");
                    }

                    if (originalField.DefaultValue != newField.DefaultValue)
                    {
                        isModified = true;
                        fieldChanges.AppendLine($"  - Default Value: {originalField.DefaultValue ?? "[None]"} -> {newField.DefaultValue ?? "[None]"}");
                    }

                    if (isModified)
                    {
                        modifiedFields.Add(fieldName);
                        sb.AppendLine($"### Modified: {fieldName}");
                        sb.Append(fieldChanges.ToString());
                        sb.AppendLine();
                    }
                }

                // Summary
                sb.AppendLine("## Summary");
                sb.AppendLine($"- Fields Removed: {removedFields.Count}");
                sb.AppendLine($"- Fields Added: {addedFields.Count}");
                sb.AppendLine($"- Fields Modified: {modifiedFields.Count}");
                sb.AppendLine($"- Total Fields in Original: {originalEntity.Fields.Count}");
                sb.AppendLine($"- Total Fields in New: {newEntity.Fields.Count}");

                DMEEditor.AddLogMessage("ClassCreator", $"Generated difference report between entities", DateTime.Now, 0, null, Errors.Ok);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ClassCreator", $"Error generating entity difference report: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Generates Blazor component for displaying and editing entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Component namespace</param>
        /// <returns>The Blazor component code</returns>
        public string GenerateBlazorComponent(EntityStructure entity, string outputPath,
                                            string namespaceName = "TheTechIdea.ProjectComponents")
        {
            string componentName = $"{entity.EntityName}Component";
            string filePath = Path.Combine(outputPath, $"{componentName}.razor.cs");
            string razorFilePath = Path.Combine(outputPath, $"{componentName}.razor");

            // Generate the component code-behind file
            StringBuilder sb = new StringBuilder();

            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;

                // Generate code-behind file
                sb.AppendLine("using Microsoft.AspNetCore.Components;");
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Threading.Tasks;");
                sb.AppendLine("using System.Linq;");
                sb.AppendLine("using TheTechIdea.Beep.DataBase;");
                sb.AppendLine("");
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
                sb.AppendLine($"    public partial class {componentName} : ComponentBase");
                sb.AppendLine("    {");

                // Parameters
                sb.AppendLine("        [Inject]");
                sb.AppendLine("        public IDMEEditor DMEEditor { get; set; }");
                sb.AppendLine("");

                sb.AppendLine("        [Parameter]");
                sb.AppendLine($"        public {entity.EntityName} Item {{ get; set; }}");
                sb.AppendLine("");

                sb.AppendLine("        [Parameter]");
                sb.AppendLine("        public string DataSourceName { get; set; }");
                sb.AppendLine("");

                sb.AppendLine("        [Parameter]");
                sb.AppendLine("        public EventCallback<bool> OnSave { get; set; }");
                sb.AppendLine("");

                sb.AppendLine("        [Parameter]");
                sb.AppendLine("        public EventCallback OnCancel { get; set; }");
                sb.AppendLine("");

                // Error message for validation
                sb.AppendLine("        private string ErrorMessage { get; set; }");
                sb.AppendLine("");

                // Loading indicator
                sb.AppendLine("        private bool IsLoading { get; set; }");
                sb.AppendLine("");

                // On initialization
                sb.AppendLine("        protected override void OnInitialized()");
                sb.AppendLine("        {");
                sb.AppendLine($"            if (Item == null) Item = new {entity.EntityName}();");
                sb.AppendLine("        }");
                sb.AppendLine("");

                // Save method
                sb.AppendLine("        private async Task SaveItem()");
                sb.AppendLine("        {");
                sb.AppendLine("            try");
                sb.AppendLine("            {");
                sb.AppendLine("                IsLoading = true;");
                sb.AppendLine("                ErrorMessage = null;");
                sb.AppendLine("");
                sb.AppendLine("                var dataSource = DMEEditor.GetDataSource(DataSourceName);");
                sb.AppendLine("                if (dataSource == null)");
                sb.AppendLine("                {");
                sb.AppendLine("                    ErrorMessage = $\"Data source {DataSourceName} not found\";");
                sb.AppendLine("                    return;");
                sb.AppendLine("                }");
                sb.AppendLine("");
                sb.AppendLine("                // Determine if this is an insert or update operation");
                sb.AppendLine($"                bool isNew = Item.Id == default;");
                sb.AppendLine("");
                sb.AppendLine("                if (isNew)");
                sb.AppendLine("                {");
                sb.AppendLine($"                    await Task.Run(() => dataSource.InsertEntity(\"{entity.EntityName}\", Item));");
                sb.AppendLine("                }");
                sb.AppendLine("                else");
                sb.AppendLine("                {");
                sb.AppendLine($"                    await Task.Run(() => dataSource.UpdateEntity(\"{entity.EntityName}\", Item));");
                sb.AppendLine("                }");
                sb.AppendLine("");
                sb.AppendLine("                await OnSave.InvokeAsync(isNew);");
                sb.AppendLine("            }");
                sb.AppendLine("            catch (Exception ex)");
                sb.AppendLine("            {");
                sb.AppendLine("                ErrorMessage = $\"Error saving {entity.EntityName}: {ex.Message}\";");
                sb.AppendLine("            }");
                sb.AppendLine("            finally");
                sb.AppendLine("            {");
                sb.AppendLine("                IsLoading = false;");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
                sb.AppendLine("");

                // Cancel method
                sb.AppendLine("        private async Task CancelEdit()");
                sb.AppendLine("        {");
                sb.AppendLine("            await OnCancel.InvokeAsync();");
                sb.AppendLine("        }");

                sb.AppendLine("    }");
                sb.AppendLine("}");

                // Write code-behind to file
                File.WriteAllText(filePath, sb.ToString());

                // Generate Razor template file
                StringBuilder razorSb = new StringBuilder();
                razorSb.AppendLine("@namespace " + namespaceName);
                razorSb.AppendLine("");
                razorSb.AppendLine("<div class=\"card\">");
                razorSb.AppendLine("    <div class=\"card-header\">");
                razorSb.AppendLine($"        <h3>{(entity.Caption ?? entity.EntityName)}</h3>");
                razorSb.AppendLine("    </div>");
                razorSb.AppendLine("    <div class=\"card-body\">");

                // Show error message if any
                razorSb.AppendLine("        @if (!string.IsNullOrEmpty(ErrorMessage))");
                razorSb.AppendLine("        {");
                razorSb.AppendLine("            <div class=\"alert alert-danger\">");
                razorSb.AppendLine("                @ErrorMessage");
                razorSb.AppendLine("            </div>");
                razorSb.AppendLine("        }");
                razorSb.AppendLine("");

                // Loading indicator
                razorSb.AppendLine("        @if (IsLoading)");
                razorSb.AppendLine("        {");
                razorSb.AppendLine("            <div class=\"d-flex justify-content-center\">");
                razorSb.AppendLine("                <div class=\"spinner-border\" role=\"status\">");
                razorSb.AppendLine("                    <span class=\"sr-only\">Loading...</span>");
                razorSb.AppendLine("                </div>");
                razorSb.AppendLine("            </div>");
                razorSb.AppendLine("        }");
                razorSb.AppendLine("");

                // Form
                razorSb.AppendLine("        <form>");

                // Generate form fields based on entity structure
                foreach (var field in entity.Fields)
                {
                    // Skip ID field as it's typically auto-generated
                    if (field.fieldname.ToLower() == "id" && field.IsKey)
                    {
                        continue;
                    }

                    razorSb.AppendLine("            <div class=\"form-group row\">");
                    razorSb.AppendLine($"                <label for=\"{field.fieldname}\" class=\"col-sm-4 col-form-label\">{GetDisplayName(field.fieldname)}</label>");
                    razorSb.AppendLine("                <div class=\"col-sm-8\">");

                    // Generate appropriate input based on field type
                    switch (field.fieldtype.ToLower())
                    {
                        case "bool":
                        case "boolean":
                            razorSb.AppendLine($"                    <div class=\"form-check\">");
                            razorSb.AppendLine($"                        <input class=\"form-check-input\" type=\"checkbox\" id=\"{field.fieldname}\" @bind=\"Item.{field.fieldname}\">");
                            razorSb.AppendLine($"                    </div>");
                            break;

                        case "datetime":
                            razorSb.AppendLine($"                    <input type=\"datetime-local\" class=\"form-control\" id=\"{field.fieldname}\" @bind=\"Item.{field.fieldname}\">");
                            break;

                        case "int":
                        case "int32":
                        case "int64":
                        case "long":
                        case "decimal":
                        case "double":
                        case "float":
                            razorSb.AppendLine($"                    <input type=\"number\" class=\"form-control\" id=\"{field.fieldname}\" @bind=\"Item.{field.fieldname}\">");
                            break;

                        case "string":
                            if (field.fieldname.ToLower().Contains("password"))
                            {
                                razorSb.AppendLine($"                    <input type=\"password\" class=\"form-control\" id=\"{field.fieldname}\" @bind=\"Item.{field.fieldname}\">");
                            }
                            else if (field.fieldname.ToLower().Contains("email"))
                            {
                                razorSb.AppendLine($"                    <input type=\"email\" class=\"form-control\" id=\"{field.fieldname}\" @bind=\"Item.{field.fieldname}\">");
                            }
                            else if (field.Size > 255) // Multiline text
                            {
                                razorSb.AppendLine($"                    <textarea class=\"form-control\" id=\"{field.fieldname}\" @bind=\"Item.{field.fieldname}\" rows=\"3\"></textarea>");
                            }
                            else // Standard text
                            {
                                razorSb.AppendLine($"                    <input type=\"text\" class=\"form-control\" id=\"{field.fieldname}\" @bind=\"Item.{field.fieldname}\">");
                            }
                            break;

                        default:
                            razorSb.AppendLine($"                    <input type=\"text\" class=\"form-control\" id=\"{field.fieldname}\" @bind=\"Item.{field.fieldname}\">");
                            break;
                    }

                    razorSb.AppendLine("                </div>");
                    razorSb.AppendLine("            </div>");
                    razorSb.AppendLine("");
                }

                // Form buttons
                razorSb.AppendLine("            <div class=\"form-group row mt-4\">");
                razorSb.AppendLine("                <div class=\"col-sm-12 text-right\">");
                razorSb.AppendLine("                    <button type=\"button\" class=\"btn btn-secondary mr-2\" @onclick=\"CancelEdit\">Cancel</button>");
                razorSb.AppendLine("                    <button type=\"button\" class=\"btn btn-primary\" @onclick=\"SaveItem\">Save</button>");
                razorSb.AppendLine("                </div>");
                razorSb.AppendLine("            </div>");

                razorSb.AppendLine("        </form>");
                razorSb.AppendLine("    </div>");
                razorSb.AppendLine("</div>");

                // Write Razor file
                File.WriteAllText(razorFilePath, razorSb.ToString());

                DMEEditor.AddLogMessage("ClassCreator", $"Generated Blazor component for {entity.EntityName} at {razorFilePath}", DateTime.Now, 0, null, Errors.Ok);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ClassCreator", $"Error generating Blazor component: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        private string GetDisplayName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return string.Empty;

            // Insert a space before each capital letter and then capitalize the first letter
            var result = System.Text.RegularExpressions.Regex.Replace(fieldName, "([A-Z])", " $1").Trim();
            return char.ToUpper(result[0]) + result.Substring(1);
        }

        /// <summary>
        /// Generates gRPC service definitions for entity
        /// </summary>
        /// <param name="entity">Entity structure</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="namespaceName">Namespace</param>
        /// <returns>The generated proto file and service implementation</returns>
        public (string ProtoFile, string ServiceImplementation) GenerateGrpcService(EntityStructure entity,
                                                                                  string outputPath,
                                                                                  string namespaceName)
        {
            string serviceName = $"{entity.EntityName}Service";
            string protoFileName = $"{entity.EntityName.ToLower()}.proto";
            string protoFilePath = Path.Combine(outputPath, protoFileName);
            string serviceFilePath = Path.Combine(outputPath, $"{serviceName}.cs");

            StringBuilder protoSb = new StringBuilder();
            StringBuilder serviceSb = new StringBuilder();

            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;

                // Generate the proto file
                protoSb.AppendLine("syntax = \"proto3\";");
                protoSb.AppendLine("");
                protoSb.AppendLine($"option csharp_namespace = \"{namespaceName}.Protos\";");
                protoSb.AppendLine("");
                protoSb.AppendLine($"package {entity.EntityName.ToLower()};");
                protoSb.AppendLine("");

                // Service definition
                protoSb.AppendLine($"service {serviceName} {{");
                protoSb.AppendLine($"  // Gets a list of all {entity.EntityName}s");
                protoSb.AppendLine($"  rpc GetAll (Get{entity.EntityName}Request) returns (stream {entity.EntityName}) {{}}");
                protoSb.AppendLine("");
                protoSb.AppendLine($"  // Gets a specific {entity.EntityName} by ID");
                protoSb.AppendLine($"  rpc GetById (Get{entity.EntityName}ByIdRequest) returns ({entity.EntityName}) {{}}");
                protoSb.AppendLine("");
                protoSb.AppendLine($"  // Creates a new {entity.EntityName}");
                protoSb.AppendLine($"  rpc Create (Create{entity.EntityName}Request) returns ({entity.EntityName}) {{}}");
                protoSb.AppendLine("");
                protoSb.AppendLine($"  // Updates an existing {entity.EntityName}");
                protoSb.AppendLine($"  rpc Update (Update{entity.EntityName}Request) returns ({entity.EntityName}) {{}}");
                protoSb.AppendLine("");
                protoSb.AppendLine($"  // Deletes a {entity.EntityName}");
                protoSb.AppendLine($"  rpc Delete (Delete{entity.EntityName}Request) returns (Delete{entity.EntityName}Response) {{}}");
                protoSb.AppendLine("}");
                protoSb.AppendLine("");

                // Message for Empty request
                protoSb.AppendLine($"message Get{entity.EntityName}Request {{");
                protoSb.AppendLine("  string data_source_name = 1;");
                protoSb.AppendLine("}");
                protoSb.AppendLine("");

                // Message for GetById request
                protoSb.AppendLine($"message Get{entity.EntityName}ByIdRequest {{");
                protoSb.AppendLine("  string data_source_name = 1;");
                protoSb.AppendLine("  int32 id = 2;");
                protoSb.AppendLine("}");
                protoSb.AppendLine("");

                // Message for Create request
                protoSb.AppendLine($"message Create{entity.EntityName}Request {{");
                protoSb.AppendLine("  string data_source_name = 1;");
                protoSb.AppendLine($"  {entity.EntityName} item = 2;");
                protoSb.AppendLine("}");
                protoSb.AppendLine("");

                // Message for Update request
                protoSb.AppendLine($"message Update{entity.EntityName}Request {{");
                protoSb.AppendLine("  string data_source_name = 1;");
                protoSb.AppendLine($"  {entity.EntityName} item = 2;");
                protoSb.AppendLine("}");
                protoSb.AppendLine("");

                // Message for Delete request
                protoSb.AppendLine($"message Delete{entity.EntityName}Request {{");
                protoSb.AppendLine("  string data_source_name = 1;");
                protoSb.AppendLine("  int32 id = 2;");
                protoSb.AppendLine("}");
                protoSb.AppendLine("");

                // Message for Delete response
                protoSb.AppendLine($"message Delete{entity.EntityName}Response {{");
                protoSb.AppendLine("  bool success = 1;");
                protoSb.AppendLine("  string message = 2;");
                protoSb.AppendLine("}");
                protoSb.AppendLine("");

                // Entity message definition
                protoSb.AppendLine($"message {entity.EntityName} {{");

                int fieldNumber = 1;
                foreach (var field in entity.Fields)
                {
                    string protoType = MapToCSharpToProtoType(field.fieldtype);
                    protoSb.AppendLine($"  {protoType} {SnakeCaseName(field.fieldname)} = {fieldNumber++};");
                }

                protoSb.AppendLine("}");

                // Write proto file
                File.WriteAllText(protoFilePath, protoSb.ToString());

                // Generate the service implementation
                serviceSb.AppendLine("using Grpc.Core;");
                serviceSb.AppendLine("using Microsoft.Extensions.Logging;");
                serviceSb.AppendLine("using System;");
                serviceSb.AppendLine("using System.Collections.Generic;");
                serviceSb.AppendLine("using System.Threading.Tasks;");
                serviceSb.AppendLine("using TheTechIdea.Beep.DataBase;");
                serviceSb.AppendLine($"using {namespaceName}.Protos;");
                serviceSb.AppendLine("");
                serviceSb.AppendLine($"namespace {namespaceName}.Services");
                serviceSb.AppendLine("{");
                serviceSb.AppendLine($"    public class {serviceName}Implementation : {serviceName}.{serviceName}Base");
                serviceSb.AppendLine("    {");
                serviceSb.AppendLine("        private readonly IDMEEditor _dmeEditor;");
                serviceSb.AppendLine("        private readonly ILogger<{serviceName}Implementation> _logger;");
                serviceSb.AppendLine("");
                serviceSb.AppendLine($"        public {serviceName}Implementation(IDMEEditor dmeEditor, ILogger<{serviceName}Implementation> logger)");
                serviceSb.AppendLine("        {");
                serviceSb.AppendLine("            _dmeEditor = dmeEditor;");
                serviceSb.AppendLine("            _logger = logger;");
                serviceSb.AppendLine("        }");
                serviceSb.AppendLine("");

                // GetAll method
                serviceSb.AppendLine($"        public override async Task GetAll(Get{entity.EntityName}Request request, IServerStreamWriter<{entity.EntityName}> responseStream, ServerCallContext context)");
                serviceSb.AppendLine("        {");
                serviceSb.AppendLine("            try");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine("                var dataSource = _dmeEditor.GetDataSource(request.DataSourceName);");
                serviceSb.AppendLine("                if (dataSource == null)");
                serviceSb.AppendLine("                {");
                serviceSb.AppendLine("                    throw new RpcException(new Status(StatusCode.NotFound, $\"Data source '{request.DataSourceName}' not found\"));");
                serviceSb.AppendLine("                }");
                serviceSb.AppendLine("");
                serviceSb.AppendLine($"                var entities = await Task.Run(() => dataSource.GetEntity(\"{entity.EntityName}\", null)) as IEnumerable<dynamic>;");
                serviceSb.AppendLine("                if (entities == null)");
                serviceSb.AppendLine("                {");
                serviceSb.AppendLine("                    return;");
                serviceSb.AppendLine("                }");
                serviceSb.AppendLine("");
                serviceSb.AppendLine("                foreach (var entity in entities)");
                serviceSb.AppendLine("                {");
                serviceSb.AppendLine($"                    var protoEntity = new {entity.EntityName}");
                serviceSb.AppendLine("                    {");

                // Map entity fields
                foreach (var field in entity.Fields)
                {
                    string protoFieldName = StringExtensions.ToPascalCase(SnakeCaseName(field.fieldname));
                    serviceSb.AppendLine($"                        {protoFieldName} = entity.{field.fieldname},");
                }

                serviceSb.AppendLine("                    };");
                serviceSb.AppendLine("                    await responseStream.WriteAsync(protoEntity);");
                serviceSb.AppendLine("                }");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("            catch (Exception ex)");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine("                _logger.LogError(ex, \"Error retrieving entities\");");
                serviceSb.AppendLine("                throw new RpcException(new Status(StatusCode.Internal, $\"Error retrieving entities: {ex.Message}\"));");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("        }");
                serviceSb.AppendLine("");

                // GetById method
                serviceSb.AppendLine($"        public override async Task<{entity.EntityName}> GetById(Get{entity.EntityName}ByIdRequest request, ServerCallContext context)");
                serviceSb.AppendLine("        {");
                serviceSb.AppendLine("            try");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine("                var dataSource = _dmeEditor.GetDataSource(request.DataSourceName);");
                serviceSb.AppendLine("                if (dataSource == null)");
                serviceSb.AppendLine("                {");
                serviceSb.AppendLine("                    throw new RpcException(new Status(StatusCode.NotFound, $\"Data source '{request.DataSourceName}' not found\"));");
                serviceSb.AppendLine("                }");
                serviceSb.AppendLine("");
                serviceSb.AppendLine("                var filters = new List<AppFilter> { new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = request.Id.ToString() } };");
                serviceSb.AppendLine($"                var entity = await Task.Run(() => dataSource.GetEntity(\"{entity.EntityName}\", filters)) as dynamic;");
                serviceSb.AppendLine("");
                serviceSb.AppendLine("                if (entity == null)");
                serviceSb.AppendLine("                {");
                serviceSb.AppendLine("                    throw new RpcException(new Status(StatusCode.NotFound, \"Entity not found\"));");
                serviceSb.AppendLine("                }");
                serviceSb.AppendLine("");
                serviceSb.AppendLine($"                return new {entity.EntityName}");
                serviceSb.AppendLine("                {");

                // Map entity fields
                foreach (var field in entity.Fields)
                {
                    string protoFieldName = StringExtensions.ToPascalCase(SnakeCaseName(field.fieldname));
                    serviceSb.AppendLine($"                    {protoFieldName} = entity.{field.fieldname},");
                }

                serviceSb.AppendLine("                };");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("            catch (RpcException)");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine("                throw;");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("            catch (Exception ex)");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine("                _logger.LogError(ex, \"Error retrieving entity by id\");");
                serviceSb.AppendLine("                throw new RpcException(new Status(StatusCode.Internal, $\"Error retrieving entity: {ex.Message}\"));");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("        }");
                serviceSb.AppendLine("");

                // Add Create, Update, Delete methods following same pattern

                serviceSb.AppendLine("        // TODO: Implement Create, Update, and Delete methods");

                serviceSb.AppendLine("    }");
                serviceSb.AppendLine("}");

                // Write service implementation file
                File.WriteAllText(serviceFilePath, serviceSb.ToString());

                DMEEditor.AddLogMessage("ClassCreator", $"Generated gRPC service for {entity.EntityName}", DateTime.Now, 0, null, Errors.Ok);

                return (protoSb.ToString(), serviceSb.ToString());
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ClassCreator", $"Error generating gRPC service: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return (null, null);
            }
        }

        // Helper method to convert camelCase or PascalCase to snake_case
        private string SnakeCaseName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var result = System.Text.RegularExpressions.Regex.Replace(name, "([A-Z])", "_$1").ToLower();
            if (result.StartsWith("_"))
                result = result.Substring(1);

            return result;
        }

      

        // Map C# types to protobuf types
        private static string MapToCSharpToProtoType(string csharpType)
        {
            csharpType = csharpType.ToLower();

            return csharpType switch
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



    }
}
