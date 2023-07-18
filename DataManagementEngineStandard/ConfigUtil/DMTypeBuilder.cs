
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Tools;

namespace TheTechIdea.Util
{
    public static class DMTypeBuilder 
    {
      
        public static IDMEEditor DMEEditor { get; set; }
        public static Type myType { get; set; }
        public static object myObject { get; set; }
        public static  TypeBuilder tb { get; set; }
        public static  AssemblyBuilder ab { get; set; }
        public static object CreateNewObject(string libname, string typename, List<EntityField> MyFields)
        {
            string typenamespace = "TheTechIdea.Classes";
            //myType = CompileResultType(libname, typenamespace,typename, MyFields);
            //myObject = Activator.CreateInstance(myType);
            EntityStructure ent=new EntityStructure() { Fields=MyFields,EntityName=libname};
            string cls = CreatEntityClass(ent, null, null, null, typenamespace, false);
            myType = CreateTypeFromCode(cls, libname);
            myObject= Activator.CreateInstance(myType);


            return myObject;

        }
        public static object CreateNewObject(string libname,string typenamespace, string typename, List<EntityField> MyFields)
        {
            
            myType = CompileResultType(libname, typenamespace,typename, MyFields);
            myObject = Activator.CreateInstance(myType);
            return myObject;

        }
        private static Type CompileResultType(string libname, string typenamespace, string typename, List<EntityField> MyFields)
        {
            tb = GetTypeBuilder(libname, typenamespace, typename);
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            // NOTE: assuming your list contains Field objects with fields FieldName(string) and FieldType(Type)
            foreach (var f in MyFields)
                CreateProperty(tb, f.fieldname, Type.GetType(f.fieldtype));

            Type objectType = tb.CreateTypeInfo();
            return objectType;
        }
        private static TypeBuilder GetTypeBuilder(string libname, string typenamespace, string typename)
        {

            ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(libname), AssemblyBuilderAccess.RunAndCollect);
            // For a single-module assembly, the module name is usually
            // the assembly name plus an extension.
            ModuleBuilder mb = ab.DefineDynamicModule(libname);
            mb.DefineType(typenamespace,
               TypeAttributes.Public | TypeAttributes.Interface,
                typeof(object));
            TypeBuilder tb = mb.DefineType(typename, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout);
            

