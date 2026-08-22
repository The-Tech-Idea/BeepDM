using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Turns the design-time blocks declared on a <see cref="FormDefinition"/>
    /// into live blocks registered with a <see cref="IUnitofWorksManager"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generated designer code declares a block with
    /// <c>this._formsHost.Definition.Blocks.Add(Ord);</c>. Until 2026-08-01
    /// nothing read that collection — <see cref="FormDefinition.Blocks"/> and
    /// <see cref="FormDefinition.AutoCreateBlocksFromDefinition"/> had no
    /// consumer anywhere — so a block the designer authored never reached the
    /// manager, and every registration that needs one failed:
    /// <c>CreateMasterDetailRelation</c> threw "Master block 'X' is not
    /// registered", triggers and LOVs went nowhere.
    /// </para>
    /// <para>
    /// This lives in the engine, not in a UI host. The WinForms and WPF hosts
    /// are thin presentation layers: they make one call each, they do not
    /// reimplement this.
    /// </para>
    /// </remarks>
    public static class DefinitionBlockRegistrar
    {
        /// <summary>
        /// Registers every block declared on <paramref name="definition"/> that
        /// the manager does not already know.
        /// </summary>
        /// <returns>The number of blocks newly registered.</returns>
        /// <remarks>
        /// Idempotent — a block the manager already holds is skipped, so
        /// re-assigning a manager does not re-register.
        /// <para>
        /// Masters are registered before details: both halves of a master-detail
        /// pair must exist before the generated <c>CreateMasterDetailRelation</c>
        /// runs, and the manager rejects a relation naming a block it does not
        /// know.
        /// </para>
        /// </remarks>
        public static int RegisterAll(IUnitofWorksManager manager, FormDefinition definition)
        {
            if (manager == null || definition == null) return 0;
            if (!definition.AutoCreateBlocksFromDefinition) return 0;
            if (definition.Blocks == null || definition.Blocks.Count == 0) return 0;

            var editor = manager.DMEEditor;
            if (editor == null) return 0;

            var registered = 0;

            var ordered = definition.Blocks
                .Where(b => b != null && !string.IsNullOrWhiteSpace(b.BlockName))
                .OrderByDescending(b => b.EntityDefinition != null && b.EntityDefinition.IsMasterBlock)
                .ToList();

            foreach (var block in ordered)
            {
                if (manager.BlockExists(block.BlockName)) continue;

                try
                {
                    if (TryRegister(manager, editor, block)) registered++;
                }
                catch (Exception ex)
                {
                    // House rule: report, never swallow. One unbindable block
                    // must not stop the rest of the form coming up.
                    editor.AddLogMessage(
                        "Beep",
                        $"DefinitionBlockRegistrar: could not register declared block '{block.BlockName}': {ex.Message}",
                        DateTime.Now, 0, null, Errors.Failed);
                }
            }

            return registered;
        }

        /// <summary>
        /// Resolves one declared block's source and registers it.
        /// </summary>
        /// <remarks>
        /// The datasource supplies the row type through <c>GetEntityType</c> —
        /// the same route <see cref="BlockFactory"/> takes. Nothing here
        /// generates or compiles a type: a driver already knows how to shape its
        /// own rows, and duplicating that would be a second thing to keep in
        /// step.
        /// </remarks>
        private static bool TryRegister(
            IUnitofWorksManager manager, IDMEEditor editor, BlockDefinition block)
        {
            var connectionName = FirstNonEmpty(
                block.ConnectionName,
                block.EntityDefinition?.ConnectionName);

            var entityName = FirstNonEmpty(
                block.EntityName,
                block.EntityDefinition?.EntityName,
                block.EntityDefinition?.DatasourceEntityName);

            if (string.IsNullOrWhiteSpace(connectionName) || string.IsNullOrWhiteSpace(entityName))
            {
                editor.AddLogMessage(
                    "Beep",
                    $"DefinitionBlockRegistrar: block '{block.BlockName}' declares no connection or entity, so it was skipped.",
                    DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

            var source = editor.GetDataSource(connectionName);
            if (source == null)
            {
                editor.AddLogMessage(
                    "Beep",
                    $"DefinitionBlockRegistrar: connection '{connectionName}' for block '{block.BlockName}' could not be opened.",
                    DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

            if (source.ConnectionStatus != System.Data.ConnectionState.Open)
            {
                source.Openconnection();
            }

            var structure = source.GetEntityStructure(entityName, true);

            // A driver that cannot build a row type may return null or throw —
            // the RDBMS plugins throw ("Failed to compile type ..."). Either way
            // it is a reason to fall back, not to fail the block.
            Type entityType = null;
            try
            {
                entityType = source.GetEntityType(entityName);
            }
            catch (Exception ex)
            {
                editor.AddLogMessage(
                    "Beep",
                    $"DefinitionBlockRegistrar: '{connectionName}.{entityName}' could not build its own " +
                    $"row type ({ex.Message.Trim()}); falling back to the engine's generator.",
                    DateTime.Now, 0, null, Errors.Ok);
            }

            // Fall back to the engine's own generator when the driver cannot
            // supply a row type.
            //
            // A driver's GetEntityType compiles inside whatever build of the
            // engine that driver was built against. The shipped RDBMS plugins
            // bind DataManagementEngine 3.1.1 as a NuGet package, and that build
            // cannot compile an entity carrying [Key]/[Required]/[MaxLength] —
            // which is every entity generated from a real database schema. The
            // driver returns null, and the block silently did not register: a
            // form over a SQL table came up completely empty while a form over a
            // JSON file worked, because file structures carry no such metadata.
            //
            // The block needs a row type; the driver is one way to get one, and
            // the engine can always build one from the EntityStructure it just
            // read. Preferring the driver keeps any type it has specialised.
            // (2026-08-02, found on the first SQLite run)
            if (entityType == null && structure != null)
            {
                entityType = Tools.EntityTypeFactory.GetOrCreate(editor, structure);

                if (entityType != null)
                {
                    editor.AddLogMessage(
                        "Beep",
                        $"DefinitionBlockRegistrar: '{connectionName}.{entityName}' supplied no row type; " +
                        "generated one from its entity structure.",
                        DateTime.Now, 0, null, Errors.Ok);
                }
            }

            if (structure == null || entityType == null)
            {
                editor.AddLogMessage(
                    "Beep",
                    $"DefinitionBlockRegistrar: '{connectionName}.{entityName}' for block '{block.BlockName}' " +
                    "could not be resolved to an entity structure and row type.",
                    DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

            ApplyAuthoredKeys(structure, block, editor);

            // CreateUnitOfWork returns IUnitOfWorkWrapper; RegisterBlock takes the
            // non-generic IUnitofWork. UnitOfWorkWrapper implements both.
            var wrapper = UnitOfWorkFactory.CreateUnitOfWork(
                entityType, editor, connectionName, entityName, structure);

            if (wrapper is not IUnitofWork unitOfWork)
            {
                editor.AddLogMessage(
                    "Beep",
                    $"DefinitionBlockRegistrar: the unit of work built for block '{block.BlockName}' " +
                    "does not implement IUnitofWork, so it cannot be registered.",
                    DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

            manager.RegisterBlock(
                block.BlockName,
                unitOfWork,
                structure,
                connectionName,
                block.EntityDefinition != null && block.EntityDefinition.IsMasterBlock);

            ApplyAuthoredFieldProperties(manager, block);

            return true;
        }

        /// <summary>
        /// Overlays each field's authored Format Mask / Default Value / Copy
        /// Value From Item / per-operation allowed flags — and its Property
        /// Class, if any — onto the <see cref="ItemInfo"/> that
        /// <c>RegisterBlock</c> just seeded from the datasource's entity
        /// structure.
        /// </summary>
        /// <remarks>
        /// <c>BlockFieldDefinition</c> carried these from the moment the IDE
        /// could author them, but nothing read them back out:
        /// <c>RegisterItemsFromEntityStructure</c> only ever saw the
        /// datasource's own column metadata (nullability, auto-increment,
        /// key), never the designer's overrides. A field marked read-only for
        /// insert in the designer stayed insertable at runtime, an authored
        /// FORMAT_MASK never reached the item store, and DEFAULT_VALUE /
        /// "Copy Value from Item" had no path onto a new record at all.
        /// (2026-08-22)
        /// </remarks>
        private static void ApplyAuthoredFieldProperties(IUnitofWorksManager manager, BlockDefinition block)
        {
            var fields = block.EntityDefinition?.Fields;
            if (fields == null || fields.Count == 0) return;

            foreach (var field in fields)
            {
                if (field == null || string.IsNullOrWhiteSpace(field.FieldName)) continue;

                // A field the designer authored that the resolved entity
                // structure doesn't actually have (renamed/dropped column) has
                // nothing to overlay onto — RegisterItemsFromEntityStructure
                // never created an ItemInfo for it.
                var item = manager.ItemProperties?.GetItem(block.BlockName, field.FieldName);
                if (item == null) continue;

                manager.PropertyClasses?.ApplyToItem(item, field);
            }
        }

        /// <summary>
        /// Overlays the key metadata the designer authored onto the entity
        /// structure the datasource returned.
        /// </summary>
        /// <remarks>
        /// A schemaless source cannot declare its own key: there is nothing in a
        /// <c>.json</c> or <c>.csv</c> file that says which column identifies a
        /// row, and <c>JsonMultiFileDataSource</c> accordingly returns an empty
        /// <c>PrimaryKeys</c> list. The block definition <em>does</em> say so —
        /// <c>BlockFieldDefinition.IsPrimaryKey</c> — but until 2026-08-01
        /// nothing carried it across, so <c>UnitofWork</c> logged "Entity dont
        /// have a primary key" and refused every update. Reads were unaffected,
        /// which is why this stayed invisible: a form over a file datasource
        /// queried and navigated normally and silently discarded edits.
        /// <para>
        /// Fills a gap only — a source that already declares keys keeps them.
        /// </para>
        /// </remarks>
        private static void ApplyAuthoredKeys(
            EntityStructure structure, BlockDefinition block, IDMEEditor editor)
        {
            if (structure?.Fields == null || structure.Fields.Count == 0) return;
            if (structure.PrimaryKeys != null && structure.PrimaryKeys.Count > 0) return;

            var authored = block.EntityDefinition?.Fields?
                .Where(f => f != null && f.IsPrimaryKey && !string.IsNullOrWhiteSpace(f.FieldName))
                .Select(f => f.FieldName)
                .ToList();

            if (authored == null || authored.Count == 0) return;

            var keys = structure.Fields
                .Where(f => authored.Any(a =>
                    string.Equals(a, f.FieldName, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (keys.Count == 0)
            {
                editor.AddLogMessage(
                    "Beep",
                    $"DefinitionBlockRegistrar: block '{block.BlockName}' declares key field(s) " +
                    $"[{string.Join(",", authored)}] that '{structure.EntityName}' does not have, " +
                    "so the entity is still keyless and updates will be refused.",
                    DateTime.Now, 0, null, Errors.Failed);
                return;
            }

            foreach (var key in keys) key.IsKey = true;
            structure.PrimaryKeys = keys;
        }

        private static string FirstNonEmpty(params string[] candidates) =>
            candidates?.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
    }
}
