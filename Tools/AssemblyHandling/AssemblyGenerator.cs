using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;


namespace TheTechIdea.Tools
{
    public static class AssemblyGenerator
    {
            
            public static Type myType { get; set; }
            public static object myObject { get; set; }
            public static TypeBuilder tb { get; set; }
            public static AssemblyBuilder ab { get; set; }
            public static AssemblyItem item { get; set; }
             public static void SaveAssembly(string path)
             {
              ab.Save(Path.Combine(path, item.Assemblyname));
              }

            public static object CreateObject(AssemblyItem pitem )
        {
            CreateNewObject(item.Assemblyname, item.Typename, item.MyFields);
            return myObject;

        }
            public static void CreateNewObject(string libname, string typename, List<AssemblyItemFieldDataTypes> MyFields)
            {
                myType = CompileResultType(libname, typename, MyFields);
                myObject = Activator.CreateInstance(myType);

            }
            private static Type CompileResultType(string libname, string typename, List<AssemblyItemFieldDataTypes> MyFields)
            {
                tb = GetTypeBuilder(libname, typename);
                ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

                // NOTE: assuming your list contains Field objects with fields FieldName(string) and FieldType(Type)
                foreach (var f in MyFields)
                    CreateProperty(tb, f.fieldName, Type.GetType(f.fieldType));

                Type objectType = tb.CreateTypeInfo();
                return objectType;
            }
            private static TypeBuilder GetTypeBuilder(string libname, string typename)
            {

                ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(libname), AssemblyBuilderAccess.RunAndCollect);
                // For a single-module assembly, the module name is usually
                // the assembly name plus an extension.
                ModuleBuilder mb = ab.DefineDynamicModule(libname);
                TypeBuilder tb = mb.DefineType(typename, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass |
                        TypeAttributes.AnsiClass |
                        TypeAttributes.BeforeFieldInit |
                        TypeAttributes.AutoLayout);

                return tb;
            }
            private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
            {
                FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

                PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
                MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
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
            public static Assembly CompileTextCodetoAssembly(string AssemblyName, string txtcode)
        {
            string src = txtcode;
            var compParms = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };
            var csProvider = new CSharpCodeProvider();
            CompilerResults compilerResults = csProvider.CompileAssemblyFromSource(compParms, src);
            return compilerResults.CompiledAssembly;
        }
            public static Enum CreateDynamicEnum(List<string> _list, string EnumName)
        {
            // Get the current application domain for the current thread.
            AppDomain currentDomain = AppDomain.CurrentDomain;

            // Create a dynamic assembly in the current application domain, 
            // and allow it to be executed and saved to disk.
            AssemblyName aName = new AssemblyName("TempAssembly");
            AssemblyBuilder ab = currentDomain.DefineDynamicAssembly(
                aName, AssemblyBuilderAccess.RunAndSave);

            // Define a dynamic module in "TempAssembly" assembly. For a single-
            // module assembly, the module has the same name as the assembly.
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");

            // Define a public enumeration with the name "Elevation" and an 
            // underlying type of Integer.
            EnumBuilder eb = mb.DefineEnum(EnumName, TypeAttributes.Public, typeof(int));

            // Define two members, "High" and "Low".
            //eb.DefineLiteral("Low", 0);
            //eb.DefineLiteral("High", 1);

            int i = 0;
            foreach (string item in _list)
            {
                eb.DefineLiteral(item, i);
                i++;
            }

            // Create the type and save the assembly.
            return (Enum)Activator.CreateInstance(eb.CreateType());
            //ab.Save(aName.Name + ".dll");


        }

    }
    
}
