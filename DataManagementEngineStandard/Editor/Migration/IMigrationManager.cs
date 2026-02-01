using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW.Helpers;
using TheTechIdea.Beep.Editor.UOW.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Summary of migration status comparing Entity classes with database state.
    /// </summary>
    public class MigrationSummary
    {
        /// <summary>
        /// List of entity names that need to be created in the database.
        /// </summary>
        public List<string> EntitiesToCreate { get; set; } = new List<string>();
        
        /// <summary>
        /// List of entity names that need updates (missing columns, etc.).
        /// </summary>
        public List<string> EntitiesToUpdate { get; set; } = new List<string>();
        
        /// <summary>
        /// List of entity names that are up-to-date with their Entity classes.
        /// </summary>
        public List<string> EntitiesUpToDate { get; set; } = new List<string>();
        
        /// <summary>
        /// List of errors encountered during migration summary generation.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// Total count of entities that need migration.
        /// </summary>
        public int TotalPendingMigrations => EntitiesToCreate.Count + EntitiesToUpdate.Count;
        
        /// <summary>
        /// Indicates if there are any pending migrations.
        /// </summary>
        public bool HasPendingMigrations => TotalPendingMigrations > 0;
    }
    public interface IMigrationManager
    {
        IDMEEditor DMEEditor { get; }
        IDataSource MigrateDataSource { get; set; }

        // Entity-level operations
        IErrorsInfo EnsureEntity(EntityStructure entity, bool createIfMissing = true, bool addMissingColumns = true);
        IErrorsInfo EnsureEntity(Type pocoType, bool createIfMissing = true, bool addMissingColumns = true, bool detectRelationships = true);
        IReadOnlyList<EntityField> GetMissingColumns(EntityStructure current, EntityStructure desired);

        IErrorsInfo CreateEntity(EntityStructure entity);
        IErrorsInfo DropEntity(string entityName);
        IErrorsInfo TruncateEntity(string entityName);
        IErrorsInfo RenameEntity(string oldName, string newName);
        IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn);
        IErrorsInfo DropColumn(string entityName, string columnName);
        IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName);
        IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null);
        
        // Entity Framework-like migration discovery and application
        /// <summary>
        /// Discovers all types that inherit from Entity in the specified namespace(s).
        /// Similar to EF Core's DbContext discovery pattern.
        /// </summary>
        List<Type> DiscoverEntityTypes(string namespaceName = null, Assembly assembly = null, bool includeSubNamespaces = true);
        
        /// <summary>
        /// Discovers all types that inherit from Entity in all loaded assemblies.
        /// Scans all assemblies in AppDomain and DMEEditor's assembly handler.
        /// </summary>
        List<Type> DiscoverAllEntityTypes(bool includeSubNamespaces = true);
        
        /// <summary>
        /// Ensures database is created with all discovered Entity types.
        /// Similar to EF Core's Database.EnsureCreated().
        /// Creates entities for all classes that inherit from Entity.
        /// </summary>
        IErrorsInfo EnsureDatabaseCreated(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, IProgress<PassedArgs> progress = null);
        
        /// <summary>
        /// Applies migrations for all discovered Entity types.
        /// Compares Entity classes with database schema and applies changes.
        /// Similar to EF Core's Database.Migrate().
        /// </summary>
        IErrorsInfo ApplyMigrations(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true, bool addMissingColumns = true, IProgress<PassedArgs> progress = null);
        
        /// <summary>
        /// Gets migration summary comparing Entity classes with current database state.
        /// Returns list of entities that need creation or updates.
        /// </summary>
        MigrationSummary GetMigrationSummary(string namespaceName = null, Assembly assembly = null, bool detectRelationships = true);
    }
}
