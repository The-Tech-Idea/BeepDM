using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using System.Reflection;
using System.Data;
using System.ComponentModel;
using System.IO;

namespace TheTechIdea.Beep.DataBase
{
    public static class EntitiesExtensions
    {
        #region "EntityStructure Single Instance Extensions"

        /// <summary>
        /// Populates EntityStructure from a .NET Type using reflection
        /// </summary>
        public static EntityStructure FromType(this EntityStructure entity, Type type)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            entity.EntityName = type.Name;
            entity.DatasourceEntityName = type.Name;
            entity.OriginalEntityName = type.Name;
            entity.Caption = type.Name;
            entity.Category = type.Namespace ?? "Unknown";

            // Clear existing fields
            entity.Fields.Clear();
            entity.PrimaryKeys.Clear();

            // Iterate over properties to create fields
            foreach (PropertyInfo propInfo in type.GetProperties())
            {
                EntityField field = CreateFieldFromProperty(propInfo);
                entity.Fields.Add(field);

                if (field.IsKey)
                {
                    entity.PrimaryKeys.Add(field);
                }
            }

            return entity;
        }

        /// <summary>
        /// Creates an EntityStructure from a generic type
        /// </summary>
        public static EntityStructure FromType<T>(this EntityStructure entity)
        {
            return entity.FromType(typeof(T));
        }

        /// <summary>
        /// Populates EntityStructure from a DataTable
        /// </summary>
        public static EntityStructure FromDataTable(this EntityStructure entity, DataTable dataTable)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            entity.EntityName = dataTable.TableName;
            entity.DatasourceEntityName = dataTable.TableName;
            entity.OriginalEntityName = dataTable.TableName;
            entity.Caption = dataTable.TableName;

            // Clear existing fields
            entity.Fields.Clear();
            entity.PrimaryKeys.Clear();

            foreach (DataColumn column in dataTable.Columns)
            {
                EntityField field = CreateFieldFromDataColumn(column);
                entity.Fields.Add(field);

                if (field.IsKey || dataTable.PrimaryKey.Contains(column))
                {
                    field.IsKey = true;
                    entity.PrimaryKeys.Add(field);
                }
            }

