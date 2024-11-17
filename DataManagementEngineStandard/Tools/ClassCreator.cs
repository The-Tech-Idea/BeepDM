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
        //private CodeCompileUnit targetUnit;
        //private CodeTypeDeclaration targetClass;

        //private CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
        //private CodeGeneratorOptions options = new CodeGeneratorOptions();
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
            //options.BracingStyle = "C";
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

                //CSharpCodeProvider codeProvider = new CSharpCodeProvider();
                //System.CodeDom.Compiler.CompilerParameters parameters = new CompilerParameters();
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
                //if (progress != null)
                //{
                //    if (listofpaths.Count > 0)
                //    {
                //        if (retval[0] == "ok")
                //        {
                //            PassedArgs ps = new PassedArgs { ParameterString1 = "Finished Creating DLL", EventType = "Finish", ParameterInt1 = i, ParameterInt2 = total };
                //            progress.Report(ps);
                //        }
                //        else
                //        {
                //            ret = $"Error in Creating DLL {outputFileName}";
                //            PassedArgs ps = new PassedArgs { Objects = new List<ObjectItem> { new ObjectItem { Name = "Errors", obj = retval } }, ParameterString1 = "Error in Creating DLL", EventType = "Fail", ParameterInt1 = i, ParameterInt2 = total };
                //            progress.Report(ps);
                //        }
                //    }


                //}
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

                //CSharpCodeProvider codeProvider = new CSharpCodeProvider();
                //System.CodeDom.Compiler.CompilerParameters parameters = new CompilerParameters();
                if (progress != null)
                {
                    PassedArgs ps = new PassedArgs { ParameterString1 = "Creating DLL", EventType = "Update", ParameterInt1 = i, ParameterInt2 = total };
                    progress.Report(ps);

                }
                string ret = "ok";
                //List<string> retval = CompileCode(provider, listofpaths, Path.Combine(outputpath, dllname + ".dll"));
                //if (progress != null)
                //{
                //    if (retval.Count > 0)
                //    {
                //        if (retval[0] == "ok")
                //        {
                //            PassedArgs ps = new PassedArgs { ParameterString1 = "Finished Creating DLL", EventType = "Finish", ParameterInt1 = i, ParameterInt2 = total };
                //            progress.Report(ps);
                //        }
                //        else
                //        {
                //            ret = $"Error in Creating DLL {outputFileName}";
                //            PassedArgs ps = new PassedArgs { Objects = new List<ObjectItem> { new ObjectItem { Name = "Errors", obj = retval } }, ParameterString1 = "Error in Creating DLL", EventType = "Fail", ParameterInt1 = i, ParameterInt2 = total };
                //            progress.Report(ps);
                //        }
                //    }


                //}
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
                //CSharpCodeProvider provider = new CSharpCodeProvider();
                //CompilerParameters parameters = new CompilerParameters();
                //// Reference to System.Drawing library
                //parameters.ReferencedAssemblies.Add("System.dll"); //netstandard.dll
                ////var assemblies = DMEEditor.ConfigEditor.LoadedAssemblies.Where(p => p.FullName.Contains("Microsoft.ML") || p.FullName.Contains("netstandard"));
                ////var assemblyLocations = assemblies.Select(a => a.Location).ToList();
                ////parameters.ReferencedAssemblies.AddRange(assemblyLocations.ToArray());

                //// True - memory generation, false - external file generation
                //parameters.GenerateInMemory = true;
                //// True - exe file generation, false - dll file generation
                //parameters.GenerateExecutable = false;

                //CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);
                //if (results.Errors.HasErrors)
                //{
                //    StringBuilder sb = new StringBuilder();

                //    foreach (CompilerError error in results.Errors)
                //    {
                //        sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                //        DMEEditor.AddLogMessage("Beep ML.NET", String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText), DateTime.Now, 0, null, Errors.Failed);
                //    }

                //    throw new InvalidOperationException(sb.ToString());
                //}

                // assembly = results.CompiledAssembly;
                assembly = RoslynCompiler.CreateAssembly(DMEEditor, code);

                //DMEEditor.ConfigEditor.LoadedAssemblies.Add(results.CompiledAssembly);
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
        #region "CodeDom Code"

      
        #endregion
    }
}
