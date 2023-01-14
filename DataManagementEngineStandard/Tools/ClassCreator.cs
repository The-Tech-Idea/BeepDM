using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Tools;
using Microsoft.CSharp;
using System.Threading;

namespace TheTechIdea.Beep.Tools
{
    public  class ClassCreator : IClassCreator
    {
        public string outputFileName { get ; set; }
        public string outputpath { get; set ; }

        public IDMEEditor DMEEditor { get; set; }
        private CodeCompileUnit targetUnit;
        private CodeTypeDeclaration targetClass;

        private CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
        private CodeGeneratorOptions options = new CodeGeneratorOptions();

      
       
        public ClassCreator(IDMEEditor pDMEEditor)
        {
            DMEEditor= pDMEEditor;
        }
        public void CompileClassFromText(string SourceString, string output)
        {
           
        }
        public string CreateDLL(string dllname, List<EntityStructure> entities, string outputpath, IProgress<PassedArgs> progress, CancellationToken token, string NameSpacestring = "TheTechIdea.ProjectClasses")
        {

            List<string> listofpaths = new List<string>();
            options.BracingStyle = "C";
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
                            PassedArgs ps = new PassedArgs { ParameterString1 = $"Error in Creating class for {item.EntityName}", EventType = "Error", ParameterInt1 = i, ParameterInt2 = total, ParameterString3 = ex.Message };
                            progress.Report(ps);

                        }
                    }
                  
