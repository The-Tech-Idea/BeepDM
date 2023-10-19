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
    public class UnitofWork<T> : IUnitofWork<T> where T:  Entity, INotifyPropertyChanged, new()
    {
        public bool IsInListMode { get; set; } = false;
        private bool IsPrimaryKeyString = false;
        private bool Ivalidated = false;
        public event EventHandler<UnitofWorkParams> PreInsert;
        public event EventHandler<UnitofWorkParams> PreUpdate;
        public event EventHandler<UnitofWorkParams> PreQuery;
        public event EventHandler<UnitofWorkParams> PostQuery;
        public event EventHandler<UnitofWorkParams> PostInsert;
        public event EventHandler<UnitofWorkParams> PostUpdate;
        public UnitofWork(IDMEEditor dMEEditor, string datasourceName, string entityName, string primarykey)
        {
            IsInListMode = false;
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
          //  EntityStructure = new EntityStructure();
            EntityName = entityName;
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
        public UnitofWork(IDMEEditor dMEEditor, string datasourceName, string entityName, EntityStructure entityStructure, string primarykey)
        {
            IsInListMode = false;
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
            EntityName = entityName;
            EntityStructure = entityStructure;
            PrimaryKey = entityStructure.PrimaryKeys.FirstOrDefault().fieldname;
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
        public UnitofWork(IDMEEditor dMEEditor, bool isInListMode, ObservableBindingList<T> ts,string primarykey)
        {
            _suppressNotification = true;
            DMEEditor = dMEEditor;
            IsInListMode = isInListMode;
            EntityStructure = new EntityStructure();
            init();
            Units = ts;
            PrimaryKey = primarykey;
            if (ts == null || ts.Count==0)
            {
                T doc=new();
                getPrimaryKey(doc);
            }
            else
            {
                getPrimaryKey(ts.FirstOrDefault());
            }
            
            _suppressNotification = false;
        }
        private bool _suppressNotification = false;
        CancellationTokenSource tokenSource;
        CancellationToken token;
        private Dictionary<int, EntityState> _entityStates=new Dictionary<int, EntityState>();
        private Dictionary<T, EntityState> _deletedentities=new Dictionary<T, EntityState>();
        protected virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
       
        private ObservableBindingList<T> _units;
        public ObservableBindingList<T> Units
        {
            get { return _units; }
            set
            {
                clearunits();
                //if (_units != null)
                //{
                //    _units.CollectionChanged -= Units_CollectionChanged;
                //    foreach (var item in _units)
                //    {
                //        item.PropertyChanged -= ItemPropertyChangedHandler;
                //    }
                   
                //}
               
                _units = value;

                if (_units != null)
                {
                    
                    _units.CollectionChanged += Units_CollectionChanged;
                  
                    foreach (var item in _units)
                    {
                        item.PropertyChanged += ItemPropertyChangedHandler; // Make sure you attach this
                    }

                }
            }
        }
        private ObservableBindingList<T> _filteredunits;
        public ObservableBindingList<T> FilteredUnits
        {
            get { return _filteredunits; }
            set
            {
                if (_filteredunits != null)
                {
                    _filteredunits.CollectionChanged -= Units_CollectionChanged;
                  

                }
                if (_filteredunits == null)
                {
                    _filteredunits = new ObservableBindingList<T>();
                }
                else
                    _filteredunits.Clear();
                _filteredunits = value;

                if (_filteredunits != null)
                {

                    _filteredunits.CollectionChanged += Units_CollectionChanged;
                   // _filteredunits.PropertyChanged += ItemPropertyChangedHandler;
                    foreach (var item in _filteredunits)
                    {
                        item.PropertyChanged += ItemPropertyChangedHandler; // Make sure you attach this
                    }

                }
            }
        }
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
        PropertyInfo Guidproperty = null;
        int keysidx;
        private bool disposedValue;
        #region "Misc Methods"
        private void clearunits()
        {
            _suppressNotification = true;
            if (Units != null)
            {
                Units.Clear();
            }
            if (FilteredUnits != null)
            {
                FilteredUnits.Clear();
            }
            DeletedKeys.Clear();
            InsertedKeys.Clear();
            InsertedKeys.Clear();
            DeletedUnits.Clear();
            _entityStates = new Dictionary<int, EntityState>();
            _deletedentities = new Dictionary<T, EntityState>();
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
            reset();
        }
        private void reset()
        {
            Units = new ObservableBindingList<T>();
            Units.CollectionChanged -= Units_CollectionChanged;
            Units.CollectionChanged += Units_CollectionChanged;
            Units.ListChanged += Units_ListChanged;
            DeletedUnits = new List<T>();
            InsertedKeys = new Dictionary<int, string>();
            UpdatedKeys = new Dictionary<int, string>();
            DeletedKeys = new Dictionary<int, string>();
            _entityStates = new Dictionary<int, EntityState>();
            _deletedentities = new Dictionary<T, EntityState>();
            keysidx = 0;
            if (!IsInListMode)
            {
                if (EntityType != null)
                {
                    EntityType = DataSource.GetEntityType(EntityName);
                }

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
            IErrorsInfo retval = await UpdateDoc(doc);
            return DMEEditor.ErrorObject;
        }
        private async Task<IErrorsInfo> InsertAsync(T doc)
        {
            if (!IsRequirmentsValidated())
            {
                return DMEEditor.ErrorObject;
            }
            UnitofWorkParams ps = new UnitofWorkParams() { Cancel = false };
            PreInsert?.Invoke(this, ps);
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
            var entityidx = DocExistByKey(doc);
            if (entityidx == -1)
            {

                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Object exist";
                return DMEEditor.ErrorObject;
            }
            IErrorsInfo retval = await InsertDoc(doc);
            return DMEEditor.ErrorObject;
        }
        private async Task<IErrorsInfo> DeleteAsync(T doc)
        {
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

            return DMEEditor.ErrorObject;
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
            int index = Getindex(entity);
            _entityStates.Add(index, EntityState.Added);
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
        public void Update(string id, T entity)
        {
            if (!Validateall())
            {
                return;
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
                    }
                    else
                    {
                        _entityStates.Add(index, EntityState.Modified);
                    }

                }

            }
        }
        public void Delete(string id)
        {
            if (!Validateall())
            {
                return;
            }
            var index = Getindex(id);

            if (index >= 0)
            {
                Units.RemoveAt(index);
                _entityStates[index] = EntityState.Deleted;
                //entity.PropertyChanged += ItemPropertyChangedHandler;
            }
        }
        #endregion
        #region "Get Methods"
        public virtual async Task<ObservableBindingList<T>> Get(List<AppFilter> filters)
        {
            if (!IsInListMode)
            {
                clearunits();
                var retval = DataSource.GetEntity(EntityName, filters);

                GetDataInUnits(retval);

            }
            else
            {
                if (filters != null && Units != null)
                {
                    if (Units.Count > 0)
                    {
                        _suppressNotification = true;
                        foreach (var filter in filters)
                        {

                            FilteredUnits = FilterCollection(Units, filters);
                            //FilteredUnits = new ObservableBindingList<T>();
                            //if (t != null)
                            //{
                            //    foreach (var item in t)
                            //    {
                            //        FilteredUnits.Add(item);
                            //    }
                            //}


                        }
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

            reset();
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
                    

                    foreach (var item in list)
                    {
                   //     item.PropertyChanged += ItemPropertyChangedHandler; // Make sure you attach this
                      //  SetIDValue(item, 1);
                        Units.Add(item);

                    }
                    _units.CollectionChanged += Units_CollectionChanged;
                    // Units =new ObservableBindingList<T>(list);
                   

                }
                else
                {
                    if (retval is DataTable)
                    {
                        DataTable dataTable = (DataTable)retval;
                        //Units
                        _units = DMEEditor.Utilfunction.ConvertDataTableToObservableBindingList<T>(dataTable);
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
        public IEnumerable<int> GetAddedEntities()
        {
            if (!Validateall())
            {
                return null;
            }
            return _entityStates.Where(x => x.Value != EntityState.Added).Select(x => x.Key);
        }
        public IEnumerable<int> GetModifiedEntities()
        {
            if (!Validateall())
            {
                return null;
            }
            return _entityStates.Where(x => x.Value != EntityState.Modified).Select(x => x.Key);
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
                    foreach (T item in e.NewItems)
                    {
                        keysidx++;
                        InsertedKeys.Add(keysidx, Convert.ToString(PKProperty.GetValue(item, null)));
                        // item.PropertyChanged += ItemPropertyChangedHandler;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (T item in e.OldItems)
                    {
                        keysidx++;
                        DeletedKeys.Add(keysidx, Convert.ToString(PKProperty.GetValue(item, null)));
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
                    clearunits();
                    break;
                default:
                    break;
            }

        }
        private void ItemPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (_suppressNotification)
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
            }
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
        #endregion

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
                        errorsInfo = await InsertAsync(Units[t]);
                        x++;
                    }
                }
                if (GetModifiedEntities()!=null)
                {
                    foreach (int t in GetModifiedEntities())
                    {
                        args.ParameterInt1 = x;
                        progress.Report(args);
                        errorsInfo = await UpdateAsync(Units[t]);
                        x++;
                    }
                }
                if (GetDeletedEntities() != null)
                {
                    foreach (T t in GetDeletedEntities())
                    {
                        args.ParameterInt1 = x;
                        progress.Report(args);
                        errorsInfo = await DeleteAsync(t);
                        x++;
                    }
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
        public virtual int GetSeq(string SeqName)
        {
            int retval = -1;
            
            return retval;
        }
        public virtual int GetPrimaryKeySequence(T doc)
        {
            int retval = -1;
            if (DataSource.Category == DatasourceCategory.RDBMS)
            {
                retval = GetSeq(PrimaryKey);
            }
            return retval;
        }
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
        //public static ObservableBindingList<T1> ConvertDataTableToObservable<T1>(DataTable table) where T1 : class, new()
        //{
        //    try
        //    {
        //        ObservableBindingList<T1> list = new ObservableBindingList<T1>();

        //        foreach (var row in table.AsEnumerable())
        //        {
        //            T1 obj = new T1();

        //            foreach (var prop in obj.GetType().GetProperties())
        //            {
        //                try
        //                {
        //                    PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
        //                    propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
        //                }
        //                catch
        //                {
        //                    continue;
        //                }
        //            }

        //            list.Add(obj);
        //        }

        //        return list;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
        //public static ObservableBindingList<T1> ConvertList2Observable<T1>(IList table) where T1 : class, new()
        //{
        //    try
        //    {
        //        ObservableBindingList<T1> list = new ObservableBindingList<T1>();

        //        foreach (var row in table)
        //        {
        //            T1 obj = new T1();

        //            foreach (var prop in obj.GetType().GetProperties())
        //            {
        //                try
        //                {
        //                    PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
        //                    var vl = propertyInfo.GetValue(row, null);
        //                    propertyInfo.SetValue(obj, Convert.ChangeType(vl, propertyInfo.PropertyType), null);
        //                }
        //                catch
        //                {
        //                    continue;
        //                }
        //            }

        //            list.Add(obj);
        //        }

        //        return list;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
    }
    public enum EntityState
    {
        Added,
        Modified,
        Deleted
    }
}
