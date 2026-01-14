using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Analysis;
using TheTechIdea.Beep.Roslyn;
using TheTechIdea.Beep.Tools;

namespace TheTechIdea.Beep.Tools.Helpers
{
    /// <summary>
    /// Helper for converting POCO classes to Entity classes with namespace scanning and runtime loading
    /// </summary>
    public class PocoToEntityGeneratorHelper
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;
        
        // Cache for generated runtime types
        private readonly ConcurrentDictionary<string, Type> _runtimeTypeCache = new ConcurrentDictionary<string, Type>();
        
        // Cache for generated assemblies
        private readonly ConcurrentDictionary<string, Assembly> _assemblyCache = new ConcurrentDictionary<string, Assembly>();

        public PocoToEntityGeneratorHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        #region Namespace Scanning

        /// <summary>
        /// Scans a namespace and discovers all POCO classes
        /// </summary>
        public List<Type> ScanNamespaceForPocos(string namespaceName, Assembly assembly = null)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));

            var assemblies = assembly != null 
                ? new[] { assembly } 
                : GetSearchableAssemblies();

            var pocoTypes = new List<Type>();

            foreach (var asm in assemblies)
            {
                try
                {
                    var types = asm.GetTypes()
                        .Where(t => t.Namespace != null && 
                                    t.Namespace.Equals(namespaceName, StringComparison.OrdinalIgnoreCase) &&
                                    IsPocoClass(t))
                        .ToList();

                    pocoTypes.AddRange(types);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Handle partially loaded assemblies
                    var loadedTypes = ex.Types.Where(t => t != null && 
                        t.Namespace != null && 
                        t.Namespace.Equals(namespaceName, StringComparison.OrdinalIgnoreCase) &&
                        IsPocoClass(t));
                    pocoTypes.AddRange(loadedTypes);
                }
                catch (Exception)
                {
                    // Skip assemblies that can't be loaded
                }
            }

            return pocoTypes.Distinct().ToList();
        }

        /// <summary>
        /// Finds a specific class by name in a namespace
        /// </summary>
        public Type FindClassByName(string namespaceName, string className, Assembly assembly = null)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentNullException(nameof(className));

            var assemblies = assembly != null 
                ? new[] { assembly } 
                : GetSearchableAssemblies();

            var fullTypeName = string.IsNullOrWhiteSpace(namespaceName) 
                ? className 
                : $"{namespaceName}.{className}";

            foreach (var asm in assemblies)
            {
                try
                {
                    // Try exact match first
                    var type = asm.GetType(fullTypeName, throwOnError: false, ignoreCase: true);
                    if (type != null && IsPocoClass(type))
                        return type;

                    // Try searching by class name only if no namespace provided
                    if (string.IsNullOrWhiteSpace(namespaceName))
                    {
                        type = asm.GetTypes()
                            .FirstOrDefault(t => t.Name.Equals(className, StringComparison.OrdinalIgnoreCase) &&
                                                  IsPocoClass(t));
                        if (type != null)
                            return type;
                    }
                }
                catch (Exception)
                {
                    // Skip assemblies that can't be loaded
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a type is a valid POCO class (not interface, abstract, static, etc.)
        /// </summary>
        private bool IsPocoClass(Type type)
        {
            if (type == null) return false;
            if (!type.IsClass) return false;
            if (type.IsAbstract) return false;
            if (type.IsInterface) return false;
            if (type.IsGenericTypeDefinition) return false;
            if (type.IsNested && !type.IsNestedPublic) return false;
            if (type.IsSealed && type.IsAbstract) return false; // static class

            // Must have parameterless constructor or be a record
            var hasParameterlessConstructor = type.GetConstructor(Type.EmptyTypes) != null ||
                                               type.GetConstructors().Length == 0;
            
            // Also check for record types (have <Clone>$ method)
            var isRecord = type.GetMethod("<Clone>$") != null;
            
            return hasParameterlessConstructor || isRecord;
        }

        /// <summary>
        /// Gets all searchable assemblies from current AppDomain
        /// </summary>
        private IEnumerable<Assembly> GetSearchableAssemblies()
        {
            var assemblies = new List<Assembly>();
            
            // Add loaded assemblies
            assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location)));

            // Add assemblies from DMEEditor's assembly handler if available
            if (_dmeEditor?.assemblyHandler?.Assemblies != null)
            {
                assemblies.AddRange(_dmeEditor.assemblyHandler.Assemblies.Select(a => a.DllLib).Where(a => a != null));
            }

            return assemblies.Distinct();
        }

        #endregion

        #region POCO to Entity Conversion

        /// <summary>
        /// Converts a POCO type to EntityStructure with relationship detection
        /// </summary>
        public EntityStructure ConvertPocoToEntity(Type pocoType, bool detectRelationships = true)
        {
            if (pocoType == null)
                throw new ArgumentNullException(nameof(pocoType));

            var entity = new EntityStructure
            {
                EntityName = pocoType.Name,
                DatasourceEntityName = pocoType.Name,
                OriginalEntityName = pocoType.Name,
                Caption = GenerateFriendlyName(pocoType.Name),
                Fields = new List<EntityField>(),
                Relations = new List<RelationShipKeys>(),
                PrimaryKeys = new List<EntityField>()
            };

            var properties = pocoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            int fieldIndex = 0;
            foreach (var prop in properties)
            {
                // Skip navigation properties (will be handled as relationships)
                if (detectRelationships && NavigationPropertyDetector.IsNavigationProperty(prop))
                {
                    continue;
                }

                var field = CreateEntityField(prop, fieldIndex);
                entity.Fields.Add(field);

                // Check if primary key
                if (IsPrimaryKeyProperty(prop))
                {
                    field.IsKey = true;
                    entity.PrimaryKeys.Add(field);
                }

                fieldIndex++;
            }

            // Detect relationships if requested
            if (detectRelationships)
            {
                var navigationProps = pocoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => NavigationPropertyDetector.IsNavigationProperty(p));

                foreach (var navProp in navigationProps)
                {
                    var relationship = CreateRelationshipFromNavigation(pocoType, navProp);
                    if (relationship != null)
                    {
                        entity.Relations.Add(relationship);
                    }
                }
            }

            return entity;
        }

        /// <summary>
        /// Creates an EntityField from a PropertyInfo
        /// </summary>
        private EntityField CreateEntityField(PropertyInfo prop, int index)
        {
            var field = new EntityField
            {
                fieldname = prop.Name,
                fieldtype = MapClrTypeToDbType(prop.PropertyType),
                Size1 = int.TryParse(GetFieldSize(prop), out int size) ? size : 0,
                AllowDBNull = IsNullable(prop),
                IsAutoIncrement = HasAutoIncrementAttribute(prop),
                IsKey = IsPrimaryKeyProperty(prop),
                IsUnique = HasUniqueAttribute(prop),
                FieldIndex = index
            };

            return field;
        }

        /// <summary>
        /// Maps CLR type to database type string
        /// </summary>
        private string MapClrTypeToDbType(Type type)
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            var typeMap = new Dictionary<Type, string>
            {
                { typeof(string), "System.String" },
                { typeof(int), "System.Int32" },
                { typeof(long), "System.Int64" },
                { typeof(short), "System.Int16" },
                { typeof(byte), "System.Byte" },
                { typeof(bool), "System.Boolean" },
                { typeof(decimal), "System.Decimal" },
                { typeof(double), "System.Double" },
                { typeof(float), "System.Single" },
                { typeof(DateTime), "System.DateTime" },
                { typeof(DateTimeOffset), "System.DateTimeOffset" },
                { typeof(TimeSpan), "System.TimeSpan" },
                { typeof(Guid), "System.Guid" },
                { typeof(byte[]), "System.Byte[]" },
                { typeof(char), "System.Char" }
            };

            return typeMap.TryGetValue(underlyingType, out var dbType) 
                ? dbType 
                : underlyingType.FullName ?? underlyingType.Name;
        }

        /// <summary>
        /// Gets the field size from StringLength or MaxLength attributes
        /// </summary>
        private string GetFieldSize(PropertyInfo prop)
        {
            var stringLengthAttr = prop.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
                return stringLengthAttr.MaximumLength.ToString();

            var maxLengthAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
            if (maxLengthAttr != null)
                return maxLengthAttr.Length.ToString();

            if (prop.PropertyType == typeof(string))
                return "255"; // Default string length

            return "0";
        }

        /// <summary>
        /// Checks if property is nullable
        /// </summary>
        private bool IsNullable(PropertyInfo prop)
        {
            if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                return true;

            if (prop.PropertyType == typeof(string))
                return !prop.GetCustomAttributes().Any(a => a.GetType().Name == "RequiredAttribute");

            return !prop.PropertyType.IsValueType;
        }

        /// <summary>
        /// Checks if property has auto-increment attribute
        /// </summary>
        private bool HasAutoIncrementAttribute(PropertyInfo prop)
        {
            var dbGeneratedAttr = prop.GetCustomAttribute<DatabaseGeneratedAttribute>();
            return dbGeneratedAttr?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity;
        }

        /// <summary>
        /// Checks if property is a primary key
        /// </summary>
        private bool IsPrimaryKeyProperty(PropertyInfo prop)
        {
            // Check for Key attribute
            if (prop.GetCustomAttribute<KeyAttribute>() != null)
                return true;

            // Convention: Id or {TypeName}Id
            var propName = prop.Name.ToLowerInvariant();
            var typeName = prop.DeclaringType?.Name.ToLowerInvariant() ?? "";
            
            return propName == "id" || propName == $"{typeName}id";
        }

        /// <summary>
        /// Checks if property has unique constraint
        /// </summary>
        private bool HasUniqueAttribute(PropertyInfo prop)
        {
            return prop.GetCustomAttributes()
                .Any(a => a.GetType().Name.Contains("Unique", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Creates a RelationShipKeys from a navigation property
        /// </summary>
        private RelationShipKeys CreateRelationshipFromNavigation(Type parentType, PropertyInfo navProp)
        {
            var referencedType = NavigationPropertyDetector.GetReferencedType(navProp);
            if (referencedType == null) return null;

            var fkProperty = NavigationPropertyDetector.FindForeignKeyProperty(navProp, parentType);
            var cardinality = RelationshipInferencer.InferCardinality(navProp, parentType, referencedType);

            var relationship = new RelationShipKeys
            {
                EntityColumnID = fkProperty?.Name ?? $"{referencedType.Name}Id",
                RelatedEntityID = referencedType.Name,
                RelatedEntityColumnID = "Id" // Convention
            };

            return relationship;
        }

        /// <summary>
        /// Generates a friendly display name from a class name
        /// </summary>
        private string GenerateFriendlyName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            
            var result = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c))
                    result.Append(' ');
                result.Append(c);
            }
            return result.ToString();
        }

        #endregion

        #region Entity Class Generation

        /// <summary>
        /// Generates entity class code from a POCO type
        /// </summary>
        public string GenerateEntityClassFromPoco(Type pocoType, string outputPath = null, 
            string namespaceString = "TheTechIdea.ProjectClasses", bool generateFile = true)
        {
            var entity = ConvertPocoToEntity(pocoType, detectRelationships: true);
            return GenerateEntityClassCode(entity, outputPath, namespaceString, generateFile);
        }

        /// <summary>
        /// Generates entity classes from all POCOs in a namespace
        /// </summary>
        public List<string> GenerateEntityClassesFromNamespace(string sourceNamespace, string outputPath = null,
            string targetNamespace = "TheTechIdea.ProjectClasses", bool generateFiles = true, Assembly assembly = null)
        {
            var pocoTypes = ScanNamespaceForPocos(sourceNamespace, assembly);
            var results = new List<string>();

            foreach (var pocoType in pocoTypes)
            {
                var code = GenerateEntityClassFromPoco(pocoType, outputPath, targetNamespace, generateFiles);
                results.Add(code);
            }

            return results;
        }

        /// <summary>
        /// Generates entity class code from EntityStructure
        /// </summary>
        private string GenerateEntityClassCode(EntityStructure entity, string outputPath, 
            string namespaceString, bool generateFile)
        {
            var sb = new StringBuilder();

            // Using statements
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.ComponentModel;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine("using TheTechIdea.Beep.DataBase;");
            sb.AppendLine();

            sb.AppendLine($"namespace {namespaceString}");
            sb.AppendLine("{");

            // Class documentation
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Entity class for {entity.EntityName}");
            sb.AppendLine($"    /// Auto-generated from POCO class");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    [Table(\"{entity.EntityName}\")]");
            sb.AppendLine($"    public class {entity.EntityName}Entity : Entity, INotifyPropertyChanged");
            sb.AppendLine("    {");

            // INotifyPropertyChanged implementation
            sb.AppendLine("        public event PropertyChangedEventHandler PropertyChanged;");
            sb.AppendLine();
            sb.AppendLine("        protected virtual void OnPropertyChanged(string propertyName)");
            sb.AppendLine("        {");
            sb.AppendLine("            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate properties
            foreach (var field in entity.Fields)
            {
                var backingField = $"_{char.ToLowerInvariant(field.fieldname[0])}{field.fieldname.Substring(1)}";
                var clrType = GetClrTypeName(field.fieldtype, field.AllowDBNull);

                // Backing field
                sb.AppendLine($"        private {clrType} {backingField};");
                sb.AppendLine();

                // Property attributes
                if (field.IsKey)
                    sb.AppendLine("        [Key]");
                if (field.IsAutoIncrement)
                    sb.AppendLine("        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
                if (!field.AllowDBNull && clrType == "string")
                    sb.AppendLine("        [Required]");
                if (field.Size1 > 0 && clrType == "string")
                    sb.AppendLine($"        [StringLength({field.Size1})]");

                // Property
                sb.AppendLine($"        public {clrType} {field.fieldname}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => {backingField};");
                sb.AppendLine("            set");
                sb.AppendLine("            {");
                sb.AppendLine($"                if ({backingField} != value)");
                sb.AppendLine("                {");
                sb.AppendLine($"                    {backingField} = value;");
                sb.AppendLine($"                    OnPropertyChanged(nameof({field.fieldname}));");
                sb.AppendLine("                }");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            // Generate navigation properties for relationships
            foreach (var relation in entity.Relations)
            {
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Navigation property to {relation.RelatedEntityID}");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        [ForeignKey(\"{relation.EntityColumnID}\")]");
                sb.AppendLine($"        public virtual {relation.RelatedEntityID}Entity {relation.RelatedEntityID} {{ get; set; }}");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var code = sb.ToString();

            // Write to file if requested
            if (generateFile && !string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = _helper.EnsureOutputDirectory(outputPath);
                var filePath = Path.Combine(outputPath, $"{entity.EntityName}Entity.cs");
                _helper.WriteToFile(filePath, code, $"{entity.EntityName}Entity");
            }

            return code;
        }

        /// <summary>
        /// Gets the CLR type name from a database type string
        /// </summary>
        private string GetClrTypeName(string dbType, bool nullable)
        {
            var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "System.String", "string" },
                { "System.Int32", nullable ? "int?" : "int" },
                { "System.Int64", nullable ? "long?" : "long" },
                { "System.Int16", nullable ? "short?" : "short" },
                { "System.Byte", nullable ? "byte?" : "byte" },
                { "System.Boolean", nullable ? "bool?" : "bool" },
                { "System.Decimal", nullable ? "decimal?" : "decimal" },
                { "System.Double", nullable ? "double?" : "double" },
                { "System.Single", nullable ? "float?" : "float" },
                { "System.DateTime", nullable ? "DateTime?" : "DateTime" },
                { "System.DateTimeOffset", nullable ? "DateTimeOffset?" : "DateTimeOffset" },
                { "System.TimeSpan", nullable ? "TimeSpan?" : "TimeSpan" },
                { "System.Guid", nullable ? "Guid?" : "Guid" },
                { "System.Byte[]", "byte[]" },
                { "System.Char", nullable ? "char?" : "char" }
            };

            return typeMap.TryGetValue(dbType, out var clrType) 
                ? clrType 
                : (nullable ? $"{dbType}?" : dbType);
        }

        #endregion

        #region Runtime Type Creation

        /// <summary>
        /// Compiles entity code and returns the Type at runtime (no file output)
        /// </summary>
        public Type CreateEntityTypeAtRuntime(EntityStructure entity, string namespaceString = "TheTechIdea.ProjectClasses")
        {
            var cacheKey = $"{namespaceString}.{entity.EntityName}Entity";
            
            // Check cache first
            if (_runtimeTypeCache.TryGetValue(cacheKey, out var cachedType))
                return cachedType;

            // Generate the code
            var code = GenerateEntityClassCode(entity, null, namespaceString, generateFile: false);

            // Compile using RoslynCompiler
            var assembly = CompileCodeToAssembly(code, $"{entity.EntityName}Entity");
            if (assembly == null)
                return null;

            // Get the type from the compiled assembly
            var typeName = $"{namespaceString}.{entity.EntityName}Entity";
            var type = assembly.GetType(typeName);

            if (type != null)
            {
                _runtimeTypeCache.TryAdd(cacheKey, type);
            }

            return type;
        }

        /// <summary>
        /// Compiles entity code from POCO and returns the Type at runtime
        /// </summary>
        public Type CreateEntityTypeFromPocoAtRuntime(Type pocoType, string namespaceString = "TheTechIdea.ProjectClasses")
        {
            var entity = ConvertPocoToEntity(pocoType, detectRelationships: true);
            return CreateEntityTypeAtRuntime(entity, namespaceString);
        }

        /// <summary>
        /// Generates and compiles multiple entity types at runtime from a namespace
        /// </summary>
        public Dictionary<string, Type> CreateEntityTypesFromNamespaceAtRuntime(string sourceNamespace,
            string targetNamespace = "TheTechIdea.ProjectClasses", Assembly assembly = null)
        {
            var result = new Dictionary<string, Type>();
            var pocoTypes = ScanNamespaceForPocos(sourceNamespace, assembly);

            // Generate all code first
            var allCode = new StringBuilder();
            allCode.AppendLine("using System;");
            allCode.AppendLine("using System.Collections.Generic;");
            allCode.AppendLine("using System.ComponentModel;");
            allCode.AppendLine("using System.ComponentModel.DataAnnotations;");
            allCode.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            allCode.AppendLine("using TheTechIdea.Beep.DataBase;");
            allCode.AppendLine();
            allCode.AppendLine($"namespace {targetNamespace}");
            allCode.AppendLine("{");

            var entityNames = new List<string>();
            foreach (var pocoType in pocoTypes)
            {
                var entity = ConvertPocoToEntity(pocoType, detectRelationships: true);
                var classCode = GenerateEntityClassBodyOnly(entity);
                allCode.AppendLine(classCode);
                entityNames.Add($"{entity.EntityName}Entity");
            }

            allCode.AppendLine("}");

            // Compile all at once for better performance
            var compiledAssembly = CompileCodeToAssembly(allCode.ToString(), $"RuntimeEntities_{Guid.NewGuid():N}");
            if (compiledAssembly == null)
                return result;

            // Get all types from compiled assembly
            foreach (var entityName in entityNames)
            {
                var typeName = $"{targetNamespace}.{entityName}";
                var type = compiledAssembly.GetType(typeName);
                if (type != null)
                {
                    result[entityName] = type;
                    _runtimeTypeCache.TryAdd(typeName, type);
                }
            }

            return result;
        }

        /// <summary>
        /// Generates entity class body only (no namespace wrapper)
        /// </summary>
        private string GenerateEntityClassBodyOnly(EntityStructure entity)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"    [Table(\"{entity.EntityName}\")]");
            sb.AppendLine($"    public class {entity.EntityName}Entity : Entity, INotifyPropertyChanged");
            sb.AppendLine("    {");

            // INotifyPropertyChanged implementation
            sb.AppendLine("        public event PropertyChangedEventHandler PropertyChanged;");
            sb.AppendLine("        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));");
            sb.AppendLine();

            foreach (var field in entity.Fields)
            {
                var backingField = $"_{char.ToLowerInvariant(field.fieldname[0])}{field.fieldname.Substring(1)}";
                var clrType = GetClrTypeName(field.fieldtype, field.AllowDBNull);

                sb.AppendLine($"        private {clrType} {backingField};");
                if (field.IsKey) sb.AppendLine("        [Key]");
                if (field.IsAutoIncrement) sb.AppendLine("        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
                
                sb.AppendLine($"        public {clrType} {field.fieldname}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => {backingField};");
                sb.AppendLine($"            set {{ if ({backingField} != value) {{ {backingField} = value; OnPropertyChanged(nameof({field.fieldname})); }} }}");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            return sb.ToString();
        }

        /// <summary>
        /// Compiles C# code to an in-memory assembly with proper references
        /// </summary>
        private Assembly CompileCodeToAssembly(string code, string assemblyName)
        {
            try
            {
                // Use RoslynCompiler.CompileClassTypeandAssembly which has proper references
                // including INotifyPropertyChanged, Entity base class, etc.
                var result = RoslynCompiler.CompileClassTypeandAssembly(assemblyName, code);
                return result?.Item2; // Return the Assembly part of the tuple
            }
            catch (Exception ex)
            {
                _dmeEditor?.AddLogMessage("Beep", $"Failed to compile entity code: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return null;
            }
        }

        #endregion

        #region DataSource Integration

        /// <summary>
        /// Generates entity types from a datasource at runtime
        /// </summary>
        public Dictionary<string, Type> CreateEntityTypesFromDataSourceAtRuntime(string datasourceName,
            List<string> entityNames = null, string targetNamespace = "TheTechIdea.ProjectClasses")
        {
            var result = new Dictionary<string, Type>();
            var ds = _dmeEditor.GetDataSource(datasourceName);
            
            if (ds == null)
            {
                _dmeEditor?.AddLogMessage("Beep", $"Datasource '{datasourceName}' not found", DateTime.Now, -1, null, Errors.Failed);
                return result;
            }

            // Get all entities or specified ones
            var entities = entityNames ?? ds.GetEntitesList().ToList();

            var allCode = new StringBuilder();
            allCode.AppendLine("using System;");
            allCode.AppendLine("using System.Collections.Generic;");
            allCode.AppendLine("using System.ComponentModel;");
            allCode.AppendLine("using System.ComponentModel.DataAnnotations;");
            allCode.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            allCode.AppendLine("using TheTechIdea.Beep.DataBase;");
            allCode.AppendLine();
            allCode.AppendLine($"namespace {targetNamespace}");
            allCode.AppendLine("{");

            var generatedNames = new List<string>();
            foreach (var entityName in entities)
            {
                var entityStructure = ds.GetEntityStructure(entityName, true);
                if (entityStructure != null)
                {
                    var classCode = GenerateEntityClassBodyOnly(entityStructure);
                    allCode.AppendLine(classCode);
                    generatedNames.Add($"{entityStructure.EntityName}Entity");
                }
            }

            allCode.AppendLine("}");

            var compiledAssembly = CompileCodeToAssembly(allCode.ToString(), $"DsEntities_{datasourceName}_{Guid.NewGuid():N}");
            if (compiledAssembly == null)
                return result;

            foreach (var name in generatedNames)
            {
                var typeName = $"{targetNamespace}.{name}";
                var type = compiledAssembly.GetType(typeName);
                if (type != null)
                {
                    result[name] = type;
                    _runtimeTypeCache.TryAdd(typeName, type);
                }
            }

            return result;
        }

        /// <summary>
        /// Clears the runtime type cache
        /// </summary>
        public void ClearCache()
        {
            _runtimeTypeCache.Clear();
            _assemblyCache.Clear();
        }

        #endregion
    }
}
