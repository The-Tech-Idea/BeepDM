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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

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
                // Cross-datasource mapping: EntityName stores table/collection logical name.
                entity.EntityName = tableAttr.Name;
                entity.DatasourceEntityName = tableAttr.Name;
                entity.Caption = tableAttr.Name;
            }
            if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Schema))
            {
                entity.SchemaOrOwnerOrDatabase = tableAttr.Schema;
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
                field.IsRequired = false;
                field.IsNotMapped = false;
                field.ColumnName = string.Empty;
                field.ColumnTypeName = string.Empty;
                field.DatabaseGeneratedOptionName = string.Empty;

                if (!propertyMap.TryGetValue(field.FieldName, out var prop))
                    continue;

                // [Column] - override column name
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                if (columnAttr != null && !string.IsNullOrWhiteSpace(columnAttr.Name))
                {
                    field.ColumnName = columnAttr.Name;
                }
                if (columnAttr != null && !string.IsNullOrWhiteSpace(columnAttr.TypeName))
                {
                    field.ColumnTypeName = columnAttr.TypeName;
                }

                // [Key] - primary key
                if (prop.GetCustomAttribute<KeyAttribute>() != null)
                {
                    field.IsKey = true;
                    entity.HasDataAnnotations = true;
                }

                // [DatabaseGenerated(Identity)] - auto increment
                var dbGenAttr = prop.GetCustomAttribute<DatabaseGeneratedAttribute>();
                if (dbGenAttr != null)
                {
                    field.DatabaseGeneratedOptionName = dbGenAttr.DatabaseGeneratedOption.ToString();
                    if (dbGenAttr.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                    {
                        field.IsAutoIncrement = true;
                    }
                    entity.HasDataAnnotations = true;
                }

                // [Required] - not nullable
                if (prop.GetCustomAttribute<RequiredAttribute>() != null)
                {
                    field.AllowDBNull = false;
                    field.IsRequired = true;
                    entity.HasDataAnnotations = true;
                }

                // [MaxLength] - field size
                var maxLenAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLenAttr != null && maxLenAttr.Length > 0)
                {
                    field.Size1 = maxLenAttr.Length;
                    field.MaxLength = maxLenAttr.Length;
                    entity.HasDataAnnotations = true;
                }
                else
                {
                    // [StringLength] fallback
                    var strLenAttr = prop.GetCustomAttribute<StringLengthAttribute>();
                    if (strLenAttr != null && strLenAttr.MaximumLength > 0)
                    {
                        field.Size1 = strLenAttr.MaximumLength;
                        field.MaxLength = strLenAttr.MaximumLength;
                        field.ValueMin = strLenAttr.MinimumLength;
                        entity.HasDataAnnotations = true;
                    }
                }

                if (prop.GetCustomAttribute<NotMappedAttribute>() != null)
                {
                    field.IsNotMapped = true;
                    entity.HasDataAnnotations = true;
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
        /// Converts EF classes defined in a C# source file to framework Entity classes.
        /// </summary>
        public string ConvertEFClassesFileToEntity(string efClassesFilePath, string outputPath,
            string namespaceString = "TheTechIdea.ProjectClasses", bool generateSingleFile = true)
        {
            if (string.IsNullOrWhiteSpace(efClassesFilePath))
                throw new ArgumentException("EF classes file path cannot be null or empty.", nameof(efClassesFilePath));
            if (!File.Exists(efClassesFilePath))
                throw new FileNotFoundException("EF classes file not found.", efClassesFilePath);

            var source = File.ReadAllText(efClassesFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var root = syntaxTree.GetRoot();
            var classNodes = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(c => c.Modifiers.Any(m => m.Text == "public"))
                .ToList();
            var knownEntityNames = new HashSet<string>(classNodes.Select(c => c.Identifier.Text), StringComparer.OrdinalIgnoreCase);

            var entities = new List<EntityStructure>();
            foreach (var classNode in classNodes)
            {
                var entity = BuildEntityFromClassSyntax(classNode, knownEntityNames);
                if (entity != null && entity.Fields.Count > 0)
                {
                    entities.Add(entity);
                }
            }

            if (entities.Count == 0)
            {
                return string.Empty;
            }

            _generationHelper.EnsureOutputDirectory(outputPath);
            if (generateSingleFile)
            {
                var combined = new StringBuilder();
                combined.AppendLine("using System;");
                combined.AppendLine("using System.ComponentModel.DataAnnotations;");
                combined.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
                combined.AppendLine("using TheTechIdea.Beep.Editor;");
                combined.AppendLine();
                combined.AppendLine($"namespace {namespaceString}");
                combined.AppendLine("{");
                foreach (var entity in entities)
                {
                    combined.AppendLine(GenerateEntityCode(entity, namespaceString));
                    combined.AppendLine();
                }
                combined.AppendLine("}");

                var targetFile = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(efClassesFilePath)}.Entity.cs");
                _generationHelper.WriteToFile(targetFile, combined.ToString(), Path.GetFileNameWithoutExtension(targetFile));
                return targetFile;
            }

            foreach (var entity in entities)
            {
                var code = new StringBuilder();
                code.AppendLine("using System;");
                code.AppendLine("using System.ComponentModel.DataAnnotations;");
                code.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
                code.AppendLine("using TheTechIdea.Beep.Editor;");
                code.AppendLine();
                code.AppendLine($"namespace {namespaceString}");
                code.AppendLine("{");
                code.AppendLine(GenerateEntityCode(entity, namespaceString));
                code.AppendLine("}");
                var filePath = Path.Combine(outputPath, $"{entity.EntityName}Entity.cs");
                _generationHelper.WriteToFile(filePath, code.ToString(), entity.EntityName);
            }

            return outputPath;
        }

        private EntityStructure BuildEntityFromClassSyntax(ClassDeclarationSyntax classNode, HashSet<string> knownEntityNames = null)
        {
            var allProperties = classNode.Members.OfType<PropertyDeclarationSyntax>().ToList();
            var propertyNames = new HashSet<string>(allProperties.Select(p => p.Identifier.Text), StringComparer.OrdinalIgnoreCase);
            var navigationTargets = allProperties
                .Select(p => new { PropertyName = p.Identifier.Text, TypeName = ExtractRelatedEntityTypeName(p.Type) })
                .Where(x => !string.IsNullOrWhiteSpace(x.TypeName))
                .ToDictionary(x => x.PropertyName, x => x.TypeName, StringComparer.OrdinalIgnoreCase);

            var entity = new EntityStructure
            {
                EntityName = classNode.Identifier.Text,
                DatasourceEntityName = classNode.Identifier.Text,
                OriginalEntityName = classNode.Identifier.Text,
                Caption = classNode.Identifier.Text,
                Fields = new List<EntityField>(),
                PrimaryKeys = new List<EntityField>(),
                Relations = new List<RelationShipKeys>()
            };

            foreach (var classAttrList in classNode.AttributeLists)
            {
                foreach (var attr in classAttrList.Attributes)
                {
                    var attrName = attr.Name.ToString();
                    if (attrName.EndsWith("Table") || attrName.EndsWith("TableAttribute"))
                    {
                        if (attr.ArgumentList?.Arguments.Count > 0)
                        {
                            entity.EntityName = ExtractAttributeArgumentValue(attr.ArgumentList.Arguments[0].Expression);
                            entity.DatasourceEntityName = entity.EntityName;
                        }
                        foreach (var arg in attr.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())
                        {
                            if (arg.NameEquals?.Name.Identifier.Text == "Schema")
                            {
                                entity.SchemaOrOwnerOrDatabase = ExtractAttributeArgumentValue(arg.Expression);
                            }
                        }
                        entity.HasDataAnnotations = true;
                    }
                }
            }

            int fieldIndex = 0;
            foreach (var prop in allProperties)
            {
                if (prop.AccessorList == null || prop.AccessorList.Accessors.Count == 0)
                    continue;

                if (IsLikelyNavigationProperty(prop, propertyNames, knownEntityNames))
                {
                    TryAddRelationshipFromNavigationProperty(entity, prop, navigationTargets);
                    continue;
                }

                var field = new EntityField
                {
                    FieldName = prop.Identifier.Text,
                    Originalfieldname = prop.Identifier.Text,
                    Fieldtype = NormalizeTypeName(prop.Type.ToString()),
                    FieldIndex = fieldIndex++,
                    AllowDBNull = IsNullableTypeSyntax(prop.Type),
                    EntityName = entity.EntityName
                };

                foreach (var attrList in prop.AttributeLists)
                {
                    foreach (var attr in attrList.Attributes)
                    {
                        var attrName = attr.Name.ToString();
                        if (attrName.EndsWith("Key") || attrName.EndsWith("KeyAttribute"))
                        {
                            field.IsKey = true;
                            entity.HasDataAnnotations = true;
                        }
                        else if (attrName.EndsWith("Required") || attrName.EndsWith("RequiredAttribute"))
                        {
                            field.IsRequired = true;
                            field.AllowDBNull = false;
                            entity.HasDataAnnotations = true;
                        }
                        else if (attrName.EndsWith("MaxLength") || attrName.EndsWith("MaxLengthAttribute"))
                        {
                            if (attr.ArgumentList?.Arguments.Count > 0 &&
                                int.TryParse(attr.ArgumentList.Arguments[0].ToString(), out var len))
                            {
                                field.Size1 = len;
                                field.MaxLength = len;
                            }
                            entity.HasDataAnnotations = true;
                        }
                        else if (attrName.EndsWith("StringLength") || attrName.EndsWith("StringLengthAttribute"))
                        {
                            if (attr.ArgumentList?.Arguments.Count > 0 &&
                                int.TryParse(attr.ArgumentList.Arguments[0].ToString(), out var maxLen))
                            {
                                field.Size1 = maxLen;
                                field.MaxLength = maxLen;
                            }
                            foreach (var arg in attr.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())
                            {
                                if (arg.NameEquals?.Name.Identifier.Text == "MinimumLength" &&
                                    int.TryParse(arg.Expression.ToString(), out var minLen))
                                {
                                    field.ValueMin = minLen;
                                }
                            }
                            entity.HasDataAnnotations = true;
                        }
                        else if (attrName.EndsWith("Column") || attrName.EndsWith("ColumnAttribute"))
                        {
                            if (attr.ArgumentList?.Arguments.Count > 0)
                            {
                                field.ColumnName = ExtractAttributeArgumentValue(attr.ArgumentList.Arguments[0].Expression);
                            }
                            foreach (var arg in attr.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())
                            {
                                if (arg.NameEquals?.Name.Identifier.Text == "TypeName")
                                {
                                    field.ColumnTypeName = ExtractAttributeArgumentValue(arg.Expression);
                                }
                            }
                            entity.HasDataAnnotations = true;
                        }
                        else if (attrName.EndsWith("DatabaseGenerated") || attrName.EndsWith("DatabaseGeneratedAttribute"))
                        {
                            var argText = attr.ArgumentList?.Arguments.FirstOrDefault().ToString() ?? string.Empty;
                            if (argText.Contains("Identity"))
                            {
                                field.IsAutoIncrement = true;
                                field.DatabaseGeneratedOptionName = "Identity";
                            }
                            else if (argText.Contains("Computed"))
                            {
                                field.DatabaseGeneratedOptionName = "Computed";
                            }
                            else if (argText.Contains("None"))
                            {
                                field.DatabaseGeneratedOptionName = "None";
                            }
                            entity.HasDataAnnotations = true;
                        }
                        else if (attrName.EndsWith("NotMapped") || attrName.EndsWith("NotMappedAttribute"))
                        {
                            field.IsNotMapped = true;
                            entity.HasDataAnnotations = true;
                        }
                        else if (attrName.EndsWith("ForeignKey") || attrName.EndsWith("ForeignKeyAttribute"))
                        {
                            var targetNavigation = GetFirstStringAttributeArgument(attr);
                            if (!string.IsNullOrWhiteSpace(targetNavigation) &&
                                navigationTargets.TryGetValue(targetNavigation, out var relatedType))
                            {
                                AddOrUpdateRelationship(entity, field.FieldName, relatedType, "Id", targetNavigation);
                                entity.HasDataAnnotations = true;
                            }
                        }
                    }
                }

                if (!field.IsNotMapped)
                {
                    entity.Fields.Add(field);
                    if (field.IsKey)
                    {
                        entity.PrimaryKeys.Add(field);
                    }
                }
            }

            if (entity.PrimaryKeys.Count == 0)
            {
                var conventionalKey = entity.Fields.FirstOrDefault(f =>
                    string.Equals(f.FieldName, "Id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.FieldName, $"{entity.EntityName}Id", StringComparison.OrdinalIgnoreCase));
                if (conventionalKey != null)
                {
                    conventionalKey.IsKey = true;
                    entity.PrimaryKeys.Add(conventionalKey);
                }
            }
            return entity;
        }

        private static string TrimAttributeString(string input)
        {
            return input.Trim().Trim('"');
        }

        private static bool IsNullableTypeSyntax(TypeSyntax typeSyntax)
        {
            if (typeSyntax is NullableTypeSyntax) return true;
            var typeText = typeSyntax.ToString();
            if (typeText.EndsWith("?")) return true;
            return typeText == "string" || typeText.StartsWith("Nullable<", StringComparison.OrdinalIgnoreCase) ||
                   typeText.StartsWith("System.Nullable<", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLikelyNavigationProperty(PropertyDeclarationSyntax prop, HashSet<string> propertyNames, HashSet<string> knownEntityNames = null)
        {
            var typeSyntax = prop.Type;
            var typeText = typeSyntax.ToString();
            if (typeText.Contains("ICollection<", StringComparison.OrdinalIgnoreCase) ||
                typeText.Contains("IEnumerable<", StringComparison.OrdinalIgnoreCase) ||
                typeText.Contains("List<", StringComparison.OrdinalIgnoreCase) ||
                typeText.Contains("HashSet<", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (HasAttribute(prop, "ForeignKey") || HasAttribute(prop, "InverseProperty"))
            {
                return true;
            }

            var isVirtual = prop.Modifiers.Any(m => m.Text.Equals("virtual", StringComparison.OrdinalIgnoreCase));
            if (isVirtual && !IsKnownScalarTypeSyntax(typeSyntax))
            {
                return true;
            }

            var relatedType = ExtractRelatedEntityTypeName(typeSyntax);
            if (!string.IsNullOrWhiteSpace(relatedType))
            {
                if (knownEntityNames != null && knownEntityNames.Contains(relatedType) && !IsKnownScalarTypeSyntax(typeSyntax))
                {
                    return true;
                }
                var conventionalFkByType = $"{relatedType}Id";
                var conventionalFkByProperty = $"{prop.Identifier.Text}Id";
                if (propertyNames.Contains(conventionalFkByType) || propertyNames.Contains(conventionalFkByProperty))
                {
                    return true;
                }
            }
            return false;
        }

        private static string NormalizeTypeName(string typeText)
        {
            if (string.IsNullOrWhiteSpace(typeText))
            {
                return typeof(string).FullName;
            }

            var normalized = typeText.Replace("global::", string.Empty).Trim();
            if (normalized.EndsWith("?"))
            {
                normalized = normalized.TrimEnd('?');
            }
            if (normalized.StartsWith("Nullable<", StringComparison.OrdinalIgnoreCase) &&
                normalized.EndsWith(">"))
            {
                normalized = normalized.Substring("Nullable<".Length, normalized.Length - "Nullable<".Length - 1).Trim();
            }
            if (normalized.StartsWith("System.Nullable<", StringComparison.OrdinalIgnoreCase) &&
                normalized.EndsWith(">"))
            {
                normalized = normalized.Substring("System.Nullable<".Length, normalized.Length - "System.Nullable<".Length - 1).Trim();
            }
            if (normalized.EndsWith("[]", StringComparison.Ordinal))
            {
                return normalized.Equals("byte[]", StringComparison.OrdinalIgnoreCase)
                    ? typeof(byte[]).FullName
                    : normalized;
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["int"] = typeof(int).FullName,
                ["long"] = typeof(long).FullName,
                ["short"] = typeof(short).FullName,
                ["byte"] = typeof(byte).FullName,
                ["bool"] = typeof(bool).FullName,
                ["decimal"] = typeof(decimal).FullName,
                ["double"] = typeof(double).FullName,
                ["float"] = typeof(float).FullName,
                ["DateTime"] = typeof(DateTime).FullName,
                ["DateTimeOffset"] = typeof(DateTimeOffset).FullName,
                ["TimeSpan"] = typeof(TimeSpan).FullName,
                ["Guid"] = typeof(Guid).FullName,
                ["DateOnly"] = typeof(DateOnly).FullName,
                ["TimeOnly"] = typeof(TimeOnly).FullName,
                ["string"] = typeof(string).FullName,
                ["System.Int32"] = typeof(int).FullName,
                ["System.Int64"] = typeof(long).FullName,
                ["System.Int16"] = typeof(short).FullName,
                ["System.Byte"] = typeof(byte).FullName,
                ["System.Boolean"] = typeof(bool).FullName,
                ["System.Decimal"] = typeof(decimal).FullName,
                ["System.Double"] = typeof(double).FullName,
                ["System.Single"] = typeof(float).FullName,
                ["System.DateTime"] = typeof(DateTime).FullName,
                ["System.DateTimeOffset"] = typeof(DateTimeOffset).FullName,
                ["System.TimeSpan"] = typeof(TimeSpan).FullName,
                ["System.Guid"] = typeof(Guid).FullName,
                ["System.String"] = typeof(string).FullName
            };

            if (map.TryGetValue(normalized, out var mapped))
            {
                return mapped;
            }

            if (normalized.StartsWith("System.Collections.Generic.", StringComparison.OrdinalIgnoreCase))
            {
                return typeof(string).FullName;
            }

            // Enum and unknown scalar-like types are stored as int by default.
            if (!normalized.Contains("<") && !normalized.Contains(".Collections.", StringComparison.OrdinalIgnoreCase))
            {
                return typeof(int).FullName;
            }
            return normalized;
        }

        private static bool IsKnownScalarTypeSyntax(TypeSyntax typeSyntax)
        {
            var typeText = typeSyntax.ToString().Replace("global::", string.Empty).Trim();
            if (typeText.EndsWith("?"))
            {
                typeText = typeText.TrimEnd('?');
            }
            if (typeText.StartsWith("Nullable<", StringComparison.OrdinalIgnoreCase) && typeText.EndsWith(">"))
            {
                typeText = typeText.Substring("Nullable<".Length, typeText.Length - "Nullable<".Length - 1).Trim();
            }
            if (typeText.StartsWith("System.Nullable<", StringComparison.OrdinalIgnoreCase) && typeText.EndsWith(">"))
            {
                typeText = typeText.Substring("System.Nullable<".Length, typeText.Length - "System.Nullable<".Length - 1).Trim();
            }

            if (typeText.Equals("byte[]", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var scalarTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "int", "long", "short", "byte", "bool", "decimal", "double", "float",
                "DateTime", "DateTimeOffset", "TimeSpan", "Guid", "string", "char",
                "DateOnly", "TimeOnly",
                "System.Int32", "System.Int64", "System.Int16", "System.Byte", "System.Boolean",
                "System.Decimal", "System.Double", "System.Single", "System.DateTime",
                "System.DateTimeOffset", "System.TimeSpan", "System.Guid", "System.String"
            };
            return scalarTypes.Contains(typeText);
        }

        private static string ExtractRelatedEntityTypeName(TypeSyntax typeSyntax)
        {
            var text = typeSyntax.ToString().Replace("global::", string.Empty).Trim();
            if (text.EndsWith("?"))
            {
                text = text.TrimEnd('?');
            }
            if (text.Contains("<") && text.EndsWith(">"))
            {
                var start = text.IndexOf('<');
                var inner = text.Substring(start + 1, text.Length - start - 2).Trim();
                if (!inner.Contains(","))
                {
                    text = inner;
                }
            }
            if (text.Contains("."))
            {
                text = text.Split('.').Last();
            }
            return text;
        }

        private static bool HasAttribute(PropertyDeclarationSyntax prop, string attributeShortName)
        {
            foreach (var attribute in prop.AttributeLists.SelectMany(a => a.Attributes))
            {
                var name = attribute.Name.ToString();
                if (name.Equals(attributeShortName, StringComparison.OrdinalIgnoreCase) ||
                    name.Equals($"{attributeShortName}Attribute", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith($".{attributeShortName}", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith($".{attributeShortName}Attribute", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static string GetFirstStringAttributeArgument(AttributeSyntax attribute)
        {
            var first = attribute.ArgumentList?.Arguments.FirstOrDefault();
            if (first == null)
            {
                return string.Empty;
            }
            return ExtractAttributeArgumentValue(first.Expression);
        }

        private static string ExtractAttributeArgumentValue(ExpressionSyntax expression)
        {
            if (expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return literal.Token.ValueText;
            }

            if (expression is InvocationExpressionSyntax invocation &&
                invocation.Expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.Text.Equals("nameof", StringComparison.OrdinalIgnoreCase))
            {
                var argument = invocation.ArgumentList.Arguments.FirstOrDefault();
                if (argument?.Expression is IdentifierNameSyntax argIdentifier)
                {
                    return argIdentifier.Identifier.Text;
                }
                if (argument?.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    return memberAccess.Name.Identifier.Text;
                }
                return argument?.Expression.ToString() ?? string.Empty;
            }

            return TrimAttributeString(expression.ToString());
        }

        private static void AddOrUpdateRelationship(EntityStructure entity, string fkFieldName, string relatedType, string relatedKey, string relationName = null)
        {
            if (string.IsNullOrWhiteSpace(fkFieldName) || string.IsNullOrWhiteSpace(relatedType))
            {
                return;
            }

            var existing = entity.Relations.FirstOrDefault(r =>
                string.Equals(r.EntityColumnID, fkFieldName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.RelatedEntityID, relatedType, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                if (string.IsNullOrWhiteSpace(existing.RelatedEntityColumnID))
                {
                    existing.RelatedEntityColumnID = string.IsNullOrWhiteSpace(relatedKey) ? "Id" : relatedKey;
                }
                if (string.IsNullOrWhiteSpace(existing.RalationName) && !string.IsNullOrWhiteSpace(relationName))
                {
                    existing.RalationName = relationName;
                }
                return;
            }

            entity.Relations.Add(new RelationShipKeys
            {
                EntityColumnID = fkFieldName,
                RelatedEntityID = relatedType,
                RelatedEntityColumnID = string.IsNullOrWhiteSpace(relatedKey) ? "Id" : relatedKey,
                RalationName = relationName
            });
        }

        private static void TryAddRelationshipFromNavigationProperty(
            EntityStructure entity,
            PropertyDeclarationSyntax navProp,
            Dictionary<string, string> navigationTargets)
        {
            var relatedType = ExtractRelatedEntityTypeName(navProp.Type);
            if (string.IsNullOrWhiteSpace(relatedType))
            {
                return;
            }

            var navName = navProp.Identifier.Text;
            var fkField = $"{relatedType}Id";
            string relationName = null;

            foreach (var attr in navProp.AttributeLists.SelectMany(a => a.Attributes))
            {
                var attrName = attr.Name.ToString();
                if (attrName.EndsWith("ForeignKey", StringComparison.OrdinalIgnoreCase) ||
                    attrName.EndsWith("ForeignKeyAttribute", StringComparison.OrdinalIgnoreCase))
                {
                    var referencedFk = GetFirstStringAttributeArgument(attr);
                    if (!string.IsNullOrWhiteSpace(referencedFk))
                    {
                        fkField = referencedFk;
                    }
                }
                else if (attrName.EndsWith("InverseProperty", StringComparison.OrdinalIgnoreCase) ||
                         attrName.EndsWith("InversePropertyAttribute", StringComparison.OrdinalIgnoreCase))
                {
                    relationName = GetFirstStringAttributeArgument(attr);
                }
            }

            if (string.IsNullOrWhiteSpace(relationName) && navigationTargets.ContainsKey(navName))
            {
                relationName = navName;
            }

            AddOrUpdateRelationship(entity, fkField, relatedType, "Id", relationName);
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
            var className = _generationHelper.GenerateSafePropertyName(entity.EntityName);
            if (entity.HasDataAnnotations)
            {
                if (!string.IsNullOrWhiteSpace(entity.SchemaOrOwnerOrDatabase))
                {
                    sb.AppendLine($"    [Table(\"{entity.EntityName}\", Schema = \"{entity.SchemaOrOwnerOrDatabase}\")]");
                }
                else
                {
                    sb.AppendLine($"    [Table(\"{entity.EntityName}\")]");
                }
            }
            sb.AppendLine($"    public class {className}Entity : Entity");
            sb.AppendLine("    {");
            sb.AppendLine($"        public {className}Entity() {{ }}");
            sb.AppendLine();

            foreach (var field in entity.Fields ?? new List<EntityField>())
            {
                var csharpType = MapFieldtypeToCSHarpType(field.Fieldtype, field.AllowDBNull);
                var propertyName = _generationHelper.GenerateSafePropertyName(field.FieldName);
                var backingField = $"_{char.ToLowerInvariant(propertyName[0])}{propertyName.Substring(1)}";
                
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// {field.FieldName}");
                sb.AppendLine($"        /// </summary>");
                foreach (var annotation in BuildFieldAnnotations(field, csharpType))
                {
                    sb.AppendLine($"        {annotation}");
                }
                sb.AppendLine($"        private {csharpType} {backingField};");
                sb.AppendLine($"        public {csharpType} {propertyName}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => {backingField};");
                sb.AppendLine($"            set => SetProperty(ref {backingField}, value);");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            return sb.ToString();
        }

        /// <summary>
        /// Maps database field type to C# type
        /// </summary>
        private string MapFieldtypeToCSHarpType(string Fieldtype, bool isNullable = true)
        {
            if (string.IsNullOrEmpty(Fieldtype))
                return "string";

            var normalized = Fieldtype.Replace("global::", string.Empty).Trim();
            var lowerType = normalized.ToLowerInvariant();
            if (lowerType == "system.byte[]" || lowerType == "byte[]")
                return "byte[]";
            if (lowerType == "system.string" || lowerType == "string")
                return "string";
            if (lowerType == "system.char" || lowerType == "char")
                return isNullable ? "char?" : "char";
            if (lowerType == "system.dateonly" || lowerType == "dateonly")
                return isNullable ? "DateOnly?" : "DateOnly";
            if (lowerType == "system.timeonly" || lowerType == "timeonly")
                return isNullable ? "TimeOnly?" : "TimeOnly";
            
            if (lowerType.Contains("int32") || lowerType == "int")
                return isNullable ? "int?" : "int";
            if (lowerType.Contains("int64") || lowerType == "long")
                return isNullable ? "long?" : "long";
            if (lowerType.Contains("int16") || lowerType == "short")
                return isNullable ? "short?" : "short";
            if (lowerType == "system.byte" || lowerType == "byte")
                return isNullable ? "byte?" : "byte";
            if (lowerType.Contains("decimal") || lowerType.Contains("numeric") || lowerType.Contains("money"))
                return isNullable ? "decimal?" : "decimal";
            if (lowerType.Contains("float") || lowerType.Contains("real"))
                return isNullable ? "float?" : "float";
            if (lowerType.Contains("double"))
                return isNullable ? "double?" : "double";
            if (lowerType.Contains("bool") || lowerType.Contains("bit"))
                return isNullable ? "bool?" : "bool";
            if (lowerType.Contains("date") || lowerType.Contains("time"))
                return isNullable ? "DateTime?" : "DateTime";
            if (lowerType.Contains("guid") || lowerType.Contains("uniqueidentifier"))
                return isNullable ? "Guid?" : "Guid";
            if (lowerType.Contains("char") || lowerType.Contains("text"))
                return "string";

            if (normalized.StartsWith("System.", StringComparison.Ordinal))
                return isNullable ? $"{normalized}?" : normalized;

            return "string";
        }

        private IEnumerable<string> BuildFieldAnnotations(EntityField field, string csharpType)
        {
            if (field.IsKey)
            {
                yield return "[Key]";
            }
            if (field.IsRequired || (!field.AllowDBNull && csharpType == "string"))
            {
                yield return "[Required]";
            }
            if (!string.IsNullOrWhiteSpace(field.ColumnName) || !string.IsNullOrWhiteSpace(field.ColumnTypeName))
            {
                if (!string.IsNullOrWhiteSpace(field.ColumnName) && !string.IsNullOrWhiteSpace(field.ColumnTypeName))
                {
                    yield return $"[Column(\"{field.ColumnName}\", TypeName = \"{field.ColumnTypeName}\")]";
                }
                else if (!string.IsNullOrWhiteSpace(field.ColumnName))
                {
                    yield return $"[Column(\"{field.ColumnName}\")]";
                }
                else
                {
                    yield return $"[Column(TypeName = \"{field.ColumnTypeName}\")]";
                }
            }
            if (field.ValueMin > 0 && field.MaxLength > 0)
            {
                yield return $"[StringLength({field.MaxLength}, MinimumLength = {field.ValueMin})]";
            }
            else if (field.MaxLength > 0)
            {
                yield return $"[MaxLength({field.MaxLength})]";
            }
            else if (field.Size1 > 0 && csharpType == "string")
            {
                yield return $"[StringLength({field.Size1})]";
            }
            if (!string.IsNullOrWhiteSpace(field.DatabaseGeneratedOptionName))
            {
                yield return $"[DatabaseGenerated(DatabaseGeneratedOption.{field.DatabaseGeneratedOptionName})]";
            }
            else if (field.IsAutoIncrement)
            {
                yield return "[DatabaseGenerated(DatabaseGeneratedOption.Identity)]";
            }
            if (field.IsNotMapped)
            {
                yield return "[NotMapped]";
            }
        }

        #endregion

        #region EF Core Dedicated Conversion (EfCoreToEntityGeneratorHelper)

        public List<Type> ScanNamespaceForEfCoreClasses(string namespaceName, Assembly assembly = null)
            => _efCoreToEntityHelper.ScanNamespaceForEfCoreClasses(namespaceName, assembly);

        public EntityStructure ConvertEfCoreTypeToEntityStructure(Type efCoreType, bool includeRelationships = true)
            => _efCoreToEntityHelper.ConvertEfCoreTypeToEntityStructure(efCoreType, includeRelationships);

        public List<EntityStructure> ConvertEfCoreTypesToEntityStructures(IEnumerable<Type> efCoreTypes, bool includeRelationships = true)
            => _efCoreToEntityHelper.ConvertEfCoreTypesToEntityStructures(efCoreTypes, includeRelationships);

        public List<EntityStructure> ConvertEfCoreNamespaceToEntityStructures(string namespaceName, Assembly assembly = null, bool includeRelationships = true)
            => _efCoreToEntityHelper.ConvertEfCoreNamespaceToEntityStructures(namespaceName, assembly, includeRelationships);

        public List<EntityStructure> ConvertEfCoreFileToEntityStructures(string filePath, bool includeRelationships = true)
            => _efCoreToEntityHelper.ConvertEfCoreFileToEntityStructures(filePath, includeRelationships);

        public List<EntityStructure> ConvertEfCoreSourceToEntityStructures(string sourceCode, bool includeRelationships = true)
            => _efCoreToEntityHelper.ConvertEfCoreSourceToEntityStructures(sourceCode, includeRelationships);

        public List<EntityStructure> ConvertEfCoreDirectoryToEntityStructures(string directoryPath, bool recursive = true, bool includeRelationships = true)
            => _efCoreToEntityHelper.ConvertEfCoreDirectoryToEntityStructures(directoryPath, recursive, includeRelationships);

        public string GenerateEntityClassFromEfCore(Type efCoreType, string outputPath = null,
            string namespaceString = "TheTechIdea.ProjectClasses", bool generateFile = true, bool includeRelationships = true)
            => _efCoreToEntityHelper.GenerateEntityClassFromEfCore(efCoreType, outputPath, namespaceString, generateFile, includeRelationships);

        public List<string> GenerateEntityClassesFromEfCoreNamespace(string sourceNamespace, string outputPath = null,
            string targetNamespace = "TheTechIdea.ProjectClasses", bool generateFiles = true, Assembly assembly = null, bool includeRelationships = true)
            => _efCoreToEntityHelper.GenerateEntityClassesFromEfCoreNamespace(sourceNamespace, outputPath, targetNamespace, generateFiles, assembly, includeRelationships);

        public List<string> GenerateEfCoreClassesFromEntityStructures(List<EntityStructure> entities, string outputPath,
            string namespaceName = "TheTechIdea.ProjectEfModels", bool generateFiles = true)
            => _efCoreToEntityHelper.GenerateEfCoreClassesFromEntityStructures(entities, outputPath, namespaceName, generateFiles);

        public string GenerateEfCoreCombinedFileFromEntityStructures(List<EntityStructure> entities, string outputFilePath,
            string namespaceName = "TheTechIdea.ProjectEfModels")
            => _efCoreToEntityHelper.GenerateEfCoreCombinedFileFromEntityStructures(entities, outputFilePath, namespaceName);

        public string GenerateEfCoreDllFromEntityStructures(List<EntityStructure> entities, string dllName, string outputPath,
            string namespaceName = "TheTechIdea.ProjectEfModels")
            => _efCoreToEntityHelper.GenerateEfCoreDllFromEntityStructures(entities, dllName, outputPath, namespaceName);

        #endregion

        #endregion
    }
}
