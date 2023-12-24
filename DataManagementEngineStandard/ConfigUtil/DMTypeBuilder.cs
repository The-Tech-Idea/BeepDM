using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Roslyn;

namespace TheTechIdea.Util
{
    public static class DMTypeBuilder 
    {
      
        public static IDMEEditor DMEEditor { get; set; }
        public static Type myType { get; set; }
        public static object myObject { get; set; }
        public static  TypeBuilder tb { get; set; }
        public static  AssemblyBuilder ab { get; set; }
        public static object CreateNewObject(IDMEEditor DMEEditor, string classnamespace, string typename, List<EntityField> MyFields)
        {
            string typenamespace = string.Empty;
            if (!string.IsNullOrEmpty(classnamespace))
            {
                typenamespace = classnamespace;
            }else typenamespace = "TheTechIdea.Classes";
            //myType = CompileResultType(libname, typenamespace,typename, MyFields);
            //myObject = Activator.CreateInstance(myType);
            EntityStructure ent=new EntityStructure() { Fields=MyFields,EntityName= typename };
            string cls = ConvertPOCOClassToEntity(DMEEditor,  ent,  typenamespace);
            Tuple<Type, Assembly> retval = RoslynCompiler.CompileClassTypeandAssembly(typename,cls);
            //myType = CreateTypeFromCode(cls, typenamespace+"."+ typename);
            // Type type = CreateTypeFromCode(cls,  typename); ;
            myType=retval.Item1;
             myObject = Activator.CreateInstance(myType);


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
   
        public static Type CreateTypeFromCode(IDMEEditor DMEEditor, string code, string outputtypename)
        {
            Type OutputType = null;
            Assembly assembly = null;
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            //    assembly = CreateAssembly(DMEEditor, code);
                 assembly=RoslynCompiler.CreateAssembly(DMEEditor, code);
                OutputType = assembly.GetType(outputtypename);

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep ML.NET", $" Error Compiling Code {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return OutputType;
        }
        public static string ConvertPOCOClassToEntity(IDMEEditor DMEEditor, EntityStructure entityStructure,string typenamespace)
        {
            string usingtxt = "using TheTechIdea.Beep.Editor;\r\nusing System.Collections.Generic;\r\nusing System.ComponentModel;\r\nusing System.Runtime.CompilerServices;" + Environment.NewLine;
            
            return DMEEditor.classCreator.CreatEntityClass(entityStructure, usingtxt, null, null, typenamespace, false);
        }
       
      
    }
}

