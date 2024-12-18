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
        public string CreatEntityClass(EntityStructure entity, string usingheader, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true)
        {
            string implementations2 = " Entity ";


            string extracode2 = null;//Environment.NewLine + " public event PropertyChangedEventHandler PropertyChanged;\r\n\r\n    // This method is called by the Set accessor of each property.\r\n    // The CallerMemberName attribute that is applied to the optional propertyName\r\n    // parameter causes the property name of the caller to be substituted as an argument.\r\n    private void NotifyPropertyChanged([CallerMemberName] string propertyName = \"\")\r\n    {\r\n        if (PropertyChanged != null)\r\n        {\r\n            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));\r\n        }\r\n    }";
            //string template2 = null;//Environment.NewLine + " private :FIELDTYPE :FIELDNAMEValue ;";
            string template2 = Environment.NewLine + $" private :FIELDTYPE  _:FIELDNAMEValue ;\r\n";
            template2 = template2 +Environment.NewLine + " public :FIELDTYPE :FIELDNAME\r\n    {\r\n        get\r\n        {\r\n            return this._:FIELDNAMEValue;\r\n        }\r\n\r\n        set\r\n        {\r\n       SetProperty(ref _:FIELDNAMEValue, value);\r\n    }\r\n    }";
            return CreateClassFromTemplate(entity.EntityName, entity, template2, usingheader, implementations2, extracode2, outputpath, nameSpacestring, GenerateCSharpCodeFiles);
        }
        public string CreateClassFromTemplate(string classname,EntityStructure entity, string template, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true)
        {
            string str = "";
            string filepath = "";
            if (string.IsNullOrEmpty(outputpath))
            {
                filepath = Path.Combine(DMEEditor.ConfigEditor.Config.ScriptsPath, $"{entity.EntityName}.cs");
            }else
                filepath = Path.Combine(outputpath, $"{entity.EntityName}.cs");
           
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                string clsname = string.Empty;
                if (string.IsNullOrEmpty(classname))
                {
                    clsname = classname;
                }
                else
                {
                    clsname = entity.EntityName;
                }
                str = usingheader + Environment.NewLine;
                str += $"namespace  {nameSpacestring} " + Environment.NewLine;
                str += "{ " + Environment.NewLine;
                if (string.IsNullOrEmpty(implementations))
                {
                    str += $"public class {clsname} " + Environment.NewLine;
                }
                else
                {
                    str += $"public class {clsname} : " + implementations + Environment.NewLine;
                }

                str += "{ " + Environment.NewLine; // start of Class
                // Create CTOR
                str += $"public  {clsname} ()"+"{}" + Environment.NewLine; 
                for (int i = 0; i < entity.Fields.Count ; i++)
                {
                    EntityField fld = entity.Fields[i];
                    if (string.IsNullOrEmpty(template))
                    {
                        str += $"public {fld.fieldtype}? {fld.fieldname}" + Environment.NewLine;
                    }
                    else
                    {
                        string extractedtemplate = template.Replace(":FIELDNAME", fld.fieldname);
                        extractedtemplate = extractedtemplate.Replace(":FIELDTYPE", fld.fieldtype+"?");
                        str += extractedtemplate + Environment.NewLine;
                    }

                }

                str += "} " + Environment.NewLine; // end of Class
                if (string.IsNullOrEmpty(extracode))
                {
                    str += extracode + Environment.NewLine;
                }
                str += Environment.NewLine;

                 str += "} " + Environment.NewLine; // end of namepspace
                string[] result = Regex.Split(str, "\r\n|\r|\n");
                if (GenerateCSharpCodeFiles)
                {
                    StreamWriter streamWriter = new StreamWriter(filepath);
                    foreach (string line in result)
                    {
                        streamWriter.WriteLine(line);
                    }
                    streamWriter.Close();
                }
              
                return str;
            }
            catch (Exception ex)
            {
                str = null;
                DMEEditor.AddLogMessage("Beep", $" Error Creating Code {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }

            return str;
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
        public string GenerateBlazorCRUDWithQuickGrid(string outputPath, string entityName, List<EntityField> fields, string dataSourceName)
        {
            try
            {
                var stringBuilder = new StringBuilder();

                // Add using directives
                stringBuilder.AppendLine("@using System.Collections.Generic");
                stringBuilder.AppendLine("@using System.Threading.Tasks");
                stringBuilder.AppendLine("@using TheTechIdea.Beep.Utilities");
                stringBuilder.AppendLine("@inject HttpClient Http");

                // Add namespace and component name
                stringBuilder.AppendLine($"<h3>{entityName} Management</h3>");

                // Search and Filters
                stringBuilder.AppendLine("<input type=\"text\" class=\"form-control\" placeholder=\"Search...\" @bind-value=\"searchText\" />");

                // Export button
                stringBuilder.AppendLine("<button @onclick=\"ExportToCsv\" class=\"btn btn-primary\">Export to CSV</button>");

                // Add QuickGrid
                stringBuilder.AppendLine("<QuickGrid Items=\"@pagedData\" RowsPerPage=\"pageSize\" @bind-CurrentPage=\"currentPage\">");

                // Columns
                foreach (var field in fields)
                {
                    stringBuilder.AppendLine($"<PropertyColumn Property=\"@nameof({entityName}.{field.fieldname})\" Title=\"{field.fieldname}\" />");
                }

                // Add actions column
                stringBuilder.AppendLine("<TemplateColumn Title=\"Actions\">");
                stringBuilder.AppendLine("    <Template Context=\"item\">");
                stringBuilder.AppendLine("        <button class=\"btn btn-primary\" @onclick=\"() => EditItem(item)\">Edit</button>");
                stringBuilder.AppendLine("        <button class=\"btn btn-danger\" @onclick=\"() => DeleteItem(item)\">Delete</button>");
                stringBuilder.AppendLine("    </Template>");
                stringBuilder.AppendLine("</TemplateColumn>");

                stringBuilder.AppendLine("</QuickGrid>");

                // Add Pagination Controls
                stringBuilder.AppendLine("<Pagination TotalItems=\"totalItems\" ItemsPerPage=\"pageSize\" @bind-CurrentPage=\"currentPage\" />");

                // Add Edit Form
                stringBuilder.AppendLine("<EditForm Model=\"@currentItem\" OnValidSubmit=\"HandleValidSubmit\">");
                foreach (var field in fields)
                {
                    string inputControl = field.fieldCategory switch
                    {
                        DbFieldCategory.String => $"<InputText id=\"{field.fieldname}\" class=\"form-control\" @bind-Value=\"currentItem.{field.fieldname}\" />",
                        DbFieldCategory.Numeric => $"<InputNumber id=\"{field.fieldname}\" class=\"form-control\" @bind-Value=\"currentItem.{field.fieldname}\" />",
                        DbFieldCategory.Date => $"<InputDate id=\"{field.fieldname}\" class=\"form-control\" @bind-Value=\"currentItem.{field.fieldname}\" />",
                        DbFieldCategory.Boolean => $"<InputCheckbox id=\"{field.fieldname}\" class=\"form-control\" @bind-Value=\"currentItem.{field.fieldname}\" />",
                        _ => $"<InputText id=\"{field.fieldname}\" class=\"form-control\" @bind-Value=\"currentItem.{field.fieldname}\" />"
                    };

                    stringBuilder.AppendLine($"<div class=\"form-group\">");
                    stringBuilder.AppendLine($"    <label for=\"{field.fieldname}\">{field.fieldname}</label>");
                    stringBuilder.AppendLine($"    {inputControl}");
                    stringBuilder.AppendLine("</div>");
                }

                stringBuilder.AppendLine("<button type=\"submit\" class=\"btn btn-success\">Save</button>");
                stringBuilder.AppendLine("</EditForm>");

                // Add @code block
                stringBuilder.AppendLine("@code {");

                // Add variables
                stringBuilder.AppendLine($"    private List<{entityName}> data = new();");
                stringBuilder.AppendLine($"    private List<{entityName}> pagedData = new();");
                stringBuilder.AppendLine("    private int totalItems;");
                stringBuilder.AppendLine("    private int pageSize = 10;");
                stringBuilder.AppendLine("    private int currentPage = 1;");
                stringBuilder.AppendLine("    private string searchText;");
                stringBuilder.AppendLine($"    private {entityName} currentItem = new();");

                // Add lifecycle methods
                stringBuilder.AppendLine("    protected override async Task OnInitializedAsync()");
                stringBuilder.AppendLine("    {");
                stringBuilder.AppendLine($"        data = await Http.GetFromJsonAsync<List<{entityName}>>($\"/api/{entityName}\");");
                stringBuilder.AppendLine("        UpdatePagedData();");
                stringBuilder.AppendLine("    }");

                // Add helper methods
                stringBuilder.AppendLine("    private void UpdatePagedData()");
                stringBuilder.AppendLine("    {");
                stringBuilder.AppendLine("        var filteredData = string.IsNullOrWhiteSpace(searchText) ? data : data.Where(d => d.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();");
                stringBuilder.AppendLine("        totalItems = filteredData.Count;");
                stringBuilder.AppendLine("        pagedData = filteredData.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();");
                stringBuilder.AppendLine("    }");

                stringBuilder.AppendLine("    private void EditItem(Entity item)");
                stringBuilder.AppendLine("    {");
                stringBuilder.AppendLine("        currentItem = item;");
                stringBuilder.AppendLine("    }");

                stringBuilder.AppendLine("    private void DeleteItem(Entity item)");
                stringBuilder.AppendLine("    {");
                stringBuilder.AppendLine("        data.Remove(item);");
                stringBuilder.AppendLine("        UpdatePagedData();");
                stringBuilder.AppendLine("    }");

                stringBuilder.AppendLine("    private async Task HandleValidSubmit()");
                stringBuilder.AppendLine("    {");
                stringBuilder.AppendLine("        if (!data.Contains(currentItem))");
                stringBuilder.AppendLine("        {");
                stringBuilder.AppendLine("            data.Add(currentItem);");
                stringBuilder.AppendLine("        }");
                stringBuilder.AppendLine("        UpdatePagedData();");
                stringBuilder.AppendLine("    }");

                stringBuilder.AppendLine("    private async Task ExportToCsv()");
                stringBuilder.AppendLine("    {");
                stringBuilder.AppendLine("        var csvData = string.Join(\"\\n\", data.Select(d => string.Join(\",\", d.GetType().GetProperties().Select(p => p.GetValue(d))));");
                stringBuilder.AppendLine("        await File.WriteAllTextAsync(\"export.csv\", csvData);");
                stringBuilder.AppendLine("    }");

                stringBuilder.AppendLine("}");

                // Write to file
                var filePath = Path.Combine(outputPath, $"{entityName}CRUD.razor");
                File.WriteAllText(filePath, stringBuilder.ToString());

                return filePath;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("BlazorCRUDGenerator", ex.Message, DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }
        public string GenerateBlazorDetailedView(string namespaceName, string entityName, List<EntityField> fields)
        {
            StringBuilder detailedViewBuilder = new StringBuilder();

            // Add header and namespace
            detailedViewBuilder.AppendLine($"@page \"/{entityName.ToLower()}-details/{{id}}\"");
            detailedViewBuilder.AppendLine($"@using {namespaceName}.Services");
            detailedViewBuilder.AppendLine($"@inject {entityName}Service {entityName}Service");
            detailedViewBuilder.AppendLine();
            detailedViewBuilder.AppendLine($"<h3>{entityName} Details</h3>");
            detailedViewBuilder.AppendLine();

            // Add code-behind section
            detailedViewBuilder.AppendLine("@code {");
            detailedViewBuilder.AppendLine($"    [Parameter] public int Id {{ get; set; }}");
            detailedViewBuilder.AppendLine($"    private {entityName} currentItem = new {entityName}();");
            detailedViewBuilder.AppendLine();
            detailedViewBuilder.AppendLine($"    protected override async Task OnInitializedAsync()");
            detailedViewBuilder.AppendLine("    {");
            detailedViewBuilder.AppendLine($"        currentItem = await {entityName}Service.Get{entityName}ByIdAsync(Id);");
            detailedViewBuilder.AppendLine("    }");
            detailedViewBuilder.AppendLine();
            detailedViewBuilder.AppendLine("    private async Task Save()");
            detailedViewBuilder.AppendLine("    {");
            detailedViewBuilder.AppendLine($"        await {entityName}Service.Update{entityName}Async(Id, currentItem);");
            detailedViewBuilder.AppendLine("    }");
            detailedViewBuilder.AppendLine("}");

            // Add table to display entity details
            detailedViewBuilder.AppendLine("<table class=\"table table-striped\">");
            detailedViewBuilder.AppendLine("    <tbody>");

            foreach (var field in fields)
            {
                detailedViewBuilder.AppendLine("        <tr>");
                detailedViewBuilder.AppendLine($"            <th>{field.fieldname}</th>");

                // Adjust input type based on DbFieldCategory
                string inputType = field.fieldCategory switch
                {
                    DbFieldCategory.String => "text",
                    DbFieldCategory.Numeric => "number",
                    DbFieldCategory.Date => "date",
                    DbFieldCategory.Boolean => "checkbox",
                    _ => "text"
                };

                string inputControl = field.fieldCategory == DbFieldCategory.Boolean
                    ? $"<InputCheckbox @bind-Value=\"currentItem.{field.fieldname}\" />"
                    : $"<InputText id=\"{field.fieldname}\" class=\"form-control\" @bind-Value=\"currentItem.{field.fieldname}\" />";

                detailedViewBuilder.AppendLine($"            <td>{inputControl}</td>");
                detailedViewBuilder.AppendLine("        </tr>");
            }

            detailedViewBuilder.AppendLine("    </tbody>");
            detailedViewBuilder.AppendLine("</table>");

            // Add save button
            detailedViewBuilder.AppendLine("<button class=\"btn btn-primary\" @onclick=\"Save\">Save</button>");
            detailedViewBuilder.AppendLine("<button class=\"btn btn-secondary\" @onclick=\"NavigateBack\">Back</button>");

            // Add navigate back function
            detailedViewBuilder.AppendLine("@code {");
            detailedViewBuilder.AppendLine("    private void NavigateBack()");
            detailedViewBuilder.AppendLine("    {");
            detailedViewBuilder.AppendLine("        NavigationManager.NavigateTo(\"/\");");
            detailedViewBuilder.AppendLine("    }");
            detailedViewBuilder.AppendLine("}");

            return detailedViewBuilder.ToString();
        }
        public string GenerateBlazorDashboardPage(string namespaceName, List<EntityStructure> entities)
        {
            StringBuilder dashboardBuilder = new StringBuilder();

            // Add page directive and imports
            dashboardBuilder.AppendLine("@page \"/dashboard\"");
            dashboardBuilder.AppendLine($"@using {namespaceName}.Services");
            dashboardBuilder.AppendLine();
            dashboardBuilder.AppendLine($"<h3>{namespaceName} Dashboard</h3>");
            dashboardBuilder.AppendLine("<div class=\"dashboard-container\">");

            // Generate entity widgets
            foreach (var entity in entities)
            {
                dashboardBuilder.AppendLine("    <div class=\"dashboard-widget\">");
                dashboardBuilder.AppendLine($"        <h4>{entity.EntityName}</h4>");
                dashboardBuilder.AppendLine("        <button class=\"btn btn-primary\" @onclick=\"() => NavigateToEntityListPage(@$\"/" + $"{entity.EntityName.ToLower()}s" + "\")\">Manage</button>");
                dashboardBuilder.AppendLine("    </div>");
            }

            dashboardBuilder.AppendLine("</div>");
            dashboardBuilder.AppendLine();

            // Add code-behind
            dashboardBuilder.AppendLine("@code {");
            dashboardBuilder.AppendLine("    private void NavigateToEntityListPage(string url)");
            dashboardBuilder.AppendLine("    {");
            dashboardBuilder.AppendLine("        NavigationManager.NavigateTo(url);");
            dashboardBuilder.AppendLine("    }");
            dashboardBuilder.AppendLine("}");

            // Add dashboard styling
            dashboardBuilder.AppendLine("<style>");
            dashboardBuilder.AppendLine(".dashboard-container {");
            dashboardBuilder.AppendLine("    display: flex;");
            dashboardBuilder.AppendLine("    flex-wrap: wrap;");
            dashboardBuilder.AppendLine("    gap: 20px;");
            dashboardBuilder.AppendLine("}");
            dashboardBuilder.AppendLine(".dashboard-widget {");
            dashboardBuilder.AppendLine("    flex: 1 1 calc(33.333% - 20px);");
            dashboardBuilder.AppendLine("    padding: 20px;");
            dashboardBuilder.AppendLine("    border: 1px solid #ccc;");
            dashboardBuilder.AppendLine("    border-radius: 8px;");
            dashboardBuilder.AppendLine("    text-align: center;");
            dashboardBuilder.AppendLine("}");
            dashboardBuilder.AppendLine(".dashboard-widget h4 {");
            dashboardBuilder.AppendLine("    margin-bottom: 15px;");
            dashboardBuilder.AppendLine("}");
            dashboardBuilder.AppendLine(".dashboard-widget button {");
            dashboardBuilder.AppendLine("    margin-top: 10px;");
            dashboardBuilder.AppendLine("}");
            dashboardBuilder.AppendLine("</style>");

            return dashboardBuilder.ToString();
        }
        #region "React Generator"
        public string GenerateReactListComponent(string entityName, List<EntityField> fields, string outputPath)
        {
            try
            {
                string componentName = $"{entityName}List";
                string fieldHeaders = string.Join("\n", fields.Select(f => $"<th>{f.fieldname}</th>"));
                string fieldRows = string.Join("\n", fields.Select(f => $"<td>{{{{item.{f.fieldname}}}}}</td>"));

                string reactCode = $@"
import React, {{ useState, useEffect }} from 'react';

export default function {componentName}() {{
    const [data, setData] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {{
        fetch('/api/{entityName.ToLower()}')
            .then(response => response.json())
            .then(data => {{
                setData(data);
                setLoading(false);
            }});
    }}, []);

    if (loading) return <p>Loading...</p>;

    return (
        <div>
            <h1>{entityName} List</h1>
            <table>
                <thead>
                    <tr>
                        {fieldHeaders}
                    </tr>
                </thead>
                <tbody>
                    {{data.map((item, index) => (
                        <tr key={{index}}>
                            {fieldRows}
                        </tr>
                    ))}}
                </tbody>
            </table>
        </div>
    );
}}
";

                // Save the generated React file
                string filePath = Path.Combine(outputPath, $"{componentName}.jsx");
                File.WriteAllText(filePath, reactCode);

                DMEEditor.AddLogMessage("ReactGenerator", $"Generated List Component for {entityName} at {filePath}", DateTime.Now, -1, null, Errors.Ok);
                return reactCode;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ReactGenerator", $"Error generating List Component for {entityName}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }
        public string GenerateReactDashboardComponent(List<string> entityNames, string outputPath)
        {
            try
            {
                string componentName = "Dashboard";
                string quickLinks = string.Join("\n", entityNames.Select(e => $"<li><a href=\"/{e.ToLower()}\">{e} List</a></li>"));

                string reactCode = $@"
import React from 'react';

export default function {componentName}() {{
    return (
        <div>
            <h1>Dashboard</h1>
            <ul>
                {quickLinks}
            </ul>
        </div>
    );
}}
";

                // Save the generated React file
                string filePath = Path.Combine(outputPath, $"{componentName}.jsx");
                File.WriteAllText(filePath, reactCode);

                DMEEditor.AddLogMessage("ReactGenerator", $"Generated Dashboard Component at {filePath}", DateTime.Now, -1, null, Errors.Ok);
                return reactCode;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ReactGenerator", $"Error generating Dashboard Component: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }
        public string GenerateReactDetailComponent(string entityName, List<EntityField> fields, string outputPath)
        {
            try
            {
                string componentName = $"{entityName}Detail";
                string detailFields = string.Join("\n", fields.Select(f =>
                {
                    return f.fieldCategory == DbFieldCategory.String
                        ? $"<div><label>{f.fieldname}</label><p>{{{{data.{f.fieldname}}}}}</p></div>"
                        : $"<div><label>{f.fieldname}</label><p>{{{{data.{f.fieldname}}}}}</p></div>";
                }));

                string reactCode = $@"
import React, {{ useState, useEffect }} from 'react';
import {{ useParams }} from 'react-router-dom';

export default function {componentName}() {{
    const {{ id }} = useParams();
    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {{
        fetch(`/api/{entityName.ToLower()}/${{id}}`)
            .then(response => response.json())
            .then(data => {{
                setData(data);
                setLoading(false);
            }});
    }}, [id]);

    if (loading) return <p>Loading...</p>;

    return (
        <div>
            <h1>{entityName} Detail</h1>
            <div>
                {detailFields}
            </div>
        </div>
    );
}}
";

                // Save the generated React file
                string filePath = Path.Combine(outputPath, $"{componentName}.jsx");
                File.WriteAllText(filePath, reactCode);

                DMEEditor.AddLogMessage("ReactGenerator", $"Generated Detail Component for {entityName} at {filePath}", DateTime.Now, -1, null, Errors.Ok);
                return reactCode;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ReactGenerator", $"Error generating Detail Component for {entityName}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }
        public string GenerateReactNavbarComponent(List<string> entityNames, string outputPath)
        {
            try
            {
                string componentName = "Navbar";
                string navLinks = string.Join("\n", entityNames.Select(e => $"<li><a href=\"/{e.ToLower()}\">{e}</a></li>"));

                string reactCode = $@"
import React from 'react';

export default function {componentName}() {{
    return (
        <nav>
            <ul>
                {navLinks}
            </ul>
        </nav>
    );
}}
";

                // Save the generated React file
                string filePath = Path.Combine(outputPath, $"{componentName}.jsx");
                File.WriteAllText(filePath, reactCode);

                DMEEditor.AddLogMessage("ReactGenerator", $"Generated Navbar Component at {filePath}", DateTime.Now, -1, null, Errors.Ok);
                return reactCode;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ReactGenerator", $"Error generating Navbar Component: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        #endregion "React Generator"
    }
}