            return entity;
        }

        /// <summary>
        /// Populates EntityStructure from a list of objects
        /// </summary>
        public static EntityStructure FromList<T>(this EntityStructure entity, List<T> list)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (list == null || !list.Any())
            {
                return entity.FromType<T>();
            }

            Type itemType = typeof(T);
            entity.EntityName = itemType.Name;
            entity.DatasourceEntityName = itemType.Name;
            entity.OriginalEntityName = itemType.Name;
            entity.Caption = itemType.Name;

            // Clear existing fields
            entity.Fields.Clear();
            entity.PrimaryKeys.Clear();

            foreach (PropertyInfo propInfo in itemType.GetProperties())
            {
                EntityField field = CreateFieldFromProperty(propInfo);
                entity.Fields.Add(field);

                if (field.IsKey)
                {
                    entity.PrimaryKeys.Add(field);
                }
            }

            return entity;
        }

        /// <summary>
        /// Converts EntityStructure to a DataTable schema (without data)
        /// </summary>
        public static DataTable ToDataTableSchema(this EntityStructure entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            DataTable table = new DataTable(entity.EntityName);

            foreach (EntityField field in entity.Fields)
            {
                Type fieldType;
                try
                {
                    fieldType = Type.GetType(field.fieldtype) ?? typeof(string);
                }
                catch
                {
                    fieldType = typeof(string);
                }

                DataColumn column = new DataColumn(field.fieldname, fieldType)
                {
                    AllowDBNull = field.AllowDBNull,
                    AutoIncrement = field.IsAutoIncrement,
                    Unique = field.IsUnique,
                    MaxLength = field.Size1 > 0 ? field.Size1 : -1
                };

                if (!string.IsNullOrEmpty(field.DefaultValue))
                {
                    try
                    {
                        column.DefaultValue = Convert.ChangeType(field.DefaultValue, fieldType);
                    }
                    catch
                    {
                        // Ignore if conversion fails
                    }
                }

                table.Columns.Add(column);
            }

            // Set primary keys
            if (entity.PrimaryKeys.Any())
            {
                DataColumn[] primaryKeys = entity.PrimaryKeys
                    .Select(pk => table.Columns[pk.fieldname])
                    .Where(col => col != null)
                    .ToArray();

                if (primaryKeys.Any())
                {
                    table.PrimaryKey = primaryKeys;
                }
            }

            return table;
        }

        /// <summary>
        /// Adds a field to the EntityStructure
        /// </summary>
        public static EntityStructure AddField(this EntityStructure entity, string fieldName, Type fieldType, bool isKey = false, bool allowNull = true)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            EntityField field = new EntityField
            {
                fieldname = fieldName,
                fieldtype = fieldType.FullName,
                fieldCategory = GetFieldCategory(fieldType),
                AllowDBNull = allowNull,
                IsKey = isKey,
                EntityName = entity.EntityName
            };

            entity.Fields.Add(field);

            if (isKey)
            {
                entity.PrimaryKeys.Add(field);
            }

            return entity;
        }

        /// <summary>
        /// Adds a field to the EntityStructure with detailed configuration
        /// </summary>
        public static EntityStructure AddField(this EntityStructure entity, EntityField field)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            field.EntityName = entity.EntityName;
            entity.Fields.Add(field);

            if (field.IsKey && !entity.PrimaryKeys.Contains(field))
            {
                entity.PrimaryKeys.Add(field);
            }

            return entity;
        }

        /// <summary>
        /// Removes a field from the EntityStructure
        /// </summary>
        public static EntityStructure RemoveField(this EntityStructure entity, string fieldName)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var field = entity.Fields.FirstOrDefault(f => 
                string.Equals(f.fieldname, fieldName, StringComparison.OrdinalIgnoreCase));

            if (field != null)
            {
                entity.Fields.Remove(field);
                entity.PrimaryKeys.Remove(field);
            }

            return entity;
        }

        /// <summary>
        /// Gets a field by name
        /// </summary>
        public static EntityField GetField(this EntityStructure entity, string fieldName)
        {
            if (entity == null || string.IsNullOrWhiteSpace(fieldName))
                return null;

            return entity.Fields.FirstOrDefault(f => 
                string.Equals(f.fieldname, fieldName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if a field exists
        /// </summary>
        public static bool HasField(this EntityStructure entity, string fieldName)
        {
            return entity?.GetField(fieldName) != null;
        }

        /// <summary>
        /// Gets all fields of a specific category
        /// </summary>
        public static List<EntityField> GetFieldsByCategory(this EntityStructure entity, DbFieldCategory category)
        {
            if (entity == null)
                return new List<EntityField>();

            return entity.Fields.Where(f => f.fieldCategory == category).ToList();
        }

        /// <summary>
        /// Gets all numeric fields
        /// </summary>
        public static List<EntityField> GetNumericFields(this EntityStructure entity)
        {
            return entity.GetFieldsByCategory(DbFieldCategory.Numeric);
        }

        /// <summary>
        /// Gets all string fields
        /// </summary>
        public static List<EntityField> GetStringFields(this EntityStructure entity)
        {
            return entity.GetFieldsByCategory(DbFieldCategory.String);
        }

        /// <summary>
        /// Gets all date fields
        /// </summary>
        public static List<EntityField> GetDateFields(this EntityStructure entity)
        {
            return entity.GetFieldsByCategory(DbFieldCategory.Date);
        }

        /// <summary>
        /// Validates the EntityStructure
        /// </summary>
        public static (bool IsValid, List<string> Errors) Validate(this EntityStructure entity)
        {
            var errors = new List<string>();

            if (entity == null)
            {
                errors.Add("Entity is null");
                return (false, errors);
            }

            if (string.IsNullOrWhiteSpace(entity.EntityName))
            {
                errors.Add("Entity name is required");
            }

            if (entity.Fields == null || !entity.Fields.Any())
            {
                errors.Add("Entity must have at least one field");
            }
            else
            {
                // Check for duplicate field names
                var duplicates = entity.Fields
                    .GroupBy(f => f.fieldname, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var duplicate in duplicates)
                {
                    errors.Add($"Duplicate field name: {duplicate}");
                }

                // Check for invalid field types
                foreach (var field in entity.Fields)
                {
                    if (string.IsNullOrWhiteSpace(field.fieldname))
                    {
                        errors.Add("Field name cannot be empty");
                    }

                    if (string.IsNullOrWhiteSpace(field.fieldtype))
                    {
                        errors.Add($"Field '{field.fieldname}' has no type specified");
                    }
                }
            }

            return (!errors.Any(), errors);
        }

        /// <summary>
        /// Merges fields from another EntityStructure
        /// </summary>
        public static EntityStructure MergeFields(this EntityStructure entity, EntityStructure sourceEntity, bool overwriteExisting = false)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (sourceEntity == null)
                throw new ArgumentNullException(nameof(sourceEntity));

            foreach (var sourceField in sourceEntity.Fields)
            {
                var existingField = entity.GetField(sourceField.fieldname);

                if (existingField == null)
                {
                    // Add new field
                    entity.AddField((EntityField)sourceField.Clone());
                }
                else if (overwriteExisting)
                {
                    // Replace existing field
                    entity.Fields.Remove(existingField);
                    entity.PrimaryKeys.Remove(existingField);
                    entity.AddField((EntityField)sourceField.Clone());
                }
            }

            return entity;
        }

        /// <summary>
        /// Creates a shallow copy of the EntityStructure with fields only
        /// </summary>
        public static EntityStructure CloneStructureOnly(this EntityStructure entity)
        {
            if (entity == null)
                return null;

            var clone = new EntityStructure
            {
                EntityName = entity.EntityName,
                OriginalEntityName = entity.OriginalEntityName,
                DatasourceEntityName = entity.DatasourceEntityName,
                DataSourceID = entity.DataSourceID,
                Caption = entity.Caption,
                Description = entity.Description,
                EntityType = entity.EntityType,
                Category = entity.Category,
                SchemaOrOwnerOrDatabase = entity.SchemaOrOwnerOrDatabase
            };

            foreach (var field in entity.Fields)
            {
                clone.Fields.Add((EntityField)field.Clone());
            }

            foreach (var pk in entity.PrimaryKeys)
            {
                var clonedPk = clone.Fields.FirstOrDefault(f => f.fieldname == pk.fieldname);
                if (clonedPk != null)
                {
                    clone.PrimaryKeys.Add(clonedPk);
                }
            }

            return clone;
        }

        #endregion

        #region "List<EntityStructure> Collection Extensions"

        /// <summary>
        /// Adds a new entity to the collection with the specified name and type
        /// </summary>
        public static EntityStructure Add(this List<EntityStructure> entities, string entityName, Type entityType)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or empty", nameof(entityName));

            var entity = new EntityStructure();
            entity.FromType(entityType);
            entity.EntityName = entityName;
            entity.DatasourceEntityName = entityName;
            entity.Caption = entityName;

            entities.Add(entity);
            return entity;
        }

        /// <summary>
        /// Adds a new entity from a generic type
        /// </summary>
        public static EntityStructure Add<T>(this List<EntityStructure> entities, string entityName = null)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entity = new EntityStructure();
            entity.FromType<T>();
            
            if (!string.IsNullOrWhiteSpace(entityName))
            {
                entity.EntityName = entityName;
                entity.DatasourceEntityName = entityName;
                entity.Caption = entityName;
            }

            entities.Add(entity);
            return entity;
        }

        /// <summary>
        /// Adds a new entity to the collection with full configuration
        /// </summary>
        public static EntityStructure Add(this List<EntityStructure> entities, string entityName, string dataSourceId, string schemaOrOwner = null)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entity = new EntityStructure
            {
                EntityName = entityName,
                DatasourceEntityName = entityName,
                OriginalEntityName = entityName,
                DataSourceID = dataSourceId,
                SchemaOrOwnerOrDatabase = schemaOrOwner,
                Caption = entityName
            };

            entities.Add(entity);
            return entity;
        }

        /// <summary>
        /// Adds an entity from a DataTable
        /// </summary>
        public static EntityStructure AddFromDataTable(this List<EntityStructure> entities, DataTable dataTable)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            var entity = new EntityStructure();
            entity.FromDataTable(dataTable);
            entities.Add(entity);
            return entity;
        }

        /// <summary>
        /// Finds an entity by name (case-insensitive)
        /// </summary>
        public static EntityStructure FindByName(this List<EntityStructure> entities, string entityName)
        {
            if (entities == null || string.IsNullOrWhiteSpace(entityName))
                return null;

            return entities.FirstOrDefault(e => 
                string.Equals(e.EntityName, entityName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.DatasourceEntityName, entityName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.OriginalEntityName, entityName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all entities from a specific data source
        /// </summary>
        public static List<EntityStructure> GetByDataSource(this List<EntityStructure> entities, string dataSourceId)
        {
            if (entities == null || string.IsNullOrWhiteSpace(dataSourceId))
                return new List<EntityStructure>();

            return entities.Where(e => 
                string.Equals(e.DataSourceID, dataSourceId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Gets all entities of a specific type
        /// </summary>
        public static List<EntityStructure> GetByType(this List<EntityStructure> entities, EntityType entityType)
        {
            if (entities == null)
                return new List<EntityStructure>();

            return entities.Where(e => e.EntityType == entityType).ToList();
        }

        /// <summary>
        /// Removes an entity by name
        /// </summary>
        public static bool RemoveByName(this List<EntityStructure> entities, string entityName)
        {
            if (entities == null || string.IsNullOrWhiteSpace(entityName))
                return false;

            var entity = entities.FindByName(entityName);
            if (entity != null)
            {
                return entities.Remove(entity);
            }

            return false;
        }

        /// <summary>
        /// Checks if an entity exists by name
        /// </summary>
        public static bool Contains(this List<EntityStructure> entities, string entityName)
        {
            return entities?.FindByName(entityName) != null;
        }

        /// <summary>
        /// Gets all table entities
        /// </summary>
        public static List<EntityStructure> GetTables(this List<EntityStructure> entities)
        {
            return entities.GetByType(EntityType.Table);
        }

        /// <summary>
        /// Gets all view entities
        /// </summary>
        public static List<EntityStructure> GetViews(this List<EntityStructure> entities)
        {
            return entities.GetByType(EntityType.View);
        }

        /// <summary>
        /// Gets all query entities
        /// </summary>
        public static List<EntityStructure> GetQueries(this List<EntityStructure> entities)
        {
            return entities.GetByType(EntityType.Query);
        }

        /// <summary>
        /// Gets entities by schema or owner
        /// </summary>
        public static List<EntityStructure> GetBySchema(this List<EntityStructure> entities, string schema)
        {
            if (entities == null || string.IsNullOrWhiteSpace(schema))
                return new List<EntityStructure>();

            return entities.Where(e => 
                string.Equals(e.SchemaOrOwnerOrDatabase, schema, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Gets entities by category
        /// </summary>
        public static List<EntityStructure> GetByCategory(this List<EntityStructure> entities, string category)
        {
            if (entities == null || string.IsNullOrWhiteSpace(category))
                return new List<EntityStructure>();

            return entities.Where(e => 
                string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Clones all entities in the collection
        /// </summary>
        public static List<EntityStructure> CloneAll(this List<EntityStructure> entities)
        {
            if (entities == null)
                return new List<EntityStructure>();

            return entities.Select(e => (EntityStructure)e.Clone()).ToList();
        }

        /// <summary>
        /// Gets all loaded entities
        /// </summary>
        public static List<EntityStructure> GetLoaded(this List<EntityStructure> entities)
        {
            if (entities == null)
                return new List<EntityStructure>();

            return entities.Where(e => e.IsLoaded).ToList();
        }

        /// <summary>
        /// Gets all entities that need to be saved
        /// </summary>
        public static List<EntityStructure> GetUnsaved(this List<EntityStructure> entities)
        {
            if (entities == null)
                return new List<EntityStructure>();

            return entities.Where(e => !e.IsSaved).ToList();
        }

        /// <summary>
        /// Updates or adds an entity (upsert operation)
        /// </summary>
        public static EntityStructure Upsert(this List<EntityStructure> entities, EntityStructure entity)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var existing = entities.FindByName(entity.EntityName);
            if (existing != null)
            {
                entities.Remove(existing);
            }

            entities.Add(entity);
            return entity;
        }

        /// <summary>
        /// Batch adds multiple entities
        /// </summary>
        public static void AddRange(this List<EntityStructure> entities, IEnumerable<EntityStructure> entitiesToAdd)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            if (entitiesToAdd != null)
            {
                entities.AddRange(entitiesToAdd);
            }
        }

        /// <summary>
        /// Validates all entities in the collection
        /// </summary>
        public static Dictionary<string, List<string>> ValidateAll(this List<EntityStructure> entities)
        {
            var validationResults = new Dictionary<string, List<string>>();

            if (entities == null)
                return validationResults;

            foreach (var entity in entities)
            {
                var (isValid, errors) = entity.Validate();
                if (!isValid)
                {
                    validationResults[entity.EntityName ?? "Unknown"] = errors;
                }
            }

            return validationResults;
        }

        #endregion

        #region "Helper Methods"

        private static EntityField CreateFieldFromProperty(PropertyInfo propInfo)
        {
            Type propertyType = propInfo.PropertyType;
            
            // Handle nullable types
            Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            bool isNullable = Nullable.GetUnderlyingType(propertyType) != null || !propertyType.IsValueType;

            EntityField field = new EntityField
            {
                fieldname = propInfo.Name,
                fieldtype = underlyingType.FullName,
                fieldCategory = GetFieldCategory(underlyingType),
                AllowDBNull = isNullable,
                Originalfieldname = propInfo.Name
            };

            // Try to determine size for strings
            if (underlyingType == typeof(string))
            {
                field.Size1 = -1; // Unlimited by default
            }

            return field;
        }

        private static EntityField CreateFieldFromDataColumn(DataColumn column)
        {
            Type columnType = column.DataType;
            Type underlyingType = Nullable.GetUnderlyingType(columnType) ?? columnType;

            EntityField field = new EntityField
            {
                EntityName = column.Table?.TableName,
                fieldname = column.ColumnName,
                fieldtype = underlyingType.FullName,
                fieldCategory = GetFieldCategory(underlyingType),
                Size1 = column.MaxLength,
                IsAutoIncrement = column.AutoIncrement,
                AllowDBNull = column.AllowDBNull,
                IsUnique = column.Unique,
                FieldIndex = column.Ordinal,
                Originalfieldname = column.ColumnName,
                DefaultValue = column.DefaultValue?.ToString()
            };

            return field;
        }

        /// <summary>
        /// Configuration for field category mapping
        /// </summary>
        public static class FieldCategoryConfig
        {
            /// <summary>
            /// If true, decimal types will be categorized as Currency, otherwise as Numeric
            /// </summary>
            public static bool TreatDecimalAsCurrency { get; set; } = true;

            /// <summary>
            /// If true, complex objects will be categorized as Json, otherwise as String
            /// </summary>
            public static bool TreatComplexObjectsAsJson { get; set; } = false;

            /// <summary>
            /// Custom type mappings for specific types
            /// </summary>
            public static Dictionary<Type, DbFieldCategory> CustomTypeMappings { get; set; } = new Dictionary<Type, DbFieldCategory>();
        }

        /// <summary>
        /// Gets the field category for a given type with configuration support
        /// </summary>
        public static DbFieldCategory GetFieldCategory(Type type)
        {
            return GetFieldCategoryInternal(type, FieldCategoryConfig.TreatDecimalAsCurrency, FieldCategoryConfig.TreatComplexObjectsAsJson);
        }

        /// <summary>
        /// Gets the field category for a given type with explicit options
        /// </summary>
        public static DbFieldCategory GetFieldCategory(Type type, bool treatDecimalAsCurrency, bool treatComplexAsJson = false)
        {
            return GetFieldCategoryInternal(type, treatDecimalAsCurrency, treatComplexAsJson);
        }

        private static DbFieldCategory GetFieldCategoryInternal(Type type, bool treatDecimalAsCurrency, bool treatComplexAsJson)
        {
            // Handle null type
            if (type == null)
                return DbFieldCategory.String;

            // Check custom mappings first
            if (FieldCategoryConfig.CustomTypeMappings.TryGetValue(type, out DbFieldCategory customCategory))
                return customCategory;

            // Handle nullable types - get underlying type
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            // Check custom mappings for underlying type as well
            if (underlyingType != type && FieldCategoryConfig.CustomTypeMappings.TryGetValue(underlyingType, out customCategory))
                return customCategory;

            // String types
            if (underlyingType == typeof(string) || underlyingType == typeof(char))
                return DbFieldCategory.String;
            
            // Numeric types - comprehensive list
            if (underlyingType == typeof(int) || underlyingType == typeof(long) || 
                underlyingType == typeof(short) || underlyingType == typeof(byte) || 
                underlyingType == typeof(uint) || underlyingType == typeof(ulong) || 
                underlyingType == typeof(ushort) || underlyingType == typeof(sbyte) ||
                underlyingType == typeof(float) || underlyingType == typeof(double))
                return DbFieldCategory.Numeric;
            
            // Decimal - can be treated as Currency or Numeric depending on context
            if (underlyingType == typeof(decimal))
                return treatDecimalAsCurrency ? DbFieldCategory.Currency : DbFieldCategory.Numeric;
            
            // Date and Time types
            if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset) ||
                underlyingType == typeof(DateOnly) || underlyingType == typeof(TimeOnly) ||
                underlyingType == typeof(TimeSpan))
                return DbFieldCategory.Date;
            
            // Boolean
            if (underlyingType == typeof(bool))
                return DbFieldCategory.Boolean;
            
            // Binary types
            if (underlyingType == typeof(byte[]) || underlyingType == typeof(Stream) ||
                underlyingType == typeof(System.IO.MemoryStream))
                return DbFieldCategory.Binary;
            
            // GUID
            if (underlyingType == typeof(Guid))
                return DbFieldCategory.Guid;
            
            // Enum types
            if (underlyingType.IsEnum)
                return DbFieldCategory.Enum;
            
            // JSON types - check type name and common JSON types
            if (underlyingType.Name.Contains("Json", StringComparison.OrdinalIgnoreCase) ||
                underlyingType == typeof(System.Text.Json.JsonDocument) ||
                underlyingType == typeof(System.Text.Json.JsonElement) ||
                underlyingType.FullName?.Contains("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase) == true)
                return DbFieldCategory.Json;
            
            // XML types
            if (underlyingType.Name.Contains("Xml", StringComparison.OrdinalIgnoreCase) ||
                underlyingType == typeof(System.Xml.XmlDocument) ||
                underlyingType == typeof(System.Xml.XmlElement) ||
                underlyingType == typeof(System.Xml.Linq.XDocument) ||
                underlyingType == typeof(System.Xml.Linq.XElement))
                return DbFieldCategory.Xml;

            // Check for collection types (arrays, lists, etc.)
            if (underlyingType.IsArray && underlyingType != typeof(byte[]))
            {
                // Arrays of types (other than byte[]) might be JSON or complex
                return treatComplexAsJson ? DbFieldCategory.Json : DbFieldCategory.String;
            }

            // Check for generic collection types
            if (underlyingType.IsGenericType)
            {
                Type genericTypeDef = underlyingType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(List<>) || 
                    genericTypeDef == typeof(IEnumerable<>) ||
                    genericTypeDef == typeof(ICollection<>) ||
                    genericTypeDef == typeof(IList<>))
                {
                    return treatComplexAsJson ? DbFieldCategory.Json : DbFieldCategory.String;
                }
            }

            // Check for known spatial types (if you work with geographic data)
            if (underlyingType.FullName?.Contains("Geography", StringComparison.OrdinalIgnoreCase) == true ||
                underlyingType.FullName?.Contains("Geometry", StringComparison.OrdinalIgnoreCase) == true ||
                underlyingType.FullName?.Contains("Spatial", StringComparison.OrdinalIgnoreCase) == true)
            {
                return DbFieldCategory.String; // or create a new category for spatial types
            }

            // Check for Uri type
            if (underlyingType == typeof(Uri))
                return DbFieldCategory.String;

            // Check for complex/reference types that should be serialized
            if (underlyingType.IsClass && !underlyingType.IsPrimitive)
            {
                // Complex objects are typically serialized to JSON or XML
                return treatComplexAsJson ? DbFieldCategory.Json : DbFieldCategory.String;
            }

            // Default fallback
            return DbFieldCategory.String;
        }

        /// <summary>
        /// Extension method to get field category directly from a Type
        /// </summary>
        public static DbFieldCategory ToFieldCategory(this Type type)
        {
            return GetFieldCategory(type);
        }

        /// <summary>
        /// Checks if a type is a numeric type
        /// </summary>
        public static bool IsNumericType(this Type type)
        {
            if (type == null) return false;
            
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            
            return underlyingType == typeof(byte) ||
                   underlyingType == typeof(sbyte) ||
                   underlyingType == typeof(short) ||
                   underlyingType == typeof(ushort) ||
                   underlyingType == typeof(int) ||
                   underlyingType == typeof(uint) ||
                   underlyingType == typeof(long) ||
                   underlyingType == typeof(ulong) ||
                   underlyingType == typeof(float) ||
                   underlyingType == typeof(double) ||
                   underlyingType == typeof(decimal);
        }

        /// <summary>
        /// Checks if a type is a date/time type
        /// </summary>
        public static bool IsDateTimeType(this Type type)
        {
            if (type == null) return false;
            
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            
            return underlyingType == typeof(DateTime) ||
                   underlyingType == typeof(DateTimeOffset) ||
                   underlyingType == typeof(DateOnly) ||
                   underlyingType == typeof(TimeOnly) ||
                   underlyingType == typeof(TimeSpan);
        }

        /// <summary>
        /// Checks if a type is a simple/primitive type (string, numeric, bool, date, guid)
        /// </summary>
        public static bool IsSimpleType(this Type type)
        {
            if (type == null) return false;
            
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            
            return underlyingType.IsPrimitive ||
                   underlyingType == typeof(string) ||
                   underlyingType == typeof(decimal) ||
                   underlyingType == typeof(DateTime) ||
                   underlyingType == typeof(DateTimeOffset) ||
                   underlyingType == typeof(DateOnly) ||
                   underlyingType == typeof(TimeOnly) ||
                   underlyingType == typeof(TimeSpan) ||
                   underlyingType == typeof(Guid) ||
                   underlyingType.IsEnum;
        }

        /// <summary>
        /// Gets a friendly display name for a database field type
        /// </summary>
        public static string GetFriendlyTypeName(this Type type)
        {
            if (type == null) return "Unknown";

            Type underlyingType = Nullable.GetUnderlyingType(type);
            bool isNullable = underlyingType != null;
            Type displayType = underlyingType ?? type;

            string friendlyName = displayType switch
            {
                Type t when t == typeof(int) => "Integer",
                Type t when t == typeof(long) => "Long Integer",
                Type t when t == typeof(short) => "Short Integer",
                Type t when t == typeof(byte) => "Byte",
                Type t when t == typeof(bool) => "Boolean",
                Type t when t == typeof(float) => "Float",
                Type t when t == typeof(double) => "Double",
                Type t when t == typeof(decimal) => "Decimal",
                Type t when t == typeof(string) => "Text",
                Type t when t == typeof(char) => "Character",
                Type t when t == typeof(DateTime) => "Date/Time",
                Type t when t == typeof(DateTimeOffset) => "Date/Time (Offset)",
                Type t when t == typeof(DateOnly) => "Date",
                Type t when t == typeof(TimeOnly) => "Time",
                Type t when t == typeof(TimeSpan) => "Time Span",
                Type t when t == typeof(Guid) => "GUID",
                Type t when t == typeof(byte[]) => "Binary",
                Type t when t.IsEnum => $"Enum ({t.Name})",
                _ => displayType.Name
            };

            return isNullable ? $"{friendlyName} (Nullable)" : friendlyName;
        }

        /// <summary>
        /// Gets a SQL-like type name for a .NET type
        /// </summary>
        public static string ToSqlTypeName(this Type type, int? maxLength = null)
        {
            if (type == null) return "VARCHAR(MAX)";

            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            string sqlType = underlyingType switch
            {
                Type t when t == typeof(int) => "INT",
                Type t when t == typeof(long) => "BIGINT",
                Type t when t == typeof(short) => "SMALLINT",
                Type t when t == typeof(byte) => "TINYINT",
                Type t when t == typeof(bool) => "BIT",
                Type t when t == typeof(float) => "REAL",
                Type t when t == typeof(double) => "FLOAT",
                Type t when t == typeof(decimal) => "DECIMAL(18,2)",
                Type t when t == typeof(string) => maxLength.HasValue && maxLength.Value > 0 
                    ? $"VARCHAR({maxLength.Value})" 
                    : "VARCHAR(MAX)",
                Type t when t == typeof(char) => "CHAR(1)",
                Type t when t == typeof(DateTime) => "DATETIME2",
                Type t when t == typeof(DateTimeOffset) => "DATETIMEOFFSET",
                Type t when t == typeof(DateOnly) => "DATE",
                Type t when t == typeof(TimeOnly) => "TIME",
                Type t when t == typeof(TimeSpan) => "TIME",
                Type t when t == typeof(Guid) => "UNIQUEIDENTIFIER",
                Type t when t == typeof(byte[]) => "VARBINARY(MAX)",
                Type t when t.IsEnum => "INT",
                _ => "VARCHAR(MAX)"
            };

            return sqlType;
        }

        #endregion
    }
}