            return tb;
        }
        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
        {
            PropertyBuilder propertyBuilder;
            MethodBuilder getPropMthdBldr;
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);
            //if (propertyType.GetTypeInfo().FullName=="System.DateTime")
            //{
            //     propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, typeof(Nullable<>).MakeGenericType(propertyType), null);
            //     getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(Nullable<>).MakeGenericType(propertyType), Type.EmptyTypes);
            //}
            //else
            //{
                 propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
                 getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            //}

          
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
        public static Assembly CreateAssembly(IDMEEditor DMEEditor, string code)
        {
            Assembly assembly = null;
            try
            {
                CSharpCodeProvider provider = new CSharpCodeProvider();
                CompilerParameters parameters = new CompilerParameters();
                // Reference to System.Drawing library
                parameters.ReferencedAssemblies.Add("System.dll"); //netstandard.dll
                //var assemblies = DMEEditor.ConfigEditor.LoadedAssemblies.Where(p => p.FullName.Contains("Microsoft.ML") || p.FullName.Contains("netstandard"));
                //var assemblyLocations = assemblies.Select(a => a.Location).ToList();
                //parameters.ReferencedAssemblies.AddRange(assemblyLocations.ToArray());

                // True - memory generation, false - external file generation
                parameters.GenerateInMemory = true;
                // True - exe file generation, false - dll file generation
                parameters.GenerateExecutable = false;

                CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);
                if (results.Errors.HasErrors)
                {
                    StringBuilder sb = new StringBuilder();

                    foreach (CompilerError error in results.Errors)
                    {
                        sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                        DMEEditor.AddLogMessage("Beep ML.NET", String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText), DateTime.Now, 0, null, Errors.Failed);
                    }

                    throw new InvalidOperationException(sb.ToString());
                }
                assembly = results.CompiledAssembly;
                //DMEEditor.ConfigEditor.LoadedAssemblies.Add(results.CompiledAssembly);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep ML.NET", $" Error Compiling Code {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return assembly;
        }
        public static Type CreateTypeFromCode(IDMEEditor DMEEditor, string code, string outputtypename)
        {
            Type OutputType = null;
            Assembly assembly = null;
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                assembly = CreateAssembly(DMEEditor, code);
                OutputType = assembly.GetType(outputtypename);

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep ML.NET", $" Error Compiling Code {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return OutputType;
        }
        public static string CreatEntityClass(EntityStructure entity, string usingheader, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true)
        {
            string implementations2 = " Entity ";


            string extracode2 = null;//Environment.NewLine + " public event PropertyChangedEventHandler PropertyChanged;\r\n\r\n    // This method is called by the Set accessor of each property.\r\n    // The CallerMemberName attribute that is applied to the optional propertyName\r\n    // parameter causes the property name of the caller to be substituted as an argument.\r\n    private void NotifyPropertyChanged([CallerMemberName] string propertyName = \"\")\r\n    {\r\n        if (PropertyChanged != null)\r\n        {\r\n            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));\r\n        }\r\n    }";
            //string template2 = null;//Environment.NewLine + " private :FIELDTYPE :FIELDNAMEValue ;";
            string template2 = Environment.NewLine + $" private :FIELDTYPE _:FIELDNAME. ;";
            template2 = Environment.NewLine + " public :FIELDTYPE :FIELDNAME\r\n    {\r\n        get\r\n        {\r\n            return this._:FIELDNAMEValue;\r\n        }\r\n\r\n        set\r\n        {\r\n            if (value != this._:FIELDNAMEValue)\r\n            {\r\n                this._:FIELDNAMEValue = value;\r\n                SetProperty(ref _:FIELDNAMEValue, value);;\r\n            }\r\n        }\r\n    }";
            return CreateClassFromTemplate(entity.EntityName, entity, template2, usingheader, implementations2, extracode2, outputpath, nameSpacestring, GenerateCSharpCodeFiles);
        }
        public static string CreateClassFromTemplate(string classname, EntityStructure entity, string template, string usingheader, string implementations, string extracode, string outputpath, string nameSpacestring = "TheTechIdea.ProjectClasses", bool GenerateCSharpCodeFiles = true)
        {
            string str = "";
            string filepath = "";
            if (string.IsNullOrEmpty(outputpath))
            {
                filepath = Path.Combine(DMEEditor.ConfigEditor.Config.ScriptsPath, $"{entity.EntityName}.cs");
            }
            else
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
                str += $"namespace  {nameSpacestring} ;" + Environment.NewLine;
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
                str += $"public  {clsname} ()" + "{}" + Environment.NewLine;
                for (int i = 0; i < entity.Fields.Count - 1; i++)
                {
                    EntityField fld = entity.Fields[i];
                    if (string.IsNullOrEmpty(template))
                    {
                        str += $"public {fld.fieldtype} {fld.fieldname}" + Environment.NewLine;
                    }
                    else
                    {
                        string extractedtemplate = template.Replace(":FIELDNAME", fld.fieldname);
                        extractedtemplate = extractedtemplate.Replace(":FIELDTYPE", fld.fieldtype);
                        str += extractedtemplate + Environment.NewLine;
                    }

                }

                str += "} " + Environment.NewLine; // end of Class
                if (string.IsNullOrEmpty(extracode))
                {
                    str += extracode + Environment.NewLine;
                }
                str += Environment.NewLine;

                //    str += "} " + Environment.NewLine; // end of namepspace
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
        public static Assembly CreateAssemblyFromCode(string code)
        {
            Assembly assembly = null;
            try
            {
                CSharpCodeProvider provider = new CSharpCodeProvider();
                CompilerParameters parameters = new CompilerParameters();
                // Reference to System.Drawing library
                parameters.ReferencedAssemblies.Add("System.dll"); //netstandard.dll
                //var assemblies = DMEEditor.ConfigEditor.LoadedAssemblies.Where(p => p.FullName.Contains("Microsoft.ML") || p.FullName.Contains("netstandard"));
                //var assemblyLocations = assemblies.Select(a => a.Location).ToList();
                //parameters.ReferencedAssemblies.AddRange(assemblyLocations.ToArray());

                // True - memory generation, false - external file generation
                parameters.GenerateInMemory = true;
                // True - exe file generation, false - dll file generation
                parameters.GenerateExecutable = false;

                CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);
                if (results.Errors.HasErrors)
                {
                    StringBuilder sb = new StringBuilder();

                    foreach (CompilerError error in results.Errors)
                    {
                        sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                        DMEEditor.AddLogMessage("Beep ML.NET", String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText), DateTime.Now, 0, null, Errors.Failed);
                    }

                    throw new InvalidOperationException(sb.ToString());
                }
                assembly = results.CompiledAssembly;
                //DMEEditor.ConfigEditor.LoadedAssemblies.Add(results.CompiledAssembly);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep ML.NET", $" Error Compiling Code {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return assembly;
        }
        public static Type CreateTypeFromCode(string code, string outputtypename)
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
    }
}

