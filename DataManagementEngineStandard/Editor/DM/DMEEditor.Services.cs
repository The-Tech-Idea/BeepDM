using TheTechIdea.Beep.Editor.EntityDiscovery;
using TheTechIdea.Beep.Editor.Migration;
using TheTechIdea.Beep.Services.DatasourceManagement;

namespace TheTechIdea.Beep
{
    /// <summary>
    /// DMEEditor partial class that exposes the Phase 15 core services
    /// (EntityDiscoveryService, DatasourceManagementService, MigrationTrackingService)
    /// as lazy-initialized properties. These services wrap existing BeepDM
    /// infrastructure — they do not duplicate logic.
    /// </summary>
    public partial class DMEEditor
    {
        private EntityDiscoveryService _entityDiscovery;
        private DatasourceManagementService _datasourceMgr;
        private MigrationTrackingService _migrationTracker;
        private readonly object _serviceLock = new();

        /// <summary>
        /// Entity discovery service — scans registered and loaded assemblies
        /// for Entity / POCO / EF Core types.
        /// </summary>
        public EntityDiscoveryService EntityDiscovery
        {
            get
            {
                if (_entityDiscovery == null)
                {
                    lock (_serviceLock)
                    {
                        _entityDiscovery ??= new EntityDiscoveryService(this, cache: null);
                    }
                }
                return _entityDiscovery;
            }
        }

        /// <summary>
        /// Datasource management service — unified datasource lifecycle:
        /// create, test, remove, apply schema, inspect schema.
        /// </summary>
        public DatasourceManagementService DatasourceMgr
        {
            get
            {
                if (_datasourceMgr == null)
                {
                    lock (_serviceLock)
                    {
                        _datasourceMgr ??= new DatasourceManagementService(this);
                    }
                }
                return _datasourceMgr;
            }
        }

        /// <summary>
        /// Migration tracking service — migration history, tracked execution,
        /// undo with compensation plan preview.
        /// </summary>
        public MigrationTrackingService MigrationTracker
        {
            get
            {
                if (_migrationTracker == null)
                {
                    lock (_serviceLock)
                    {
                        _migrationTracker ??= new MigrationTrackingService(this);
                    }
                }
                return _migrationTracker;
            }
        }
    }
}
