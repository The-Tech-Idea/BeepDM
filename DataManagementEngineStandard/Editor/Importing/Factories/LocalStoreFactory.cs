using System.Linq;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.History;
using TheTechIdea.Beep.Editor.Importing.Sync;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Importing.Factories
{
    /// <summary>
    /// Creates the correct store implementations based on what is available in the environment.
    /// <list type="bullet">
    ///   <item>When a local-DB capable driver (SQLite / LiteDB) is registered → returns DataSource-backed stores.</item>
    ///   <item>Otherwise → returns JSON-file-backed stores (zero-config default).</item>
    /// </list>
    /// All stores are created with <paramref name="editor"/> so that they can resolve the
    /// connection name set in <see cref="LocalStoreConnectionName"/>.
    /// </summary>
    public static class LocalStoreFactory
    {
        /// <summary>
        /// Name of the default local connection used by the DataSource-backed stores.
        /// Must match a registered connection in <c>editor.ConfigEditor.DataConnections</c>.
        /// Callers may change this before calling any <c>Create*</c> method.
        /// </summary>
        public static string LocalStoreConnectionName { get; set; } = "BeepImportStore";

        // ------------------------------------------------------------------
        // Public factory methods
        // ------------------------------------------------------------------

        /// <summary>Creates the appropriate <see cref="IWatermarkStore"/>.</summary>
        /// <remarks>
        /// Watermarks are tiny per-pipeline key-value pairs.  File-based storage is always
        /// preferred — it requires no schema and is safe across all environments.
        /// </remarks>
        public static IWatermarkStore CreateWatermarkStore(IDMEEditor editor) =>
            new FileWatermarkStore();

        /// <summary>Creates the appropriate <see cref="IImportErrorStore"/>.</summary>
        public static IImportErrorStore CreateErrorStore(IDMEEditor editor) =>
            HasLocalDriver(editor)
                ? new DataSourceImportErrorStore(editor, LocalStoreConnectionName)
                : new JsonFileImportErrorStore();

        /// <summary>Creates the appropriate <see cref="IImportRunHistoryStore"/>.</summary>
        public static IImportRunHistoryStore CreateHistoryStore(IDMEEditor editor) =>
            HasLocalDriver(editor)
                ? new DataSourceImportRunHistoryStore(editor, LocalStoreConnectionName)
                : new JsonFileImportRunHistoryStore();

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns <c>true</c> when at least one local-DB driver (SQLite / LiteDB) is registered.
        /// </summary>
        public static bool HasLocalDriver(IDMEEditor editor) =>
            editor?.ConfigEditor?.DataDriversClasses != null &&
            editor.ConfigEditor.DataDriversClasses.Any(d =>
                d.DatasourceType is
                    DataSourceType.SqlLite or
                    DataSourceType.LiteDB);
    }
}
