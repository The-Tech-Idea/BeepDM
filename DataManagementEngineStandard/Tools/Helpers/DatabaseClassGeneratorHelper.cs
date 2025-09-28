using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools.Interfaces;
using TheTechIdea.Beep.Tools.Helpers;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools.Helpers
{
    /// <summary>
    /// Helper class for generating database-related classes
    /// </summary>
    public class DatabaseClassGeneratorHelper : IDatabaseClassGenerator
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;

        public DatabaseClassGeneratorHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        /// <summary>
        /// Generates a data access layer class for an entity
        /// </summary>
        public string GenerateDataAccessLayer(EntityStructure entity, string outputPath)
        {
            var className = $"{entity.EntityName}Repository";
            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = Path.Combine(outputPath, $"{className}.cs");

            var sb = new StringBuilder();
            
            // Add using statements
            sb.AppendLine(_helper.GenerateStandardUsings(
                "using System.Linq;",
                "using TheTechIdea.Beep.DataBase;",
                "using TheTechIdea.Beep.Editor;"
            ));
            sb.AppendLine();

            sb.AppendLine($"namespace {entity.EntityName}.DataAccess");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Data access layer for {entity.EntityName}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly IDataSource _dataSource;");
            sb.AppendLine();

            // Constructor
            sb.AppendLine($"        public {className}(IDataSource dataSource)");
            sb.AppendLine("        {");
            sb.AppendLine("            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate CRUD methods
            GenerateDataAccessMethods(sb, entity);

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var result = sb.ToString();
            _helper.WriteToFile(filePath, result, entity.EntityName);
            return filePath;
        }

        /// <summary>
        /// Generates an EF DbContext class for the given list of entities
        /// </summary>
        public string GenerateDbContext(List<EntityStructure> entities, string namespaceString, string outputPath)
        {
            var className = "ApplicationDbContext";
            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = Path.Combine(outputPath, $"{className}.cs");

            var sb = new StringBuilder();
            
            // Add using statements
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine(_helper.GenerateStandardUsings());
            sb.AppendLine();

            sb.AppendLine($"namespace {namespaceString}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Entity Framework DbContext for the application");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {className} : DbContext");
            sb.AppendLine("    {");
            
            // Constructor
            sb.AppendLine($"        public {className}(DbContextOptions<{className}> options) : base(options) {{ }}");
            sb.AppendLine();

            // DbSet properties
            foreach (var entity in entities)
            {
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// DbSet for {entity.EntityName} entities");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public DbSet<{entity.EntityName}> {entity.EntityName}s {{ get; set; }}");
                sb.AppendLine();
            }

            // OnModelCreating method
            sb.AppendLine("        protected override void OnModelCreating(ModelBuilder modelBuilder)");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnModelCreating(modelBuilder);");
            sb.AppendLine();
            
            foreach (var entity in entities)
            {
                sb.AppendLine($"            modelBuilder.Entity<{entity.EntityName}>(entity =>");
                sb.AppendLine("            {");
                sb.AppendLine($"                entity.ToTable(\"{entity.EntityName}\");");
                
                // Configure primary key if exists
                var primaryKeyField = entity.Fields.FirstOrDefault(f => f.IsKey);
                if (primaryKeyField != null)
                {
                    var safePropertyName = _helper.GenerateSafePropertyName(primaryKeyField.fieldname);
                    sb.AppendLine($"                entity.HasKey(e => e.{safePropertyName});");
                }
                
                sb.AppendLine("            });");
                sb.AppendLine();
            }
            
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var result = sb.ToString();
            _helper.WriteToFile(filePath, result, "DbContext");
            return filePath;
        }

        /// <summary>
        /// Generates EF Core configuration classes for the given entity
        /// </summary>
        public string GenerateEntityConfiguration(EntityStructure entity, string namespaceString, string outputPath)
        {
            var className = $"{entity.EntityName}Configuration";
            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = Path.Combine(outputPath, $"{className}.cs");

            var sb = new StringBuilder();
            
            // Add using statements
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
            sb.AppendLine(_helper.GenerateStandardUsings());
            sb.AppendLine();

            sb.AppendLine($"namespace {namespaceString}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Entity Framework configuration for {entity.EntityName}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {className} : IEntityTypeConfiguration<{entity.EntityName}>");
            sb.AppendLine("    {");
            sb.AppendLine($"        public void Configure(EntityTypeBuilder<{entity.EntityName}> builder)");
            sb.AppendLine("        {");
            sb.AppendLine($"            builder.ToTable(\"{entity.EntityName}\");");
            sb.AppendLine();

            // Configure properties
            foreach (var field in entity.Fields)
            {
                var safePropertyName = _helper.GenerateSafePropertyName(field.fieldname);
                sb.AppendLine($"            builder.Property(e => e.{safePropertyName})");
                sb.AppendLine($"                   .HasColumnName(\"{field.fieldname}\")");
                
                if (field.IsRequired)
                {
                    sb.AppendLine("                   .IsRequired()");
                }
                
                if (field.Size > 0 && _helper.IsReferenceType(field.fieldtype))
                {
                    sb.AppendLine($"                   .HasMaxLength({field.Size})");
                }
                
                sb.AppendLine("                   ;");
                sb.AppendLine();
            }

            // Configure primary key
            var primaryKeyField = entity.Fields.FirstOrDefault(f => f.IsKey);
            if (primaryKeyField != null)
            {
                var safePropertyName = _helper.GenerateSafePropertyName(primaryKeyField.fieldname);
                sb.AppendLine($"            builder.HasKey(e => e.{safePropertyName});");
                sb.AppendLine();
            }

            // Configure indexes
            var indexFields = entity.Fields.Where(f => f.IsUnique || f.IsIndexed).ToList();
            foreach (var field in indexFields)
            {
                var safePropertyName = _helper.GenerateSafePropertyName(field.fieldname);
                sb.AppendLine($"            builder.HasIndex(e => e.{safePropertyName})");
                
                if (field.IsUnique)
                {
                    sb.AppendLine("                   .IsUnique()");
                }
                
                sb.AppendLine($"                   .HasDatabaseName(\"IX_{entity.EntityName}_{field.fieldname}\");");
                sb.AppendLine();
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var result = sb.ToString();
            _helper.WriteToFile(filePath, result, entity.EntityName);
            return filePath;
        }

        /// <summary>
        /// Generates repository pattern implementation for an entity
        /// </summary>
        public string GenerateRepositoryImplementation(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectRepositories", bool interfaceOnly = false)
        {
            var repoInterfaceName = $"I{entity.EntityName}Repository";
            var repoClassName = $"{entity.EntityName}Repository";
            
            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = interfaceOnly 
                ? Path.Combine(outputPath, $"{repoInterfaceName}.cs")
                : Path.Combine(outputPath, $"{repoClassName}.cs");

            var sb = new StringBuilder();
            
            // Add using statements
            sb.AppendLine(_helper.GenerateStandardUsings(
                "using System.Threading.Tasks;",
                "using System.Linq;",
                "using TheTechIdea.Beep.DataBase;",
                "using TheTechIdea.Beep.Editor;"
            ));
            sb.AppendLine();

            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");

            // Generate interface
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Repository interface for {entity.EntityName}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public interface {repoInterfaceName}");
            sb.AppendLine("    {");
            GenerateRepositoryInterfaceMethods(sb, entity);
            sb.AppendLine("    }");

            if (!interfaceOnly)
            {
                sb.AppendLine();
                // Generate implementation
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Repository implementation for {entity.EntityName}");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    public class {repoClassName} : {repoInterfaceName}");
                sb.AppendLine("    {");
                GenerateRepositoryImplementation(sb, entity);
                sb.AppendLine("    }");
            }

            sb.AppendLine("}");

            var result = sb.ToString();
            _helper.WriteToFile(filePath, result, entity.EntityName);
            return filePath;
        }

        /// <summary>
        /// Generates Entity Framework Core migration code for entity
        /// </summary>
        public string GenerateEFCoreMigration(EntityStructure entity, string outputPath,
            string namespaceName = "TheTechIdea.ProjectMigrations")
        {
            var migrationName = $"Create{entity.EntityName}Table";
            var className = migrationName;
            
            outputPath = _helper.EnsureOutputDirectory(outputPath);
            var filePath = Path.Combine(outputPath, $"{className}.cs");

            var sb = new StringBuilder();
            
            // Add using statements
            sb.AppendLine("using Microsoft.EntityFrameworkCore.Migrations;");
            sb.AppendLine("using Microsoft.EntityFrameworkCore.Metadata;");
            sb.AppendLine(_helper.GenerateStandardUsings());
            sb.AppendLine();

            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Migration to create {entity.EntityName} table");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public partial class {className} : Migration");
            sb.AppendLine("    {");

            // Up method
            sb.AppendLine("        protected override void Up(MigrationBuilder migrationBuilder)");
            sb.AppendLine("        {");
            GenerateMigrationUpMethod(sb, entity);
            sb.AppendLine("        }");
            sb.AppendLine();

            // Down method
            sb.AppendLine("        protected override void Down(MigrationBuilder migrationBuilder)");
            sb.AppendLine("        {");
            sb.AppendLine($"            migrationBuilder.DropTable(name: \"{entity.EntityName}\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var result = sb.ToString();
            _helper.WriteToFile(filePath, result, entity.EntityName);
            return filePath;
        }

        #region Private Helper Methods

        /// <summary>
        /// Generates data access methods for repository
        /// </summary>
        private void GenerateDataAccessMethods(StringBuilder sb, EntityStructure entity)
        {
            // GetAll method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets all {entity.EntityName} records");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public IEnumerable<object> GetAll()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return _dataSource.GetEntity(\"{entity.EntityName}\", null);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // GetById method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets a {entity.EntityName} by ID");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public object GetById(int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            var filters = new List<AppFilter>");
            sb.AppendLine("            {");
            sb.AppendLine("                new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() }");
            sb.AppendLine("            };");
            sb.AppendLine($"            return _dataSource.GetEntity(\"{entity.EntityName}\", filters);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Add, Update, Delete methods
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Adds a new {entity.EntityName}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public void Add(object entity)");
            sb.AppendLine("        {");
            sb.AppendLine($"            _dataSource.InsertEntity(\"{entity.EntityName}\", entity);");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Updates a {entity.EntityName}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public void Update(object entity)");
            sb.AppendLine("        {");
            sb.AppendLine($"            _dataSource.UpdateEntity(\"{entity.EntityName}\", entity);");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Deletes a {entity.EntityName} by ID");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public void Delete(int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            var filters = new List<AppFilter>");
            sb.AppendLine("            {");
            sb.AppendLine("                new AppFilter { FieldName = \"Id\", Operator = \"=\", FilterValue = id.ToString() }");
            sb.AppendLine("            };");
            sb.AppendLine($"            _dataSource.DeleteEntity(\"{entity.EntityName}\", filters);");
            sb.AppendLine("        }");
        }

        /// <summary>
        /// Generates repository interface methods
        /// </summary>
        private void GenerateRepositoryInterfaceMethods(StringBuilder sb, EntityStructure entity)
        {
            sb.AppendLine($"        Task<IEnumerable<{entity.EntityName}>> GetAllAsync();");
            sb.AppendLine($"        Task<{entity.EntityName}> GetByIdAsync(int id);");
            sb.AppendLine($"        Task<{entity.EntityName}> AddAsync({entity.EntityName} entity);");
            sb.AppendLine($"        Task<bool> UpdateAsync({entity.EntityName} entity);");
            sb.AppendLine("        Task<bool> DeleteAsync(int id);");
        }

        /// <summary>
        /// Generates repository implementation
        /// </summary>
        private void GenerateRepositoryImplementation(StringBuilder sb, EntityStructure entity)
        {
            sb.AppendLine("        private readonly IDataSource _dataSource;");
            sb.AppendLine();
            sb.AppendLine($"        public {entity.EntityName}Repository(IDataSource dataSource)");
            sb.AppendLine("        {");
            sb.AppendLine("            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Implement interface methods
            sb.AppendLine($"        public async Task<IEnumerable<{entity.EntityName}>> GetAllAsync()");
            sb.AppendLine("        {");
            sb.AppendLine("            return await Task.Run(() => {");
            sb.AppendLine($"                var results = _dataSource.GetEntity(\"{entity.EntityName}\", null);");
            sb.AppendLine($"                return results as IEnumerable<{entity.EntityName}>;");
            sb.AppendLine("            });");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Additional async methods would follow similar pattern...
        }

        /// <summary>
        /// Generates migration Up method
        /// </summary>
        private void GenerateMigrationUpMethod(StringBuilder sb, EntityStructure entity)
        {
            sb.AppendLine($"            migrationBuilder.CreateTable(");
            sb.AppendLine($"                name: \"{entity.EntityName}\",");
            sb.AppendLine($"                columns: table => new");
            sb.AppendLine($"                {{");

            // Generate columns
            foreach (var field in entity.Fields)
            {
                var safePropertyName = _helper.GenerateSafePropertyName(field.fieldname);
                var sqlType = _helper.MapFieldTypeToSqlType(field.fieldtype);
                var nullable = field.IsRequired ? "false" : "true";

                sb.AppendLine($"                    {safePropertyName} = table.Column<{field.fieldtype}>(type: \"{sqlType}\", nullable: {nullable}),");
            }

            sb.AppendLine($"                }},");
            sb.AppendLine($"                constraints: table =>");
            sb.AppendLine($"                {{");

            // Primary key constraint
            var primaryKeyField = entity.Fields.FirstOrDefault(f => f.IsKey);
            if (primaryKeyField != null)
            {
                var safePropertyName = _helper.GenerateSafePropertyName(primaryKeyField.fieldname);
                sb.AppendLine($"                    table.PrimaryKey(\"PK_{entity.EntityName}\", x => x.{safePropertyName});");
            }

            sb.AppendLine($"                }});");

            // Create indexes
            var indexFields = entity.Fields.Where(f => f.IsUnique || f.IsIndexed).ToList();
            if (indexFields.Any())
            {
                sb.AppendLine();
                foreach (var field in indexFields)
                {
                    var safePropertyName = _helper.GenerateSafePropertyName(field.fieldname);
                    sb.AppendLine($"            migrationBuilder.CreateIndex(");
                    sb.AppendLine($"                name: \"IX_{entity.EntityName}_{field.fieldname}\",");
                    sb.AppendLine($"                table: \"{entity.EntityName}\",");
                    sb.AppendLine($"                column: \"{safePropertyName}\",");
                    sb.AppendLine($"                unique: {field.IsUnique.ToString().ToLower()});");
                }
            }
        }

        #endregion
    }
}