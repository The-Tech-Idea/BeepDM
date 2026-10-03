using System;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Roslyn;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Builds — and caches — a concrete <see cref="Entity"/>-derived runtime type
    /// for an <see cref="EntityStructure"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generates Entity-derived types for datasource GetEntityType consumers.
    /// Captures metadata and keys by generated source identity, not entity name alone.
    /// </para>
    /// <para>
    /// <c>UnitofWork&lt;T&gt;</c> is constrained to
    /// <c>T : Entity, new()</c> and <c>UnitOfWorkFactory</c> reaches it through
    /// <c>MakeGenericType</c>. A type that does not derive from <c>Entity</c>
    /// cannot back a unit of work, so a block over such a datasource registers
    /// and then holds no records — no query, no navigation, no master-detail.
    /// </para>
    /// <para>
    /// The generated class comes from <see cref="ClassCreator.CreateEntityClass"/>,
    /// the engine's existing POCO generator, so there is one definition of what a
    /// generated entity looks like.
    /// </para>
    /// </remarks>
    public static class EntityTypeFactory
    {
        private readonly record struct TypeIdentity(string DataSource, string Entity, string SourceHash);
        private static readonly BoundedCompilationCache<TypeIdentity, Type> Cache = new(256);

        /// <summary>Namespace the generated entity classes are emitted into.</summary>
        public const string GeneratedNamespace = "TheTechIdea.Beep.Generated.Entities";

        /// <summary>
        /// The runtime type for <paramref name="structure"/>, generating and
        /// caching it on first use.
        /// </summary>
        /// <returns>
        /// An <see cref="Entity"/>-derived type, or <c>null</c> when the
        /// structure carries no fields to build one from — callers should treat
        /// null as "this entity cannot back a unit of work" and report it.
        /// </returns>
        public static Type GetOrCreate(IDMEEditor editor, EntityStructure structure)
        {
            if (editor == null || structure == null) return null;
            if (structure.Fields == null || structure.Fields.Count == 0) return null;
            if (string.IsNullOrWhiteSpace(structure.EntityName)) return null;

            try
            {
                var captured = EntityMetadataSnapshot.Capture(structure);
                var creator = new ClassCreator(editor);

                // CreateEntityClass — NOT CreateClass. The latter emits a plain
                // POCO with no base type, which fails the T : Entity constraint
                // at MakeGenericType with a message that names neither.
                var code = creator.CreateEntityClass(
                    captured,
                    usingHeader: null,
                    extraCode: null,
                    outputPath: null,
                    namespaceString: GeneratedNamespace,
                    generateFiles: false);

                var key = new TypeIdentity(captured.DataSourceID, captured.EntityName, RoslynCompiler.GeneratedSourceHash(code));
                return Cache.GetOrAdd(key, () =>
                {
                    var type = RoslynCompiler.CompileClassTypeandAssembly($"{GeneratedNamespace}.{captured.EntityName}", code).Item1;
                    return type ?? throw new InvalidOperationException("Generated source does not contain the requested entity type.");
                });
            }
            catch (Exception ex)
            {
                // House rule: report, never swallow.
                editor.AddLogMessage(
                    "Beep",
                    $"EntityTypeFactory: generating a runtime type failed ({ex.GetType().Name}).",
                    DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Drops cached types for a datasource, for when its schema is re-read.
        /// </summary>
        public static void Invalidate(string dataSourceName)
        {
            if (string.IsNullOrWhiteSpace(dataSourceName)) return;

            Cache.RemoveWhere(key => string.Equals(key.DataSource, dataSourceName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
