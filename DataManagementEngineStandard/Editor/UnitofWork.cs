using DataManagementModels.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Util;


namespace TheTechIdea.Beep.Editor
{
    public class UnitofWork<T> :  IUnitofWork<T> where T:  Entity, new() 
    {
        private bool _suppressNotification = false;
        CancellationTokenSource tokenSource;
        CancellationToken token;
        private bool IsPrimaryKeyString = false;
        private bool Ivalidated = false;
        private bool IsNewRecord = false;
        private bool IsFilterOn = false;
        public bool IsDirty { get { return GetIsDirty(); } }
        #region "Inotify"
       

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
    #endregion
        #region "Collections"
    private ObservableBindingList<T> Tempunits;
        private ObservableBindingList<T> _units;
        //public ObservableBindingList<T> Units
        //{
        //    get
        //    {
        //        if (IsFilterOn)
        //        {
        //            return _filteredunits;
        //        }
        //        return _units; 
            
        //    }
        //    set
        //    {
               
        //        if (_units != value) // Check if it's a new collection
        //        {
        //            if (_units != null)
        //            {
        //                foreach (var item in _units)
        //                {
        //                    item.PropertyChanged -= ItemPropertyChangedHandler; // Remove previous event handlers
        //                }
        //                _units.CollectionChanged -= Units_CollectionChanged;
        //            }
        //            if (_filteredunits != null)
        //            {
        //                foreach (var item in _filteredunits)
        //                {
        //                    item.PropertyChanged -= ItemPropertyChangedHandler; // Remove previous event handlers
        //                }
        //                _filteredunits.CollectionChanged -= Units_CollectionChanged;
        //                _filteredunits=new ObservableBindingList<T> ();
        //            }
        //        }
        //         _units = value;
        //        if (_units != null)
        //        {
        //            foreach (var item in _units)
        //            {
        //                item.PropertyChanged += ItemPropertyChangedHandler; // Make sure you attach this
        //            }
        //            _units.CollectionChanged += Units_CollectionChanged;
        //        }
        //    }
        //}
        private ObservableBindingList<T> _filteredunits;
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

        private void SetUnits(ObservableBindingList<T> value)
        {
            if (_units != value)
            {
                DetachHandlers(_units);
                _units = value;
                AttachHandlers(_units);
                // You might want to update _filteredunits based on new _units here
                OnPropertyChanged(nameof(Units)); // Notify UI of change
            }
        }

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

        #endregion
        #region "Properties"
        public bool IsInListMode { get; set; } = false;
        private Dictionary<int, EntityState> _entityStates = new Dictionary<int, EntityState>();
        private Dictionary<T, EntityState> _deletedentities = new Dictionary<T, EntityState>();
        Stack<Tuple<T, int>> undoDeleteStack = new Stack<Tuple<T, int>>();
        protected virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public string Sequencer { get; set; }
        public string DatasourceName { get; set; }
        public List<T> DeletedUnits { get; set; } = new List<T>();
        public Dictionary<int, string> InsertedKeys { get; set; } = new Dictionary<int, string>();
        public Dictionary<int, string> UpdatedKeys { get; set; } = new Dictionary<int, string>();
        public Dictionary<int, string> DeletedKeys { get; set; } = new Dictionary<int, string>();
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
        #endregion
        #region "Constructors"
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
            
            _suppressNotification = false;
        }
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
         
