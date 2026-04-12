using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.Forms.Helpers;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Enhanced UnitofWorksManager with modular design using helper classes.
    /// Simulates Oracle Forms environment with master-detail relationships,
    /// triggers, and form-level operations management.
    /// 
    /// This is the main coordinator class - complex operations are delegated to:
    /// - Helper classes for specific functionality
    /// - Partial classes for related operations
    /// - The main class stays lean and focused on coordination
    /// </summary>
    public partial class FormsManager : IUnitofWorksManager
    {
        #region Fields
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks = new();
        private readonly ConcurrentDictionary<string, List<DataBlockRelationship>> _relationships = new(StringComparer.OrdinalIgnoreCase);
        private readonly MasterDetailKeyResolver _masterDetailKeyResolver = new();

        // Helper managers
        private readonly IDirtyStateManager _dirtyStateManager;
        private readonly IEventManager _eventManager;
        private readonly IFormsSimulationHelper _formsSimulationHelper;
        private readonly IPerformanceManager _performanceManager;
        private readonly IConfigurationManager _configurationManager;
        private readonly ISystemVariablesManager _systemVariablesManager;
        private readonly IValidationManager _validationManager;
        private readonly ILOVManager _lovManager;
        private readonly IItemPropertyManager _itemPropertyManager;
        private readonly ITriggerManager _triggerManager;
        private readonly ISavepointManager _savepointManager;
        private readonly ILockManager _lockManager;
        private readonly IQueryBuilderManager _queryBuilderManager;
        private readonly IBlockErrorLog _errorLog;
        private readonly IMessageQueueManager _messageManager;
        private readonly IBlockFactory _blockFactory;
        private readonly IBlockPropertyManager _blockPropertyManager;
        private readonly IAlertProvider _alertProvider;
        private readonly ISequenceProvider _sequenceProvider;
        private readonly ITimerManager _timerManager;

        // Phase 3 — multi-form
        private readonly IFormRegistry _formRegistry;
        private readonly IFormMessageBus _messageBus;
        private readonly ISharedBlockManager _sharedBlockManager;
        private readonly Stack<FormCallStackEntry> _callStack = new Stack<FormCallStackEntry>();
        private readonly ConcurrentDictionary<string, object> _formParameters = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private readonly object _lockObject = new object();
        private bool _disposed;
        private string _currentFormName;
        private string _currentBlockName;

        // Phase 4 helpers
        private readonly CrossBlockValidationManager _crossBlockValidation;
        private readonly NavigationHistoryManager _navHistoryManager = new();
        private readonly ConcurrentDictionary<string, EventHandler<ItemChangedEventArgs<Entity>>> _itemChangedHandlers = new();

        // Master-detail: current-record change hook (Phase 7.6)
        private readonly ConcurrentDictionary<string, EventHandler> _mdCurrentChangedHandlers = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, int> _syncSuppressCount = new(StringComparer.OrdinalIgnoreCase);

        // Phase 5 — audit
        private IAuditManager _auditManager;

        // Phase 6 — security
        private ISecurityManager _securityManager;

        // Phase 7 — paging
        private IPagingManager _pagingManager;
        #endregion

        #region Phase 4-F – Block Change Feed
        /// <inheritdoc/>
        public event EventHandler<BlockFieldChangedEventArgs> OnBlockFieldChanged;
        #endregion

        #region Constructors

        /// <summary>
        /// Constructor with full dependency injection
        /// </summary>
        public FormsManager(
            IDMEEditor dmeEditor,
            IDirtyStateManager dirtyStateManager = null,
            IEventManager eventManager = null,
            IFormsSimulationHelper formsSimulationHelper = null,
            IPerformanceManager performanceManager = null,
            IConfigurationManager configurationManager = null,
            ISystemVariablesManager systemVariablesManager = null,
            IValidationManager validationManager = null,
            ILOVManager lovManager = null,
            IItemPropertyManager itemPropertyManager = null,
            ITriggerManager triggerManager = null,
            ISavepointManager savepointManager = null,
            ILockManager lockManager = null,
            IQueryBuilderManager queryBuilderManager = null,
            IBlockErrorLog errorLog = null,
            IMessageQueueManager messageManager = null,
            IBlockFactory blockFactory = null,
            IBlockPropertyManager blockPropertyManager = null,
            IAlertProvider alertProvider = null,
            ISequenceProvider sequenceProvider = null,
            ITimerManager timerManager = null,
            IFormRegistry formRegistry = null,
            IFormMessageBus messageBus = null,
            ISharedBlockManager sharedBlockManager = null)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));

            // Initialize helper managers with defaults if not provided
            _dirtyStateManager = dirtyStateManager ?? new DirtyStateManager(_dmeEditor, _blocks, GetDetailBlocks, GetBlock);
            _eventManager = eventManager ?? new EventManager(_dmeEditor);
            _formsSimulationHelper = formsSimulationHelper ?? new FormsSimulationHelper(_dmeEditor, _blocks);
            _performanceManager = performanceManager ?? new PerformanceManager(_dmeEditor);
            _configurationManager = configurationManager ?? new ConfigurationManager();
            _systemVariablesManager = systemVariablesManager ?? new SystemVariablesManager(_dmeEditor, _blocks);
            _validationManager = validationManager ?? new ValidationManager();
            _lovManager = lovManager ?? new LOVManager(_dmeEditor, _blocks);
            _itemPropertyManager = itemPropertyManager ?? new ItemPropertyManager(_dmeEditor);
            _triggerManager = triggerManager ?? new TriggerManager(_dmeEditor, _blocks);
            _savepointManager = savepointManager ?? new SavepointManager();
            _lockManager = lockManager ?? new LockManager();
            _queryBuilderManager = queryBuilderManager ?? new QueryBuilderManager();
            _errorLog = errorLog ?? new BlockErrorLog();
            _messageManager = messageManager ?? new MessageQueueManager();
            _blockFactory = blockFactory ?? new BlockFactory(_dmeEditor);
            _blockPropertyManager = blockPropertyManager ?? new Forms.Helpers.BlockPropertyManager(_blocks);
            _alertProvider = alertProvider ?? new Forms.Helpers.DefaultAlertProvider();
            _sequenceProvider = sequenceProvider ?? new Forms.Helpers.SequenceProvider();
            _timerManager = timerManager ?? new Forms.Helpers.TimerManager();
            _timerManager.TimerFired += OnTimerManagerFired;
            _formRegistry = formRegistry ?? new Forms.Helpers.FormRegistry();
            _messageBus = messageBus ?? new Forms.Helpers.FormMessageBus();
            _messageBus.OnFormMessage += OnMessageBusFormMessage;
            _sharedBlockManager = sharedBlockManager ?? new Forms.Helpers.SharedBlockManager();
            _securityManager = new Forms.Helpers.SecurityManager();
            _pagingManager = new Forms.Helpers.PagingManager();
            _auditManager = new Forms.Helpers.AuditManager();
            _crossBlockValidation = new CrossBlockValidationManager(GetUnitOfWork);

            InitializeManager();
            InitializeTriggerChaining();
            InitializeAudit();
            InitializeSecurity();
            InitializePerformance();
        }

        /// <summary>
        /// Simple constructor for backward compatibility
        /// </summary>
        public FormsManager(IDMEEditor dmeEditor)
            : this(dmeEditor, null)
        {
        }
        #endregion
    }
}