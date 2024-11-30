using TheTechIdea.Beep.Editor;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Connections;
using TheTechIdea.Beep.Addin;




namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Represents a unit of work for managing entities of type T.
    /// </summary>
    /// <typeparam name="T">The type of entity.</typeparam>
#if WINDOWS &&  NET6_0_OR_GREATER
    [DesignerCategory("TheTechIdea")]
    [ToolboxItem(true)]
    [ToolboxBitmap(typeof(UnitOfWork<>), "TheTechIdea.Beep.GFX.unitofwork.ico")]
    [DisplayName("Unit of Work")]
#endif
    public class UnitofWork<T> : IUnitofWork<T>, INotifyPropertyChanged where T : Entity, new()
    {
        private Stack<ChangeLogEntry<T>> changeLog = new Stack<ChangeLogEntry<T>>();
        
        /// <summary>Indicates whether notifications should be suppressed.</summary>
        private bool _suppressNotification = false;

        /// <summary>A source for creating cancellation tokens.</summary>
        CancellationTokenSource tokenSource;
        /// <summary>A token that can be used to request cancellation of an operation.</summary>
        CancellationToken token;
        /// <summary>Indicates whether the primary key is a string.</summary>
        private bool IsPrimaryKeyString = false;
        /// <summary>Indicates whether the object has been validated.</summary>
        /// <remarks>
        /// This property is used to track whether the object has been validated or not.
        /// It is initially set to false and should be set to true after the validation process is completed.
        /// </remarks>
        private bool Ivalidated = false;
        /// <summary>Indicates whether a new record is being created.</summary>
        private bool IsNewRecord = false;

        /// <summary>Indicates whether the filter is currently turned on.</summary>
        private bool IsFilterOn = false;

        /// <summary>Gets a value indicating whether the object is dirty.</summary>
        /// <returns>True if the object is dirty; otherwise, false.</returns>
        public bool IsDirty { get { return GetIsDirty(); } }
        #region "Inotify"


        ///// <summary>Raises the PropertyChanged event.</summary>
        ///// <param name="propertyName">The name of the property that has changed.</param>
        //protected virtual void OnPropertyChanged(string propertyName)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        #endregion
        #region "Collections"
        /// <summary>A private observable binding list of type T.</summary>
        private ObservableBindingList<T> Tempunits;

        /// <summary>The collection of units.</summary>
        private ObservableBindingList<T> _units;


        /// <summary>The filtered units collection.</summary>
        private ObservableBindingList<T> _filteredunits;

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
                            item.PropertyChanged -= ItemPropertyChangedHandler; // Remove previous event handlers
                        }
                        _filteredunits.CollectionChanged -= Units_CollectionChanged;
                    }
                }
                _filteredunits = value;

                if (_filteredunits != null)
                {
                    foreach (var item in _filteredunits)
                    {
                        item.PropertyChanged += ItemPropertyChangedHandler; // Make sure you attach this

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

        /// <summary>Sets the units collection and raises the PropertyChanged event for the Units property.</summary>
        /// <param name="value">The new units collection.</param>
        private void SetUnits(ObservableBindingList<T> value)
        {
            if (_units != value)
            {
                DetachHandlers(_units);
                _units = value;
                AttachHandlers(_units);

                // Create a deep copy of _units and assign it to Tempunits
                Tempunits = new ObservableBindingList<T>(value.Select(item => CloneItem(item)).ToList());
            }
        }

        private T CloneItem(T item)
        {
            // Assuming T is a reference type with a parameterless constructor.
            // You might need to implement this more carefully depending on the complexity of T.
            var clonedItem = new T();

            // Use reflection or manual copying to clone the properties.
            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanWrite)
                {
                    var value = property.GetValue(item);
                    property.SetValue(clonedItem, value);
                }
            }

            return clonedItem;
        }
        /// <summary>Detaches event handlers from the specified collection and its items.</summary>
        /// <param name="collection">The collection to detach event handlers from.</param>
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

        /// <summary>Attaches event handlers to a collection and its items.</summary>
        /// <param name="collection">The collection to attach event handlers to.</param>
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
        public T CurrentItem => Units.Current;
        
        #endregion
        #region "Properties"
        public bool IsInListMode { get; set; } = false;
        private Dictionary<int, EntityState> _entityStates = new Dictionary<int, EntityState>();
        private Dictionary<T, EntityState> _deletedentities = new Dictionary<T, EntityState>();
        public List<T> DeletedUnits { get; set; } = new List<T>();
        public Dictionary<int, string> InsertedKeys { get; set; } = new Dictionary<int, string>();
        public Dictionary<int, string> UpdatedKeys { get; set; } = new Dictionary<int, string>();
        public Dictionary<int, string> DeletedKeys { get; set; } = new Dictionary<int, string>();

        Stack<Tuple<T, int>> undoDeleteStack = new Stack<Tuple<T, int>>();
       
        public bool IsIdentity { get; set; }= false;
      
        public string Sequencer { get; set; }
        public string DatasourceName { get; set; }
      
        public EntityStructure EntityStructure { get; set; }
        public IDMEEditor DMEEditor { get; }
        public IDataSource DataSource { get; set; }
        public string EntityName { get; set; }
        public Type EntityType { get; set; }
        string _primarykey;
        public string PrimaryKey { get { return _primarykey; } set { _primarykey = value; } }
        public string GuidKey { get; set; }
        PropertyInfo PKProperty = null;
        PropertyInfo CurrentProperty = null;
        PropertyInfo Guidproperty = null;
        int keysidx;
        private bool disposedValue;

        // Paging and filtering properties
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public int TotalItemCount => Units.Count;
        public string FilterExpression { get; set; }
        #endregion
        #region "Constructors"
        // Parameterless constructor for designer support
        public UnitofWork() : base()
        {
            // Initialization logic specific for design-time
            // This can be empty or can set default values
        }
        public UnitofWork(IDMEEditor dMEEditor, string datasourceName, string entityName)
        {
            IsInListMode = false;
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
            EntityName = entityName;
            if (OpenDataSource())
            {
                init();
            }
            if (!DMTypeBuilder.DataSourceNameSpace.ContainsValue(typeof(T).FullName))
            {
                DMTypeBuilder.DataSourceNameSpace.Add(EntityName, typeof(T).FullName);

            }
            if (!DMTypeBuilder.typeCache.ContainsValue(typeof(T)))
            {

                DMTypeBuilder.typeCache.Add(typeof(T).FullName, typeof(T));
            }
            _suppressNotification = false;
        }

        public UnitofWork(IDMEEditor dMEEditor, string datasourceName, string entityName, EntityStructure entityStructure)
        {
            IsInListMode = false;
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
            EntityName = entityName;
            EntityStructure = entityStructure;
            if (OpenDataSource())
            {
                init();
            }
            if (!DMTypeBuilder.DataSourceNameSpace.ContainsValue(typeof(T).FullName))
            {
                DMTypeBuilder.DataSourceNameSpace.Add(EntityName, typeof(T).FullName);
               
            }
            if (!DMTypeBuilder.typeCache.ContainsValue(typeof(T)))
            {
               
                DMTypeBuilder.typeCache.Add(typeof(T).FullName, typeof(T));
            }
            _suppressNotification = false;
        }

        public UnitofWork(IDMEEditor dMEEditor, bool isInListMode, ObservableBindingList<T> ts)
        {
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            IsInListMode = isInListMode;
            init();
            EntityStructure = new EntityStructure();
            EntityStructure.Fields = new List<EntityField>();
            EntityStructure = DMEEditor.Utilfunction.GetEntityStructureFromList<T>(ts.ToList());
            Units = ts;
            if (!DMTypeBuilder.DataSourceNameSpace.ContainsValue(typeof(T).FullName))
            {
                DMTypeBuilder.DataSourceNameSpace.Add(EntityName, typeof(T).FullName);

            }
            if (!DMTypeBuilder.typeCache.ContainsValue(typeof(T)))
            {

                DMTypeBuilder.typeCache.Add(typeof(T).FullName, typeof(T));
            }
            _suppressNotification = false;
        }

        /// <summary>Initializes a new instance of the UnitofWork class.</summary>
        /// <param name="dMEEditor">The IDMEEditor instance.</param>
        /// <param name="datasourceName">The name of the data source.</param>
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="primaryKey">The primary key.</param>
        public UnitofWork(IDMEEditor dMEEditor, string datasourceName, string entityName, string primarykey)
        {
            IsInListMode = false;
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
            PrimaryKey = primarykey;
            //  EntityStructure = new EntityStructure();
            EntityName = entityName;
            if (OpenDataSource())
            {
                init();
            }
            if (Units == null || Units.Count == 0)
            {
                T doc = new();
                getPrimaryKey(doc);
            }
            else
            {
                getPrimaryKey(Units.FirstOrDefault());
            }
            if (!DMTypeBuilder.DataSourceNameSpace.ContainsValue(typeof(T).FullName))
            {
                DMTypeBuilder.DataSourceNameSpace.Add(EntityName, typeof(T).FullName);

            }
            if (!DMTypeBuilder.typeCache.ContainsValue(typeof(T)))
            {

                DMTypeBuilder.typeCache.Add(typeof(T).FullName, typeof(T));
            }

            _suppressNotification = false;
        }
        /// <summary>Initializes a new instance of the UnitOfWork class.</summary>
        /// <param name="dMEEditor">The IDMEEditor instance.</param>
        /// <param name="datasourceName">The name of the data source.</param>
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="entityStructure">The structure of the entity.</param>
        /// <param name="primaryKey">The primary key of the entity.</param>
        /// <remarks>
        /// This constructor initializes the UnitOfWork with the provided parameters. It sets the IsInListMode property to false and suppresses notifications temporarily.
        /// It assigns the IDMEEditor instance, data source name, entity name, entity structure
        public UnitofWork(IDMEEditor dMEEditor, string datasourceName, string entityName, EntityStructure entityStructure, string primarykey)
        {
            IsInListMode = false;
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
            EntityName = entityName;
            EntityStructure = entityStructure;
            PrimaryKey = primarykey;
            if (OpenDataSource())
            {
                init();

            }
            PrimaryKey = primarykey;
            if (Units == null || Units.Count == 0)
            {
                T doc = new();
                getPrimaryKey(doc);
            }
            else
            {
                getPrimaryKey(Units.FirstOrDefault());
            }
            if (!DMTypeBuilder.DataSourceNameSpace.ContainsValue(typeof(T).FullName))
            {
                DMTypeBuilder.DataSourceNameSpace.Add(EntityName, typeof(T).FullName);

            }
            if (!DMTypeBuilder.typeCache.ContainsValue(typeof(T)))
            {

                DMTypeBuilder.typeCache.Add(typeof(T).FullName, typeof(T));
            }
            _suppressNotification = false;
        }
        /// <summary>Initializes a new instance of the UnitofWork class.</summary>
        /// <param name="dMEEditor">The IDMEEditor instance.</param>
        /// <param name="isInListMode">A boolean indicating whether the unit is in list mode.</param>
        /// <param name="ts">The ObservableBindingList of type T.</param>
        /// <param name="primarykey">The primary key string.</param>
        /// <remarks>
        /// This constructor initializes a new instance of the UnitofWork class with the provided parameters.
        /// It sets the _suppressNotification flag to true, assigns the IDMEEditor instance, sets the IsInListMode property,
        /// creates a new EntityStructure, and calls the init()
        public UnitofWork(IDMEEditor dMEEditor, bool isInListMode, ObservableBindingList<T> ts, string primarykey)
        {
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            IsInListMode = isInListMode;
            init();
            EntityStructure = new EntityStructure();
            EntityStructure.Fields = new List<EntityField>();
            EntityStructure = DMEEditor.Utilfunction.GetEntityStructureFromList<T>(ts.ToList());
            if (string.IsNullOrEmpty(EntityName))
            {
                EntityName = typeof(T).FullName;
                EntityStructure.EntityName= EntityName;
            }
            Units = ts;
            PrimaryKey = primarykey;
            if (ts == null || ts.Count == 0)
            {
                T doc = new();
                getPrimaryKey(doc);
            }
            else
            {
                getPrimaryKey(ts.FirstOrDefault());
            }
            if (!DMTypeBuilder.DataSourceNameSpace.ContainsValue(typeof(T).FullName))
            {
                DMTypeBuilder.DataSourceNameSpace.Add(EntityName, typeof(T).FullName);

            }
            if (!DMTypeBuilder.typeCache.ContainsValue(typeof(T)))
            {

                DMTypeBuilder.typeCache.Add(typeof(T).FullName, typeof(T));
            }
            _suppressNotification = false;
        }

        #endregion "Constructors"
        #region "Events"
        public event EventHandler<UnitofWorkParams> PreInsert;
        public event EventHandler<UnitofWorkParams> PostCreate;
        public event EventHandler<UnitofWorkParams> PostEdit;
        public event EventHandler<UnitofWorkParams> PreDelete;
        public event EventHandler<UnitofWorkParams> PreUpdate;
        public event EventHandler<UnitofWorkParams> PreQuery;
        public event EventHandler<UnitofWorkParams> PostQuery;
        public event EventHandler<UnitofWorkParams> PostInsert;
        public event EventHandler<UnitofWorkParams> PostUpdate;
        // Event declarations
        public event EventHandler CurrentChanged;
        public event EventHandler ListChanged;
        public event EventHandler FilterApplied;
        public event EventHandler PageChanged;

        #endregion
        #region "Misc Methods"
        /// <summary>Clears the data in the collection.</summary>
        /// <remarks>
        /// This method clears the data in the collection by performing the following steps:
        /// 1. Sets the <c>IsFilterOn</c> property to <c>false</c>.
        /// 2. Clears the <c>Units</c> collection if it is not null.
        /// 3. Initializes a new instance of the <c>_deletedentities</c> dictionary.
        /// 4. If the collection is not in list mode, sets the <c>EntityType</c> property to the entity type obtained from the data source.
        /// </remarks>
        public void Clear()
        {

            _suppressNotification = true;
            IsFilterOn = false;
            if (Units != null)
            {
                _units.Clear();
            }
            else
                Units = new ObservableBindingList<T>();
            if (FilteredUnits != null)
            {
                FilteredUnits.Clear();
            }
            else
                _filteredunits = new ObservableBindingList<T>();
            keysidx = 0;

            DeletedUnits = new List<T>();
            InsertedKeys = new Dictionary<int, string>();
            UpdatedKeys = new Dictionary<int, string>();
            DeletedKeys = new Dictionary<int, string>();
            _entityStates = new Dictionary<int, EntityState>();
            _deletedentities = new Dictionary<T, EntityState>();
            if (!IsInListMode)
            {
                if (EntityType != null)
                {
                    EntityType = DataSource.GetEntityType(EntityName);
                }

            }
            _suppressNotification = false;
        }

        /// <summary>Determines the primary key of a document.</summary>
        /// <typeparam name="T">The type of the document.</typeparam>
        /// <param name="doc">The document.</param>
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
        /// <summary>Initializes the object.</summary>
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

        /// <summary>Handles the event when the current unit is changed.</summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method is called when the current unit is changed. It checks if the notification is suppressed and returns if it is.
        /// </remarks>
        private void Units_CurrentChanged(object sender, EventArgs e)
        {
            if (_suppressNotification)
            {
                return;
            }
            OnPropertyChanged("CurrentItem");
        }

        /// <summary>Sets the value of the primary key property for the specified entity.</summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity object.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="ArgumentException">Thrown when the primary key property is not found on the entity.</exception>
        public void SetIDValue(T entity, object value)
        {
            if (!Validateall())
            {
                return;
            }

            var propertyInfo = entity.GetType().GetProperty(PrimaryKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
            {
                throw new ArgumentException($"Property '{PrimaryKey}' not found on '{entity.GetType().Name}'");
            }

            // If you want to handle type mismatch more gracefully, you should add some checks here.
            propertyInfo.SetValue(entity, Convert.ChangeType(value, propertyInfo.PropertyType), null);
        }
        /// <summary>Retrieves the value of the primary key property for the specified entity.</summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity object.</param>
        /// <returns>The value of the primary key property.</returns>
        /// <remarks>
        /// If the primary key property is not valid or cannot be retrieved, null is returned.
        /// </remarks>
        public object GetIDValue(T entity)
        {
            if (!Validateall())
            {
                return null;
            }
            var idValue = entity.GetType().GetProperty(PrimaryKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.GetValue(entity, null);

            return idValue;

        }
        /// <summary>Returns the index of an entity with the specified ID.</summary>
        /// <param name="id">The ID of the entity.</param>
        /// <returns>The index of the entity in the collection, or -1 if not found.</returns>
        /// <remarks>
        /// This method first validates all entities in the collection using the Validateall() method.
        /// If validation fails, -1 is returned.
        /// Otherwise, it searches for an entity with a matching ID using reflection.
        /// If found, it returns the index of the entity in the collection.
        /// If not found, -1 is returned.
        /// </remarks>
        public int Getindex(string id)
        {
            if (!Validateall())
            {
                return -1;
            }
            int index = -1;

            var tentity = Units.FirstOrDefault(x => x.GetType().GetProperty(PrimaryKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.GetValue(x, null).ToString() == id.ToString());
            if (tentity != null)
            {
                index = Units.IndexOf(tentity);
                // Now index holds the position of the entity in the Units collection.
            }
            return index;

        }
        /// <summary>Returns the index of the specified entity in the list of units.</summary>
        /// <param name="entity">The entity to find the index of.</param>
        /// <returns>The index of the entity in the list of units. Returns -1 if the list is not valid.</returns>
        public int Getindex(T entity)
        {
            if (!Validateall())
            {
                return -1;
            }
           
            int index = _units.IndexOf(entity);
            return index;

        }
        /// <summary>Returns the Last identity of the specified entity in the list of units.</summary>
        /// <param name="entity">The entity to find the Idnetity of.</param>
        /// <returns>The Identity of the entity in the list of units. Returns -1 if the list is not valid.</returns>
        public double GetLastIdentity()
        {
            double identity = -1;
            
            if (IsIdentity)
            {
                identity =DataSource.GetScalar(RDBMSHelper.GenerateFetchLastIdentityQuery(DataSource.DatasourceType));
            }
            return identity;
        }
        #endregion
        #region "CRUD no KEY"
        public T Read(Func<T, bool> predicate)
        {
            if (!Validateall())
            {
                return default(T);
            }
            return Units.FirstOrDefault(predicate);
        }
        public Task<ObservableBindingList<T>> MultiRead(Func<T, bool> predicate)
        {
            if (!Validateall())
            {
                return null;
            }
            return  Task.FromResult<ObservableBindingList<T>>(new ObservableBindingList<T>(Units.Where(predicate)));
        }
        public ErrorsInfo Update(Func<T, bool> predicate, T updatedEntity)
        {
            ErrorsInfo errorsInfo = new ErrorsInfo();
            if (!Validateall())
            {
                errorsInfo.Message = "Validation Failed";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
            var entity = Units.FirstOrDefault(predicate);
            if (entity != null)
            {
                var index = Units.IndexOf(entity);
                Units[index] = updatedEntity;
                changeLog.Push(new ChangeLogEntry<T>
                {
                    Entity = updatedEntity,
                    ChangeType = ChangeType.Update,
                    OriginalValues = GetEntityValues(entity)
                });
                errorsInfo.Message = "Update Done";
                errorsInfo.Flag = Errors.Ok;
                return errorsInfo;
            }
            else
            {
                errorsInfo.Message = "Object not found";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
        }
        public ErrorsInfo Delete(Func<T, bool> predicate)
        {
            ErrorsInfo errorsInfo = new ErrorsInfo();
            if (!Validateall())
            {
                errorsInfo.Message = "Validation Failed";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
            var entity = Units.FirstOrDefault(predicate);
            if (entity != null)
            {
                Units.Remove(entity);
                changeLog.Push(new ChangeLogEntry<T>
                {
                    Entity = entity,
                    ChangeType = ChangeType.Delete
                });
                errorsInfo.Message = "Delete Done";
                errorsInfo.Flag = Errors.Ok;
                return errorsInfo;
            }
            else
            {
                errorsInfo.Message = "Object not found";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
        }
        private Dictionary<string, object> GetEntityValues(T entity)
        {
            var values = new Dictionary<string, object>();
            foreach (var prop in typeof(T).GetProperties())
            {
                values[prop.Name] = prop.GetValue(entity);
            }
            return values;
        }
        #endregion "CRUD no KEY"
        #region "CRUD Operations"
        /// <summary>Updates a document asynchronously.</summary>
        /// <param name="doc">The document to be updated.</param>
        /// <returns>An object containing information about any errors that occurred during the update.</returns>
        public async Task<IErrorsInfo> UpdateAsync(T doc)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            if (!IsRequirmentsValidated())
            {
                return DMEEditor.ErrorObject;
            }
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false };
            PreUpdate?.Invoke(doc, ps);
            if (ps.Cancel)
            {
                return DMEEditor.ErrorObject;
            }
            if (doc == null)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Object is null";
                return DMEEditor.ErrorObject;
            }
            IErrorsInfo retval = await UpdateDoc(doc);
            return retval;
        }
        /// <summary>Inserts a document asynchronously.</summary>
        /// <param name="doc">The document to be inserted.</param>
        /// <returns>An object containing information about any errors that occurred during the insertion process.</returns>
        public async Task<IErrorsInfo> InsertAsync(T doc)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            if (!IsRequirmentsValidated())
            {
                return DMEEditor.ErrorObject;
            }
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false };
            PreInsert?.Invoke(doc, ps);
            if (ps.Cancel)
            {
                return DMEEditor.ErrorObject;
            }
            if (doc == null)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Object is null";
                return DMEEditor.ErrorObject;
            }
            IErrorsInfo retval = await InsertDoc(doc);
            return retval;
        }
        /// <summary>Deletes a document asynchronously.</summary>
        /// <param name="doc">The document to be deleted.</param>
        /// <returns>An object containing information about any errors that occurred during the deletion process.</returns>
        public async Task<IErrorsInfo> DeleteAsync(T doc)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            if (!IsRequirmentsValidated())
            {
                return DMEEditor.ErrorObject;
            }
            if (doc == null)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Object is null";
                return DMEEditor.ErrorObject;
            }
            
            IErrorsInfo retval = await DeleteDoc(doc);

            return retval;
        }
        /// <summary>Inserts a document into the data source.</summary>
        /// <typeparam name="T">The type of the document.</typeparam>
        /// <param name="doc">The document to insert.</param>
        /// <returns>An object containing information about any errors that occurred during the insertion.</returns>
        public Task<IErrorsInfo> InsertDoc(T doc)
        {

            string[] classnames = doc.ToString().Split(new Char[] { ' ', ',', '.', '-', '\n', '\t' });
            string cname = classnames[classnames.Count() - 1];

            if (!string.IsNullOrEmpty(GuidKey))
            {
                if (Guidproperty == null)
                {
                    Guidproperty = doc.GetType().GetProperty(GuidKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                }
                Guidproperty.SetValue(Guid.NewGuid().ToString(), null);

            }
            IErrorsInfo retval;
            if (!IsInListMode)
            {
                retval = DataSource.InsertEntity(cname, doc);
            }
            else
            {
                int idx = Getindex(doc);
                if (idx > -1)
                {
                    _units[Getindex(doc)] = doc;
                    retval = new ErrorsInfo { Flag = Errors.Ok, Message = "object already there, updated" };
                }
                else
                {
                    _units.Add(doc);
                    retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Add object to list" };
                }

            }
            return Task.FromResult<IErrorsInfo>(retval);
        }
        /// <summary>Updates a document and returns information about any errors that occurred.</summary>
        /// <param name="doc">The document to update.</param>
        /// <returns>An object containing information about any errors that occurred during the update.</returns>
        public Task<IErrorsInfo> UpdateDoc(T doc)
        {
            IErrorsInfo retval;
            string[] classnames = doc.ToString().Split(new Char[] { ' ', ',', '.', '-', '\n', '\t' });
            string cname = classnames[classnames.Count() - 1];
            if (!IsInListMode)
            {
                retval = DataSource.UpdateEntity(cname, doc);
            }else
            {
                int idx = Getindex(doc);
                if (idx > -1)
                {
                    _units[Getindex(doc)] = doc;
                    retval=new ErrorsInfo { Flag = Errors.Ok, Message = "Update Done" };
                }else
                {
                    retval=new ErrorsInfo { Flag = Errors.Failed, Message = "Object not found - Coud not Update" };
                }
                
            }
            
            return Task.FromResult<IErrorsInfo>(retval);
        }
        /// <summary>Deletes a document and returns information about any errors that occurred.</summary>
        /// <param name="doc">The document to delete.</param>
        /// <returns>An object containing information about any errors that occurred during the deletion process.</returns>
        public Task<IErrorsInfo> DeleteDoc(T doc)
        {
            string[] classnames = doc.ToString().Split(new Char[] { ' ', ',', '.', '-', '\n', '\t' });
            string cname = classnames[classnames.Count() - 1];
            IErrorsInfo retval ;
            Tracking tracking = _units.GetTrackingITem(doc);
           
            if (!IsInListMode)
            {
                
                retval =  DataSource.DeleteEntity(cname, doc);
            }
            else
            {
                int idx = Getindex(doc);
                if (idx > -1)
                {
                    _units.RemoveAt(idx);
                    retval = new ErrorsInfo { Flag = Errors.Ok, Message = "Deleted is Done" };
                }
                else
                {
                    retval = new ErrorsInfo { Flag = Errors.Failed, Message = "Object not found - Coud not Delete" };
                }

            }
         

            return Task.FromResult<IErrorsInfo>(retval);
        }
        /// <summary>Adds a new entity to the collection and subscribes to its PropertyChanged event.</summary>
        /// <param name="entity">The entity to be added.</param>
        /// <remarks>
        /// This method first validates all entities in the collection using the Validateall method.
        /// If the validation fails, the method returns without adding the entity.
        /// Otherwise, the entity is added to the Units collection and the ItemPropertyChangedHandler is subscribed to its PropertyChanged event.
        /// </remarks>
        public void Create()
        {
            if (!Validateall())
            {
                return;
            }
            T entity = new T();
            Units.Add(entity);
            changeLog.Push(new ChangeLogEntry<T>
            {
                Entity = entity,
                ChangeType = ChangeType.Insert
            });
            // Subscribe to PropertyChanged event
            entity.PropertyChanged += ItemPropertyChangedHandler;
        }

        /// <summary>Adds a new entity to the collection and subscribes to its PropertyChanged event.</summary>
        /// <param name="entity">The entity to be added.</param>
        /// <remarks>
        /// This method first validates all entities in the collection using the Validateall method.
        /// If the validation fails, the method returns without adding the entity.
        /// Otherwise, the entity is added to the Units collection and the ItemPropertyChangedHandler is subscribed to its PropertyChanged event.
        /// </remarks>
        public void Create(T entity)
        {
            if (!Validateall())
            {
                return;
            }
            Units.Add(entity);
            changeLog.Push(new ChangeLogEntry<T>
            {
                Entity = entity,
                ChangeType = ChangeType.Insert
            });
            // Subscribe to PropertyChanged event
            entity.PropertyChanged += ItemPropertyChangedHandler;
        }
       

        /// <summary>Reads an item from a collection based on its ID.</summary>
        /// <param name="id">The ID of the item to read.</param>
        /// <returns>The item with the specified ID, or the default value of the item type if the ID is not found or the collection is not valid.</returns>
        public T Read(string id)
        {
            if (!Validateall())
            {
                return default(T);
            }
            return Units[Getindex(id)];
        }
        /// <summary>Deletes an object based on its ID.</summary>
        /// <param name="id">The ID of the object to delete.</param>
        /// <returns>An ErrorsInfo object indicating the result of the delete operation.</returns>
        /// <remarks>
        /// If the validation fails, the ErrorsInfo object will have a message of "Validation Failed" and a flag of Errors.Failed.
        /// If the object is found and successfully deleted, the ErrorsInfo object will have a message of "Delete Done" and a flag of Errors.Ok.
        /// If the object is not found, the ErrorsInfo object will have a message of "Object not found" and a flag of Errors.Failed.
        /// </remarks>
        public ErrorsInfo Delete(string id)
        {
            ErrorsInfo errorsInfo = new ErrorsInfo();
            if (!Validateall())
            {
                errorsInfo.Message = "Validation Failed";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
            var index = Getindex(id);
          
            if (index >= 0)
            {
                // Assuming ID is the value used in InsertedKeys and UpdatedKeys
                // Remove the entity from Units
                T entity = Units[index];
            
                Tracking tracking = _units.GetTrackingITem(entity);
                // Remove references from InsertedKeys and UpdatedKeys
                if (_entityStates.Count > 0)
                {
                    if (_entityStates.ContainsKey(tracking.OriginalIndex))
                    {
                        if (InsertedKeys.ContainsValue(tracking.OriginalIndex.ToString()))
                        {
                            // Attempt to remove the entity from InsertedKeys and UpdatedKeys.
                            var insertedKey = InsertedKeys.FirstOrDefault(kvp => kvp.Value == tracking.OriginalIndex.ToString());
                            if (!default(KeyValuePair<int, string>).Equals(insertedKey))
                            {
                                InsertedKeys.Remove(insertedKey.Key);
                            }
                            var updatedKey = UpdatedKeys.FirstOrDefault(kvp => kvp.Value == tracking.OriginalIndex.ToString());
                            if (!default(KeyValuePair<int, string>).Equals(updatedKey))
                            {
                                UpdatedKeys.Remove(updatedKey.Key);
                            }
                            if (tracking.IsNew && !tracking.IsSaved)
                            {
                                var deletedKey = DeletedKeys.FirstOrDefault(kvp => kvp.Value == tracking.OriginalIndex.ToString());
                                if (!default(KeyValuePair<int, string>).Equals(deletedKey))
                                {
                                    DeletedKeys.Remove(deletedKey.Key);
                                }
                                // Remove the entity's state from _entityStates if present.
                                if (_entityStates.ContainsKey(tracking.OriginalIndex))
                                {
                                    _entityStates.Remove(tracking.OriginalIndex);
                                }
                                DeletedUnits.Remove(entity);

                            }
                        }
                        changeLog.Push(new ChangeLogEntry<T>
                        {
                            Entity = entity,
                            ChangeType = ChangeType.Delete
                        });
                        _units.RemoveAt(index);
                        errorsInfo.Message = "Delete Done";
                        errorsInfo.Flag = Errors.Ok;
                        return errorsInfo;
                    }
                }
              
            }
           
                errorsInfo.Message = "Object not found";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            
        }
        public ErrorsInfo Delete()
        {
            ErrorsInfo errorsInfo = new ErrorsInfo();
            if (!Validateall())
            {
                errorsInfo.Message = "Validation Failed";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
            if(_units.Count==0)
            {
                errorsInfo.Message = "No Object to delete";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
            if(_units.Current==null)
            {
                errorsInfo.Message = "No Object to delete";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
            T entity=_units.Current;
           // var entityKeyAsString = Convert.ToString(PKProperty.GetValue(entity, null));
            var index = Getindex(entity);
            Tracking tracking = _units.GetTrackingITem(entity);
            //if (!InsertedKeys.ContainsValue(Convert.ToString(tracking.OriginalIndex)))
            //{
            //    InsertedKeys.Add(keysidx, Convert.ToString(tracking.OriginalIndex));
            //    _entityStates.Add(tracking.OriginalIndex, EntityState.Added);
            //}
            if (index >= 0)
            {
                if (_entityStates.Count > 0)
                {
                    if (_entityStates.ContainsKey(tracking.OriginalIndex))
                    {
                        if (InsertedKeys.ContainsValue(tracking.OriginalIndex.ToString()))
                        {
                            // Attempt to remove the entity from InsertedKeys and UpdatedKeys.
                            var insertedKey = InsertedKeys.FirstOrDefault(kvp => kvp.Value == tracking.OriginalIndex.ToString());
                            if (!default(KeyValuePair<int, string>).Equals(insertedKey))
                            {
                                InsertedKeys.Remove(insertedKey.Key);
                            }
                            var updatedKey = UpdatedKeys.FirstOrDefault(kvp => kvp.Value == tracking.OriginalIndex.ToString());
                            if (!default(KeyValuePair<int, string>).Equals(updatedKey))
                            {
                                UpdatedKeys.Remove(updatedKey.Key);
                            }
                            if (tracking.IsNew && !tracking.IsSaved)
                            {
                                var deletedKey = DeletedKeys.FirstOrDefault(kvp => kvp.Value == tracking.OriginalIndex.ToString());
                                if (!default(KeyValuePair<int, string>).Equals(deletedKey))
                                {
                                    DeletedKeys.Remove(deletedKey.Key);
                                }
                                // Remove the entity's state from _entityStates if present.
                                if (_entityStates.ContainsKey(tracking.OriginalIndex))
                                {
                                    _entityStates.Remove(tracking.OriginalIndex);
                                }
                                DeletedUnits.Remove(entity);

                            }
                        }
                        changeLog.Push(new ChangeLogEntry<T>
                        {
                            Entity = entity,
                            ChangeType = ChangeType.Delete
                        });
                        _units.RemoveAt(index);
                        errorsInfo.Message = "Delete Done";
                        errorsInfo.Flag = Errors.Ok;
                        return errorsInfo;
                    }
                }
                errorsInfo.Message = "Delete Done";
                errorsInfo.Flag = Errors.Ok;
                return errorsInfo;
            }
            else
            {
                if (!_units.Remove(entity))
                {
                    errorsInfo.Message = "Object not found";
                    errorsInfo.Flag = Errors.Failed;
                    return errorsInfo;
                }
                else
                {
                    errorsInfo.Message = "Delete Done";
                    errorsInfo.Flag = Errors.Ok;
                    return errorsInfo;
                }
            }
        }
        /// <summary>Deletes an entity and returns information about the operation.</summary>
        /// <param name="entity">The entity to delete.</param>
        /// <returns>An ErrorsInfo object containing information about the delete operation.</returns>
        /// <remarks>
        /// If the entity passes validation, it will be deleted and the ErrorsInfo object will have a Flag of Errors.Ok and a Message of "Delete Done".
        /// If the entity fails validation, the ErrorsInfo object will have a Flag of Errors.Failed and a Message of "Validation Failed".
        /// If the entity is not found, the ErrorsInfo object will have a Flag of Errors.Failed and a Message of "Object not found".
        /// </remarks>
        public ErrorsInfo Delete(T entity)
        {
            ErrorsInfo errorsInfo = new ErrorsInfo();
            if (!Validateall())
            {
                errorsInfo.Message = "Validation Failed";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
           // var entityKeyAsString = Convert.ToString(PKProperty.GetValue(entity, null));
            var index = Getindex(entity);
            Tracking tracking = _units.GetTrackingITem(entity);
            if(tracking.IsSaved && tracking.EntityState== EntityState.Deleted)
            {
                errorsInfo.Message = "Object already deleted";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
          
            //if (!InsertedKeys.ContainsValue(Convert.ToString(tracking.OriginalIndex)))
            //{
            //    InsertedKeys.Add(keysidx, Convert.ToString(tracking.OriginalIndex));
            //    _entityStates.Add(tracking.OriginalIndex, EntityState.Added);
            //}
            if (index >= 0)
            {
                if (_entityStates.Count > 0)
                {
                    if (_entityStates.ContainsKey(tracking.OriginalIndex))
                    {
                        if (InsertedKeys.ContainsValue(tracking.OriginalIndex.ToString()))
                        {
                            // Attempt to remove the entity from InsertedKeys and UpdatedKeys.
                            var insertedKey = InsertedKeys.FirstOrDefault(kvp => kvp.Value == tracking.OriginalIndex.ToString());
                            if (!default(KeyValuePair<int, string>).Equals(insertedKey))
                            {
                                InsertedKeys.Remove(insertedKey.Key);
                            }
                            var updatedKey = UpdatedKeys.FirstOrDefault(kvp => kvp.Value == tracking.OriginalIndex.ToString());
                            if (!default(KeyValuePair<int, string>).Equals(updatedKey))
                            {
                                UpdatedKeys.Remove(updatedKey.Key);
                            }
                           
                        }
                        if (tracking.IsNew && !tracking.IsSaved)
                        {
                            var deletedKey = DeletedKeys.FirstOrDefault(kvp => kvp.Value == tracking.OriginalIndex.ToString());
                            if (!default(KeyValuePair<int, string>).Equals(deletedKey))
                            {
                                DeletedKeys.Remove(deletedKey.Key);
                            }
                            // Remove the entity's state from _entityStates if present.
                            if (_entityStates.ContainsKey(tracking.OriginalIndex))
                            {
                                _entityStates.Remove(tracking.OriginalIndex);
                            }
                            DeletedUnits.Remove(entity);

                        }
                        else
                        {

                            changeLog.Push(new ChangeLogEntry<T>
                            {
                                Entity = entity,
                                ChangeType = ChangeType.Delete
                            });

                        }

                        _units.RemoveAt(index);
                        errorsInfo.Message = "Delete Done";
                        errorsInfo.Flag = Errors.Ok;
                        return errorsInfo;
                    }
                }
                errorsInfo.Message = "Delete Done";
                errorsInfo.Flag = Errors.Ok;
                return errorsInfo;
            }
            else
            {
                if (!_units.Remove(entity))
                {
                    errorsInfo.Message = "Object not found";
                    errorsInfo.Flag = Errors.Failed;
                    return errorsInfo;
                }
                else
                {
                    errorsInfo.Message = "Delete Done";
                    errorsInfo.Flag = Errors.Ok;
                    return errorsInfo;
                }
            }
        }
        /// <summary>Updates an entity and returns information about the operation.</summary>
        /// <param name="entity">The entity to be updated.</param>
        /// <returns>An ErrorsInfo object containing information about the update operation.</returns>
        /// <remarks>
        /// If the entity fails validation, the ErrorsInfo object will have a message indicating the failure and a flag set to Errors.Failed.
        /// If the entity is successfully updated, the ErrorsInfo object will have a message indicating the success and a flag set to Errors.Ok.
        /// If the entity is not found, the ErrorsInfo object will have a message indicating the failure and a flag set to Errors.Failed.
        /// </remarks>
        public ErrorsInfo Update(T entity)
        {
            ErrorsInfo errorsInfo = new ErrorsInfo();
            if (!Validateall())
            {
                errorsInfo.Message = "Validation Failed";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
            var index = DocExistByKey(entity);
            if (index >= 0)
            {
                _units[index] = entity;
                if (_entityStates.Count > 0)
                {
                    if (_entityStates.ContainsKey(index))
                    {
                        if (_entityStates[index] != EntityState.Added)
                        {
                            _entityStates[index] = EntityState.Modified;
                        }
                        errorsInfo.Message = "Update Done";
                        errorsInfo.Flag = Errors.Ok;
                        return errorsInfo;
                    }
                }
                else
                {
                    _entityStates.Add(index, EntityState.Modified);

                }
                var originalEntity = Units.FirstOrDefault(e => e.Equals(entity));
                if (originalEntity != null)
                {
                    var originalValues = new Dictionary<string, object>();
                    foreach (var prop in typeof(T).GetProperties())
                    {
                        originalValues[prop.Name] = prop.GetValue(originalEntity);
                    }

                   changeLog.Push(new ChangeLogEntry<T>
                        {
                            Entity = entity,
                            OriginalValues = originalValues,
                            ChangeType = ChangeType.Update
                        });
 
                }

               
                errorsInfo.Message = "Update Done";
                errorsInfo.Flag = Errors.Ok;
                return errorsInfo;
            }
            else
            {
                errorsInfo.Message = "Object not found";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
        }
        /// <summary>Updates an entity with the specified ID.</summary>
        /// <param name="id">The ID of the entity to update.</param>
        /// <param name="entity">The updated entity.</param>
        /// <returns>An ErrorsInfo object indicating the result of the update operation.</returns>
        public ErrorsInfo Update(string id, T entity)
        {
            ErrorsInfo errorsInfo = new ErrorsInfo();
            if (!Validateall())
            {
                errorsInfo.Message = "Validation Failed";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
            var index = Getindex(id);
            if (index >= 0)
            {
                Units[index] = entity;
                if (_entityStates.Count > 0)
                {
                    if (_entityStates.ContainsKey(index))
                    {
                        if (_entityStates[index] != EntityState.Added)
                        {
                            _entityStates[index] = EntityState.Modified;
                        }
                        errorsInfo.Message = "Update Done";
                        errorsInfo.Flag = Errors.Ok;
                        return errorsInfo;
                    }
                }
                else
                {
                    _entityStates.Add(index, EntityState.Modified);
                }
                errorsInfo.Message = "Update Done";
                errorsInfo.Flag = Errors.Ok;
                return errorsInfo;
            }
            else
            {
                errorsInfo.Message = "Object not found";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
        }
      
        /// <summary>Commits changes and returns information about any errors that occurred.</summary>
        /// <param name="progress">An object that reports progress during the commit process.</param>
        /// <param name="token">A cancellation token that can be used to cancel the commit process.</param>
        /// <returns>An object containing information about any errors that occurred during the commit process.</returns>
        public virtual async Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token)
        {
            _suppressNotification = true;
            if (IsInListMode)
            {
                _suppressNotification = false;
                IsFilterOn = false;
                return DMEEditor.ErrorObject;
            }
            PassedArgs args = new PassedArgs();
            IErrorsInfo errorsInfo = new ErrorsInfo();
            int x = 1;
            args.ParameterInt1 = InsertedKeys.Count + UpdatedKeys.Count + DeletedKeys.Count;
            args.ParameterString1 = $"Started Saving Changes {args.ParameterInt1}";
            args.Messege = $"Started Saving Changes {args.ParameterInt1}";
            progress.Report(args);
            try
            {
                if (GetAddedEntities() != null)
                {
                    foreach (int t in GetAddedEntities())
                    {
                        args.ParameterInt1 = x;

                        progress.Report(args);
                        //  int r = Getindex(t.ToString());
                        IErrorsInfo errorsInfo1 = await InsertAsync(Units[t]);
                        if (errorsInfo1.Flag == Errors.Ok)
                        {
                            InsertedKeys.Remove(t);
                            _entityStates.Remove(t);
                        }
                        else
                        {
                            errorsInfo.Flag = Errors.Failed;
                            if (errorsInfo1.Ex != null)
                            {
                                errorsInfo.Message = errorsInfo.Message + "." + errorsInfo1.Ex.Message;

                            }
                        }
                        x++;
                    }
                }
                if (GetModifiedEntities() != null)
                {
                    foreach (int t in GetModifiedEntities())
                    {
                        args.ParameterInt1 = x;
                        progress.Report(args);
                        //  int r = Getindex(t.ToString());
                        IErrorsInfo errorsInfo1 = await UpdateAsync(Units[t]);
                        if (errorsInfo1.Flag == Errors.Ok)
                        {
                            UpdatedKeys.Remove(t);
                            _entityStates.Remove(t);
                        }
                        else
                        {
                            errorsInfo.Flag = Errors.Failed;
                            if (errorsInfo1.Ex != null)
                            {
                                errorsInfo.Message = errorsInfo.Message + "." + errorsInfo1.Ex.Message;

                            }
                        }
                        x++;
                    }
                }
                if (GetDeletedEntities() != null)
                {
                    foreach (T t in GetDeletedEntities())
                    {
                        args.ParameterInt1 = x;
                        progress.Report(args);
                        // int r = Getindex(t.ToString());
                        IErrorsInfo errorsInfo1 = await DeleteAsync(t);
                        if (errorsInfo1.Flag == Errors.Ok)
                        {

                            _deletedentities.Remove(t);
                        }
                        else
                        {
                            errorsInfo.Flag = Errors.Failed;
                            if (errorsInfo1.Ex != null)
                            {
                                errorsInfo.Message = errorsInfo.Message + "." + errorsInfo1.Ex.Message;

                            }
                        }
                        x++;
                    }
                    DeletedKeys.Clear();
                    undoDeleteStack.Clear();
                }


                args.Messege = $"Ended Saving Changes";
                args.ParameterString1 = $"Ended Saving Changes";
                args.ErrorCode = "END";
                progress.Report(args);
                _suppressNotification = false;
            }
            catch (Exception ex)
            {
                errorsInfo.Flag = Errors.Failed;
                _suppressNotification = false;
                args.Messege = $"Error Saving Changes {ex.Message}";
                args.ParameterString1 = $"Error Saving Changes {ex.Message}";
                progress.Report(args);
                errorsInfo.Ex = ex;
                DMEEditor.AddLogMessage("UnitofWork", $"Saving and Commiting Changes error {ex.Message}", DateTime.Now, args.ParameterInt1, ex.Message, Errors.Failed);
            }
            _suppressNotification = false;
            return await Task.FromResult<IErrorsInfo>(errorsInfo);
        }
        /// <summary>Commits changes made in the unit of work.</summary>
        /// <returns>An object containing information about any errors that occurred during the commit.</returns>
        /// <remarks>
        /// This method is responsible for committing changes made in the unit of work. If the unit of work is in list mode,
        /// it returns a predefined error object. Otherwise, it creates a new instance of the PassedArgs class, an object to store
        /// information about the commit process. It also creates an instance of the ErrorsInfo class to store any errors that occur.
        /// The method then attempts to commit the changes, and if an exception is thrown, it logs the error and sets the ErrorsInfo
        /// object's Ex property to the exception. Finally
        public virtual async Task<IErrorsInfo> Commit()
        {
            _suppressNotification = true;
            if (IsInListMode)
            {
                _suppressNotification = false;
                IsFilterOn = false;
                return DMEEditor.ErrorObject;
            }
            PassedArgs args = new PassedArgs();
            IErrorsInfo errorsInfo = new ErrorsInfo();
            int x = 1;

            try
            {
                if (GetAddedEntities() != null)
                {
                    foreach (int t in GetAddedEntities())
                    {
                        //  int r = Getindex(t.ToString());
                        IErrorsInfo errorsInfo1 = await InsertAsync(_units[t]);
                        if (errorsInfo1.Flag == Errors.Ok)
                        {
                            if (IsIdentity && DataSource.Category== DatasourceCategory.RDBMS)
                            {
                                SetIDValue(_units[t], GetLastIdentity());
                            }
                            var key= InsertedKeys.Where(x => x.Value == t.ToString()).FirstOrDefault().Key;
                            InsertedKeys.Remove(key);
                            _entityStates.Remove(t);
                        }
                        else
                        {
                            errorsInfo.Flag = Errors.Failed;
                            errorsInfo.Message= errorsInfo.Message+ "."+errorsInfo1.Ex.Message;
                        }
                        x++;
                    }
                }
                if (GetModifiedEntities() != null)
                {
                    foreach (int t in GetModifiedEntities())
                    {

                        //    int r = Getindex(t.ToString());
                        IErrorsInfo errorsInfo1 = await UpdateAsync(Units[t]);
                        if (errorsInfo1.Flag == Errors.Ok)
                        {
                            var key = UpdatedKeys.Where(x => x.Value == t.ToString()).FirstOrDefault().Key;
                            UpdatedKeys.Remove(key);
                            _entityStates.Remove(t);
                        }
                        else
                        {
                            errorsInfo.Flag = Errors.Failed;
                            if (errorsInfo1.Ex != null)
                            {
                                errorsInfo.Message = errorsInfo.Message + "." + errorsInfo1.Ex.Message;

                            }
                        }
                        x++;
                    }
                }
                if (GetDeletedEntities() != null)
                {
                    foreach (T entity in GetDeletedEntities())
                    {

                        if (_deletedentities.TryGetValue(entity, out EntityState state))
                        {
                            // Skip calling DeleteAsync for entities that were added but not yet saved to the database
                            if (state == EntityState.Added)
                            {
                                // Since the entity was never persisted, we can remove it directly
                                _deletedentities.Remove(entity);
                                continue; // Move to the next entity
                            }

                            // Proceed with async deletion for entities that were persisted (either modified or initially persisted then marked for deletion)
                            IErrorsInfo errorsInfo1 = await DeleteAsync(entity);
                            if (errorsInfo1.Flag == Errors.Ok)
                            {
                                // Successfully deleted from the database, remove from _deletedentities
                                _deletedentities.Remove(entity);
                            }
                            else
                            {
                                // If deletion failed, handle accordingly
                                errorsInfo.Flag = Errors.Failed;
                                if (errorsInfo1.Ex != null)
                                {
                                    errorsInfo.Message += "." + errorsInfo1.Ex.Message;
                                }
                            }
                        }
                    }
                    DeletedKeys.Clear();
                    undoDeleteStack.Clear();
                }
                _suppressNotification = false;
            }
            catch (Exception ex)
            {
                _suppressNotification = false;
                errorsInfo.Flag = Errors.Failed;
                errorsInfo.Ex = ex;
                DMEEditor.AddLogMessage("UnitofWork", $"Saving and Commiting Changes error {ex.Message}", DateTime.Now, args.ParameterInt1, ex.Message, Errors.Failed);
            }
            _suppressNotification = false;
            return await Task.FromResult<IErrorsInfo>(errorsInfo);
        }
        /// <summary>Gets the next value of a sequence.</summary>
        /// <param name="SeqName">The name of the sequence.</param>
        /// <returns>The next value of the sequence.</returns>
        /// <remarks>
        /// This method retrieves the next value of a sequence from the data source.
        /// If the data source is a relational database management system (RDBMS),
        /// it generates a query to fetch the next sequence value and executes it.
        /// The method returns -1 if the sequence value cannot be retrieved or if the data source is not an RDBMS.
        /// </remarks>
        public virtual int GetSeq(string SeqName)
        {
            int retval = -1;
            if (DataSource.Category == DatasourceCategory.RDBMS)
            {
                string str = RDBMSHelper.GenerateFetchNextSequenceValueQuery(DataSource.DatasourceType, SeqName);
                if (!string.IsNullOrEmpty(str))
                {
                    var r = DataSource.GetScalar(str);
                    if (DMEEditor.ErrorObject.Flag == Errors.Ok || r<=0)
                    {
                        if (r != null)
                        {
                            retval = (int)r;
                        }

                    }

                }
            }
            return retval;
        }
        /// <summary>Gets the primary key sequence for a document.</summary>
        /// <param name="doc">The document for which to retrieve the primary key sequence.</param>
        /// <returns>The primary key sequence value.</returns>
        /// <remarks>
        /// This method retrieves the primary key sequence for a document. It checks if the data source category is RDBMS
        /// and if a sequencer is specified. If both conditions are met, it retrieves the sequence value using the specified sequencer.
        /// If the sequence value is greater than 0, it sets the ID value of the document to the sequence value.
        /// </remarks>
        public virtual int GetPrimaryKeySequence(T doc)
        {
            int retval = -1;
            if (DataSource.Category == DatasourceCategory.RDBMS && !string.IsNullOrEmpty(Sequencer))
            {
                retval = GetSeq(Sequencer);
                if (retval > 0)
                {
                    SetIDValue(doc, retval);
                }
            }
            return retval;
        }
        public Task<IErrorsInfo> Rollback()
        {
            try
            {
                Clear();
                Units = Tempunits;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep",$" Unit of Work Could not Rollback {ex.Message} ", DateTime.Now, -1, null, Errors.Failed);   
                
            }
            return Task.FromResult<IErrorsInfo>(DMEEditor.ErrorObject);
        }
        #endregion
        #region "Get Methods"
        /// <summary>
        /// Retrieves a list of items based on the specified filters.
        /// </summary>
        /// <param name="filters">The list of filters to apply.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the list of items that match the filters.
        /// </returns>
        public virtual async Task<ObservableBindingList<T>> GetQuery(string query)
        {
            if (query == null)
            {
                IsFilterOn = false;
                return Units;
            }
            else
                IsFilterOn = true;

            if (!IsInListMode)
            {
                //clearunits();
                
                var retval = DataSource.GetEntity(query,null);

                GetDataInUnits(retval);

            }
            else
            {
                if (query != null && _units != null)
                {
                    if (_units.Count > 0)
                    {
                        _suppressNotification = true;
                        FilteredUnits = FilterCollection(_units, null);
                        _suppressNotification = false;
                        return await Task.FromResult(FilteredUnits);

                    }

                }
            }
            return await Task.FromResult(Units);



        }
        /// <summary>
        /// Retrieves a list of items based on the specified filters.
        /// </summary>
        /// <param name="filters">The list of filters to apply.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the list of items that match the filters.
        /// </returns>
        public virtual async Task<ObservableBindingList<T>> Get(List<AppFilter> filters)
        {

            if (filters == null)
            {
                IsFilterOn = false;
                return Units;
            }
            else
                IsFilterOn = true;

            if (!IsInListMode)
            {
                //clearunits();
                var retval = DataSource.GetEntity(EntityName, filters);

                GetDataInUnits(retval);

            }
            else
            {
                if (filters != null && _units != null)
                {
                    if (_units.Count > 0)
                    {
                        _suppressNotification = true;
                        FilteredUnits = FilterCollection(_units, filters);
                        _suppressNotification = false;
                        return await Task.FromResult(FilteredUnits);

                    }

                }
            }
            return await Task.FromResult(Units);



        }
        /// <summary>Retrieves a collection of entities asynchronously.</summary>
        /// <returns>An observable binding list of entities.</returns>
        /// <remarks>
        /// This method retrieves a collection of entities from the data source. If the application is not in list mode,
        /// it first gets the entity data from the data source and then processes the data in units. If an exception occurs
        /// during the data processing, a log message is added. Finally, the method returns the observable binding list of entities.
        /// </remarks>
        public virtual async Task<ObservableBindingList<T>> Get()
        {
            _suppressNotification = true;
            IsFilterOn = false;
            if (!IsInListMode)
            {
                
                var retval = DataSource.GetEntity(EntityName, null);
                try
                {
                    GetDataInUnits(retval);
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("Beep", $" Unit of Work Could not get Data in units {ex.Message} ", DateTime.Now, -1, null, Errors.Failed);
                }

            }
            _suppressNotification = false;
            return await Task.FromResult(Units);
        }
        /// <summary>Retrieves the value associated with the specified key.</summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <returns>The value associated with the specified key.</returns>
        public virtual T Get(int key)
        {
            return Units[key];
        }
        /// <summary>Returns an object of type T based on the provided primary key.</summary>
        /// <param name="PrimaryKeyid">The value of the primary key.</param>
        /// <returns>An object of type T that matches the provided primary key, or null if no match is found.</returns>
        public virtual T Get(string PrimaryKeyid)
        {
            if(Units == null || Units.Count==0)
            {
                Get(new List<AppFilter>() { new AppFilter() { FieldName = PrimaryKey, Operator = "=", FilterValue = PrimaryKeyid } });
            }
            
            var retval = Units.FirstOrDefault(p => p.GetType().GetProperty(PrimaryKey).GetValue(p, null).ToString() == PrimaryKeyid);
            return retval;
        }
        /// <summary>Converts data to units and updates the internal state.</summary>
        /// <param name="retval">The data to be converted.</param>
        /// <returns>True if the conversion was successful, false otherwise.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during the conversion.</exception>
        private bool GetDataInUnits(object retval)
        {

            Clear();
            _suppressNotification = true;
            if (retval == null)
            {
                _suppressNotification = false;
                DMEEditor.AddLogMessage("Beep", $"No Data Found", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
            try
            {

                List<T> list = new List<T>();
                _suppressNotification = true;
                var type = retval.GetType();
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObservableBindingList<>))
                {
                  //  IBindingListView ls = (IBindingListView)retval;
                    Units = (ObservableBindingList<T>)retval;
                }else
                if (retval is IBindingListView)
                {
                     IBindingListView  ls= (IBindingListView)retval;
                    if(EntityType == null)
                    {
                        EntityType = DataSource.GetEntityType(EntityName);
                    }
                    Units = new ObservableBindingList<T>(ls);
                }else
                if (retval is IList)
                {
                    list = (List<T>)retval;
                    if (EntityType == null)
                    {
                        EntityType = DataSource.GetEntityType(EntityName);
                    }
                    Units = new ObservableBindingList<T>(list);
                }
                else
                {
                    if (retval is DataTable)
                    {
                        DataTable dataTable = (DataTable)retval;
                        //Units
                        Units = new ObservableBindingList<T>(dataTable); //DMEEditor.Utilfunction.ConvertDataTable<T>();
                    }
                }
                _suppressNotification = false;
                return true;
            }
            catch (Exception ex)
            {
                _suppressNotification = false;
                DMEEditor.AddLogMessage("Beep", $"Error Converting Data to Units {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                Units= new ObservableBindingList<T>();
                return false;
            }

        }
        #endregion
        #region "Find Methods"
        /// public Task<string> GetClipboardContentAsTextAsync()
        public virtual int FindDocIdx(T doc)
        {
            object docPrimaryKeyValue = doc.GetType().GetProperty(PrimaryKey)?.GetValue(doc, null);

            // Find the first matching item in _units.
            var item = _units.FirstOrDefault(p => object.Equals(p.GetType().GetProperty(PrimaryKey)?.GetValue(p, null), docPrimaryKeyValue));

            // Get the index of the found item.
            int retval = _units.IndexOf(item);

            return retval;
        }
        /// <summary>Checks if a document exists in the collection based on its primary key.</summary>
        /// <typeparam name="T">The type of document.</typeparam>
        /// <param name="doc">The document to check.</param>
        /// <returns>The index of the document in the collection if it exists, otherwise -1.</returns>
        public virtual int DocExistByKey(T doc)
        {
            // Retrieve the primary key value from the document `doc`.
            object docPrimaryKeyValue = doc.GetType().GetProperty(PrimaryKey)?.GetValue(doc, null);

            // Find the first matching item in _units.
            var item = _units.FirstOrDefault(p => object.Equals(p.GetType().GetProperty(PrimaryKey)?.GetValue(p, null), docPrimaryKeyValue));

            // Get the index of the found item.
            int retval = _units.IndexOf(item);

            return retval;
        }
        /// <summary>Checks if a document exists in the collection and returns its index.</summary>
        /// <typeparam name="T">The type of document.</typeparam>
        /// <param name="doc">The document to check.</param>
        /// <returns>The index of the document if it exists in the collection, otherwise -1.</returns>
        public virtual int DocExist(T doc)
        {
            object docPrimaryKeyValue = doc.GetType().GetProperty(PrimaryKey)?.GetValue(doc, null);

            // Find the first matching item in _units.
            var item = _units.FirstOrDefault(p => object.Equals(p.GetType().GetProperty(PrimaryKey)?.GetValue(p, null), docPrimaryKeyValue));

            // Get the index of the found item.
            int retval = _units.IndexOf(item);

            return retval;
        }
        #endregion
        #region "Entity Management"
        /// <summary>Checks if the object is dirty.</summary>
        /// <returns>True if the object is dirty, false otherwise.</returns>
        public bool GetIsDirty()
        {
            if (!Validateall())
            {
                return false;
            }
            if (InsertedKeys != null)
            {
                if (InsertedKeys.Count > 0 )
                {
                    return true;
                }
            }
            if (UpdatedKeys != null)
            {
                if ( UpdatedKeys.Count > 0 )
                {
                    return true;
                }
            }
            if(DeletedKeys != null)
            {
                if (DeletedKeys.Count > 0)
                {
                    return true;
                }
            }
            
            return false;
        }
        /// <summary>Returns a collection of all the added entities.</summary>
        /// <returns>An IEnumerable of integers representing the added entities.</returns>
        /// <remarks>
        /// If the validation of all entities fails, null is returned.
        /// The added entities are determined by filtering the _entityStates dictionary
        /// and selecting the keys (integers) where the corresponding value is EntityState.Added.
        /// </remarks>
        public IEnumerable<int> GetAddedEntities()
        {
            if (!Validateall())
            {
                return null;
            }
            return _entityStates.Where(x => x.Value == EntityState.Added).Select(x => x.Key);
        }
        /// <summary>Returns a collection of modified entity IDs.</summary>
        /// <returns>An IEnumerable of integers representing the IDs of modified entities.</returns>
        /// <remarks>
        /// If all entities pass the validation, the method returns the IDs of entities whose EntityState is set to Modified.
        /// If the validation fails, the method returns null.
        /// </remarks>
        public IEnumerable<int> GetModifiedEntities()
        {
            if (!Validateall())
            {
                return null;
            }
            return _entityStates.Where(x => x.Value == EntityState.Modified).Select(x => x.Key);
        }
        /// <summary>Returns a collection of deleted entities.</summary>
        /// <typeparam name="T">The type of entities.</typeparam>
        /// <returns>A collection of deleted entities.</returns>
        /// <remarks>
        /// This method checks if all entities are valid using the Validateall() method.
        /// If not all entities are valid, it returns null.
        /// Otherwise, it returns a collection of entities that have been marked as deleted.
        /// </remarks>
        public IEnumerable<T> GetDeletedEntities()
        {
            if (!Validateall())
            {
                return null;
            }
            return _deletedentities.Where(x => x.Value == EntityState.Deleted).Select(x => x.Key);
        }
        /// <summary>Handles the event when the list of units changes.</summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The event arguments containing information about the change.</param>
        /// <remarks>
        /// This method is called when the list of units changes. It checks if the notification is suppressed,
        /// and if so, it returns without performing any further actions. If the change type is an item change,
        /// it retrieves the item at the specified index from the list of units. If the item's primary key value
        /// is not already present in the UpdatedKeys collection, it adds the key value to the collection along
        /// with an incremented index value.
        /// </remarks>
        private void Units_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (_units.SuppressNotification == true)
            {
                return;
            }
            if (_suppressNotification)
            {
                return;
            }
            if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                if (_suppressNotification)
                {
                    return;
                }
                T item = _units[e.NewIndex];
                Tracking tracking = _units.GetTrackingITem(item);
                var entityKeyAsString = Convert.ToString(tracking.OriginalIndex);
                if (!UpdatedKeys.Any(p => p.Value.Equals(entityKeyAsString)))
                {
                    keysidx++;
                    UpdatedKeys.Add(keysidx, Convert.ToString(entityKeyAsString));
                }
            }
        }
        /// <summary>Handles the CollectionChanged event of the Units collection.</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Units_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_units.SuppressNotification == true)
            {
                return;
            }
            if (_suppressNotification)
            {
                return;
            }
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    IsNewRecord = true;
                    foreach (T item in e.NewItems)
                    {
                        UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false };
                        PostCreate?.Invoke(item, ps);
                        if (!ps.Cancel)
                        {
                            keysidx++;
                            item.PropertyChanged += ItemPropertyChangedHandler;
                            if(!IsInListMode)
                            {
                                if (!IsIdentity)
                                {
                                    GetPrimaryKeySequence(item);
                                }
                            }
                            Tracking tracking=_units.GetTrackingITem(item);
                            if (!InsertedKeys.ContainsValue(Convert.ToString(tracking.OriginalIndex)))
                            {
                                InsertedKeys.Add(keysidx, Convert.ToString(tracking.OriginalIndex));
                                _entityStates.Add(tracking.OriginalIndex, EntityState.Added);
                            }
                        }
                    }
                    IsNewRecord = false;
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (T item in e.OldItems)
                    {
                        UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false };
                        PreDelete?.Invoke(item, ps);
                        Tracking tracking = _units.GetTrackingITem(item);
                        if (tracking != null)
                        {
                            if(!tracking.IsSaved && tracking.IsNew)
                            {
                                return ;
                            }
                        }
                        if (!ps.Cancel)
                        {
                           
                            undoDeleteStack.Push(new Tuple<T, int>(item, tracking.OriginalIndex));
                            keysidx++;
                            var entityKeyAsString = Convert.ToString(tracking.OriginalIndex);
                            DeletedKeys.Add(keysidx, entityKeyAsString);
                            EntityState state= EntityState.Deleted;
                            var insertedKey = InsertedKeys.FirstOrDefault(kvp => kvp.Value == entityKeyAsString);
                            if (!default(KeyValuePair<int, string>).Equals(insertedKey))
                            {
                                state= EntityState.Added;
                            }

                            var updatedKey = UpdatedKeys.FirstOrDefault(kvp => kvp.Value == entityKeyAsString);
                            if (!default(KeyValuePair<int, string>).Equals(updatedKey))
                            {
                                state= EntityState.Modified;
                            }
                            _deletedentities.Add(item, state);
                        }
                        else
                            UndoDelete(item, tracking.OriginalIndex);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    //foreach (T item in e.OldItems)
                    //{
                    //    keysidx++;
                    //    DeletedKeys.Add(keysidx, (string)PKProperty.GetValue(item, null));

                    //}
                    //foreach (T item in e.NewItems)
                    //{
                    //    keysidx++;
                    //    InsertedKeys.Add(keysidx, (string)PKProperty.GetValue(item, null));
                    //}
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    foreach (T item in e.NewItems)
                    {

                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    Clear();
                    break;
                default:
                    break;
            }

        }
        /// <summary>
        /// Event handler for property changes in an item.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The event arguments containing information about the changed property.</param>
        private void ItemPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            
            if (_units.SuppressNotification == true)
            {
                return;
            }
            if (_suppressNotification || IsNewRecord)
            {
                return;
            }
            T item = (T)sender;
            Tracking tracking = _units.GetTrackingITem(item);
            if (item != null)
            {
                if(InsertedKeys.Count>0)
                {
                    if (InsertedKeys.ContainsValue(Convert.ToString(tracking.OriginalIndex)))
                    {

                        return;
                    }
                }
               
            }
            if(!InsertedKeys.Any(p => p.Value.Equals(Convert.ToString(tracking.OriginalIndex))))
            {
                if (!UpdatedKeys.Any(p => p.Value.Equals(Convert.ToString(tracking.OriginalIndex))))
                {
                    keysidx++;
                    UpdatedKeys.Add(keysidx, Convert.ToString(tracking.OriginalIndex));
                    int x = tracking.OriginalIndex;// Getindex(item);
                    if (!_entityStates.ContainsKey(x))
                    {
                        _entityStates.Add(x, EntityState.Modified);
                    }
                    CurrentProperty = item.GetType().GetProperty(e.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false, PropertyName = e.PropertyName, PropertyValue = Convert.ToString(CurrentProperty.GetValue(item, null)) };
                    PostEdit?.Invoke(item, ps);
                }

            }
            // Get the current property that was changed
            CurrentProperty = item.GetType().GetProperty(e.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (CurrentProperty != null)
            {
                // Get the new value
                var newValue = CurrentProperty.GetValue(item, null);

                // Get the original item using the tracking index
                var originalItem = Tempunits.ElementAtOrDefault(tracking.OriginalIndex);
                var originalValue = originalItem != null ? CurrentProperty.GetValue(originalItem, null) : null;
                changeLog.Push(new ChangeLogEntry<T>
                {
                    Entity = item,
                    OriginalValues = { { e.PropertyName, originalValue } },
                    NewValues = { { e.PropertyName, newValue } },
                    ChangeType = ChangeType.Update
                });
            }



        }
        /// <summary>Filters a collection based on a list of filters.</summary>
        /// <param name="originalCollection">The original collection to filter.</param>
        /// <param name="filters">The list of filters to apply.</param>
        /// <returns>A filtered collection.</returns>
        /// <remarks>
        /// This method uses reflection to dynamically build an expression tree based on the provided filters.
        /// Each filter is applied to the specified property of the collection's elements.
        /// If an error occurs during the filtering process, a log message is added and null is returned.
        /// </remarks>
        private ObservableBindingList<T> FilterCollection(ObservableBindingList<T> originalCollection, List<AppFilter> filters)
        {
            try
            {
                // Separate pagination filters
                var paginationFilters = filters.Where(f => f.FieldName == "PageIndex" || f.FieldName == "PageSize").ToList();
                filters = filters.Except(paginationFilters).ToList();

                var parameter = Expression.Parameter(typeof(T), "x");

                Expression combinedExpression = null;
                foreach (var filter in filters)
                {
                    var property = Expression.Property(parameter, filter.FieldName);
                    var propertyType = property.Type;
                    // Handle nullable properties
                    if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        propertyType = Nullable.GetUnderlyingType(propertyType);
                    }
                    object convertedValue;
                    if (propertyType.IsEnum)
                    {
                        convertedValue = Enum.Parse(propertyType, filter.FilterValue.ToString());
                    }
                    else if (propertyType == typeof(DateTime))
                    {
                        convertedValue = DateTime.Parse(filter.FilterValue.ToString());
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(filter.FilterValue, propertyType);
                    }

                    var constant = Expression.Constant(convertedValue, propertyType);
                    BinaryExpression comparisonExpression;

                    switch (filter.Operator)
                    {
                        case "=":
                            comparisonExpression = Expression.Equal(property, constant);
                            break;
                        case ">":
                            comparisonExpression = Expression.GreaterThan(property, constant);
                            break;
                        case "<":
                            comparisonExpression = Expression.LessThan(property, constant);
                            break;
                        case "!=":
                        case "<>":
                            comparisonExpression = Expression.NotEqual(property, constant);
                            break;
                        case ">=":
                            comparisonExpression = Expression.GreaterThanOrEqual(property, constant);
                            break;
                        case "<=":
                            comparisonExpression = Expression.LessThanOrEqual(property, constant);
                            break;
                        default:
                            throw new NotSupportedException($"Unsupported comparison type: {filter.Operator}");
                    }

                    combinedExpression = combinedExpression == null ? comparisonExpression : Expression.AndAlso(combinedExpression, comparisonExpression);
                }

                if (combinedExpression == null)
                {
                    return originalCollection;  // or throw an exception if no filters should mean no results
                }

                var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
                var filteredData = originalCollection.AsQueryable().Where(lambda.Compile()).ToList();
                // Apply pagination
                if (paginationFilters.Any())
                {
                    int pageIndex = int.Parse(paginationFilters.FirstOrDefault(f => f.FieldName == "PageIndex")?.FilterValue ?? "1") - 1;
                    int pageSize = int.Parse(paginationFilters.FirstOrDefault(f => f.FieldName == "PageSize")?.FilterValue ?? "10");
                    filteredData = filteredData.Skip(pageIndex * pageSize).Take(pageSize).ToList();
                }

                return new ObservableBindingList<T>(filteredData);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Filtering Data {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }
        /// <summary>Filters a collection based on a specified property and value.</summary>
        /// <param name="originalCollection">The original collection to filter.</param>
        /// <param name="propertyName">The name of the property to filter on.</param>
        /// <param name="value">The value to filter by.</param>
        /// <returns>A filtered collection based on the specified property and value.</returns>
        /// <remarks>
        /// This method uses reflection to dynamically filter the collection based on the specified property and value.
        /// If an error occurs during the filtering process, an error message is logged and null is returned.
        /// </remarks>
        private ObservableBindingList<T> FilterCollection(ObservableBindingList<T> originalCollection, string propertyName, object value)
        {
            try
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(parameter, propertyName);
                var propertyType = property.Type;

                object convertedValue;
                if (propertyType.IsEnum)
                {
                    // Convert the string to an Enum
                    convertedValue = Enum.Parse(propertyType, value.ToString());
                }
                else
                {
                    convertedValue = Convert.ChangeType(value, propertyType);
                }

                var constant = Expression.Constant(convertedValue, propertyType);
                //    DMEEditor.AddLogMessage("Beep", $"Property type: {property.Type}. Constant type: {constant.Type}", DateTime.Now, 0, null, Errors.Ok);
                var equality = Expression.Equal(property, constant);
                var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);

                var filteredData = new ObservableBindingList<T>(
     originalCollection.AsQueryable().Where(lambda.Compile()).ToList()
 );


                return filteredData;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Filtering Data {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }

        }
        /// <summary>Undoes a delete operation by reinserting an item at a specified index.</summary>
        /// <param name="itemToReinsert">The item to be reinserted.</param>
        /// <param name="indexToReinsertAt">The index at which the item should be reinserted.</param>
        private void UndoDelete(T itemToReinsert, int indexToReinsertAt)
        {
            // Insert the item back into the list at the original index
            // Assuming 'YourList' is the list from which items were deleted
            Units.Insert(indexToReinsertAt, itemToReinsert);

            // Optionally, remove any state or keys related to the deleted item
            // (like undoing changes to DeletedKeys, _entityStates, etc.)
        }
        // Function to undo a delete operation
        /// <summary>Undoes the most recent deletion operation.</summary>
        /// <remarks>
        /// This method retrieves the most recently deleted item from the undo delete stack and reinserts it into the original collection at the original index.
        /// </remarks>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="Units">The collection of items.</param>
        /// <param name="undoDeleteStack">The stack that stores the deleted items and their original indices.</param>
        /// <exception cref="InvalidOperationException">Thrown when the undo delete stack is empty.</exception>
        public void UndoDelete()
        {
            if (undoDeleteStack.Count > 0)
            {
                var undoItem = undoDeleteStack.Pop();
                T itemToReinsert = undoItem.Item1;
                int indexToReinsertAt = undoItem.Item2;

                // Insert the item back into the list at the original index
                // Assuming 'YourList' is the list from which items were deleted
                Units.Insert(indexToReinsertAt, itemToReinsert);

                // Optionally, remove any state or keys related to the deleted item
                // (like undoing changes to DeletedKeys, _entityStates, etc.)
            }
        }
        #endregion
        #region "Logging"
        public bool IsLogging { get { return _units.IsLoggin; } set { _units.IsLoggin = value; } }
        public Dictionary<DateTime, EntityUpdateInsertLog> UpdateLog { get { return _units.UpdateLog; } set { } }
        public bool SaveLog(string pathandname)
        {
            bool retval = false;
            string dirpath=string.Empty;
            try
            {
                if (string.IsNullOrEmpty(pathandname))
                {
                    DMEEditor.AddLogMessage("Beep",$"Error Saving Log File {pathandname} ", DateTime.Now, -1, null, Errors.Failed);
                    return false;
                }
                dirpath = Path.GetDirectoryName(pathandname);
                if (!Directory.Exists(dirpath))
                {
                    try
                    {
                        Directory.CreateDirectory(dirpath);
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.AddLogMessage("Beep",$"Error Creating Directory {dirpath} {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                        return false;
                    }
                    
                }
                if (IsLogging)
                {

                    if (UpdateLog != null)
                    {
                        if (UpdateLog.Count > 0)
                        {
                            string json = JsonConvert.SerializeObject(UpdateLog, Formatting.Indented);
                            File.WriteAllText(pathandname, json);
                            retval = true;
                        }
                        else
                        {
                            DMEEditor.AddLogMessage("Beep",$"No Logs Records found", DateTime.Now, -1, null, Errors.Failed);
                        }
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Beep", $"No Logs found", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                else { DMEEditor.AddLogMessage("Beep", $"Log is not Enabled", DateTime.Now, -1, null, Errors.Failed); }
            }
            catch { retval = false; }
            return retval;
        }
        #endregion
        #region "Misc"
        /// <summary>Checks if the requirements for a valid operation are validated.</summary>
        /// <returns>True if the requirements are validated, false otherwise.</returns>
        /// <remarks>
        /// The method checks for the following requirements:
        /// - EntityStructure: If it is null, sets the ErrorObject flag to Errors.Failed and the ErrorObject message to "Missing Entity Structure".
        /// - Entity PrimaryKey: Sets the ErrorObject flag to Errors.Failed and the ErrorObject message to "Missing Entity PrimaryKey".
        /// - DataSource: If it is null, sets the ErrorObject flag to Errors.Failed and the ErrorObject message to "Missing Entity Datasource".
        /// </remarks>
        private bool IsRequirmentsValidated()
        {
            bool retval = true;
            if (EntityStructure == null)
            {
                retval = false;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Missing Entity Structure";

            }
            else
             if (EntityStructure.PrimaryKeys.Count == 0)
            {
                retval = false;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Missing Entity PrimaryKey";
            }
            if (DataSource == null)
            {
                retval = false;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Missing Entity Datasource";
            }
            return retval;
        }
        /// <summary>Opens the data source.</summary>
        /// <returns>True if the data source is successfully opened, false otherwise.</returns>
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
        /// <summary>Validates all necessary conditions before performing an operation.</summary>
        /// <returns>True if all conditions are valid, otherwise false.</returns>
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
        public void AssignToUnits(object retval, out object units)
        {
            units = null;

            var type = retval.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObservableBindingList<>))
            {
                units = retval;
            }
        }
        public T GetItemFroCurrentList(int index)
        {
            return _units.GetItemFroCurrentList(index);

        }
        public Tracking GetTrackingITem(T item)
        {


            return _units.GetTrackingITem(item);
        }
        #endregion "Misc"
        #region "Undo"
        public void UndoLastChange()
        {
            Tracking tracking;
            if (changeLog.Count > 0)
            {
                var lastChange = changeLog.Pop();
                switch (lastChange.ChangeType)
                {
                    case ChangeType.Insert:
                        Units.Remove(lastChange.Entity);
                        Delete(lastChange.Entity);
                        break;
                    case ChangeType.Update:
                        Units.SuppressNotification = true;
                        foreach (var originalValue in lastChange.OriginalValues)
                        {
                            var property = lastChange.Entity.GetType().GetProperty(originalValue.Key);
                            if (property != null)
                            {
                                property.SetValue(lastChange.Entity, originalValue.Value);
                            }
                        }
                        tracking= _units.GetTrackingITem(lastChange.Entity);
                        if(tracking!= null)
                        {
                            if (tracking.IsNew)
                            {
                                Units.Remove(lastChange.Entity);
                                Delete(lastChange.Entity);

                            }
                            else
                            {
                                var entityKeyAsString = Convert.ToString(tracking.OriginalIndex);
                                if (UpdatedKeys.ContainsValue(entityKeyAsString))
                                {
                                    var key = UpdatedKeys.FirstOrDefault(x => x.Value == entityKeyAsString).Key;
                                    UpdatedKeys.Remove(key);
                                }
                                tracking.EntityState = EntityState.Unchanged;

                            }
                        }
                        Units.SuppressNotification = false;
                        break;
                    case ChangeType.Delete:
                        Units.Add(lastChange.Entity);
                        tracking = _units.GetTrackingITem(lastChange.Entity);
                        if (tracking != null)
                        {
                                var entityKeyAsString = Convert.ToString(tracking.OriginalIndex);
                                if (DeletedKeys.ContainsValue(entityKeyAsString))
                                {
                                    var key = DeletedKeys.FirstOrDefault(x => x.Value == entityKeyAsString).Key;
                                    DeletedKeys.Remove(key);
                                }
                                tracking.EntityState = EntityState.Unchanged;
                           
                        }
                        break;
                }
            }
        }

        #endregion "Undo"
        #region "Moving"
        public void MoveFirst()
        {
            Units.MoveFirst();
        }

        public void MoveNext()
        {
            Units.MoveNext();
        }

        public void MovePrevious()
        {
            Units.MovePrevious();
        }

        public void MoveLast()
        {
            Units.MoveLast();
        }

        public void MoveTo(int index)
        {
            Units.MoveTo(index);
        }
        #endregion "Moving"
        public virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (_suppressNotification) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }
        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~UnitofWork()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
    public class ChangeLogEntry<T>
    {
        public T Entity { get; set; }
        public Dictionary<string, object> OriginalValues { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; set; } = new Dictionary<string, object>();
        public ChangeType ChangeType { get; set; }
    }
    public enum ChangeType
    {
        Insert,
        Update,
        Delete
    }

}