            _suppressNotification = false;
        }
        public UnitofWork(IDMEEditor dMEEditor, bool isInListMode, ObservableBindingList<T> ts, string primarykey)
        {
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            IsInListMode = isInListMode;
            EntityStructure = new EntityStructure();
            init();
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
        #endregion
        #region "Misc Methods"
        public void Clear()
        {
           
            _suppressNotification = true;
            IsFilterOn = false;
            if (Units != null)
            {
                _units.Clear();
            }else
            Units=new ObservableBindingList<T> ();
            if (FilteredUnits != null)
            {
                FilteredUnits.Clear();
            }else
                _filteredunits=new ObservableBindingList<T> ();
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
        private void init()
        {
            if (!Validateall())
            {
                return;
            }
             Clear();
        }
     

        private void Units_CurrentChanged(object sender, EventArgs e)
        {
            if (_suppressNotification)
            {
                return;
            }
        }

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
        public object GetIDValue(T entity)
        {
            if (!Validateall())
            {
                return null;
            }
            var idValue = entity.GetType().GetProperty(PrimaryKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.GetValue(entity, null);

            return idValue;

        }
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
        public int Getindex(T entity)
        {
            if (!Validateall())
            {
                return -1;
            }
            int index = Units.IndexOf(entity);
            return index;

        }
        #endregion
        #region "CRUD Operations"
        private async Task<IErrorsInfo> UpdateAsync(T doc)
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
        private async Task<IErrorsInfo> InsertAsync(T doc)
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
         //   var entityidx = DocExistByKey(doc);
            //if (entityidx == -1)
            //{

            //    DMEEditor.ErrorObject.Flag = Errors.Failed;
            //    DMEEditor.ErrorObject.Message = "Object exist";
            //    return DMEEditor.ErrorObject;
            //}
            IErrorsInfo retval = await InsertDoc(doc);
            return retval;
        }
        private async Task<IErrorsInfo> DeleteAsync(T doc)
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
        private Task<IErrorsInfo> InsertDoc(T doc)
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
            IErrorsInfo retval = DataSource.InsertEntity(cname, doc);

            return Task.FromResult<IErrorsInfo>(retval);
        }
        private Task<IErrorsInfo> UpdateDoc(T doc)
        {
            string[] classnames = doc.ToString().Split(new Char[] { ' ', ',', '.', '-', '\n', '\t' });
            string cname = classnames[classnames.Count() - 1];
            IErrorsInfo retval = DataSource.UpdateEntity(cname, doc);
            return Task.FromResult<IErrorsInfo>(retval);
        }
        private Task<IErrorsInfo> DeleteDoc(T doc)
        {
            string[] classnames = doc.ToString().Split(new Char[] { ' ', ',', '.', '-', '\n', '\t' });
            string cname = classnames[classnames.Count() - 1];
            IErrorsInfo retval = DataSource.DeleteEntity(cname, doc);
            return Task.FromResult<IErrorsInfo>(retval);
        }
      
        public void Create(T entity)
        {
            if (!Validateall())
            {
                return;
            }
            Units.Add(entity);
           // int index = Getindex(entity);
        //    _entityStates.Add(index, EntityState.Added);
            // Subscribe to PropertyChanged event
            entity.PropertyChanged += ItemPropertyChangedHandler;
        }
        public T Read(string id)
        {
            if (!Validateall())
            {
                return default(T);
            }
            return Units[Getindex(id)];
        }
        public ErrorsInfo Delete(T entity)
        {
            ErrorsInfo errorsInfo = new ErrorsInfo();
            if (!Validateall())
            {
                errorsInfo.Message = "Validation Failed";
                errorsInfo.Flag = Errors.Failed;
                return errorsInfo;
            }
            var index = Getindex(entity);
            if (index >= 0)
            {
                Units[index] = entity;
                Units.RemoveAt(index);  
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
        public ErrorsInfo Update( T entity)
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
                Units.RemoveAt(index);
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
        public virtual async Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token)
        {
            _suppressNotification = true;
            if (IsInListMode)
            {
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
                        errorsInfo = await InsertAsync(Units[t]);
                        if (errorsInfo.Flag == Errors.Ok)
                        {
                            InsertedKeys.Remove(t);
                            _entityStates.Remove(t);
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
                        errorsInfo = await UpdateAsync(Units[t]);
                        if (errorsInfo.Flag == Errors.Ok)
                        {
                            UpdatedKeys.Remove(t);
                            _entityStates.Remove(t);
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
                        errorsInfo = await DeleteAsync(t);
                        if (errorsInfo.Flag == Errors.Ok)
                        {

                            _deletedentities.Remove(t);
                        }
                        x++;
                    }
                    DeletedKeys.Clear();
                    undoDeleteStack.Clear();
                }


                //foreach (var t in InsertedKeys)
                //{
                //    args.ParameterInt1 = x;
                //    progress.Report(args);
                //    errorsInfo = await InsertAsync(Units[t.Value]);
                //    x++;
                //}
                //foreach (var t in UpdatedKeys)
                //{
                //    args.ParameterInt1 = x;
                //    progress.Report(args);
                //    errorsInfo = await UpdateAsync(Units[t.Value]);
                //    x++;
                //}
                //foreach (var t in DeletedKeys)
                //{
                //    args.ParameterInt1 = x;
                //    progress.Report(args);
                //    errorsInfo = await DeleteAsync(Units[t.Value]);
                //    x++;
                //}
                args.Messege = $"Ended Saving Changes";
                args.ParameterString1 = $"Ended Saving Changes";
                args.ErrorCode = "END";
                progress.Report(args);
                _suppressNotification = false;
            }
            catch (Exception ex)
            {
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
        public virtual async Task<IErrorsInfo> Commit()
        {
            _suppressNotification = true;
            if (IsInListMode)
            {
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
                        errorsInfo = await InsertAsync(Units[t]);
                        if (errorsInfo.Flag == Errors.Ok)
                        {
                            InsertedKeys.Remove(t);
                            _entityStates.Remove(t);
                        }
                        x++;
                    }
                }
                if (GetModifiedEntities() != null)
                {
                    foreach (int t in GetModifiedEntities())
                    {
                       
                    //    int r = Getindex(t.ToString());
                        errorsInfo = await UpdateAsync(Units[t]);
                        if(errorsInfo.Flag== Errors.Ok)
                        {
                            UpdatedKeys.Remove(t);
                            _entityStates.Remove(t);
                        }
                        x++;
                    }
                }
                if (GetDeletedEntities() != null)
                {
                    foreach (T t in GetDeletedEntities())
                    {
                       
                        // int r = Getindex(t.ToString());
                        errorsInfo = await DeleteAsync(t);
                        if (errorsInfo.Flag == Errors.Ok)
                        {
                            
                            _deletedentities.Remove(t);
                        }
                        x++;
                    }
                    DeletedKeys.Clear();
                    undoDeleteStack.Clear();
                }


           
                _suppressNotification = false;
            }
            catch (Exception ex)
            {
                _suppressNotification = false;
             
                errorsInfo.Ex = ex;
                DMEEditor.AddLogMessage("UnitofWork", $"Saving and Commiting Changes error {ex.Message}", DateTime.Now, args.ParameterInt1, ex.Message, Errors.Failed);
            }
            _suppressNotification = false;
            return await Task.FromResult<IErrorsInfo>(errorsInfo);
        }
        public virtual int GetSeq(string SeqName) 
        {
            int retval = -1;
            if(DataSource.Category== DatasourceCategory.RDBMS)
            {
                string str = RDBMSHelper.GenerateFetchNextSequenceValueQuery(DataSource.DatasourceType, SeqName);
                if (!string.IsNullOrEmpty(str))
                {
                    var r= DataSource.GetScalar(str);
                     if(r!=null)
                    {
                        retval = (int)r;
                    }
                    
                }
            }
            return retval;
        }
        public virtual int GetPrimaryKeySequence(T doc)
        {
            int retval = -1;
            if (DataSource.Category == DatasourceCategory.RDBMS && !string.IsNullOrEmpty(Sequencer))
            {
                retval = GetSeq(Sequencer);
                if(retval > 0)
                {
                    SetIDValue(doc, retval);
                }
            }
            return retval;
        }
        #endregion
        #region "Get Methods"
        public virtual async Task<ObservableBindingList<T>> Get(List<AppFilter> filters)
        {
            if (filters == null)
            {
                IsFilterOn=false;
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
                        FilteredUnits = FilterCollection(Units, filters);
                        _suppressNotification = false;
                        return await Task.FromResult(FilteredUnits);

                    }

                }
            }
            return await Task.FromResult(Units);



        }
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
                    DMEEditor.AddLogMessage("Beep",$" Unit of Work Could not get Data in units {ex.Message} ", DateTime.Now, -1, null, Errors.Failed); 
                }
              
            }
            _suppressNotification = false;
            return await Task.FromResult(Units);
        }
        public virtual T Get(int key)
        {
            return Units[key];
        }
        public virtual T Get(string PrimaryKeyid)
        {

            var retval = Units.FirstOrDefault(p => p.GetType().GetProperty(PrimaryKey).GetValue(p, null).ToString() == PrimaryKeyid);
            return retval;
        }
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
                if (retval is IList)
                {
                    list = (List<T>)retval;

                    Units = new ObservableBindingList<T>(list);
                }
                else
                {
                    if (retval is DataTable)
                    {
                        DataTable dataTable = (DataTable)retval;
                        //Units
                        Units = new ObservableBindingList<T>(DMEEditor.Utilfunction.ConvertDataTable<T>(dataTable));
                    }
                }
                _suppressNotification = false;
                return true;
            }
            catch (Exception ex)
            {
                _suppressNotification = false;
                DMEEditor.AddLogMessage("Beep", $"Error Converting Data to Units {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

        }
        #endregion
        #region "Find Methods"
        public virtual int FindDocIdx(T doc)
        {
            int retval = -1;

            retval = Units.IndexOf(doc);

            return retval;
        }
        public virtual int DocExistByKey(T doc)
        {
            int retval = Units.IndexOf(Units.FirstOrDefault(p => p.GetType().GetProperty(PrimaryKey).GetValue(doc, null).Equals(doc.GetType().GetProperty(PrimaryKey).GetValue(doc, null))));
            return retval;
        }
        public virtual int DocExist(T doc)
        {
            int retval = -1;

            retval = Units.IndexOf(doc);

            return retval;
        }
        #endregion
        #region "Entity Management"
        public bool GetIsDirty()
        {
            if (!Validateall())
            {
                return false;
            }
            if (InsertedKeys.Count > 0 || UpdatedKeys.Count > 0 || DeletedKeys.Count > 0)
            {
                return true;
            }
            return false;
        }
        public IEnumerable<int> GetAddedEntities()
        {
            if (!Validateall())
            {
                return null;
            }
            return _entityStates.Where(x => x.Value == EntityState.Added).Select(x => x.Key);
        }
        public IEnumerable<int> GetModifiedEntities()
        {
            if (!Validateall())
            {
                return null;
            }
            return _entityStates.Where(x => x.Value == EntityState.Modified).Select(x => x.Key);
        }
        public IEnumerable<T> GetDeletedEntities()
        {
            if (!Validateall())
            {
                return null;
            }
            return _deletedentities.Where(x => x.Value == EntityState.Deleted).Select(x => x.Key);
        }
        private void Units_ListChanged(object sender, ListChangedEventArgs e)
        {
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
                if (!UpdatedKeys.Any(p => p.Value.Equals(Convert.ToString(PKProperty.GetValue(item, null)))))
                {
                    keysidx++;
                    UpdatedKeys.Add(keysidx, Convert.ToString(PKProperty.GetValue(item, null)));
                }
            }
        }
        private void Units_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
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
                            GetPrimaryKeySequence(item);
                            if (!InsertedKeys.ContainsValue(Convert.ToString(PKProperty.GetValue(item, null))))
                            {
                                InsertedKeys.Add(keysidx, Convert.ToString(PKProperty.GetValue(item, null)));
                                _entityStates.Add(e.NewStartingIndex, EntityState.Added);
                            }
                        }
                    }
                    IsNewRecord=false;
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (T item in e.OldItems)
                    {
                        UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false };
                        PreDelete?.Invoke(item, ps);
                        if (!ps.Cancel)
                        {
                            undoDeleteStack.Push(new Tuple<T, int>(item, e.OldStartingIndex));
                            keysidx++;
                            DeletedKeys.Add(keysidx, Convert.ToString(PKProperty.GetValue(item, null)));
                            //_entityStates.Add(e.OldStartingIndex, EntityState.Deleted);
                            _deletedentities.Add(item, EntityState.Deleted);
                        }
                        else
                            UndoDelete(item,e.OldStartingIndex);
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
        private void ItemPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (_suppressNotification || IsNewRecord )
            {
                return;
            }
            T item = (T)sender;
            if (item != null)
            {
                if (InsertedKeys.ContainsValue(Convert.ToString(PKProperty.GetValue(item, null))))
                {
                    
                    return;
                }
            }
            if (!UpdatedKeys.Any(p => p.Value.Equals(Convert.ToString(PKProperty.GetValue(item, null)))))
            {
                keysidx++;
                UpdatedKeys.Add(keysidx, Convert.ToString(PKProperty.GetValue(item, null)));
                int x= Getindex(item);
                _entityStates.Add(x, EntityState.Modified);
            }
            CurrentProperty = item.GetType().GetProperty(e.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false,PropertyName=e.PropertyName,PropertyValue= Convert.ToString(CurrentProperty.GetValue(item, null))};
            PostEdit?.Invoke(item, ps);

        }
        private ObservableBindingList<T> FilterCollection(ObservableBindingList<T> originalCollection, List<AppFilter> filters)
        {
            try
            {
                var parameter = Expression.Parameter(typeof(T), "x");

                Expression combinedExpression = null;
                foreach (var filter in filters)
                {
                    var property = Expression.Property(parameter, filter.FieldName);
                    var propertyType = property.Type;

                    object convertedValue;
                    if (propertyType.IsEnum)
                    {
                        convertedValue = Enum.Parse(propertyType, filter.FilterValue.ToString());
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(filter.FilterValue, propertyType);
                    }

                    var constant = Expression.Constant(convertedValue, propertyType);
                    var equality = Expression.Equal(property, constant);

                    if (combinedExpression == null)
                    {
                        combinedExpression = equality;
                    }
                    else
                    {
                        combinedExpression = Expression.AndAlso(combinedExpression, equality);
                    }
                }

                if (combinedExpression == null)
                {
                    throw new Exception("No filters provided.");
                }

                var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);

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
        private void UndoDelete(T itemToReinsert, int indexToReinsertAt)
        {
            // Insert the item back into the list at the original index
            // Assuming 'YourList' is the list from which items were deleted
            Units.Insert(indexToReinsertAt, itemToReinsert);

            // Optionally, remove any state or keys related to the deleted item
            // (like undoing changes to DeletedKeys, _entityStates, etc.)
        }
        // Function to undo a delete operation
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
        private bool Validateall()
        {
            if (Ivalidated)
            {
                return true;
            }
            bool retval = true;
            if(IsInListMode)
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
    public enum EntityState
    {
        Added,
        Modified,
        Deleted
    }
}
