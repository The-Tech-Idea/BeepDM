using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Roslyn;

namespace TheTechIdea.Beep.Utilities
{
    /// <summary>
    /// A utility class for building and manipulating dynamic types.
    /// </summary>
    public static class DMTypeBuilder
    {
        /// <summary>Gets or sets the DMEEditor instance.</summary>
        public static IDMEEditor DMEEditor { get; set; }

        public static Type MyType { get; set; }
        public static object MyObject { get; set; }
        private static TypeBuilder typeBuilder;
        private static AssemblyBuilder assemblyBuilder;
        private static ModuleBuilder moduleBuilder;

        /// <summary>Caches generated types to improve performance.</summary>
        public static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

        /// <summary>Maintains namespace mappings for types.</summary>
        public static Dictionary<string, string> DataSourceNameSpace { get; set; } = new Dictionary<string, string>();

        private static readonly object logLock = new object(); // Thread-safe logging

        /// <summary>
        /// Creates a new dynamic object based on the specified parameters.
        /// </summary>
        /// <param name="editor">The IDMEEditor instance.</param>
        /// <param name="classNamespace">The namespace for the class.</param>
        /// <param name="dataSourceName">The data source name to build namespace.</param>
        /// <param name="typeName">The name of the type.</param>
        /// <param name="fields">List of fields defining the type structure.</param>
        /// <returns>A new dynamic object of the generated type.</returns>
        public static object CreateNewObject(IDMEEditor editor, string classNamespace, string dataSourceName, string typeName, List<EntityField> fields)
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("Type name cannot be null or empty.", nameof(typeName));

            // Namespace resolution
            string fullNamespace = string.IsNullOrEmpty(classNamespace)
                ? (!string.IsNullOrEmpty(dataSourceName) ? dataSourceName : "TheTechIdea.Classes")
                : classNamespace;

            string fullTypeName = $"{fullNamespace}.{typeName}";

            // Type caching
            if (!typeCache.ContainsKey(fullTypeName))
            {
                EntityStructure entity = new EntityStructure { Fields = fields, EntityName = typeName };
                string code = ConvertPOCOClassToEntity(editor, entity, fullNamespace);

                var compiled = RoslynCompiler.CompileClassTypeandAssembly(typeName, code);
                if (compiled != null)
                {
                    MyType = compiled.Item1;
                    MyObject = Activator.CreateInstance(MyType);
                    typeCache[fullTypeName] = MyType;
                }
                else
                    throw new InvalidOperationException($"Failed to compile type '{typeName}' in namespace '{fullNamespace}'.");
            }
            else
            {
                MyType = typeCache[fullTypeName];
                MyObject = Activator.CreateInstance(MyType);
            }

            return MyObject;
        }
        /// <summary>
        /// Creates a new dynamic object based on the specified parameters.
        /// </summary>
        /// <param name="editor">The IDMEEditor instance.</param>
        /// <param name="classNamespace">The namespace for the class. Defaults to "TheTechIdea.Classes" if null.</param>
        /// <param name="typeName">The name of the type.</param>
        /// <param name="fields">List of fields defining the type structure.</param>
        /// <returns>A new dynamic object of the generated type.</returns>
        public static object CreateNewObject(IDMEEditor editor, string classNamespace, string typeName, List<EntityField> fields)
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("Type name cannot be null or empty.", nameof(typeName));

            string fullNamespace = string.IsNullOrEmpty(classNamespace) ? "TheTechIdea.Classes" : classNamespace;
            string fullTypeName = $"{fullNamespace}.{typeName}";

            // Type caching
            if (!typeCache.ContainsKey(fullTypeName))
            {
                EntityStructure entity = new EntityStructure { Fields = fields, EntityName = typeName };
                string code = ConvertPOCOClassToEntity(editor, entity, fullNamespace);

                var compiled = RoslynCompiler.CompileClassTypeandAssembly(typeName, code);
                if (compiled != null)
                {
                    MyType = compiled.Item1;
                    MyObject = Activator.CreateInstance(MyType);
                    typeCache[fullTypeName] = MyType;
                }
                else
                    throw new InvalidOperationException($"Failed to compile type '{typeName}' in namespace '{fullNamespace}'.");
            }
            else
            {
                MyType = typeCache[fullTypeName];
                MyObject = Activator.CreateInstance(MyType);
            }

