
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TheTechIdea.DataManagment_Engine.DataBase;

namespace TheTechIdea.Util
{
    public static class DMTypeBuilder 
    {
        public static Type myType { get; set; }
        public static object myObject { get; set; }
        public static  TypeBuilder tb { get; set; }
        public static  AssemblyBuilder ab { get; set; }
        public static object CreateNewObject(string libname, string typename, List<EntityField> MyFields)
        {
            myType = CompileResultType(libname, typename, MyFields);
            myObject = Activator.CreateInstance(myType);
            return myObject;

        }
        public static Type CompileResultType(string libname, string typename, List<EntityField> MyFields)
        {
            tb = GetTypeBuilder(libname, typename);
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            // NOTE: assuming your list contains Field objects with fields FieldName(string) and FieldType(Type)
            foreach (var f in MyFields)
                CreateProperty(tb, f.fieldname, Type.GetType(f.fieldtype));

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

    }
}

