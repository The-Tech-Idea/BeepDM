using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOW;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        #region Properties
        /// <summary>Gets the DME Editor instance</summary>
        public IDMEEditor DMEEditor => _dmeEditor;

        /// <summary>Gets or sets the current form name</summary>
        public string CurrentFormName 
        { 
            get => _currentFormName; 
            set => _currentFormName = value; 
        }

        /// <summary>Gets or sets the current active block name</summary>
        public string CurrentBlockName
        {
            get => _currentBlockName;
            set => _currentBlockName = value;
        }

        /// <summary>Gets all registered blocks</summary>
        public IReadOnlyDictionary<string, DataBlockInfo> Blocks => _blocks;

        /// <summary>Gets whether any block has unsaved changes</summary>
        public bool IsDirty => _blocks.Values.Any(block => block.UnitOfWork?.IsDirty == true);

        /// <summary>Gets the current status message</summary>
        public string Status { get; private set; } = "Ready";

        /// <summary>Gets the count of registered blocks</summary>
        public int BlockCount => _blocks.Count;

        /// <summary>Gets the dirty state manager</summary>
        public IDirtyStateManager DirtyStateManager => _dirtyStateManager;

        /// <summary>Gets the performance manager</summary>
        public IPerformanceManager PerformanceManager => _performanceManager;

        /// <summary>Gets the system variables manager</summary>
        public ISystemVariablesManager SystemVariables => _systemVariablesManager;

        /// <summary>Gets the validation manager</summary>
        public IValidationManager Validation => _validationManager;

        /// <summary>Gets the LOV manager</summary>
        public ILOVManager LOV => _lovManager;

        /// <summary>Gets the item property manager</summary>
        public IItemPropertyManager ItemProperties => _itemPropertyManager;

        /// <summary>Gets the trigger manager</summary>
        public ITriggerManager Triggers => _triggerManager;

        /// <summary>Gets the savepoint manager</summary>
        public ISavepointManager Savepoints => _savepointManager;

        /// <summary>Gets the record locking manager</summary>
        public ILockManager Locking => _lockManager;

        /// <summary>Gets the query builder manager</summary>
        public IQueryBuilderManager QueryBuilder => _queryBuilderManager;

        /// <summary>Gets the per-block error log</summary>
        public IBlockErrorLog ErrorLog => _errorLog;

        /// <summary>Gets the message queue manager</summary>
        public IMessageQueueManager Messages => _messageManager;

        /// <summary>Gets the block factory</summary>
        public IBlockFactory BlockFactory => _blockFactory;

        /// <summary>Gets the block property manager (SET/GET_BLOCK_PROPERTY)</summary>
        public IBlockPropertyManager BlockProperties => _blockPropertyManager;

        /// <summary>Gets the alert provider for SHOW_ALERT-style dialogs</summary>
        public IAlertProvider AlertProvider => _alertProvider;

        /// <summary>Gets the sequence provider for auto-increment sequences</summary>
        public ISequenceProvider Sequences => _sequenceProvider;

        /// <summary>Gets the timer manager for CREATE/DELETE_TIMER built-ins</summary>
        public ITimerManager Timers => _timerManager;

        /// <summary>Gets the form registry for multi-form navigation (CALL_FORM / OPEN_FORM / NEW_FORM)</summary>
        public IFormRegistry Registry => _formRegistry;

        /// <summary>Gets the inter-form message bus</summary>
        public IFormMessageBus MessageBus => _messageBus;

        /// <summary>Gets the shared data block manager</summary>
        public ISharedBlockManager SharedBlocks => _sharedBlockManager;

        /// <summary>Gets the configuration</summary>
        public UnitofWorksManagerConfiguration Configuration => _configurationManager?.Configuration;
        #endregion
    }
}