            return MyObject;
        }

        /// <summary>Generates or retrieves a namespace for a type.</summary>
        private static string GetOrCreateNamespace(string typeName, string dataSourceName)
        {
            if (!DataSourceNameSpace.ContainsKey(typeName))
                DataSourceNameSpace[typeName] = $"{dataSourceName}.{typeName}";

            return DataSourceNameSpace[typeName].Split('.').Reverse().Skip(1).Reverse().Aggregate((a, b) => $"{a}.{b}");
        }

        /// <summary>Compiles a dynamic type with the specified fields.</summary>
        private static Type CompileResultType(string libName, string namespaceName, string typeName, List<EntityField> fields)
        {
            typeBuilder = GetTypeBuilder(libName, namespaceName, typeName);
            foreach (var field in fields)
                CreateProperty(typeBuilder, SanitizeFieldName(field.fieldname), ResolveType(field.fieldtype));
            return typeBuilder.CreateTypeInfo();
        }

        /// <summary>Defines a dynamic TypeBuilder.</summary>
        private static TypeBuilder GetTypeBuilder(string libName, string namespaceName, string typeName)
        {
            if (assemblyBuilder == null || moduleBuilder == null)
            {
                assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(libName), AssemblyBuilderAccess.RunAndCollect);
                moduleBuilder = assemblyBuilder.DefineDynamicModule(libName);
            }
            return moduleBuilder.DefineType($"{namespaceName}.{typeName}", TypeAttributes.Public | TypeAttributes.Class);
        }

        /// <summary>Creates a property dynamically.</summary>
        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
        {
            FieldBuilder field = tb.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private);

            PropertyBuilder property = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getMethod = tb.DefineMethod($"get_{propertyName}", MethodAttributes.Public | MethodAttributes.SpecialName, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getMethod.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, field);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setMethod = tb.DefineMethod($"set_{propertyName}", MethodAttributes.Public | MethodAttributes.SpecialName, null, new[] { propertyType });
            ILGenerator setIl = setMethod.GetILGenerator();
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, field);
            setIl.Emit(OpCodes.Ret);

            property.SetGetMethod(getMethod);
            property.SetSetMethod(setMethod);
        }

        /// <summary>Converts a POCO class definition into an entity class.</summary>
        public static string ConvertPOCOClassToEntity(IDMEEditor editor, EntityStructure entityStructure, string namespaceName)
        {
            string usingText = "using System;\nusing System.ComponentModel;\nusing System.Runtime.CompilerServices;";
            return editor.classCreator.CreatEntityClass(entityStructure, usingText, null, null, namespaceName, false);
        }

        /// <summary>Resolves .NET type names into actual `Type` objects.</summary>
        private static Type ResolveType(string typeName)
        {
            try
            {
                return typeName switch
                {
                    "System.Int32" => typeof(int),
                    "System.String" => typeof(string),
                    "System.Boolean" => typeof(bool),
                    "System.Decimal" => typeof(decimal),
                    "System.DateTime" => typeof(DateTime),
                    _ => Type.GetType(typeName) ?? typeof(object)
                };
            }
            catch (Exception ex)
            {
                LogError($"Error resolving type: {typeName}", ex);
                return typeof(object); // Fallback type
            }
        }

        /// <summary>Sanitizes field names to ensure they are valid .NET identifiers.</summary>
        private static string SanitizeFieldName(string fieldName)
        {
            return string.IsNullOrWhiteSpace(fieldName) ? "UnnamedField" : fieldName.Replace(" ", "_").Replace("-", "_");
        }

        /// <summary>Logs errors safely in a thread-safe manner.</summary>
        private static void LogError(string message, Exception ex)
        {
            lock (logLock)
            {
                DMEEditor?.AddLogMessage("Error", $"{message}: {ex.Message}", DateTime.Now, 0, ex.StackTrace, Errors.Failed);
            }
        }
    }
}
