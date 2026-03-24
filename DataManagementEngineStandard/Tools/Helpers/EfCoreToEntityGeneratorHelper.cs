using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Roslyn;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools.Helpers
{
    /// <summary>
    /// Dedicated helper for EF Core decorated model conversion.
    /// Direction A: EF Core models -> EntityStructure / Beep Entity classes.
    /// Direction B: List&lt;EntityStructure&gt; -> EF Core C# classes / DLL.
    /// </summary>
    public class EfCoreToEntityGeneratorHelper
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _generation;

        public EfCoreToEntityGeneratorHelper(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _generation = new ClassGenerationHelper(dmeEditor);
        }

        #region EF -> EntityStructure (Type)

        public List<Type> ScanNamespaceForEfCoreClasses(string namespaceName, Assembly assembly = null)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));

            var assemblies = assembly != null ? new[] { assembly } : GetSearchableAssemblies();
            var result = new List<Type>();

            foreach (var asm in assemblies)
            {
                try
                {
                    var types = asm.GetTypes()
                        .Where(t => t != null
                                    && t.IsClass
                                    && !t.IsAbstract
                                    && t.Namespace != null
                                    && t.Namespace.Equals(namespaceName, StringComparison.OrdinalIgnoreCase)
                                    && IsEfDecoratedType(t));
                    result.AddRange(types);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    var types = ex.Types
                        .Where(t => t != null
                                    && t.IsClass
                                    && !t.IsAbstract
                                    && t.Namespace != null
                                    && t.Namespace.Equals(namespaceName, StringComparison.OrdinalIgnoreCase)
                                    && IsEfDecoratedType(t));
                    result.AddRange(types!);
                }
                catch
                {
                    // ignore problematic assemblies
                }
            }

            return result.Distinct().ToList();
        }

        public EntityStructure ConvertEfCoreTypeToEntityStructure(Type efCoreType, bool includeRelationships = true)
        {
            if (efCoreType == null) throw new ArgumentNullException(nameof(efCoreType));
            return BuildEntityFromEfType(efCoreType, includeRelationships);
        }

        public List<EntityStructure> ConvertEfCoreTypesToEntityStructures(IEnumerable<Type> efCoreTypes, bool includeRelationships = true)
        {
            if (efCoreTypes == null) return new List<EntityStructure>();
            return efCoreTypes
                .Where(t => t != null)
                .Select(t => ConvertEfCoreTypeToEntityStructure(t, includeRelationships))
                .ToList();
        }

        public List<EntityStructure> ConvertEfCoreNamespaceToEntityStructures(
            string namespaceName,
            Assembly assembly = null,
            bool includeRelationships = true)
        {
            var efTypes = ScanNamespaceForEfCoreClasses(namespaceName, assembly);
            return ConvertEfCoreTypesToEntityStructures(efTypes, includeRelationships);
        }

        #endregion

        #region EF Source/File/Directory -> EntityStructure

        public List<EntityStructure> ConvertEfCoreFileToEntityStructures(string filePath, bool includeRelationships = true)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("EF model file not found", filePath);

            var source = File.ReadAllText(filePath);
            return ConvertEfCoreSourceToEntityStructures(source, includeRelationships);
        }

        public List<EntityStructure> ConvertEfCoreSourceToEntityStructures(string sourceCode, bool includeRelationships = true)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
                return new List<EntityStructure>();

            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(c => c.Modifiers.Any(m => m.Text == "public"))
                .ToList();

            if (classes.Count == 0) return new List<EntityStructure>();

            var knownNames = new HashSet<string>(classes.Select(c => c.Identifier.Text), StringComparer.OrdinalIgnoreCase);
            var entities = new List<EntityStructure>();
            foreach (var c in classes)
            {
                if (!IsEfDecoratedClassSyntax(c)) continue;
                var e = BuildEntityFromClassSyntax(c, knownNames, includeRelationships);
                if (e != null && e.Fields.Count > 0)
                    entities.Add(e);
            }

            return entities;
        }

        public List<EntityStructure> ConvertEfCoreDirectoryToEntityStructures(
            string directoryPath,
            bool recursive = true,
            bool includeRelationships = true)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentNullException(nameof(directoryPath));
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException(directoryPath);

            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(directoryPath, "*.cs", option);
            var result = new List<EntityStructure>();

            foreach (var file in files)
            {
                result.AddRange(ConvertEfCoreFileToEntityStructures(file, includeRelationships));
            }

            return result;
        }

        #endregion

        #region EF -> Beep Entity classes

        public string GenerateEntityClassFromEfCore(
            Type efCoreType,
            string outputPath = null,
            string namespaceString = "TheTechIdea.ProjectClasses",
            bool generateFile = true,
            bool includeRelationships = true)
        {
            var entity = ConvertEfCoreTypeToEntityStructure(efCoreType, includeRelationships);
            var code = GenerateBeepEntityClassCode(entity, namespaceString);

            if (generateFile && !string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = _generation.EnsureOutputDirectory(outputPath);
                var filePath = Path.Combine(outputPath, $"{SanitizeClassName(entity.EntityName)}Entity.cs");
                _generation.WriteToFile(filePath, code, $"{entity.EntityName}Entity");
            }

            return code;
        }

        public List<string> GenerateEntityClassesFromEfCoreNamespace(
            string sourceNamespace,
            string outputPath = null,
            string targetNamespace = "TheTechIdea.ProjectClasses",
            bool generateFiles = true,
            Assembly assembly = null,
            bool includeRelationships = true)
        {
            var efTypes = ScanNamespaceForEfCoreClasses(sourceNamespace, assembly);
            var results = new List<string>();
            foreach (var type in efTypes)
            {
                results.Add(GenerateEntityClassFromEfCore(type, outputPath, targetNamespace, generateFiles, includeRelationships));
            }
            return results;
        }

        #endregion

        #region EntityStructure -> EF C# / DLL

        public List<string> GenerateEfCoreClassesFromEntityStructures(
            List<EntityStructure> entities,
            string outputPath,
            string namespaceName = "TheTechIdea.ProjectEfModels",
            bool generateFiles = true)
        {
            if (entities == null || entities.Count == 0) return new List<string>();

            outputPath = _generation.EnsureOutputDirectory(outputPath);
            var result = new List<string>();

            foreach (var entity in entities)
            {
                var code = GenerateEfCoreClassCode(entity, namespaceName);
                if (generateFiles)
                {
                    var filePath = Path.Combine(outputPath, $"{SanitizeClassName(entity.EntityName)}.cs");
                    if (_generation.WriteToFile(filePath, code, entity.EntityName))
                        result.Add(filePath);
                }
                else
                {
                    result.Add(code);
                }
            }

            return result;
        }

        public string GenerateEfCoreCombinedFileFromEntityStructures(
            List<EntityStructure> entities,
            string outputFilePath,
            string namespaceName = "TheTechIdea.ProjectEfModels")
        {
            if (entities == null || entities.Count == 0)
                throw new ArgumentException("Entities cannot be null or empty", nameof(entities));
            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentNullException(nameof(outputFilePath));

            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            foreach (var entity in entities)
            {
                sb.AppendLine(GenerateEfClassBodyOnly(entity));
                sb.AppendLine();
            }
            sb.AppendLine("}");

            _generation.WriteToFile(outputFilePath, sb.ToString(), "EF Combined Models");
            return outputFilePath;
        }

        public string GenerateEfCoreDllFromEntityStructures(
            List<EntityStructure> entities,
            string dllName,
            string outputPath,
            string namespaceName = "TheTechIdea.ProjectEfModels")
        {
            if (entities == null || entities.Count == 0)
                throw new ArgumentException("Entities cannot be null or empty", nameof(entities));
            if (string.IsNullOrWhiteSpace(dllName))
                throw new ArgumentNullException(nameof(dllName));

            outputPath = _generation.EnsureOutputDirectory(outputPath);
            var dllPath = Path.Combine(outputPath, dllName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? dllName : $"{dllName}.dll");

            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            foreach (var entity in entities)
            {
                sb.AppendLine(GenerateEfClassBodyOnly(entity));
                sb.AppendLine();
            }
            sb.AppendLine("}");

            var ok = RoslynCompiler.CompileClassFromStringToDLL(sb.ToString(), dllPath);
            if (!ok)
            {
                _generation.LogMessage("EfCoreToEntity", $"Failed to compile EF model DLL: {dllPath}", Errors.Failed);
                return string.Empty;
            }

            _generation.LogMessage("EfCoreToEntity", $"Generated EF model DLL: {dllPath}", Errors.Ok);
            return dllPath;
        }

        public string GenerateEfCoreClassCode(EntityStructure entity, string namespaceName = "TheTechIdea.ProjectEfModels")
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine(GenerateEfClassBodyOnly(entity));
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string GenerateEfClassBodyOnly(EntityStructure entity)
        {
            var sb = new StringBuilder();
            var className = SanitizeClassName(entity?.EntityName);

            if (!string.IsNullOrWhiteSpace(entity?.EntityName))
            {
                if (!string.IsNullOrWhiteSpace(entity.SchemaOrOwnerOrDatabase))
                    sb.AppendLine($"    [Table(\"{entity.EntityName}\", Schema = \"{entity.SchemaOrOwnerOrDatabase}\")]");
                else
                    sb.AppendLine($"    [Table(\"{entity.EntityName}\")]");
            }

            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            foreach (var field in entity?.Fields ?? new List<EntityField>())
            {
                var propType = MapEntityFieldToClrType(field);
                var propName = _generation.GenerateSafePropertyName(field.FieldName, field.FieldIndex);

                foreach (var ann in BuildEfFieldAttributes(field, propType))
                    sb.AppendLine($"        {ann}");

                sb.AppendLine($"        public {propType} {propName} {{ get; set; }}");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            return sb.ToString();
        }

        #endregion

        #region Internal mapping/parsing helpers

        private bool IsEfDecoratedType(Type type)
        {
            if (type == null || !type.IsClass || type.IsAbstract) return false;
            if (type.GetCustomAttribute<TableAttribute>(inherit: false) != null) return true;
            foreach (var p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (p.GetCustomAttribute<ColumnAttribute>(false) != null) return true;
                if (p.GetCustomAttribute<KeyAttribute>(false) != null) return true;
                if (p.GetCustomAttribute<RequiredAttribute>(false) != null) return true;
                if (p.GetCustomAttribute<MaxLengthAttribute>(false) != null) return true;
                if (p.GetCustomAttribute<StringLengthAttribute>(false) != null) return true;
                if (p.GetCustomAttribute<DatabaseGeneratedAttribute>(false) != null) return true;
                if (p.GetCustomAttribute<NotMappedAttribute>(false) != null) return true;
                if (p.GetCustomAttribute<ForeignKeyAttribute>(false) != null) return true;
            }
            return false;
        }

        private EntityStructure BuildEntityFromEfType(Type efType, bool includeRelationships)
        {
            var tableAttr = efType.GetCustomAttribute<TableAttribute>(inherit: false);
            var entity = new EntityStructure
            {
                EntityName = tableAttr?.Name ?? efType.Name,
                DatasourceEntityName = tableAttr?.Name ?? efType.Name,
                OriginalEntityName = efType.Name,
                Caption = efType.Name,
                SchemaOrOwnerOrDatabase = tableAttr?.Schema ?? string.Empty,
                HasDataAnnotations = tableAttr != null,
                Fields = new List<EntityField>(),
                PrimaryKeys = new List<EntityField>(),
                Relations = new List<RelationShipKeys>()
            };

            var props = efType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite)
                .ToList();

            var fieldIndex = 0;
            foreach (var prop in props)
            {
                var notMapped = prop.GetCustomAttribute<NotMappedAttribute>(false) != null;
                var navigation = IsNavigationType(prop.PropertyType);
                if (navigation)
                {
                    if (includeRelationships)
                        TryAddRelationshipFromNavigation(entity, prop);
                    continue;
                }
                if (notMapped)
                {
                    entity.HasDataAnnotations = true;
                    continue;
                }

                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>(false);
                var keyAttr = prop.GetCustomAttribute<KeyAttribute>(false);
                var reqAttr = prop.GetCustomAttribute<RequiredAttribute>(false);
                var maxAttr = prop.GetCustomAttribute<MaxLengthAttribute>(false);
                var strAttr = prop.GetCustomAttribute<StringLengthAttribute>(false);
                var dbGenAttr = prop.GetCustomAttribute<DatabaseGeneratedAttribute>(false);

                var field = new EntityField
                {
                    EntityName = entity.EntityName,
                    FieldName = prop.Name,
                    Originalfieldname = prop.Name,
                    FieldIndex = fieldIndex++,
                    Fieldtype = NormalizeTypeName(prop.PropertyType.FullName ?? prop.PropertyType.Name),
                    AllowDBNull = IsNullableReflectionProperty(prop),
                    IsKey = keyAttr != null,
                    IsRequired = reqAttr != null,
                    ColumnName = columnAttr?.Name ?? string.Empty,
                    ColumnTypeName = columnAttr?.TypeName ?? string.Empty,
                    DatabaseGeneratedOptionName = dbGenAttr?.DatabaseGeneratedOption.ToString() ?? string.Empty,
                    IsAutoIncrement = dbGenAttr?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity,
                    Size1 = maxAttr?.Length ?? strAttr?.MaximumLength ?? 0,
                    MaxLength = maxAttr?.Length ?? strAttr?.MaximumLength ?? 0,
                    ValueMin = strAttr?.MinimumLength ?? 0,
                    IsNotMapped = false
                };

                if (field.IsRequired)
                    field.AllowDBNull = false;

                if (field.IsKey || field.IsRequired || field.IsAutoIncrement ||
                    !string.IsNullOrWhiteSpace(field.ColumnName) || !string.IsNullOrWhiteSpace(field.ColumnTypeName) ||
                    field.Size1 > 0 || field.MaxLength > 0 || field.ValueMin > 0)
                {
                    entity.HasDataAnnotations = true;
                }

                entity.Fields.Add(field);
                if (field.IsKey)
                    entity.PrimaryKeys.Add(field);

                var fkAttr = prop.GetCustomAttribute<ForeignKeyAttribute>(false);
                if (fkAttr != null && !string.IsNullOrWhiteSpace(fkAttr.Name))
                {
                    var navProp = props.FirstOrDefault(p => p.Name.Equals(fkAttr.Name, StringComparison.OrdinalIgnoreCase));
                    if (navProp != null && IsNavigationType(navProp.PropertyType))
                    {
                        var related = GetRelatedType(navProp.PropertyType);
                        if (related != null)
                        {
                            entity.Relations.Add(new RelationShipKeys
                            {
                                EntityColumnID = prop.Name,
                                RelatedEntityID = related.Name,
                                RelatedEntityColumnID = "Id",
                                RalationName = navProp.Name
                            });
                            entity.HasDataAnnotations = true;
                        }
                    }
                }
            }

            if (entity.PrimaryKeys.Count == 0)
            {
                var conventional = entity.Fields.FirstOrDefault(f =>
                    string.Equals(f.FieldName, "Id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.FieldName, $"{efType.Name}Id", StringComparison.OrdinalIgnoreCase));
                if (conventional != null)
                {
                    conventional.IsKey = true;
                    if (conventional.Fieldtype == typeof(int).FullName || conventional.Fieldtype == typeof(long).FullName)
                        conventional.IsAutoIncrement = true;
                    entity.PrimaryKeys.Add(conventional);
                }
            }

            return entity;
        }

        private static bool IsEfDecoratedClassSyntax(ClassDeclarationSyntax classNode)
        {
            bool HasAttr(string name, IEnumerable<AttributeListSyntax> lists)
                => lists.SelectMany(a => a.Attributes).Any(a =>
                {
                    var n = a.Name.ToString();
                    return n.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                           n.Equals($"{name}Attribute", StringComparison.OrdinalIgnoreCase) ||
                           n.EndsWith($".{name}", StringComparison.OrdinalIgnoreCase) ||
                           n.EndsWith($".{name}Attribute", StringComparison.OrdinalIgnoreCase);
                });

            if (HasAttr("Table", classNode.AttributeLists)) return true;
            foreach (var p in classNode.Members.OfType<PropertyDeclarationSyntax>())
            {
                if (HasAttr("Column", p.AttributeLists)) return true;
                if (HasAttr("Key", p.AttributeLists)) return true;
                if (HasAttr("Required", p.AttributeLists)) return true;
                if (HasAttr("MaxLength", p.AttributeLists)) return true;
                if (HasAttr("StringLength", p.AttributeLists)) return true;
                if (HasAttr("DatabaseGenerated", p.AttributeLists)) return true;
                if (HasAttr("NotMapped", p.AttributeLists)) return true;
                if (HasAttr("ForeignKey", p.AttributeLists)) return true;
            }
            return false;
        }

        private EntityStructure BuildEntityFromClassSyntax(
            ClassDeclarationSyntax classNode,
            HashSet<string> knownEntityNames,
            bool includeRelationships)
        {
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

            foreach (var classAttr in classNode.AttributeLists.SelectMany(a => a.Attributes))
            {
                var attrName = classAttr.Name.ToString();
                if (attrName.EndsWith("Table", StringComparison.OrdinalIgnoreCase) ||
                    attrName.EndsWith("TableAttribute", StringComparison.OrdinalIgnoreCase))
                {
                    if (classAttr.ArgumentList?.Arguments.Count > 0)
                    {
                        var tableName = ExtractAttributeArgumentValue(classAttr.ArgumentList.Arguments[0].Expression);
                        if (!string.IsNullOrWhiteSpace(tableName))
                        {
                            entity.EntityName = tableName;
                            entity.DatasourceEntityName = tableName;
                        }
                    }
                    foreach (var arg in classAttr.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())
                    {
                        if (arg.NameEquals?.Name.Identifier.Text == "Schema")
                            entity.SchemaOrOwnerOrDatabase = ExtractAttributeArgumentValue(arg.Expression);
                    }
                    entity.HasDataAnnotations = true;
                }
            }

            var props = classNode.Members.OfType<PropertyDeclarationSyntax>().ToList();
            var propertyNames = new HashSet<string>(props.Select(p => p.Identifier.Text), StringComparer.OrdinalIgnoreCase);
            var fieldIndex = 0;
            foreach (var prop in props)
            {
                if (prop.AccessorList == null || prop.AccessorList.Accessors.Count == 0)
                    continue;

                if (IsLikelyNavigationProperty(prop, propertyNames, knownEntityNames))
                {
                    if (includeRelationships)
                        TryAddRelationshipFromNavigation(entity, prop);
                    continue;
                }

                var field = new EntityField
                {
                    FieldName = prop.Identifier.Text,
                    Originalfieldname = prop.Identifier.Text,
                    Fieldtype = NormalizeTypeName(prop.Type.ToString()),
                    FieldIndex = fieldIndex++,
                    AllowDBNull = IsNullableTypeSyntax(prop.Type),
                    EntityName = entity.EntityName,
                    ColumnName = string.Empty,
                    ColumnTypeName = string.Empty,
                    DatabaseGeneratedOptionName = string.Empty
                };

                bool notMapped = false;
                foreach (var attr in prop.AttributeLists.SelectMany(a => a.Attributes))
                {
                    var name = attr.Name.ToString();
                    if (EndsWithAny(name, "Key", "KeyAttribute"))
                    {
                        field.IsKey = true;
                        entity.PrimaryKeys.Add(field);
                        entity.HasDataAnnotations = true;
                    }
                    else if (EndsWithAny(name, "Required", "RequiredAttribute"))
                    {
                        field.IsRequired = true;
                        field.AllowDBNull = false;
                        entity.HasDataAnnotations = true;
                    }
                    else if (EndsWithAny(name, "MaxLength", "MaxLengthAttribute"))
                    {
                        if (TryGetIntArg(attr, out var len))
                        {
                            field.Size1 = len;
                            field.MaxLength = len;
                        }
                        entity.HasDataAnnotations = true;
                    }
                    else if (EndsWithAny(name, "StringLength", "StringLengthAttribute"))
                    {
                        if (TryGetIntArg(attr, out var len))
                        {
                            field.Size1 = len;
                            field.MaxLength = len;
                        }
                        foreach (var arg in attr.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())
                        {
                            if (arg.NameEquals?.Name.Identifier.Text == "MinimumLength" && int.TryParse(arg.Expression.ToString(), out var min))
                                field.ValueMin = min;
                        }
                        entity.HasDataAnnotations = true;
                    }
                    else if (EndsWithAny(name, "Column", "ColumnAttribute"))
                    {
                        if (attr.ArgumentList?.Arguments.Count > 0)
                            field.ColumnName = ExtractAttributeArgumentValue(attr.ArgumentList.Arguments[0].Expression);
                        foreach (var arg in attr.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())
                        {
                            if (arg.NameEquals?.Name.Identifier.Text == "TypeName")
                                field.ColumnTypeName = ExtractAttributeArgumentValue(arg.Expression);
                        }
                        entity.HasDataAnnotations = true;
                    }
                    else if (EndsWithAny(name, "DatabaseGenerated", "DatabaseGeneratedAttribute"))
                    {
                        var token = attr.ArgumentList?.Arguments.FirstOrDefault()?.ToString() ?? string.Empty;
                        if (token.Contains("Identity", StringComparison.OrdinalIgnoreCase))
                        {
                            field.DatabaseGeneratedOptionName = "Identity";
                            field.IsAutoIncrement = true;
                        }
                        else if (token.Contains("Computed", StringComparison.OrdinalIgnoreCase))
                            field.DatabaseGeneratedOptionName = "Computed";
                        else if (token.Contains("None", StringComparison.OrdinalIgnoreCase))
                            field.DatabaseGeneratedOptionName = "None";
                        entity.HasDataAnnotations = true;
                    }
                    else if (EndsWithAny(name, "NotMapped", "NotMappedAttribute"))
                    {
                        notMapped = true;
                        entity.HasDataAnnotations = true;
                    }
                }

                if (!notMapped)
                    entity.Fields.Add(field);
            }

            if (entity.PrimaryKeys.Count == 0)
            {
                var conventional = entity.Fields.FirstOrDefault(f =>
                    string.Equals(f.FieldName, "Id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.FieldName, $"{classNode.Identifier.Text}Id", StringComparison.OrdinalIgnoreCase));
                if (conventional != null)
                {
                    conventional.IsKey = true;
                    entity.PrimaryKeys.Add(conventional);
                }
            }

            return entity;
        }

        private static bool EndsWithAny(string value, params string[] suffixes)
            => suffixes.Any(s => value.EndsWith(s, StringComparison.OrdinalIgnoreCase));

        private static bool TryGetIntArg(AttributeSyntax attr, out int value)
        {
            value = 0;
            var first = attr.ArgumentList?.Arguments.FirstOrDefault();
            return first != null && int.TryParse(first.ToString(), out value);
        }

        private static string ExtractAttributeArgumentValue(ExpressionSyntax expression)
        {
            if (expression is LiteralExpressionSyntax literal &&
                literal.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression)
                return literal.Token.ValueText;

            var raw = expression?.ToString() ?? string.Empty;
            return raw.Trim().Trim('"');
        }

        private static bool IsNullableTypeSyntax(TypeSyntax typeSyntax)
        {
            if (typeSyntax is NullableTypeSyntax) return true;
            var text = typeSyntax.ToString();
            return text.EndsWith("?") || text.Equals("string", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLikelyNavigationProperty(
            PropertyDeclarationSyntax prop,
            HashSet<string> propertyNames,
            HashSet<string> knownEntityNames)
        {
            var t = prop.Type.ToString();
            if (t.Contains("ICollection<", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("IEnumerable<", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("List<", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("HashSet<", StringComparison.OrdinalIgnoreCase))
                return true;

            if (prop.Modifiers.Any(m => m.Text.Equals("virtual", StringComparison.OrdinalIgnoreCase)) && !IsKnownScalarTypeSyntax(prop.Type))
                return true;

            var related = ExtractRelatedEntityTypeName(prop.Type);
            if (!string.IsNullOrWhiteSpace(related))
            {
                if (knownEntityNames.Contains(related)) return true;
                if (propertyNames.Contains($"{related}Id")) return true;
                if (propertyNames.Contains($"{prop.Identifier.Text}Id")) return true;
            }

            return false;
        }

        private static string ExtractRelatedEntityTypeName(TypeSyntax typeSyntax)
        {
            var text = typeSyntax.ToString().Replace("global::", string.Empty).Trim().TrimEnd('?');
            if (text.Contains("<") && text.EndsWith(">"))
            {
                var start = text.IndexOf('<');
                var inner = text.Substring(start + 1, text.Length - start - 2).Trim();
                if (!inner.Contains(",")) text = inner;
            }
            if (text.Contains(".")) text = text.Split('.').Last();
            return text;
        }

        private static bool IsKnownScalarTypeSyntax(TypeSyntax typeSyntax)
        {
            var t = typeSyntax.ToString().Replace("global::", string.Empty).Trim().TrimEnd('?');
            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "int","long","short","byte","bool","decimal","double","float","DateTime","DateTimeOffset","TimeSpan","Guid","string","char",
                "System.Int32","System.Int64","System.Int16","System.Byte","System.Boolean","System.Decimal","System.Double","System.Single",
                "System.DateTime","System.DateTimeOffset","System.TimeSpan","System.Guid","System.String","byte[]"
            };
            return known.Contains(t);
        }

        private static bool IsNullableReflectionProperty(PropertyInfo prop)
        {
            if (Nullable.GetUnderlyingType(prop.PropertyType) != null) return true;
            if (prop.PropertyType == typeof(string)) return prop.GetCustomAttribute<RequiredAttribute>(false) == null;
            return !prop.PropertyType.IsValueType;
        }

        private static bool IsNavigationType(Type type)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;
            if (t.IsPrimitive || t.IsEnum) return false;
            if (t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(DateTimeOffset) ||
                t == typeof(TimeSpan) || t == typeof(Guid) || t == typeof(byte[]))
                return false;
            if (t != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(t))
                return true;
            return t.IsClass;
        }

        private static Type GetRelatedType(Type type)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;
            if (t == typeof(string)) return null;
            if (t.IsGenericType)
            {
                var args = t.GetGenericArguments();
                if (args.Length == 1) return args[0];
            }
            return t;
        }

        private static string NormalizeTypeName(string raw)
        {
            var text = (raw ?? "string").Replace("global::", string.Empty).Trim().TrimEnd('?');
            if (text.StartsWith("Nullable<", StringComparison.OrdinalIgnoreCase) && text.EndsWith(">"))
                text = text.Substring("Nullable<".Length, text.Length - "Nullable<".Length - 1).Trim();

            return text.ToLowerInvariant() switch
            {
                "int" => typeof(int).FullName,
                "long" => typeof(long).FullName,
                "short" => typeof(short).FullName,
                "byte" => typeof(byte).FullName,
                "bool" => typeof(bool).FullName,
                "decimal" => typeof(decimal).FullName,
                "double" => typeof(double).FullName,
                "float" => typeof(float).FullName,
                "datetime" => typeof(DateTime).FullName,
                "datetimeoffset" => typeof(DateTimeOffset).FullName,
                "timespan" => typeof(TimeSpan).FullName,
                "guid" => typeof(Guid).FullName,
                "string" => typeof(string).FullName,
                "byte[]" => typeof(byte[]).FullName,
                _ => text
            };
        }

        private static void TryAddRelationshipFromNavigation(EntityStructure entity, PropertyDeclarationSyntax navProp)
        {
            var related = ExtractRelatedEntityTypeName(navProp.Type);
            if (string.IsNullOrWhiteSpace(related)) return;

            var fkField = $"{related}Id";
            foreach (var attr in navProp.AttributeLists.SelectMany(a => a.Attributes))
            {
                var name = attr.Name.ToString();
                if (EndsWithAny(name, "ForeignKey", "ForeignKeyAttribute"))
                {
                    var arg = attr.ArgumentList?.Arguments.FirstOrDefault();
                    if (arg != null)
                    {
                        var val = ExtractAttributeArgumentValue(arg.Expression);
                        if (!string.IsNullOrWhiteSpace(val))
                            fkField = val;
                    }
                }
            }

            entity.Relations.Add(new RelationShipKeys
            {
                EntityColumnID = fkField,
                RelatedEntityID = related,
                RelatedEntityColumnID = "Id",
                RalationName = navProp.Identifier.Text
            });
        }

        private static void TryAddRelationshipFromNavigation(EntityStructure entity, PropertyInfo navProp)
        {
            var related = GetRelatedType(navProp.PropertyType);
            if (related == null) return;

            var fkField = $"{related.Name}Id";
            var fkAttr = navProp.GetCustomAttribute<ForeignKeyAttribute>(false);
            if (!string.IsNullOrWhiteSpace(fkAttr?.Name))
                fkField = fkAttr.Name;

            entity.Relations.Add(new RelationShipKeys
            {
                EntityColumnID = fkField,
                RelatedEntityID = related.Name,
                RelatedEntityColumnID = "Id",
                RalationName = navProp.Name
            });
            if (fkAttr != null)
                entity.HasDataAnnotations = true;
        }

        private IEnumerable<string> BuildEfFieldAttributes(EntityField field, string clrType)
        {
            if (field.IsKey) yield return "[Key]";
            if (field.IsRequired || (!field.AllowDBNull && clrType == "string")) yield return "[Required]";

            if (!string.IsNullOrWhiteSpace(field.ColumnName) || !string.IsNullOrWhiteSpace(field.ColumnTypeName))
            {
                if (!string.IsNullOrWhiteSpace(field.ColumnName) && !string.IsNullOrWhiteSpace(field.ColumnTypeName))
                    yield return $"[Column(\"{field.ColumnName}\", TypeName = \"{field.ColumnTypeName}\")]";
                else if (!string.IsNullOrWhiteSpace(field.ColumnName))
                    yield return $"[Column(\"{field.ColumnName}\")]";
                else
                    yield return $"[Column(TypeName = \"{field.ColumnTypeName}\")]";
            }

            if (field.ValueMin > 0 && field.MaxLength > 0)
                yield return $"[StringLength({field.MaxLength}, MinimumLength = {field.ValueMin})]";
            else if (field.MaxLength > 0)
                yield return $"[MaxLength({field.MaxLength})]";
            else if (field.Size1 > 0 && clrType == "string")
                yield return $"[StringLength({field.Size1})]";

            if (!string.IsNullOrWhiteSpace(field.DatabaseGeneratedOptionName))
                yield return $"[DatabaseGenerated(DatabaseGeneratedOption.{field.DatabaseGeneratedOptionName})]";
            else if (field.IsAutoIncrement)
                yield return "[DatabaseGenerated(DatabaseGeneratedOption.Identity)]";

            if (field.IsNotMapped)
                yield return "[NotMapped]";
        }

        private static string SanitizeClassName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "EfEntity";
            var valid = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            if (string.IsNullOrWhiteSpace(valid)) valid = "EfEntity";
            if (char.IsDigit(valid[0])) valid = $"E{valid}";
            return valid;
        }

        private static string MapEntityFieldToClrType(EntityField field)
        {
            var dbType = field?.Fieldtype ?? typeof(string).FullName;
            var nullable = field?.AllowDBNull ?? true;
            var normalized = dbType.Replace("global::", string.Empty).Trim();

            return normalized switch
            {
                "System.String" or "string" => "string",
                "System.Int32" or "int" => nullable ? "int?" : "int",
                "System.Int64" or "long" => nullable ? "long?" : "long",
                "System.Int16" or "short" => nullable ? "short?" : "short",
                "System.Byte" or "byte" => nullable ? "byte?" : "byte",
                "System.Boolean" or "bool" => nullable ? "bool?" : "bool",
                "System.Decimal" or "decimal" => nullable ? "decimal?" : "decimal",
                "System.Double" or "double" => nullable ? "double?" : "double",
                "System.Single" or "float" => nullable ? "float?" : "float",
                "System.DateTime" or "DateTime" => nullable ? "DateTime?" : "DateTime",
                "System.DateTimeOffset" or "DateTimeOffset" => nullable ? "DateTimeOffset?" : "DateTimeOffset",
                "System.TimeSpan" or "TimeSpan" => nullable ? "TimeSpan?" : "TimeSpan",
                "System.Guid" or "Guid" => nullable ? "Guid?" : "Guid",
                "System.Byte[]" or "byte[]" => "byte[]",
                _ => nullable ? $"{normalized}?" : normalized
            };
        }

        private string GenerateBeepEntityClassCode(EntityStructure entity, string namespaceString)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine("using TheTechIdea.Beep.Editor;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceString}");
            sb.AppendLine("{");
            sb.AppendLine(GenerateBeepEntityClassBodyOnly(entity));
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string GenerateBeepEntityClassBodyOnly(EntityStructure entity)
        {
            var sb = new StringBuilder();
            var className = SanitizeClassName(entity?.EntityName);

            if (!string.IsNullOrWhiteSpace(entity?.EntityName))
            {
                if (!string.IsNullOrWhiteSpace(entity.SchemaOrOwnerOrDatabase))
                    sb.AppendLine($"    [Table(\"{entity.EntityName}\", Schema = \"{entity.SchemaOrOwnerOrDatabase}\")]");
                else
                    sb.AppendLine($"    [Table(\"{entity.EntityName}\")]");
            }

            sb.AppendLine($"    public class {className}Entity : Entity");
            sb.AppendLine("    {");

            foreach (var field in entity?.Fields ?? new List<EntityField>())
            {
                var clrType = MapEntityFieldToClrType(field);
                var propName = _generation.GenerateSafePropertyName(field.FieldName, field.FieldIndex);
                var backing = $"_{char.ToLowerInvariant(propName[0])}{(propName.Length > 1 ? propName.Substring(1) : string.Empty)}";

                foreach (var ann in BuildEfFieldAttributes(field, clrType))
                    sb.AppendLine($"        {ann}");

                sb.AppendLine($"        private {clrType} {backing};");
                sb.AppendLine($"        public {clrType} {propName}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => {backing};");
                sb.AppendLine($"            set => SetProperty(ref {backing}, value);");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            return sb.ToString();
        }

        private IEnumerable<Assembly> GetSearchableAssemblies()
        {
            var assemblies = new List<Assembly>();
            assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic));
            if (_dmeEditor?.assemblyHandler?.Assemblies != null)
            {
                assemblies.AddRange(_dmeEditor.assemblyHandler.Assemblies
                    .Select(a => a.DllLib)
                    .Where(a => a != null));
            }
            return assemblies.Distinct();
        }

        #endregion
    }
}

