using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Entity class generation using EntitiesExtensions for type conversion
    /// </summary>
    public partial class ClassCreator
    {
        #region Type to EntityStructure Conversion Methods

        /// <summary>
        /// Creates an EntityStructure from a .NET Type using EntitiesExtensions
        /// </summary>
        /// <param name="type">The .NET type to convert</param>
        /// <param name="entityName">Optional entity name (defaults to type name)</param>
        /// <returns>EntityStructure populated from the type</returns>
        public EntityStructure CreateEntityStructureFromType(Type type, string entityName = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var entity = new EntityStructure();
            entity.FromType(type);
            
            if (!string.IsNullOrWhiteSpace(entityName))
            {
                entity.EntityName = entityName;
                entity.DatasourceEntityName = entityName;
                entity.Caption = entityName;
            }

            // Use DataTypesHelper to ensure proper field type mapping
            if (DMEEditor?.typesHelper != null)
            {
                foreach (var field in entity.Fields)
                {
                    // Convert .NET type to appropriate database type if needed
                    var netType = Type.GetType(field.fieldtype);
                    if (netType != null)
                    {
                        // Use DataTypesHelper to get proper field category
                        field.fieldCategory = EntitiesExtensions.GetFieldCategory(netType);
                    }
                }
            }

            return entity;
        }

        /// <summary>
        /// Creates an EntityStructure from a generic type using EntitiesExtensions
        /// </summary>
        /// <typeparam name="T">The type to convert</typeparam>
        /// <param name="entityName">Optional entity name (defaults to type name)</param>
        /// <returns>EntityStructure populated from the type</returns>
        public EntityStructure CreateEntityStructureFromType<T>(string entityName = null)
        {
            return CreateEntityStructureFromType(typeof(T), entityName);
        }

        /// <summary>
        /// Creates an EntityStructure from a DataTable using EntitiesExtensions
        /// </summary>
        /// <param name="dataTable">The DataTable to convert</param>
        /// <returns>EntityStructure populated from the DataTable</returns>
        public EntityStructure CreateEntityStructureFromDataTable(DataTable dataTable)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            var entity = new EntityStructure();
            entity.FromDataTable(dataTable);
            return entity;
        }

        /// <summary>
        /// Creates EntityStructure objects from a list of objects using EntitiesExtensions
        /// </summary>
        /// <typeparam name="T">The type of objects in the list</typeparam>
        /// <param name="list">The list of objects</param>
        /// <param name="entityName">Optional entity name</param>
        /// <returns>EntityStructure populated from the list</returns>
        public EntityStructure CreateEntityStructureFromList<T>(List<T> list, string entityName = null)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            var entity = new EntityStructure();
            entity.FromList(list);
            
            if (!string.IsNullOrWhiteSpace(entityName))
            {
                entity.EntityName = entityName;
                entity.DatasourceEntityName = entityName;
                entity.Caption = entityName;
            }

            return entity;
        }

        /// <summary>
        /// Creates multiple EntityStructure objects from multiple types
        /// </summary>
        /// <param name="types">The types to convert</param>
        /// <returns>List of EntityStructure objects</returns>
        public List<EntityStructure> CreateEntityStructuresFromTypes(params Type[] types)
        {
            if (types == null || types.Length == 0)
                return new List<EntityStructure>();

            return types.Select(type => CreateEntityStructureFromType(type)).ToList();
        }

        #endregion

        #region Enhanced Entity Class Generation with Type Conversion

        /// <summary>
        /// Generates an Entity class from a .NET Type that inherits from Entity base class
        /// Uses EntitiesExtensions to convert the type to EntityStructure first
        /// </summary>
        /// <param name="type">The .NET type to convert and generate class from</param>
        /// <param name="outputPath">Output directory for generated file</param>
        /// <param name="namespaceString">Namespace for generated class</param>
        /// <param name="entityName">Optional entity name (defaults to type name)</param>
        /// <param name="generateFiles">Whether to generate .cs files</param>
        /// <returns>Path to generated file</returns>
        public string CreateEntityClassFromType(Type type, string outputPath, 
            string namespaceString = "TheTechIdea.ProjectClasses", string entityName = null, bool generateFiles = true)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Convert type to EntityStructure using EntitiesExtensions
            var entity = CreateEntityStructureFromType(type, entityName);
            
            // Generate Entity class that inherits from Entity base class
            return CreateEntityClass(entity, 
                "using System;\nusing System.Collections.Generic;\nusing System.ComponentModel;\nusing System.Runtime.CompilerServices;\nusing TheTechIdea.Beep.Editor;", 
                "", 
                outputPath, 
                namespaceString, 
                generateFiles);
        }

        /// <summary>
        /// Generates an Entity class from a generic type that inherits from Entity base class
        /// </summary>
        /// <typeparam name="T">The type to convert and generate class from</typeparam>
        /// <param name="outputPath">Output directory for generated file</param>
        /// <param name="namespaceString">Namespace for generated class</param>
        /// <param name="entityName">Optional entity name</param>
        /// <param name="generateFiles">Whether to generate .cs files</param>
        /// <returns>Path to generated file</returns>
        public string CreateEntityClassFromType<T>(string outputPath, 
            string namespaceString = "TheTechIdea.ProjectClasses", string entityName = null, bool generateFiles = true)
        {
            return CreateEntityClassFromType(typeof(T), outputPath, namespaceString, entityName, generateFiles);
        }

        /// <summary>
        /// Generates Entity classes from multiple types that inherit from Entity base class
        /// </summary>
        /// <param name="outputPath">Output directory for generated files</param>
        /// <param name="namespaceString">Namespace for generated classes</param>
        /// <param name="generateFiles">Whether to generate .cs files</param>
        /// <param name="types">The types to convert and generate classes from</param>
        /// <returns>List of paths to generated files</returns>
        public List<string> CreateEntityClassesFromTypes(string outputPath, 
            string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true, params Type[] types)
        {
            if (types == null || types.Length == 0)
                return new List<string>();

            var filePaths = new List<string>();
            foreach (var type in types)
            {
                try
                {
                    var filePath = CreateEntityClassFromType(type, outputPath, namespaceString, null, generateFiles);
                    if (!string.IsNullOrEmpty(filePath))
                        filePaths.Add(filePath);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error generating Entity class from type {type.Name}: {ex.Message}", Errors.Failed);
                }
            }

            return filePaths;
        }

        /// <summary>
        /// Generates an Entity class from a DataTable that inherits from Entity base class
        /// </summary>
        /// <param name="dataTable">The DataTable to convert</param>
        /// <param name="outputPath">Output directory for generated file</param>
        /// <param name="namespaceString">Namespace for generated class</param>
        /// <param name="generateFiles">Whether to generate .cs files</param>
        /// <returns>Path to generated file</returns>
        public string CreateEntityClassFromDataTable(DataTable dataTable, string outputPath, 
            string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            // Convert DataTable to EntityStructure using EntitiesExtensions
            var entity = CreateEntityStructureFromDataTable(dataTable);
            
            // Generate Entity class that inherits from Entity base class
            return CreateEntityClass(entity, 
                "using System;\nusing System.Collections.Generic;\nusing System.ComponentModel;\nusing System.Runtime.CompilerServices;\nusing TheTechIdea.Beep.Editor;", 
                "", 
                outputPath, 
                namespaceString, 
                generateFiles);
        }

        /// <summary>
        /// Generates an Entity class from a list of objects that inherits from Entity base class
        /// </summary>
        /// <typeparam name="T">The type of objects in the list</typeparam>
        /// <param name="list">The list of objects</param>
        /// <param name="outputPath">Output directory for generated file</param>
        /// <param name="namespaceString">Namespace for generated class</param>
        /// <param name="entityName">Optional entity name</param>
        /// <param name="generateFiles">Whether to generate .cs files</param>
        /// <returns>Path to generated file</returns>
        public string CreateEntityClassFromList<T>(List<T> list, string outputPath, 
            string namespaceString = "TheTechIdea.ProjectClasses", string entityName = null, bool generateFiles = true)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            // Convert list to EntityStructure using EntitiesExtensions
            var entity = CreateEntityStructureFromList(list, entityName);
            
            // Generate Entity class that inherits from Entity base class
            return CreateEntityClass(entity, 
                "using System;\nusing System.Collections.Generic;\nusing System.ComponentModel;\nusing System.Runtime.CompilerServices;\nusing TheTechIdea.Beep.Editor;", 
                "", 
                outputPath, 
                namespaceString, 
                generateFiles);
        }

        #endregion

        #region Enhanced Entity Class Generation with DataType Mapping

        /// <summary>
        /// Generates an Entity class with proper data type mapping using DataTypesHelper
        /// </summary>
        /// <param name="entity">The EntityStructure to generate class from</param>
        /// <param name="dataSourceName">Data source name for type mapping</param>
        /// <param name="outputPath">Output directory for generated file</param>
        /// <param name="namespaceString">Namespace for generated class</param>
        /// <param name="usingHeader">Using statements</param>
        /// <param name="extraCode">Extra code to include</param>
        /// <param name="generateFiles">Whether to generate .cs files</param>
        /// <returns>Path to generated file</returns>
        public string CreateEntityClassWithTypeMapping(EntityStructure entity, string dataSourceName, 
            string outputPath, string namespaceString = "TheTechIdea.ProjectClasses", 
            string usingHeader = null, string extraCode = null, bool generateFiles = true)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (string.IsNullOrWhiteSpace(usingHeader))
            {
                usingHeader = "using System;\nusing System.Collections.Generic;\nusing System.ComponentModel;\nusing System.Runtime.CompilerServices;\nusing TheTechIdea.Beep.Editor;";
            }

            // Enhance entity fields with proper type mapping if DataTypesHelper is available
            if (DMEEditor?.typesHelper != null && !string.IsNullOrWhiteSpace(dataSourceName))
            {
                foreach (var field in entity.Fields)
                {
                    try
                    {
                        // Get proper .NET type from DataTypesHelper
                        var netType = DMEEditor.typesHelper.GetDataType(dataSourceName, field);
                        if (!string.IsNullOrEmpty(netType))
                        {
                            field.fieldtype = netType;
                        }

                        // Get field category if not set
                        if (field.fieldCategory == DbFieldCategory.String)
                        {
                            var type = Type.GetType(field.fieldtype);
                            if (type != null)
                            {
                                field.fieldCategory = EntitiesExtensions.GetFieldCategory(type);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Warning: Could not map type for field {field.fieldname}: {ex.Message}", Errors.Warning);
                    }
                }
            }

            // Generate Entity class that inherits from Entity base class
            return CreateEntityClass(entity, usingHeader, extraCode ?? "", outputPath, namespaceString, generateFiles);
        }

        /// <summary>
        /// Generates Entity classes from types with proper data type mapping
        /// </summary>
        /// <param name="dataSourceName">Data source name for type mapping</param>
        /// <param name="outputPath">Output directory for generated files</param>
        /// <param name="namespaceString">Namespace for generated classes</param>
        /// <param name="generateFiles">Whether to generate .cs files</param>
        /// <param name="types">The types to convert and generate classes from</param>
        /// <returns>List of paths to generated files</returns>
        public List<string> CreateEntityClassesFromTypesWithMapping(string dataSourceName, string outputPath, 
            string namespaceString = "TheTechIdea.ProjectClasses", bool generateFiles = true, params Type[] types)
        {
            if (types == null || types.Length == 0)
                return new List<string>();

            var filePaths = new List<string>();
            foreach (var type in types)
            {
                try
                {
                    // Convert type to EntityStructure
                    var entity = CreateEntityStructureFromType(type);
                    
                    // Generate with type mapping
                    var filePath = CreateEntityClassWithTypeMapping(entity, dataSourceName, outputPath, 
                        namespaceString, null, null, generateFiles);
                    
                    if (!string.IsNullOrEmpty(filePath))
                        filePaths.Add(filePath);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error generating Entity class from type {type.Name}: {ex.Message}", Errors.Failed);
                }
            }

            return filePaths;
        }

        #endregion
    }
}
