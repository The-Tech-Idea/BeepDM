using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Resolves IUnitofWork + IEntityStructure from a connection name and entity name
    /// using IDMEEditor as the underlying service locator.
    /// Eliminates the WinForms-side coupling of ConnectionName/EntityName on BeepDataBlock.
    /// </summary>
    public class BlockFactory : IBlockFactory
    {
        private readonly IDMEEditor _editor;

        /// <summary>
        /// Creates a block factory that resolves datasources and entities through the supplied editor instance.
        /// </summary>
        /// <param name="editor">Editor used to locate datasources and construct units of work.</param>
        public BlockFactory(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Resolves and creates the unit-of-work and entity metadata pair for a block source.
        /// </summary>
        public async Task<(IUnitofWork UoW, IEntityStructure Structure)> CreateBlockAsync(
            string connectionName,
            string entityName,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(connectionName))
                throw new ArgumentException("connectionName is required.", nameof(connectionName));
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("entityName is required.", nameof(entityName));

            // Open the data source
            var ds = _editor.GetDataSource(connectionName);
            if (ds == null)
            {
                _editor.AddLogMessage("BlockFactory",
                    $"Data source '{connectionName}' not found.", DateTime.Now, -1,
                    connectionName, Errors.Failed);
                return (null, null);
            }

            if (ds.ConnectionStatus != System.Data.ConnectionState.Open)
            {
                await Task.Run(() => ds.Openconnection(), ct).ConfigureAwait(false);
            }

            // Resolve entity structure
            var structure = ds.GetEntityStructure(entityName, true);
            if (structure == null)
            {
                _editor.AddLogMessage("BlockFactory",
                    $"Entity '{entityName}' not found in '{connectionName}'.", DateTime.Now, -1,
                    entityName, Errors.Failed);
                return (null, null);
            }

            // Determine entity type and create UOW via factory
            var entityType = ds.GetEntityType(entityName);
            if (entityType == null)
            {
                _editor.AddLogMessage("BlockFactory",
                    $"Could not resolve runtime type for entity '{entityName}'.", DateTime.Now, -1,
                    entityName, Errors.Failed);
                return (null, null);
            }

            try
            {
                var wrapper = UnitOfWorkFactory.CreateUnitOfWork(
                    entityType, _editor, connectionName, entityName,
                    (EntityStructure)structure);

                return (wrapper as IUnitofWork, structure);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BlockFactory",
                    $"Failed to create UnitOfWork for '{connectionName}.{entityName}': {ex.Message}",
                    DateTime.Now, -1, entityName, Errors.Failed);
                return (null, null);
            }
        }

        /// <summary>
        /// Returns whether the supplied connection and entity can be resolved into a usable block source.
        /// </summary>
        public async Task<bool> ValidateBlockSourceAsync(
            string connectionName,
            string entityName,
            CancellationToken ct = default)
        {
            try
            {
                var result = await CreateBlockAsync(connectionName, entityName, ct).ConfigureAwait(false);
                return result.UoW != null && result.Structure != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
