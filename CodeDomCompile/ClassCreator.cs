using System;
using System.Reflection;
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using TheTechIdea.DataManagment_Engine.DataBase;
using Microsoft.CSharp;

namespace TheTechIdea.Tools
{
   

    public class ClassCreator : IClassCreator
    {
        CodeCompileUnit targetUnit;
        CodeTypeDeclaration targetClass;
        public string outputFileName { get; set; }
        public string outputpath { get; set; }
        public ClassCreator()
        {

        }

       
        public void CreateClassFromText(string SourceString,string output)
        {
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            ICodeCompiler icc = codeProvider.CreateCompiler();
            System.CodeDom.Compiler.CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateExecutable = true;
            parameters.OutputAssembly = output;
            CompilerResults results = icc.CompileAssemblyFromSource(parameters, SourceString);

        }
        //public ClassCreator(string classname, List<EntityField> flds, string outpath)
        //{

        //    outputpath = outpath;
        //    outputFileName = classname.ToLower();
        //    CreateClass(outputFileName, flds, outpath);

        //}
        public void CreateClass(string classname, List<EntityField> flds, string outpath)
        {
            outputpath = outpath;
            outputFileName = classname.ToLower();
            CreateClass(classname);
            outputFileName = classname;
            AddConstructor();

            foreach (var f in flds)
                AddProperties(f.fieldname.ToLower(), MemberAttributes.Public | MemberAttributes.Final, Type.GetType(f.fieldtype), "");
            AddProperties("ClassName", MemberAttributes.Public | MemberAttributes.Final, Type.GetType("System.String"), "");
            if (outputpath == null)
            {
                outputpath = Environment.CurrentDirectory;
            }
            GenerateCSharpCode(Path.Combine(outputpath, outputFileName + ".cs"));

        }
        private void CreateClass(string classname)
        {
            targetUnit = new CodeCompileUnit();
            CodeNamespace samples = new CodeNamespace("TheTechIdea.ProjectClasses");
            samples.Imports.Add(new CodeNamespaceImport("System"));
            targetClass = new CodeTypeDeclaration(classname);
            targetClass.IsClass = true;
            targetClass.TypeAttributes = TypeAttributes.Public | TypeAttributes.Serializable;
            samples.Types.Add(targetClass);
            targetUnit.Namespaces.Add(samples);
        }
        private void AddFields(string fieldname, MemberAttributes attributes, System.Type type, string comments)
        {
            // Declare the widthValue field.
            CodeMemberField widthValueField = new CodeMemberField();
            widthValueField.Attributes = attributes;
            widthValueField.Name = fieldname;

            widthValueField.Type = new CodeTypeReference(type);
            widthValueField.Comments.Add(new CodeCommentStatement(
                comments));
            targetClass.Members.Add(widthValueField);


        }
        private void AddProperties(string propertyname, MemberAttributes attributes, System.Type type, string comments)
        {
            // Declare the read-only Width property.
            AddFields(propertyname + "Value", attributes, type, comments);
            CodeMemberProperty widthProperty = new CodeMemberProperty();
            widthProperty.Attributes = attributes; //                MemberAttributes.Public | MemberAttributes.Final;
            widthProperty.Name = propertyname;
            widthProperty.HasGet = true;
            widthProperty.HasSet = true;
            widthProperty.Type = new CodeTypeReference(type);
            widthProperty.Comments.Add(new CodeCommentStatement(
                comments));
            widthProperty.GetStatements.Add(new CodeMethodReturnStatement(
                new CodeFieldReferenceExpression(
                new CodeThisReferenceExpression(), propertyname + "Value")));
            widthProperty.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), propertyname + "Value"), new CodePropertySetValueReferenceExpression()));
            targetClass.Members.Add(widthProperty);
        }
        private void AddConstructor()
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
        public void GenerateCSharpCode(string fileName)
        {
            outputFileName = fileName;
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "C";
            using (StreamWriter sourceWriter = new StreamWriter(fileName))
            {
                provider.GenerateCodeFromCompileUnit(
                    targetUnit, sourceWriter, options);
            }
        }
        //private void AddInterface()
        //{
        //    helloWorldTypeBuilder.AddInterfaceImplementation(typeof(IGenericDataClass));

        //    // Generate IL for 'SayHello' method.
        //    ILGenerator myMethodIL = myMethodBuilder.GetILGenerator();
        //    myMethodIL.EmitWriteLine(myGreetingField);
        //    myMethodIL.Emit(OpCodes.Ret);
        //    MethodInfo sayHelloMethod = typeof(IHello).GetMethod("SayHello");
        //    helloWorldTypeBuilder.DefineMethodOverride(myMethodBuilder, sayHelloMethod);
        //}
        private static void loadByAssemblyNameAndTypeName(string assemblyName, string typeName)
        {
            Assembly loadedAssembly = Assembly.LoadFile(assemblyName);

        }
    }
}
