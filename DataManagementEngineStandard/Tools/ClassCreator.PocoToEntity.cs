using System;
using System.Collections.Generic;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Tools.Helpers;
using TheTechIdea.Beep.Tools;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Partial class extending ClassCreator with POCO to Entity generation capabilities.
    /// Supports namespace scanning, runtime type loading, and datasource integration.
    /// </summary>
    public partial class ClassCreator
    {
        #region Namespace Scanning

        /// <summary>
        /// Scans a namespace and discovers all POCO classes
        /// </summary>
        /// <param name="namespaceName">The namespace to scan</param>
        /// <param name="assembly">Optional specific assembly to search (searches all if null)</param>
        /// <returns>List of discovered POCO types</returns>
        public List<Type> ScanNamespaceForPocos(string namespaceName, Assembly assembly = null)
        {
            return _pocoToEntityHelper.ScanNamespaceForPocos(namespaceName, assembly);
        }

        /// <summary>
        /// Finds a specific class by name in a namespace
        /// </summary>
        /// <param name="namespaceName">The namespace to search (can be null for global search)</param>
        /// <param name="className">The class name to find</param>
        /// <param name="assembly">Optional specific assembly to search</param>
        /// <returns>The found Type or null</returns>
        public Type FindClassByName(string namespaceName, string className, Assembly assembly = null)
        {
            return _pocoToEntityHelper.FindClassByName(namespaceName, className, assembly);
        }

        #endregion

        #region POCO to Entity Conversion

        /// <summary>
        /// Converts a POCO type to EntityStructure with relationship detection
        /// </summary>
        /// <param name="pocoType">The POCO type to convert</param>
        /// <param name="detectRelationships">Whether to detect navigation property relationships</param>
        /// <returns>EntityStructure representing the POCO</returns>
        public EntityStructure ConvertPocoToEntity(Type pocoType, bool detectRelationships = true)
        {
            return _pocoToEntityHelper.ConvertPocoToEntity(pocoType, detectRelationships);
        }

        #endregion

        #region Entity Class Generation
        /// <summary>
        /// Generates entity class code from a POCO type
        /// </summary>
        /// <param name="pocoType">The source POCO type</param>
        /// <param name="outputPath">Output path for file generation (null = no file)</param>
        /// <param name="namespaceString">Target namespace for generated class</param>
        /// <param name="generateFile">Whether to write the code to a file</param>
        /// <returns>Generated C# code string</returns>
        public string GenerateEntityClassFromPoco(Type pocoType, string outputPath = null,
            string namespaceString = "TheTechIdea.ProjectClasses", bool generateFile = true)
        {
            return _pocoToEntityHelper.GenerateEntityClassFromPoco(pocoType, outputPath, namespaceString, generateFile);
        }

        /// <summary>
        /// Generates entity classes from all POCOs in a namespace
        /// </summary>
        /// <param name="sourceNamespace">The source namespace containing POCOs</param>
        /// <param name="outputPath">Output path for file generation (null = no files)</param>
        /// <param name="targetNamespace">Target namespace for generated classes</param>
        /// <param name="generateFiles">Whether to write code to files</param>
        /// <param name="assembly">Optional specific assembly to scan</param>
        /// <returns>List of generated C# code strings</returns>
        public List<string> GenerateEntityClassesFromNamespace(string sourceNamespace, string outputPath = null,
            string targetNamespace = "TheTechIdea.ProjectClasses", bool generateFiles = true, Assembly assembly = null)
        {
            return _pocoToEntityHelper.GenerateEntityClassesFromNamespace(sourceNamespace, outputPath, 
                targetNamespace, generateFiles, assembly);
        }

        #endregion

        #region Runtime Type Creation

        /// <summary>
        /// Compiles entity code and returns the Type at runtime (no file output)
        /// </summary>
        /// <param name="entity">The entity structure to compile</param>
        /// <param name="namespaceString">Target namespace for the type</param>
        /// <returns>Compiled Type or null on failure</returns>
        public Type CreateEntityTypeAtRuntime(EntityStructure entity, string namespaceString = "TheTechIdea.ProjectClasses")
        {
            return _pocoToEntityHelper.CreateEntityTypeAtRuntime(entity, namespaceString);
        }

        /// <summary>
        /// Compiles entity code from POCO and returns the Type at runtime
        /// </summary>
        /// <param name="pocoType">The source POCO type</param>
        /// <param name="namespaceString">Target namespace for the type</param>
        /// <returns>Compiled Type or null on failure</returns>
        public Type CreateEntityTypeFromPocoAtRuntime(Type pocoType, string namespaceString = "TheTechIdea.ProjectClasses")
        {
            return _pocoToEntityHelper.CreateEntityTypeFromPocoAtRuntime(pocoType, namespaceString);
        }

        /// <summary>
        /// Generates and compiles multiple entity types at runtime from a namespace
        /// </summary>
        /// <param name="sourceNamespace">The source namespace containing POCOs</param>
        /// <param name="targetNamespace">Target namespace for generated types</param>
        /// <param name="assembly">Optional specific assembly to scan</param>
        /// <returns>Dictionary mapping type names to compiled Types</returns>
        public Dictionary<string, Type> CreateEntityTypesFromNamespaceAtRuntime(string sourceNamespace,
            string targetNamespace = "TheTechIdea.ProjectClasses", Assembly assembly = null)
        {
            return _pocoToEntityHelper.CreateEntityTypesFromNamespaceAtRuntime(sourceNamespace, targetNamespace, assembly);
        }

        /// <summary>
        /// Generates entity types from a datasource at runtime
        /// </summary>
        /// <param name="datasourceName">Name of the datasource</param>
        /// <param name="entityNames">Specific entity names (null = all entities)</param>
        /// <param name="targetNamespace">Target namespace for generated types</param>
        /// <returns>Dictionary mapping type names to compiled Types</returns>
        public Dictionary<string, Type> CreateEntityTypesFromDataSourceAtRuntime(string datasourceName,
            List<string> entityNames = null, string targetNamespace = "TheTechIdea.ProjectClasses")
        {
            return _pocoToEntityHelper.CreateEntityTypesFromDataSourceAtRuntime(datasourceName, entityNames, targetNamespace);
        }

        #endregion

#region ConvertToEntityStructure
        /// <summary>
        /// Converts an Entity/POCO type to EntityStructure using FromType extension
        /// then enriches fields with data annotation attributes ([Key], [Required], 
        /// [MaxLength], [DatabaseGenerated], [Column], [Table], [NotMapped]).
        /// Supports both Entity-derived types and plain POCOs.
        /// </summary>
        public EntityStructure ConvertEntityTypeToEntityStructure(Type EntityType,
            KeyDetectionStrategy strategy = KeyDetectionStrategy.AttributeThenConvention,
            string entityName = null, string datasourceName = null)
        {
            if (EntityType == null)
                throw new ArgumentNullException(nameof(EntityType));

            // Step 1: Use FromType extension to populate basic structure from reflection
            EntityStructure entity = new EntityStructure();
            entity.FromType(EntityType);

            // Step 2: Set datasource name if provided
            if (!string.IsNullOrWhiteSpace(datasourceName))
            {
                entity.DataSourceID = datasourceName;
               
            }

            // Step 3: Override entity name if provided, or from [Table] attribute
            var tableAttr = EntityType.GetCustomAttribute<TableAttribute>();
            if (!string.IsNullOrWhiteSpace(entityName))
            {
                entity.EntityName = entityName;
                entity.DatasourceEntityName = entityName;
                entity.Caption = entityName;
            }
            else if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
            {
                entity.DatasourceEntityName = tableAttr.Name;
            }

            // Step 4: Build property lookup for attribute reading
            var properties = EntityType.GetProperties();
            var propertyMap = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in properties)
            {
                propertyMap[prop.Name] = prop;
            }

            // Step 5: Remove [NotMapped] fields and navigation properties
            var fieldsToRemove = new List<EntityField>();
            foreach (var field in entity.Fields)
            {
                if (propertyMap.TryGetValue(field.FieldName, out var prop))
                {
                    if (prop.GetCustomAttribute<NotMappedAttribute>() != null)
                    {
                        fieldsToRemove.Add(field);
                        continue;
                    }
                    if (IsNavigationProperty(prop.PropertyType))
                    {
                        fieldsToRemove.Add(field);
                        continue;
                    }
                }
            }
            foreach (var f in fieldsToRemove)
            {
                entity.Fields.Remove(f);
                entity.PrimaryKeys.Remove(f);
            }

            // Step 6: Enrich fields with data annotation attributes
            entity.PrimaryKeys.Clear();
            int fieldIndex = 0;
            foreach (var field in entity.Fields)
            {
                field.FieldIndex = fieldIndex++;
                field.EntityName = entity.EntityName;

                if (!propertyMap.TryGetValue(field.FieldName, out var prop))
                    continue;

                // [Column] - override column name
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                if (columnAttr != null && !string.IsNullOrWhiteSpace(columnAttr.Name))
                {
                    field.Originalfieldname = field.FieldName;
                    field.FieldName = columnAttr.Name;
                }

                // [Key] - primary key
                if (prop.GetCustomAttribute<KeyAttribute>() != null)
                {
                    field.IsKey = true;
                }

                // [DatabaseGenerated(Identity)] - auto increment
                var dbGenAttr = prop.GetCustomAttribute<DatabaseGeneratedAttribute>();
                if (dbGenAttr != null && dbGenAttr.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                {
                    field.IsAutoIncrement = true;
                }

                // [Required] - not nullable
                if (prop.GetCustomAttribute<RequiredAttribute>() != null)
                {
                    field.AllowDBNull = false;
                }

                // [MaxLength] - field size
                var maxLenAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLenAttr != null && maxLenAttr.Length > 0)
                {
                    field.Size1 = maxLenAttr.Length;
                }
                else
                {
                    // [StringLength] fallback
                    var strLenAttr = prop.GetCustomAttribute<StringLengthAttribute>();
                    if (strLenAttr != null && strLenAttr.MaximumLength > 0)
                    {
                        field.Size1 = strLenAttr.MaximumLength;
                    }
                }

                if (field.IsKey)
                {
                    entity.PrimaryKeys.Add(field);
                }
            }

            // Step 7: Convention-based key detection if no [Key] found
            if (entity.PrimaryKeys.Count == 0 &&
                (strategy == KeyDetectionStrategy.ConventionOnly ||
                 strategy == KeyDetectionStrategy.AttributeThenConvention))
            {
                ApplyConventionBasedKeyDetection(entity, EntityType);
            }

            return entity;
        }

        /// <summary>
        /// Checks if a property type is a navigation property (collection or complex type)
        /// that should be excluded from entity fields.
        /// </summary>
        private static bool IsNavigationProperty(Type propType)
        {
            var underlyingType = Nullable.GetUnderlyingType(propType);
            if (underlyingType != null)
                return false;

            if (propType.IsPrimitive || propType == typeof(string) || propType == typeof(decimal) ||
                propType == typeof(DateTime) || propType == typeof(DateTimeOffset) ||
                propType == typeof(TimeSpan) || propType == typeof(Guid) ||
                propType == typeof(byte[]) || propType.IsEnum)
            {
                return false;
            }

            if (propType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(propType))
            {
                return true;
            }

            if (propType.IsClass && propType != typeof(string) && propType != typeof(byte[]))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Applies convention-based key detection: looks for "Id" or "{TypeName}Id" properties.
        /// </summary>
        private static void ApplyConventionBasedKeyDetection(EntityStructure entity, Type entityType)
        {
            var idField = entity.Fields.FirstOrDefault(f =>
                string.Equals(f.FieldName, "Id", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(f.Originalfieldname, "Id", StringComparison.OrdinalIgnoreCase));

            if (idField == null)
            {
                string typeNameId = entityType.Name + "Id";
                idField = entity.Fields.FirstOrDefault(f =>
                    string.Equals(f.FieldName, typeNameId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.Originalfieldname, typeNameId, StringComparison.OrdinalIgnoreCase));
            }

            if (idField != null)
            {
                idField.IsKey = true;
                entity.PrimaryKeys.Add(idField);

                if (idField.Fieldtype == typeof(int).FullName ||
                    idField.Fieldtype == typeof(long).FullName)
                {
                    idField.IsAutoIncrement = true;
                }
            }
        }
#endregion
        #region Cache Management

        /// <summary>
        /// Clears the runtime type cache
        /// </summary>
        public void ClearPocoToEntityCache()
        {
            _pocoToEntityHelper.ClearCache();
        }

        /// <summary>
        /// Converts a POCO type to EntityStructure using generic type with KeyDetectionStrategy.
        /// Uses ConvertEntityTypeToEntityStructure which handles both Entity and POCO types.
        /// </summary>
        public EntityStructure ConvertToEntityStructure<T>(
            KeyDetectionStrategy strategy = KeyDetectionStrategy.AttributeThenConvention,
            string entityName = null) where T : class
        {
            return ConvertEntityTypeToEntityStructure(typeof(T), strategy, entityName);
        }

        /// <summary>
        /// Converts a runtime POCO/Entity type to EntityStructure with KeyDetectionStrategy.
        /// Uses ConvertEntityTypeToEntityStructure which handles both Entity and POCO types.
        /// </summary>
        public EntityStructure ConvertToEntityStructure(Type pocoType,
            KeyDetectionStrategy strategy = KeyDetectionStrategy.AttributeThenConvention,
            string entityName = null)
        {
            return ConvertEntityTypeToEntityStructure(pocoType, strategy, entityName);
        }

        /// <summary>
        /// Converts a POCO/Entity object instance to EntityStructure with KeyDetectionStrategy.
        /// Uses ConvertEntityTypeToEntityStructure which handles both Entity and POCO types.
        /// </summary>
        public EntityStructure ConvertToEntityStructure(object instance,
            KeyDetectionStrategy strategy = KeyDetectionStrategy.AttributeThenConvention,
            string entityName = null)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            return ConvertEntityTypeToEntityStructure(instance.GetType(), strategy, entityName);
        }

        /// <summary>
        /// Gets circular reference diagnostics for a POCO type
        /// </summary>
        public List<string> GetCircularReferences<T>() where T : class
        {
            // TODO: Implement circular reference detection
            return new List<string>();
        }

        #region Batch POCO Conversion Methods

        /// <summary>
        /// Converts multiple POCO types to EntityStructures
        /// </summary>
        public List<EntityStructure> ConvertPocosToEntities(List<Type> pocoTypes, bool detectRelationships = true)
        {
            return pocoTypes?.Select(type => ConvertPocoToEntity(type, detectRelationships)).ToList() ?? new List<EntityStructure>();
        }

        /// <summary>
        /// Converts multiple objects to EntityStructures
        /// </summary>
        public List<EntityStructure> ConvertToEntityStructures(List<object> instances,
            KeyDetectionStrategy strategy = KeyDetectionStrategy.AttributeThenConvention)
        {
            return instances?.Select(instance => ConvertToEntityStructure(instance, strategy)).ToList() ?? new List<EntityStructure>();
        }

        /// <summary>
        /// Converts multiple POCO types to EntityStructures using generic method
        /// </summary>
        public List<EntityStructure> ConvertToEntityStructures<T>(List<T> instances,
            KeyDetectionStrategy strategy = KeyDetectionStrategy.AttributeThenConvention) where T : class
        {
            return instances?.Select(instance => ConvertToEntityStructure((object)instance, strategy)).ToList() ?? new List<EntityStructure>();
        }

        /// <summary>
        /// Generates entity classes from multiple POCO types
        /// </summary>
        public List<string> GenerateEntityClassesFromPocos(List<Type> pocoTypes, string outputPath = null,
            string namespaceString = "TheTechIdea.ProjectClasses", bool generateFile = true)
        {
            return pocoTypes?.Select(type => GenerateEntityClassFromPoco(type, outputPath, namespaceString, generateFile)).ToList() ?? new List<string>();
        }

        /// <summary>
        /// Creates runtime types from multiple EntityStructures
        /// </summary>
        public List<Type> CreateEntityTypesAtRuntime(List<EntityStructure> entities,
            string namespaceString = "TheTechIdea.ProjectClasses")
        {
            return entities?.Select(entity => CreateEntityTypeAtRuntime(entity, namespaceString)).ToList() ?? new List<Type>();
        }

        /// <summary>
        /// Creates runtime types from multiple POCO types
        /// </summary>
        public List<Type> CreateEntityTypesFromPocosAtRuntime(List<Type> pocoTypes,
            string namespaceString = "TheTechIdea.ProjectClasses")
        {
            return pocoTypes?.Select(type => CreateEntityTypeFromPocoAtRuntime(type, namespaceString)).ToList() ?? new List<Type>();
        }

        #endregion

        #region Namespace-Based Conversion Methods

        /// <summary>
        /// Converts all POCO classes from a namespace in loaded library to Entity classes and saves to files
        /// </summary>
        public List<string> ConvertNamespacePocoClassesToEntities(string namespaceName, string outputPath,
            Assembly assembly = null, string namespaceString = "TheTechIdea.ProjectClasses")
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty", nameof(namespaceName));

            // If no assembly specified, search all loaded assemblies in current AppDomain
            var pocoTypes = new List<Type>();
            if (assembly != null)
            {
                pocoTypes = ScanNamespaceForPocos(namespaceName, assembly);
            }
            else
            {
                foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var typesInAssembly = ScanNamespaceForPocos(namespaceName, loadedAssembly);
                    if (typesInAssembly != null && typesInAssembly.Count > 0)
                        pocoTypes.AddRange(typesInAssembly);
                }
            }
            if (pocoTypes == null || pocoTypes.Count == 0)
                return new List<string>();

            var results = new List<string>();
            foreach (var pocoType in pocoTypes)
            {
                var entity = ConvertPocoToEntity(pocoType, detectRelationships: true);
                var filePath = GenerateEntityClassFromPoco(pocoType, outputPath, namespaceString, generateFile: true);
                if (!string.IsNullOrEmpty(filePath))
                    results.Add(filePath);
            }

            return results;
        }

        /// <summary>
        /// Converts all POCO classes from a namespace in loaded library to Entity code and saves to single file
        /// </summary>
        public string ConvertNamespacePocoClassesToEntityFile(string namespaceName, string outputPath,
            Assembly assembly = null, string namespaceString = "TheTechIdea.ProjectClasses")
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty", nameof(namespaceName));

            // If no assembly specified, search all loaded assemblies in current AppDomain
            var pocoTypes = new List<Type>();
            if (assembly != null)
            {
                pocoTypes = ScanNamespaceForPocos(namespaceName, assembly);
            }
            else
            {
                foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var typesInAssembly = ScanNamespaceForPocos(namespaceName, loadedAssembly);
                    if (typesInAssembly != null && typesInAssembly.Count > 0)
                        pocoTypes.AddRange(typesInAssembly);
                }
            }
            if (pocoTypes == null || pocoTypes.Count == 0)
                return null;

            var allCode = new StringBuilder();
            allCode.AppendLine("using System;");
            allCode.AppendLine("using System.Collections.Generic;");
            allCode.AppendLine("using TheTechIdea.Beep.Editor;");
            allCode.AppendLine();
            allCode.AppendLine($"namespace {namespaceString}");
            allCode.AppendLine("{");

            foreach (var pocoType in pocoTypes)
            {
                var entity = ConvertPocoToEntity(pocoType);
                var code = _pocoToEntityHelper.GenerateEntityClassFromPoco(pocoType, null, namespaceString, generateFile: false);
                if (!string.IsNullOrEmpty(code))
                {
                    allCode.AppendLine(code);
                    allCode.AppendLine();
                }
            }

            allCode.AppendLine("}");

            var fileName = $"{namespaceName}_Entities.cs";
            var filePath = Path.Combine(outputPath, fileName);
            _generationHelper.EnsureOutputDirectory(outputPath);
            _generationHelper.WriteToFile(filePath, allCode.ToString(), $"{namespaceName} Entities");

            return filePath;
        }

        /// <summary>
        /// Converts all EF Core mapped classes from a namespace in loaded library to Entity classes
        /// Automatically excludes navigation properties and virtual collections
        /// </summary>
        public List<string> ConvertNamespaceEFCoreClassesToEntities(string namespaceName, string outputPath,
            Assembly assembly = null, string namespaceString = "TheTechIdea.ProjectClasses")
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty", namespaceString);

            // If no assembly specified, search all loaded assemblies in current AppDomain
            var efTypes = new List<Type>();
            if (assembly != null)
            {
                efTypes = ScanNamespaceForPocos(namespaceName, assembly);
            }
            else
            {
                foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var typesInAssembly = ScanNamespaceForPocos(namespaceName, loadedAssembly);
                    if (typesInAssembly != null && typesInAssembly.Count > 0)
                        efTypes.AddRange(typesInAssembly);
                }
            }
            if (efTypes == null || efTypes.Count == 0)
                return new List<string>();

            var results = new List<string>();
            foreach (var efType in efTypes)
            {
                var entity = ConvertPocoToEntity(efType, detectRelationships: false);
                
                // Remove navigation properties
                if (entity?.Fields != null)
                {
                    entity.Fields = entity.Fields
                        .Where(f => !IsNavigationProperty(efType, f.FieldName))
                        .ToList();
                }

                var code = GenerateEntityCode(entity, namespaceString);
                var filePath = Path.Combine(outputPath, $"{entity.EntityName}.cs");
                _generationHelper.EnsureOutputDirectory(outputPath);
                
                if (_generationHelper.WriteToFile(filePath, code, entity.EntityName))
                    results.Add(filePath);
            }

            return results;
        }

        /// <summary>
        /// Converts all EF Core mapped classes from a namespace in loaded library to Entity code and saves to single file
        /// Automatically excludes navigation properties and virtual collections
        /// </summary>
        public string ConvertNamespaceEFCoreClassesToEntityFile(string namespaceName, string outputPath,
            Assembly assembly = null, string namespaceString = "TheTechIdea.ProjectClasses")
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty", namespaceString);

            // If no assembly specified, search all loaded assemblies in current AppDomain
            var efTypes = new List<Type>();
            if (assembly != null)
            {
                efTypes = ScanNamespaceForPocos(namespaceName, assembly);
            }
            else
            {
                foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var typesInAssembly = ScanNamespaceForPocos(namespaceName, loadedAssembly);
                    if (typesInAssembly != null && typesInAssembly.Count > 0)
                        efTypes.AddRange(typesInAssembly);
                }
            }
            if (efTypes == null || efTypes.Count == 0)
                return null;

            var allCode = new StringBuilder();
            allCode.AppendLine("using System;");
            allCode.AppendLine("using System.Collections.Generic;");
            allCode.AppendLine("using TheTechIdea.Beep.Editor;");
            allCode.AppendLine();
            allCode.AppendLine($"namespace {namespaceString}");
            allCode.AppendLine("{");

            foreach (var efType in efTypes)
            {
                var entity = ConvertPocoToEntity(efType, detectRelationships: false);
                
                // Remove navigation properties
                if (entity?.Fields != null)
                {
                    entity.Fields = entity.Fields
                        .Where(f => !IsNavigationProperty(efType, f.FieldName))
                        .ToList();
                }

                var code = GenerateEntityCode(entity, namespaceString);
                if (!string.IsNullOrEmpty(code))
                {
                    allCode.AppendLine(code);
                    allCode.AppendLine();
                }
            }

            allCode.AppendLine("}");

            var fileName = $"{namespaceName}_EFCoreEntities.cs";
            var filePath = Path.Combine(outputPath, fileName);
            _generationHelper.EnsureOutputDirectory(outputPath);
            _generationHelper.WriteToFile(filePath, allCode.ToString(), $"{namespaceName} EF Core Entities");

            return filePath;
        }

        /// <summary>
        /// Checks if a property is a navigation property (virtual ICollection or object references)
        /// </summary>
        private bool IsNavigationProperty(Type type, string propertyName)
        {
            var prop = type.GetProperty(propertyName);
            if (prop == null)
                return false;

            // Check if virtual (EF Core convention)
            if (prop.GetGetMethod()?.IsVirtual == true)
                return true;

            // Check if ICollection (indicates navigation property)
            var propType = prop.PropertyType;
            if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(ICollection<>))
                return true;

            // Check if complex object (not primitive)
            if (!propType.IsPrimitive && propType != typeof(string) && propType != typeof(decimal) &&
                propType != typeof(DateTime) && propType != typeof(Guid) && 
                !propType.IsValueType && propType.Namespace != "System")
                return true;

            return false;
        }

        /// <summary>
        /// Generates Entity class code for an EntityStructure
        /// </summary>
        private string GenerateEntityCode(EntityStructure entity, string namespaceString)
        {
            if (entity == null)
                return null;

            var sb = new StringBuilder();
            sb.AppendLine($"    public class {entity.EntityName} : Entity");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {entity.EntityName}() {{ }}");
            sb.AppendLine();

            foreach (var field in entity.Fields ?? new List<EntityField>())
            {
                var csharpType = MapFieldtypeToCSHarpType(field.Fieldtype);
                var propertyName = _generationHelper.GenerateSafePropertyName(field.FieldName);
                
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// {field.FieldName}");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public {csharpType} {propertyName} {{ get; set; }}");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            return sb.ToString();
        }

        /// <summary>
        /// Maps database field type to C# type
        /// </summary>
        private string MapFieldtypeToCSHarpType(string Fieldtype)
        {
            if (string.IsNullOrEmpty(Fieldtype))
                return "string";

            var lowerType = Fieldtype.ToLower();
            
            if (lowerType.Contains("int"))
                return "int";
            if (lowerType.Contains("long"))
                return "long";
            if (lowerType.Contains("decimal") || lowerType.Contains("numeric") || lowerType.Contains("money"))
                return "decimal";
            if (lowerType.Contains("float") || lowerType.Contains("real"))
                return "float";
            if (lowerType.Contains("double"))
                return "double";
            if (lowerType.Contains("bool") || lowerType.Contains("bit"))
                return "bool";
            if (lowerType.Contains("date") || lowerType.Contains("time"))
                return "DateTime";
            if (lowerType.Contains("guid") || lowerType.Contains("uniqueidentifier"))
                return "Guid";
            if (lowerType.Contains("byte"))
                return "byte[]";
            if (lowerType.Contains("char") || lowerType.Contains("text"))
                return "string";

            return "string";
        }

        #endregion

        #endregion
    }
}
