using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TheTechIdea.DataManagment_Engine.DataBase;
using TheTechIdea.Tools;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.Util;
using Microsoft.CSharp;

namespace TheTechIdea.DataManagment_Engine.Tools
{
    public  class ClassCreatorv2 : IClassCreator
    {
        public string outputFileName { get ; set; }
        public string outputpath { get; set ; }

        public IDMEEditor DMEEditor { get; set; }
        private CodeCompileUnit targetUnit;
        private CodeTypeDeclaration targetClass;

        private CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
        private CodeGeneratorOptions options = new CodeGeneratorOptions();

      
       
        public ClassCreatorv2()
        {
           
        }
        public void CompileClassFromText(string SourceString, string output)
        {
            throw new NotImplementedException();
        }
        public string CreateDLL(string dllname, List<EntityStructure> entities, string outputpath, string NameSpacestring = "TheTechIdea.ProjectClasses")
        {
           

            options.BracingStyle = "C";
            targetUnit = new CodeCompileUnit();
            CodeNamespace namespaces = new CodeNamespace(NameSpacestring);
            try
            {
                foreach (EntityStructure item in entities)
                {
                    string classname = item.EntityName;
                    targetClass = new CodeTypeDeclaration(classname);
                    targetClass.IsClass = true;
                    targetClass.TypeAttributes =
                        TypeAttributes.Public;
                    namespaces.Types.Add(targetClass);
                    targetUnit.Namespaces.Add(namespaces);
                    
               
                    AddConstructor();

                    foreach (var f in item.Fields)
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
                    entity.fieldname = "Name";
                    entity.fieldtype = "System.String";
                    AddProperties(entity);
                    entity = new EntityField();
                    entity.fieldname = "RN";
                    entity.fieldtype = "System.Int64";
                    AddProperties(entity);



                }
                outputFileName = dllname+".dll";
                if (outputpath == null)
                {
                    outputpath = Assembly.GetEntryAssembly().Location + "\\";
                }
                GenerateCSharpCode(Path.Combine(outputpath, dllname + ".cs"));
                CSharpCodeProvider codeProvider = new CSharpCodeProvider();
                System.CodeDom.Compiler.CompilerParameters parameters = new CompilerParameters();
                //parameters.GenerateExecutable = true;
                //parameters.OutputAssembly = dllname+".dll";
                CompileCode(provider, Path.Combine(outputpath, dllname + ".cs"), Path.Combine(outputpath, dllname + ".dll"));
                //  CompilerResults results = codeProvider.CompileAssemblyFromFile(parameters,);
                return "ok";
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
            snippet.Comments.Add(new CodeCommentStatement(" Generated by DeepDM property", true));
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
        public static bool CompileCode(CodeDomProvider provider,
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
            cp.IncludeDebugInformation = true;

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
        #endregion
    }
}
