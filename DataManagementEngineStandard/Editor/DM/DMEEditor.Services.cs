using TheTechIdea.Beep.Editor.EntityDiscovery;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Migration;
using TheTechIdea.Beep.Services.AppMap;
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
        private SolutionDiscoveryService _solutionDiscovery;
        private AppMapService _appMap;
        private EnvironmentManagementService _environment;
        private VersionManagementService _version;
        private IdentityManagementService _identity;
        private MultiProjectSyncService _multiSync;
        private DataImportManager _dataImport;
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

        /// <summary>
        /// Solution discovery service — scans for .sln files, parses .csproj
        /// metadata, builds project dependency graphs, and auto-detects Data folders.
        /// </summary>
        public ISolutionDiscoveryService SolutionDiscovery
        {
            get
            {
                if (_solutionDiscovery == null)
                {
                    lock (_serviceLock)
                    {
                        _solutionDiscovery ??= new SolutionDiscoveryService(this);
                    }
                }
                return _solutionDiscovery;
            }
        }

        /// <summary>
        /// AppMap service — creates AppMap from solution discovery, detects project
        /// roles via heuristics, allows manual role override, and persists to JSON.
        /// </summary>
        public IAppMapService AppMap
        {
            get
            {
                if (_appMap == null)
                {
                    lock (_serviceLock)
                    {
                        _appMap ??= new AppMapService(this);
                    }
                }
                return _appMap;
            }
        }

        /// <summary>
        /// Environment management service — per-project environment profiles,
        /// standard tier seeding (Local/Dev/Test/Staging/Production),
        /// promote config between environments, environment-wide switching.
        /// </summary>
        public IEnvironmentManagementService Environment
        {
            get
            {
                if (_environment == null)
                {
                    lock (_serviceLock)
                    {
                        _environment ??= new EnvironmentManagementService(this);
                    }
                }
                return _environment;
            }
        }

        /// <summary>
        /// Version management service — database and application version tracking,
        /// version comparison, and JSON persistence.
        /// </summary>
        public IVersionManagementService Version
        {
            get
            {
                if (_version == null)
                {
                    lock (_serviceLock)
                    {
                        _version ??= new VersionManagementService(this);
                    }
                }
                return _version;
            }
        }

        /// <summary>
        /// Identity management service — auto-detects ASP.NET Identity tables,
        /// user/role CRUD with custom table mapping for non-ASP.NET systems.
        /// </summary>
        public IIdentityManagementService Identity
        {
            get
            {
                if (_identity == null)
                {
                    lock (_serviceLock)
                    {
                        _identity ??= new IdentityManagementService(this);
                    }
                }
                return _identity;
            }
        }

        /// <summary>
        /// Data import manager — full import pipeline with validation, transformation,
        /// batch processing, progress tracking, defaults, and history.
        /// </summary>
        public DataImportManager DataImport
        {
            get
            {
                if (_dataImport == null)
                {
                    lock (_serviceLock)
                    {
                        _dataImport ??= new DataImportManager(this);
                    }
                }
                return _dataImport;
            }
        }

        /// <summary>
        /// Multi-project sync service — detects shared Data projects,
        /// auto-links consumers, syncs schema to shared database.
        /// </summary>
        public IMultiProjectSyncService MultiSync
        {
            get
            {
                if (_multiSync == null)
                {
                    lock (_serviceLock)
                    {
                        _multiSync ??= new MultiProjectSyncService(this);
                    }
                }
                return _multiSync;
            }
        }

        private IAppRelationshipService _appRelationship;

        /// <summary>
        /// App-Environment-Datasource relationship service — tracks which
        /// datasource is used by which app in which environment.
        /// </summary>
        public IAppRelationshipService AppRelationship
        {
            get
            {
                if (_appRelationship == null)
                {
                    lock (_serviceLock)
                    {
                        _appRelationship ??= new AppRelationshipService(this);
                    }
                }
                return _appRelationship;
            }
        }
    }
}
