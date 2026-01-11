using System;
using System.Collections.Generic;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Tools.Helpers;
using TheTechIdea.Beep.Tools.Interfaces;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Partial class extending ClassCreator with POCO to Entity generation capabilities.
    /// Supports namespace scanning, runtime type loading, and datasource integration.
    /// </summary>
    public partial class ClassCreator
    {
        #region Private Fields

        private PocoToEntityGeneratorHelper _pocoToEntityHelper;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the POCO to Entity generator helper (lazy initialization)
        /// </summary>
        protected PocoToEntityGeneratorHelper PocoToEntityGenerator
        {
            get
            {
                if (_pocoToEntityHelper == null)
                {
                    _pocoToEntityHelper = new PocoToEntityGeneratorHelper(DMEEditor);
                }
                return _pocoToEntityHelper;
            }
        }

        #endregion

        #region Namespace Scanning

        /// <summary>
        /// Scans a namespace and discovers all POCO classes
        /// </summary>
        /// <param name="namespaceName">The namespace to scan</param>
        /// <param name="assembly">Optional specific assembly to search (searches all if null)</param>
        /// <returns>List of discovered POCO types</returns>
        public List<Type> ScanNamespaceForPocos(string namespaceName, Assembly assembly = null)
        {
            return PocoToEntityGenerator.ScanNamespaceForPocos(namespaceName, assembly);
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
            return PocoToEntityGenerator.FindClassByName(namespaceName, className, assembly);
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
            return PocoToEntityGenerator.ConvertPocoToEntity(pocoType, detectRelationships);
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
            return PocoToEntityGenerator.GenerateEntityClassFromPoco(pocoType, outputPath, namespaceString, generateFile);
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
            return PocoToEntityGenerator.GenerateEntityClassesFromNamespace(sourceNamespace, outputPath, 
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
            return PocoToEntityGenerator.CreateEntityTypeAtRuntime(entity, namespaceString);
        }

        /// <summary>
        /// Compiles entity code from POCO and returns the Type at runtime
        /// </summary>
        /// <param name="pocoType">The source POCO type</param>
        /// <param name="namespaceString">Target namespace for the type</param>
        /// <returns>Compiled Type or null on failure</returns>
        public Type CreateEntityTypeFromPocoAtRuntime(Type pocoType, string namespaceString = "TheTechIdea.ProjectClasses")
        {
            return PocoToEntityGenerator.CreateEntityTypeFromPocoAtRuntime(pocoType, namespaceString);
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
            return PocoToEntityGenerator.CreateEntityTypesFromNamespaceAtRuntime(sourceNamespace, targetNamespace, assembly);
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
            return PocoToEntityGenerator.CreateEntityTypesFromDataSourceAtRuntime(datasourceName, entityNames, targetNamespace);
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Clears the runtime type cache
        /// </summary>
        public void ClearPocoToEntityCache()
        {
            PocoToEntityGenerator.ClearCache();
        }

        #endregion
    }
}
