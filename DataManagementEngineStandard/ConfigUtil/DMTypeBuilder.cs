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
    /// <summary>
    /// A utility class for building and manipulating dynamic types.
    /// </summary>
    public static class DMTypeBuilder 
    {
      
         /// <summary>
        /// Gets or sets the DMEEditor instance.
        /// </summary>
        public static IDMEEditor DMEEditor { get; set; }
        public static Type myType { get; set; }
        public static object myObject { get; set; }
        public static  TypeBuilder tb { get; set; }
        public static  AssemblyBuilder ab { get; set; }
        private static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();
        /// <summary>Creates a new object of a specified type.</summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="classnamespace">The namespace of the class.</param>
        /// <param name="typename">The name of the type.</param>
        /// <param name="MyFields">A list of EntityField objects representing the fields of the entity.</param>
        /// <returns>A new object of the specified type.</returns>
        /// <remarks>
        /// If the classnamespace is not provided, the default namespace "TheTechIdea.Classes" will be used.
        /// The method first creates an EntityStructure object using the provided typename and MyFields.
        /// Then, it converts the P
        public static object CreateNewObject(IDMEEditor DMEEditor, string classnamespace, string typename, List<EntityField> MyFields)
        {

            string typenamespace = string.Empty;
            if (!string.IsNullOrEmpty(classnamespace))
            {
                typenamespace = classnamespace;
            }else typenamespace = "TheTechIdea.Classes";

            string fullTypeName = $"{typenamespace}.{typename}".ToUpper();
            // Check if the type is already cached
            if (!typeCache.ContainsKey(fullTypeName))
            {
                EntityStructure ent = new EntityStructure() { Fields = MyFields, EntityName = typename };
                string cls = ConvertPOCOClassToEntity(DMEEditor, ent, typenamespace);
                Tuple<Type, Assembly> retval = RoslynCompiler.CompileClassTypeandAssembly(typename, cls);
                if (retval != null)
                {
                    myType = retval.Item1;
                    myObject = Activator.CreateInstance(myType);

                    typeCache.Add(fullTypeName, myType);

                }
            }
            else
            {
                // Use the cached type
                myType = typeCache[fullTypeName];
                myObject = Activator.CreateInstance(myType);
            }
            
            
           

            return myObject;

        }
        /// <summary>Creates a new object based on the specified library, type namespace, type name, and list of entity fields.</summary>
        /// <param name="libname">The name of the library.</param>
        /// <param name="typenamespace">The namespace of the type.</param>
        /// <param name="typename">The name of the type.</param>
        /// <param name="MyFields">The list of entity fields.</param>
        /// <returns>A new object of the specified type.</returns>
        public static object CreateNewObject(string libname,string typenamespace, string typename, List<EntityField> MyFields)
        {
            
            myType = CompileResultType(libname, typenamespace,typename, MyFields);
            myObject = Activator.CreateInstance(myType);
            return myObject;

        }
       
        /// <summary>Compiles a result type dynamically based on the provided parameters.</summary>
        /// <param name="libname">The name of the library.</param>
        /// <param name="typenamespace">The namespace of the type.</param>
        /// <param name="typename">The name of the type.</param>
        /// <param name="MyFields">A list of EntityField objects representing the fields of the type.</param>
        /// <returns>The compiled result type.</returns>
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
        /// <summary>Creates a dynamic type builder.</summary>
        /// <param name="libname">The name of the dynamic assembly.</param>
        /// <param name="typenamespace">The namespace of the type.</param>
        /// <param name="typename">The name of the type.</param>
        /// <returns>A TypeBuilder object representing the dynamic type.</returns>
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
        /// <summary>Creates a property in a dynamically generated type.</summary>
        /// <param name="tb">The type builder representing the dynamically generated type.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyType">The type of the property.</param>
        /// <remarks>
        /// This method creates a property with the specified name and type in a dynamically generated type represented by the type builder.
        /// The property has a private backing field with a name prefixed by an underscore.
        /// The property has both a getter and a setter method.
        /// The getter method retrieves the value of the backing field.
        /// The setter method sets the value of the backing field.
        /// </remarks>
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
   
        /// <summary>Creates a type from code using the Roslyn compiler.</summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="code">The code to compile.</param>
        /// <param name="outputtypename">The name of the output type.</param>
        /// <returns>The created type.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during compilation.</exception>
        public static Type CreateTypeFromCode(IDMEEditor DMEEditor, string code, string outputtypename)
        {
            Type OutputType = null;
            Assembly assembly = null;
            try
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                string fullTypeName = $"TheTechIdea.Classes.{outputtypename}".ToUpper();
                // Check if the type is already cached
                if (!typeCache.ContainsKey(fullTypeName))
                {
                    assembly = RoslynCompiler.CreateAssembly(DMEEditor, code);
                    OutputType = assembly.GetType(outputtypename);
                }else
                {

                   // Use the cached type
                    OutputType = typeCache[fullTypeName];
                }
                  

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep ML.NET", $" Error Compiling Code {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return OutputType;
        }
        /// <summary>Converts a Plain Old CLR Object (POCO) class to an entity class.</summary>
        /// <param name="DMEEditor">The IDMEEditor instance.</param>
        /// <param name="entityStructure">The EntityStructure object representing the POCO class.</param>
        /// <param name="typenamespace">The namespace of the entity class.</param>
        /// <returns>A string representing the entity class.</returns>
        /// <remarks>
        /// This method converts a POCO class to an entity class by utilizing the classCreator property of the DMEEditor instance.
        /// It adds necessary using statements and passes them to the CreatEntityClass method of the classCreator.
        /// The resulting entity class is returned as a
        public static string ConvertPOCOClassToEntity(IDMEEditor DMEEditor, EntityStructure entityStructure,string typenamespace)
        {
            string usingtxt = "using TheTechIdea.Beep.Editor;\r\nusing System.Collections.Generic;\r\nusing System.ComponentModel;\r\nusing System.Runtime.CompilerServices;" + Environment.NewLine;
            
            return DMEEditor.classCreator.CreatEntityClass(entityStructure, usingtxt, null, null, typenamespace, false);
        }
       
      
    }
}

