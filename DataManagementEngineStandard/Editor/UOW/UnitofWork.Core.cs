using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW.Helpers;
using TheTechIdea.Beep.Editor.UOW.Interfaces;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW
{
    /// <summary>
    /// Core partial class for UnitofWork - Contains core properties, constructors and basic functionality
    /// This is the refactored version with helper-based architecture and DefaultsManager integration
    /// </summary>
    /// <typeparam name="T">The type of entity.</typeparam>
#if WINDOWS &&  NET6_0_OR_GREATER
    [DesignerCategory("TheTechIdea")]
    [ToolboxItem(true)]
    [ToolboxBitmap(typeof(UnitOfWork<>), "TheTechIdea.Beep.GFX.unitofwork.ico")]
    [DisplayName("Unit of Work")]
#endif
    public partial class UnitofWork<T> : IUnitofWork<T>, INotifyPropertyChanged where T : Entity, new()
    {
        #region Private Fields

        /// <summary>Indicates whether notifications should be suppressed.</summary>
        protected bool _suppressNotification = false;

        /// <summary>A source for creating cancellation tokens.</summary>
        protected CancellationTokenSource tokenSource;
        
        /// <summary>A token that can be used to request cancellation of an operation.</summary>
        protected CancellationToken token;
        
        /// <summary>Indicates whether the primary key is a string.</summary>
        protected bool IsPrimaryKeyString = false;
        
        /// <summary>Indicates whether the object has been validated.</summary>
        protected bool Ivalidated = false;
        
        /// <summary>Indicates whether a new record is being created.</summary>
        protected bool IsNewRecord = false;

        /// <summary>Indicates whether the filter is currently turned on.</summary>
        protected bool IsFilterOn = false;

        /// <summary>A private observable binding list of type T for backup/rollback purposes.</summary>
        protected ObservableBindingList<T> Tempunits;

        /// <summary>The collection of units.</summary>
        protected ObservableBindingList<T> _units;

        /// <summary>The filtered units collection.</summary>
        protected ObservableBindingList<T> _filteredunits;

        /// <summary>Entity states tracking</summary>
        protected Dictionary<int, EntityState> _entityStates = new Dictionary<int, EntityState>();
        
        /// <summary>Deleted entities tracking</summary>
        protected Dictionary<T, EntityState> _deletedentities = new Dictionary<T, EntityState>();

        /// <summary>Primary key property info</summary>
        protected PropertyInfo PKProperty = null;
        
        /// <summary>Current property being processed</summary>
        protected PropertyInfo CurrentProperty = null;
        
        /// <summary>GUID property info</summary>
        protected PropertyInfo Guidproperty = null;

        /// <summary>Keys index counter</summary>
        protected int keysidx;

        /// <summary>Primary key field name</summary>
        protected string _primarykey;

        /// <summary>Disposal flag</summary>
        protected bool disposedValue;

        #endregion

        #region Helper Instances

        /// <summary>Helper for default value operations</summary>
        protected IUnitofWorkDefaults<T> _defaultsHelper;

        /// <summary>Helper for validation operations</summary>
        protected IUnitofWorkValidation<T> _validationHelper;

        /// <summary>Helper for data operations</summary>
        protected IUnitofWorkDataHelper<T> _dataHelper;

        /// <summary>Helper for state management</summary>
        protected IUnitofWorkStateHelper<T> _stateHelper;

        /// <summary>Helper for event management</summary>
        protected IUnitofWorkEventHelper<T> _eventHelper;

        /// <summary>Helper for collection operations</summary>
        protected IUnitofWorkCollectionHelper<T> _collectionHelper;

        #endregion

        #region Core Properties

        /// <summary>Gets a value indicating whether the object is dirty.</summary>
        /// <returns>True if the object is dirty; otherwise, false.</returns>
        public bool IsDirty { get { return GetIsDirty(); } }

        /// <summary>Gets or sets whether the unit of work is in list mode</summary>
        public bool IsInListMode { get; set; } = false;

        /// <summary>Gets the list of deleted units</summary>
        public List<T> DeletedUnits { get; set; } = new List<T>();

        /// <summary>Gets the dictionary of inserted keys</summary>
        public Dictionary<int, string> InsertedKeys { get; set; } = new Dictionary<int, string>();

        /// <summary>Gets the dictionary of updated keys</summary>
        public Dictionary<int, string> UpdatedKeys { get; set; } = new Dictionary<int, string>();

        /// <summary>Gets the dictionary of deleted keys</summary>
        public Dictionary<int, string> DeletedKeys { get; set; } = new Dictionary<int, string>();

        /// <summary>Gets or sets whether the primary key is identity</summary>
        public bool IsIdentity { get; set; } = false;

        /// <summary>Gets or sets the sequencer name</summary>
        public string Sequencer { get; set; }

        /// <summary>Gets or sets the data source name</summary>
        public string DatasourceName { get; set; }

        /// <summary>Gets or sets the entity structure</summary>
        public EntityStructure EntityStructure { get; set; }

        /// <summary>Gets the DME Editor instance</summary>
        public IDMEEditor DMEEditor { get; }

        /// <summary>Gets or sets the data source</summary>
        public IDataSource DataSource { get; set; }

        /// <summary>Gets or sets the entity name</summary>
        public string EntityName { get; set; }

        /// <summary>Gets or sets the entity type</summary>
        public Type EntityType { get; set; }

        /// <summary>Gets or sets the primary key field name</summary>
        public string PrimaryKey 
        { 
            get { return _primarykey; } 
            set { _primarykey = value; } 
        }

        /// <summary>Gets or sets the GUID key field name</summary>
        public string GuidKey { get; set; }

        // Paging and filtering properties
        /// <summary>Gets or sets the current page index</summary>
        public int PageIndex { get; set; } = 0;

        /// <summary>Gets or sets the page size</summary>
        public int PageSize { get; set; } = 10;

        /// <summary>Gets the total item count</summary>
        public int TotalItemCount => Units.Count;

        /// <summary>Gets or sets the filter expression</summary>
        public string FilterExpression { get; set; }

        /// <summary>Gets the current item</summary>
        public T CurrentItem => Units.Current;

        #endregion

        #region Collection Properties

        /// <summary>Gets or sets the filtered units.</summary>
        /// <value>The filtered units.</value>
        /// <remarks>
        /// This property represents a collection of units that have been filtered based on certain criteria.
        /// When setting the value, the property will unsubscribe from the previous collection's PropertyChanged event and CollectionChanged event, if applicable.
        /// It will then subscribe to the new collection's PropertyChanged event and CollectionChanged event, if applicable.
        /// </remarks>
        public ObservableBindingList<T> FilteredUnits
        {
            get { return _filteredunits; }
            set
            {
                if (_filteredunits != value) // Check if it's a new collection
                {
                    if (_filteredunits != null)
                    {
                        foreach (var item in _filteredunits)
                        {
                            item.PropertyChanged -= ItemPropertyChangedHandler;
                        }
                        _filteredunits.CollectionChanged -= Units_CollectionChanged;
                    }
                }
                _filteredunits = value;

                if (_filteredunits != null)
                {
                    foreach (var item in _filteredunits)
                    {
                        item.PropertyChanged += ItemPropertyChangedHandler;
                    }
                    _filteredunits.CollectionChanged += Units_CollectionChanged;
                }
            }
        }

        /// <summary>Gets or sets the collection of units.</summary>
        /// <value>The collection of units.</value>
        /// <remarks>
        /// If the filter is applied, the filtered units collection will be returned.
        /// Otherwise, the original units collection will be returned.
        /// </remarks>
#if WINDOWS
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
#endif
        public ObservableBindingList<T> Units
        {
            get
            {
                return IsFilterOn ? _filteredunits : _units;
            }
            set
            {
                SetUnits(value);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Parameterless constructor for designer support
        /// </summary>
        public UnitofWork() : base()
        {
            // Initialization logic specific for design-time
            // This can be empty or can set default values
        }

        /// <summary>
        /// Initializes a new instance of the UnitofWork class
        /// </summary>
        /// <param name="dMEEditor">The IDMEEditor instance</param>
        /// <param name="datasourceName">The name of the data source</param>
        /// <param name="entityName">The name of the entity</param>
        public UnitofWork(IDMEEditor dMEEditor, string datasourceName, string entityName)
        {
            IsInListMode = false;
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
            EntityName = entityName;
            
            InitializeHelpers();
            
            if (OpenDataSource())
            {
                init();
            }
            
            RegisterEntityType();
            _suppressNotification = false;
        }

        /// <summary>
        /// Initializes a new instance of the UnitofWork class with entity structure
        /// </summary>
        /// <param name="dMEEditor">The IDMEEditor instance</param>
        /// <param name="datasourceName">The name of the data source</param>
        /// <param name="entityName">The name of the entity</param>
        /// <param name="entityStructure">The entity structure</param>
        public UnitofWork(IDMEEditor dMEEditor, string datasourceName, string entityName, EntityStructure entityStructure)
        {
            IsInListMode = false;
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
            EntityName = entityName;
            EntityStructure = entityStructure;
            
            InitializeHelpers();
            
            if (OpenDataSource())
            {
                init();
            }
            
            RegisterEntityType();
            _suppressNotification = false;
        }

        /// <summary>
        /// Initializes a new instance of the UnitofWork class for list mode operations
        /// </summary>
        /// <param name="dMEEditor">The IDMEEditor instance</param>
        /// <param name="isInListMode">A boolean indicating whether the unit is in list mode</param>
        /// <param name="ts">The ObservableBindingList of type T</param>
        public UnitofWork(IDMEEditor dMEEditor, bool isInListMode, ObservableBindingList<T> ts)
        {
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            IsInListMode = isInListMode;
            
            InitializeHelpers();
            init();
            
            EntityStructure = new EntityStructure();
            EntityStructure.Fields = new List<EntityField>();
            EntityStructure = DMEEditor.Utilfunction.GetEntityStructureFromList(ts.ToList());
            Units = ts;
            
            RegisterEntityType();
            _suppressNotification = false;
        }

        /// <summary>
        /// Initializes a new instance of the UnitofWork class with primary key
        /// </summary>
        /// <param name="dMEEditor">The IDMEEditor instance</param>
        /// <param name="datasourceName">The name of the data source</param>
        /// <param name="entityName">The name of the entity</param>
        /// <param name="primarykey">The primary key</param>
        public UnitofWork(IDMEEditor dMEEditor, string datasourceName, string entityName, string primarykey)
        {
            IsInListMode = false;
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
            PrimaryKey = primarykey;
            EntityName = entityName;
            
            InitializeHelpers();
            
            if (OpenDataSource())
            {
                init();
            }
            
            InitializePrimaryKey();
            RegisterEntityType();
            _suppressNotification = false;
        }

        /// <summary>
        /// Initializes a new instance of the UnitofWork class with entity structure and primary key
        /// </summary>
        /// <param name="dMEEditor">The IDMEEditor instance</param>
        /// <param name="datasourceName">The name of the data source</param>
        /// <param name="entityName">The name of the entity</param>
        /// <param name="entityStructure">The structure of the entity</param>
        /// <param name="primarykey">The primary key of the entity</param>
        public UnitofWork(IDMEEditor dMEEditor, string datasourceName, string entityName, EntityStructure entityStructure, string primarykey)
        {
            IsInListMode = false;
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
            EntityName = entityName;
            EntityStructure = entityStructure;
            PrimaryKey = primarykey;
            
            InitializeHelpers();
            
            if (OpenDataSource())
            {
                init();
            }
            
            InitializePrimaryKey();
            RegisterEntityType();
            _suppressNotification = false;
        }

        /// <summary>
        /// Initializes a new instance of the UnitofWork class for list mode with primary key
        /// </summary>
        /// <param name="dMEEditor">The IDMEEditor instance</param>
        /// <param name="isInListMode">A boolean indicating whether the unit is in list mode</param>
        /// <param name="ts">The ObservableBindingList of type T</param>
        /// <param name="primarykey">The primary key string</param>
        public UnitofWork(IDMEEditor dMEEditor, bool isInListMode, ObservableBindingList<T> ts, string primarykey)
        {
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            IsInListMode = isInListMode;
            
            InitializeHelpers();
            init();
            
            EntityStructure = new EntityStructure();
            EntityStructure.Fields = new List<EntityField>();
            EntityStructure = DMEEditor.Utilfunction.GetEntityStructureFromList(ts.ToList());
            
            if (string.IsNullOrEmpty(EntityName))
            {
                EntityName = typeof(T).FullName;
                EntityStructure.EntityName = EntityName;
            }
            
            Units = ts;
            PrimaryKey = primarykey;
            
            InitializePrimaryKey();
            RegisterEntityType();
            _suppressNotification = false;
        }

        #endregion

        #region Initialization Methods

        /// <summary>
        /// Initializes all helper instances
        /// </summary>
        private void InitializeHelpers()
        {
            try
            {
                _dataHelper = new UnitofWorkDataHelper<T>(DMEEditor);
                _validationHelper = new UnitofWorkValidationHelper<T>(DMEEditor, EntityStructure, PrimaryKey);
                _defaultsHelper = new UnitofWorkDefaultsHelper<T>(DMEEditor, DatasourceName, EntityName);
                
                // Initialize other helpers as they are created
                // _stateHelper = new UnitofWorkStateHelper<T>(DMEEditor);
                // _eventHelper = new UnitofWorkEventHelper<T>(DMEEditor);
                // _collectionHelper = new UnitofWorkCollectionHelper<T>(DMEEditor);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("UnitofWork", 
                    $"Error initializing helpers: {ex.Message}", 
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Initializes primary key information
        /// </summary>
        private void InitializePrimaryKey()
        {
            if (Units == null || Units.Count == 0)
            {
                T doc = new();
                getPrimaryKey(doc);
            }
            else
            {
                getPrimaryKey(Units.FirstOrDefault());
            }
        }

        /// <summary>
        /// Registers the entity type in the type cache
        /// </summary>
        private void RegisterEntityType()
        {
            if (!DMTypeBuilder.DataSourceNameSpace.ContainsValue(typeof(T).FullName))
            {
                DMTypeBuilder.DataSourceNameSpace.Add(EntityName, typeof(T).FullName);
            }
            
            if (!DMTypeBuilder.typeCache.ContainsValue(typeof(T)))
            {
                DMTypeBuilder.typeCache.Add(typeof(T).FullName, typeof(T));
            }
        }

        /// <summary>
        /// Initializes the object
        /// </summary>
        /// <remarks>
        /// This method performs initialization tasks for the object. It first validates all necessary conditions
        /// using the Validateall() method. If the validation fails, the method returns without performing any further
        /// initialization. If the validation succeeds, the method proceeds to clear any existing data using the Clear() method.
        /// </remarks>
        private void init()
        {
            if (!Validateall())
            {
                return;
            }
            Clear();
        }

        /// <summary>
        /// Determines the primary key of a document
        /// </summary>
        /// <param name="doc">The document</param>
        /// <remarks>
        /// If the primary key is already set, this method does nothing.
        /// Otherwise, it attempts to find the primary key property of the document using the provided primary key name.
        /// If the primary key property is found, it checks if its type is string and sets the IsPrimaryKeyString flag accordingly.
        /// </remarks>
        private void getPrimaryKey(T doc)
        {
            if (!string.IsNullOrEmpty(PrimaryKey))
            {
                if (PKProperty == null)
                {
                    PKProperty = doc.GetType().GetProperty(PrimaryKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                }
                if (PKProperty != null)
                {
                    if (PKProperty.PropertyType == typeof(string))
                    {
                        IsPrimaryKeyString = true;
                    }
                    else
                        IsPrimaryKeyString = false;
                }
            }
        }

        #endregion

        #region Core Utility Methods

        /// <summary>
        /// Sets the units collection and raises the PropertyChanged event for the Units property
        /// </summary>
        /// <param name="value">The new units collection</param>
        private void SetUnits(ObservableBindingList<T> value)
        {
            if (_units != value)
            {
                DetachHandlers(_units);
                _units = value;
                AttachHandlers(_units);

                // Create a deep copy using the data helper
                if (_dataHelper != null && value != null)
                {
                    var clonedItems = new List<T>();
                    foreach (var item in value)
                    {
                        var clonedItem = _dataHelper.CloneEntity(item);
                        if (clonedItem != null)
                        {
                            clonedItems.Add(clonedItem);
                        }
                    }
                    Tempunits = new ObservableBindingList<T>(clonedItems);
                }
            }
        }

        /// <summary>
        /// Detaches event handlers from the specified collection and its items
        /// </summary>
        /// <param name="collection">The collection to detach event handlers from</param>
        private void DetachHandlers(ObservableBindingList<T> collection)
        {
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    item.PropertyChanged -= ItemPropertyChangedHandler;
                }
                collection.CollectionChanged -= Units_CollectionChanged;
            }
        }

        /// <summary>
        /// Attaches event handlers to a collection and its items
        /// </summary>
        /// <param name="collection">The collection to attach event handlers to</param>
        /// <remarks>
        /// This method attaches a PropertyChanged event handler to each item in the collection,
        /// and a CollectionChanged event handler to the collection itself.
        /// </remarks>
        private void AttachHandlers(ObservableBindingList<T> collection)
        {
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    item.PropertyChanged += ItemPropertyChangedHandler;
                }
                collection.CollectionChanged += Units_CollectionChanged;
            }
        }

        /// <summary>
        /// Opens the data source
        /// </summary>
        /// <returns>True if the data source is successfully opened, false otherwise</returns>
        private bool OpenDataSource()
        {
            bool retval = true;
            if (IsInListMode)
            {
                return true;
            }
            if (DataSource == null)
            {
                if (!string.IsNullOrEmpty(DatasourceName))
                {
                    DataSource = DMEEditor.GetDataSource(DatasourceName);
                    if (DataSource == null)
                    {
                        DMEEditor.AddLogMessage("Beep", $"Error Opening DataSource in UnitofWork {DatasourceName}", DateTime.Now, -1, DatasourceName, Errors.Failed);
                        retval = false;
                    }
                    else
                        retval = true;
                }
            }
            return retval;
        }

        /// <summary>
        /// Validates all necessary conditions before performing an operation
        /// </summary>
        /// <returns>True if all conditions are valid, otherwise false</returns>
        private bool Validateall()
        {
            if (Ivalidated)
            {
                return true;
            }
            bool retval = true;
            if (IsInListMode)
            {
                return true;
            }
            if (!OpenDataSource())
            {
                DMEEditor.AddLogMessage("Beep", $"Error Opening DataSource in UnitofWork {DatasourceName}", DateTime.Now, -1, DatasourceName, Errors.Failed);
                retval = false;
            }
            if (EntityStructure == null)
            {
                EntityStructure = DataSource.GetEntityStructure(EntityName, false);
            }
            if (EntityStructure != null)
            {
                if (EntityStructure.PrimaryKeys.Count == 0)
                {
                    if (!string.IsNullOrEmpty(PrimaryKey))
                    {
                        EntityStructure.PrimaryKeys.Add(new EntityField() { fieldname = PrimaryKey, EntityName = EntityStructure.EntityName });
                    }
                }
            }

            if (EntityStructure == null)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Entity Not Found in UnitofWork {EntityName}", DateTime.Now, -1, EntityName, Errors.Failed);
                retval = false;
            }
            if (EntityStructure.PrimaryKeys.Count == 0)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Entity dont have a primary key in UnitofWork {EntityName}", DateTime.Now, -1, EntityName, Errors.Failed);
                retval = false;
            }
            Ivalidated = true;
            return retval;
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Event for property changed notifications
        /// </summary>
        public virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        /// <param name="propertyName">The name of the property that changed</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (_suppressNotification) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the object
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    DetachHandlers(_units);
                    DetachHandlers(_filteredunits);
                    
                    _defaultsHelper = null;
                    _validationHelper = null;
                    _dataHelper = null;
                    _stateHelper = null;
                    _eventHelper = null;
                    _collectionHelper = null;
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}