                    i++;
                }
                outputFileName = dllname+".dll";
                if (outputpath == null)
                {
                    outputpath = Assembly.GetEntryAssembly().Location + "\\";
                }
               
                CSharpCodeProvider codeProvider = new CSharpCodeProvider();
                System.CodeDom.Compiler.CompilerParameters parameters = new CompilerParameters();
                if (progress != null)
                {
                    PassedArgs ps = new PassedArgs { ParameterString1 = "Creating DLL", EventType = "Update", ParameterInt1 = i, ParameterInt2 = total };
                    progress.Report(ps);

                }
                string ret = "ok";
               List<string> retval= CompileCode(provider, listofpaths,Path.Combine(outputpath, dllname + ".dll"));
                if (progress != null)
                {
                    if (retval.Count > 0)
                    {
                        if (retval[0]=="ok")
                        {
                            PassedArgs ps = new PassedArgs {  ParameterString1 = "Finished Creating DLL", EventType = "Finish", ParameterInt1 = i, ParameterInt2 = total };
                            progress.Report(ps);
                        }
                        else
                        {
                            ret = $"Error in Creating DLL {outputFileName}";
                            PassedArgs ps = new PassedArgs { Objects= new List<ObjectItem> { new ObjectItem { Name = "Errors", obj = retval } }, ParameterString1 = "Error in Creating DLL", EventType = "Fail", ParameterInt1 = i, ParameterInt2 = total };
                            progress.Report(ps);
                        }
                    }
                    

                }
                return ret;
            }
            catch (Exception ex)
            {

                return ex.Message;
            }
          
        }
        public string CreateClass(string classname, List<EntityField> flds, string poutputpath, string NameSpacestring = "TheTechIdea.ProjectClasses")
        {
            options.BracingStyle = "C";
            targetUnit = new CodeCompileUnit();
            CodeNamespace namespaces = new CodeNamespace(NameSpacestring);
           // namespaces.Imports.Add(new CodeNamespaceImport("System"));
            targetClass = new CodeTypeDeclaration(classname);
            targetClass.IsClass = true;
            targetClass.TypeAttributes =
                TypeAttributes.Public ;
            namespaces.Types.Add(targetClass);
            targetUnit.Namespaces.Add(namespaces);
            outputpath = poutputpath;
            outputFileName = classname;
            AddConstructor();

            foreach (var f in flds)
            {
                try
                {
                    AddProperties(f);
                }
                catch (Exception ex)
                {

                    throw;
                }

            }

            EntityField entity = new EntityField();
            //entity.fieldname = "Name";
            //entity.fieldtype= "System.String";
            //AddProperties(entity);
            entity = new EntityField();
            entity.fieldname = "RN";
            entity.fieldtype = "System.Int64";
            AddProperties(entity);
         
            if (outputpath == null)
            {
                outputpath = Assembly.GetEntryAssembly().Location + "\\";
            }
            GenerateCSharpCode(Path.Combine(outputpath, outputFileName + ".cs"));
            return NameSpacestring + "." + classname;
            return "ok";
            
        }

        public void GenerateCSharpCode(string fileName)
        {
         
            using (StreamWriter sourceWriter = new StreamWriter(fileName))
            {
                provider.GenerateCodeFromCompileUnit(
                    targetUnit, sourceWriter, options);
                
            }
        }

        #region "CodeDom Code"
       
        public void AddFields(EntityField fld)
        {
            CodeMemberField widthValueField = new CodeMemberField();
            widthValueField.Attributes = MemberAttributes.Private;
            widthValueField.Name = fld.fieldname+"Value";
            widthValueField.Type = new CodeTypeReference(Type.GetType(fld.fieldtype));
            //widthValueField.Comments.Add(new CodeCommentStatement(
            //    "The width of the object."));
            targetClass.Members.Add(widthValueField);
        }
        public void AddProperties(EntityField fld)
        {
            // Declare the read-only Width property.
            CodeTypeDeclaration newType = new CodeTypeDeclaration(fld.fieldtype);
            CodeSnippetTypeMember snippet = new CodeSnippetTypeMember();
           // snippet.Comments.Add(new CodeCommentStatement(" Generated by DeepDM property", true));
            string fldtype= Type.GetType(fld.fieldtype).ToString();
            if (fld.fieldtype.ToLower().Contains("decimal") || fld.fieldtype.ToLower().Contains("datetime"))
            {
                fldtype =fldtype + "?";
            }
            snippet.Text = "public " + fldtype + " "+ fld.fieldname + "  { get; set; }";
            targetClass.Members.Add(snippet);

            ////var thisReference = new CodeThisReferenceExpression();
            //CodeMemberProperty widthProperty = new CodeMemberProperty();
            //widthProperty.Attributes =
            //    MemberAttributes.Public | MemberAttributes.Final;
            //widthProperty.Name = fld.fieldname;
            //widthProperty.HasGet = true;
            //widthProperty.HasSet = true;
          
            ////widthProperty.Comments.Add(new CodeCommentStatement(
            ////    "The Width property for the object."));
            //widthProperty.Type = new CodeTypeReference(Type.GetType(fld.fieldtype));
           

            //widthProperty.Type = new CodeTypeReference(Type.GetType(fld.fieldtype));

            

            //widthProperty.GetStatements.Add(new CodeMethodReturnStatement(
            // new CodeFieldReferenceExpression(
            // new CodeThisReferenceExpression(), fld.fieldname + "Value")));
            //widthProperty.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(widthProperty, fld.fieldname + "Value"), new CodePropertySetValueReferenceExpression()));

            ///targetClass.Members.Add(widthProperty);

        }
        public void AddConstructor()
        {
            // Declare the constructor
            CodeConstructor constructor = new CodeConstructor();
            constructor.Attributes =
                MemberAttributes.Public | MemberAttributes.Final;

            //// Add parameters.
            //constructor.Parameters.Add(new CodeParameterDeclarationExpression(
            //    typeof(System.Double), "width"));
            //constructor.Parameters.Add(new CodeParameterDeclarationExpression(
            //    typeof(System.Double), "height"));

            // Add field initialization logic
            //CodeFieldReferenceExpression widthReference =
            //    new CodeFieldReferenceExpression(
            //    new CodeThisReferenceExpression(), "widthValue");
            //constructor.Statements.Add(new CodeAssignStatement(widthReference,
            //    new CodeArgumentReferenceExpression("width")));
            //CodeFieldReferenceExpression heightReference =
            //    new CodeFieldReferenceExpression(
            //    new CodeThisReferenceExpression(), "heightValue");
            //constructor.Statements.Add(new CodeAssignStatement(heightReference,
            //    new CodeArgumentReferenceExpression("height")));
            targetClass.Members.Add(constructor);
        }
        public  bool CompileCode(CodeDomProvider provider,
             String sourceFile,
             String exeFile)
        {

            CompilerParameters cp = new CompilerParameters();

            // Generate an executable instead of
            // a class library.
            cp.GenerateExecutable = false;

            // Set the assembly file name to generate.
            cp.OutputAssembly = exeFile;

            // Generate debug information.
            cp.IncludeDebugInformation = false;

            // Add an assembly reference.
            cp.ReferencedAssemblies.Add("System.dll");

            // Save the assembly as a physical file.
            cp.GenerateInMemory = false;

            // Set the level at which the compiler
            // should start displaying warnings.
            cp.WarningLevel = 3;

            // Set whether to treat all warnings as errors.
            cp.TreatWarningsAsErrors = false;

            // Set compiler argument to optimize output.
            cp.CompilerOptions = "/optimize";

            // Set a temporary files collection.
            // The TempFileCollection stores the temporary files
            // generated during a build in the current directory,
            // and does not delete them after compilation.
            cp.TempFiles = new TempFileCollection(".", true);

            //if (provider.Supports(GeneratorSupport.EntryPointMethod))
            //{
            //    // Specify the class that contains
            //    // the main method of the executable.
            //    cp.MainClass = "Samples.Class1";
            //}

            if (Directory.Exists("Resources"))
            {
                if (provider.Supports(GeneratorSupport.Resources))
                {
                    // Set the embedded resource file of the assembly.
                    // This is useful for culture-neutral resources,
                    // or default (fallback) resources.
                    cp.EmbeddedResources.Add("Resources\\Default.resources");

                    // Set the linked resource reference files of the assembly.
                    // These resources are included in separate assembly files,
                    // typically localized for a specific language and culture.
                    cp.LinkedResources.Add("Resources\\nb-no.resources");
                }
            }

            // Invoke compilation.
            CompilerResults cr = provider.CompileAssemblyFromFile(cp, sourceFile);

            if (cr.Errors.Count > 0)
            {
                // Display compilation errors.
                Console.WriteLine("Errors building {0} into {1}",
                    sourceFile, cr.PathToAssembly);
                foreach (CompilerError ce in cr.Errors)
                {
                    Console.WriteLine("  {0}", ce.ToString());
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("Source {0} built into {1} successfully.",
                    sourceFile, cr.PathToAssembly);
                Console.WriteLine("{0} temporary files created during the compilation.",
                    cp.TempFiles.Count.ToString());
            }

            // Return the results of compilation.
            if (cr.Errors.Count > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public  List<string> CompileCode(CodeDomProvider provider,
           List<string> sourceFiles,
           String exeFile)
        {
            List<string> retval = new List<string>();
            CompilerParameters cp = new CompilerParameters();

            // Generate an executable instead of
            // a class library.
            cp.GenerateExecutable = false;

            // Set the assembly file name to generate.
            cp.OutputAssembly = exeFile;

            // Generate debug information.
            cp.IncludeDebugInformation = false;

            // Add an assembly reference.
          //  cp.ReferencedAssemblies.Add("System.dll");

            // Save the assembly as a physical file.
            cp.GenerateInMemory = false;

            // Set the level at which the compiler
            // should start displaying warnings.
            cp.WarningLevel = 3;

            // Set whether to treat all warnings as errors.
            cp.TreatWarningsAsErrors = false;

            // Set compiler argument to optimize output.
            cp.CompilerOptions = "/optimize";

            // Set a temporary files collection.
            // The TempFileCollection stores the temporary files
            // generated during a build in the current directory,
            // and does not delete them after compilation.
            cp.TempFiles = new TempFileCollection(".", true);

            //if (provider.Supports(GeneratorSupport.EntryPointMethod))
            //{
            //    // Specify the class that contains
            //    // the main method of the executable.
            //    cp.MainClass = "Samples.Class1";
            //}

            if (Directory.Exists("Resources"))
            {
                if (provider.Supports(GeneratorSupport.Resources))
                {
                    // Set the embedded resource file of the assembly.
                    // This is useful for culture-neutral resources,
                    // or default (fallback) resources.
                    cp.EmbeddedResources.Add("Resources\\Default.resources");

                    // Set the linked resource reference files of the assembly.
                    // These resources are included in separate assembly files,
                    // typically localized for a specific language and culture.
                    cp.LinkedResources.Add("Resources\\nb-no.resources");
                }
            }

            // Invoke compilation.
            CompilerResults cr = provider.CompileAssemblyFromFile(cp, sourceFiles.ToArray());

            if (cr.Errors.Count > 0)
            {
                // Display compilation errors.
                Console.WriteLine("Errors building {0} into {1}",
                    exeFile, cr.PathToAssembly);
                retval.Add($"Errors building {exeFile} into {cr.PathToAssembly}");
                foreach (CompilerError ce in cr.Errors)
                {
                    Console.WriteLine("  {0}", ce.ToString());
                    retval.Add(ce.ToString());
                    Console.WriteLine();
                }
            }
            else
            {
                retval.Add("ok");
                Console.WriteLine("Source {0} built into {1} successfully.",
                    exeFile, cr.PathToAssembly);
                Console.WriteLine("{0} temporary files created during the compilation.",
                    cp.TempFiles.Count.ToString());
            }

            //// Return the results of compilation.
            //if (cr.Errors.Count > 0)
            //{
            //    return false;
            //}
            //else
            //{
            //    return true;
            //}
            return retval;
        }
        #endregion
    }
}
