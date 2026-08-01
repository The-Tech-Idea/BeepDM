using System;
using System.Collections.Concurrent;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// Builds — and caches — a concrete <see cref="Entity"/>-derived runtime type
    /// for an <see cref="EntityStructure"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every datasource's <c>GetEntityType</c> should call this.</b> They were
    /// each solving it differently and none of them correctly:
    /// <c>JsonMultiFileDataSource</c> used <c>DMTypeBuilder</c>, which emits
    /// types deriving from <c>object</c>; <c>InMemoryCacheDataSource</c> returned
    /// <c>Dictionary&lt;string, object&gt;</c> with a "could implement dynamic
    /// type creation later" note.
    /// </para>
    /// <para>
    /// That matters because <c>UnitofWork&lt;T&gt;</c> is constrained to
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
        // Keyed by datasource + entity: two blocks over the same entity share a
        // type, and re-opening a form must not recompile it.
        private static readonly ConcurrentDictionary<string, Type> Cache =
            new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

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

            var key = $"{structure.DataSourceID}.{structure.EntityName}";

            return Cache.GetOrAdd(key, _ => Build(editor, structure));
        }

        private static Type Build(IDMEEditor editor, EntityStructure structure)
        {
            try
            {
                var creator = new ClassCreator(editor);

                // CreateEntityClass — NOT CreateClass. The latter emits a plain
                // POCO with no base type, which fails the T : Entity constraint
                // at MakeGenericType with a message that names neither.
                var code = creator.CreateEntityClass(
                    structure,
                    usingHeader: null,
                    extraCode: null,
                    outputPath: null,
                    namespaceString: GeneratedNamespace,
                    generateFiles: false);

                // CreateEntityClass names the class after the entity.
                var type = creator.CreateTypeFromCode(
                    code, $"{GeneratedNamespace}.{structure.EntityName}");

                if (type == null)
                {
                    editor.AddLogMessage(
                        "Beep",
                        $"EntityTypeFactory: could not build a runtime type for " +
                        $"'{structure.EntityName}'.",
                        DateTime.Now, 0, null, Errors.Failed);
                }

                return type;
            }
            catch (Exception ex)
            {
                // House rule: report, never swallow.
                editor.AddLogMessage(
                    "Beep",
                    $"EntityTypeFactory: generating a runtime type for " +
                    $"'{structure.EntityName}' failed: {ex.Message}",
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

            var prefix = dataSourceName + ".";
            foreach (var key in Cache.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    Cache.TryRemove(key, out _);
                }
            }
        }
    }
}
