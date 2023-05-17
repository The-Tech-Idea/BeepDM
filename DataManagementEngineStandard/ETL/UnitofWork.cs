using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;

using System.Linq;

using System.Reflection;

using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Util;



namespace TheTechIdea.Beep.Editor
{
    public class UnitofWork<T> where T : class, INotifyPropertyChanged, new() 
    {
        CancellationTokenSource tokenSource;
        CancellationToken token;
        private  Dictionary<T, EntityState> _entityStates;
        protected virtual event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        public UnitofWork(IDMEEditor dMEEditor,string datasourceName)
        {
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
            if (OpenDataSource())
            {
                init();
            }
        }
        public UnitofWork(IDMEEditor dMEEditor, string datasourceName, string entityName)
        {
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
            EntityName = entityName;
            if (OpenDataSource())
            {
                init();
            }
        }
        public UnitofWork(IDMEEditor dMEEditor, string datasourceName, string entityName,EntityStructure entityStructure)
        {
            DMEEditor = dMEEditor;
            DatasourceName = datasourceName;
            EntityName = entityName;
            EntityStructure=entityStructure;
            if (OpenDataSource())
            {
                init();
            }
        }
        public  ObservableCollection<T> Units { get; set; }
        public string DatasourceName { get; set; }
        public List<T> DeletedUnits { get; set; } = new List<T>();
        public Dictionary<int,int> InsertedKeys { get; set; } = new Dictionary<int,int>();
        public Dictionary<int,int>  UpdatedKeys { get; set; } = new Dictionary<int,int>();
        public Dictionary<int,int>  DeletedKeys { get; set; } = new Dictionary<int,int>();
        public EntityStructure EntityStructure { get; set;}
        public IDMEEditor DMEEditor { get; }
        public IDataSource DataSource { get; set; }
        public string EntityName { get; set; }
        public Type EntityType { get; set; }
        public string PrimaryKey { get; set; }
        public string GuidKey { get; set; }

        PropertyInfo PKProperty=null;
        PropertyInfo Guidproperty = null;

        int keysidx ;
        private void init()
        {
            Units = new ObservableCollection<T>();
         
            Units.CollectionChanged -= Units_CollectionChanged;
            Units.CollectionChanged += Units_CollectionChanged;
            DeletedUnits  = new List<T>();
            InsertedKeys= new Dictionary<int,int>();
            UpdatedKeys  = new Dictionary<int,int>();
            DeletedKeys  = new Dictionary<int,int>();
            _entityStates = new Dictionary<T, EntityState>();
            PrimaryKey = EntityStructure.PrimaryKeys.FirstOrDefault().fieldname;
            EntityType = DataSource.GetEntityType(EntityName);
            keysidx = 0;
            
        }
        public void Create(T entity)
        {
            Units.Add(entity);
            _entityStates[entity] = EntityState.Added;
        }
        public T Read(int id)
        {
            return Units.FirstOrDefault(x => x.GetType().GetProperty("Id")?.GetValue(x, null).ToString() == id.ToString());
        }
        public void Update(int id, T entity)
        {
            var index = Units.IndexOf(Units.FirstOrDefault(x => x.GetType().GetProperty("Id")?.GetValue(x, null).ToString() == id.ToString()));
            if (index >= 0)
            {
                Units[index] = entity;
                _entityStates[entity] = EntityState.Modified;
            }
        }
       public void Delete(int id)
        {
            var entity = Units.FirstOrDefault(x => x.GetType().GetProperty("Id")?.GetValue(x, null).ToString() == id.ToString());
            if (entity != null)
            {
                Units.Remove(entity);
                _entityStates[entity] = EntityState.Deleted;
            }
        }

        public IEnumerable<T> GetAddedEntities()
        {
            return _entityStates.Where(x => x.Value != EntityState.Added).Select(x => x.Key);
        }
        public IEnumerable<T> GetModifiedEntities()
        {
            return _entityStates.Where(x => x.Value != EntityState.Modified).Select(x => x.Key);
        }
        public IEnumerable<T> GetDeletedEntities()
        {
            return _entityStates.Where(x => x.Value == EntityState.Deleted).Select(x => x.Key);
        }

       
        private void Units_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (T item in e.NewItems)
                    {
                        keysidx++;
                        InsertedKeys.Add(keysidx, (int)PKProperty.GetValue(item, null));
                        item.PropertyChanged += ItemPropertyChangedHandler;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (T item in e.OldItems) {
                        keysidx++;
                        DeletedKeys.Add(keysidx, (int)PKProperty.GetValue(item, null));
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    foreach (T item in e.OldItems)
                    {
                        keysidx++;
                        DeletedKeys.Add(keysidx, (int)PKProperty.GetValue(item, null));

                    }
                    foreach (T item in e.NewItems)
                    {
                        keysidx++;
                        InsertedKeys.Add(keysidx, (int)PKProperty.GetValue(item, null));
                    }
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
            if (e.OldItems != null)
            {
                foreach (T item in e.OldItems)
                {
                    _entityStates[item] = EntityState.Deleted;
                }
            }

            if (e.NewItems != null)
            {
                foreach (T item in e.NewItems)
                {
                    _entityStates[item] = EntityState.Added;
                }
            }
        }

        private void ItemPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
          
            T item = (T)sender;
            if (!UpdatedKeys.Any(p=>p.Value.Equals((int)PKProperty.GetValue(item, null))))
            {
                keysidx++;
                UpdatedKeys.Add(keysidx, (int)PKProperty.GetValue(item, null));
            }
        }

        public virtual async Task<ObservableCollection<T>> GetAllFromSource()
        {
            clearunits();
            var retval=DataSource.GetEntityAsync(EntityName,null).Result;
            if (retval != null)
            {
                if(retval is DataTable)
                {
                    DataTable dataTable = (DataTable)retval;
                    Units = ConvertDataTableToObservable<T>(dataTable);
                }
                if(retval is IList)
                {
                    List<T> list = (List<T>)retval;
                    Units = ConvertList2Observable<T>(list);
                }
            }
            return await Task.FromResult(Units);
        }
        private void clearunits()
        {
            Units.Clear();
            DeletedKeys.Clear();
            InsertedKeys.Clear();
            InsertedKeys.Clear();
            DeletedUnits.Clear();
            _entityStates = new Dictionary<T, EntityState>();
        }
        public virtual async Task<IErrorsInfo> Commit(IProgress<PassedArgs> progress, CancellationToken token)
        {
            PassedArgs args = new PassedArgs();
            IErrorsInfo errorsInfo = new ErrorsInfo();
            int x = 1;
            args.ParameterInt1=InsertedKeys.Count+UpdatedKeys.Count+DeletedKeys.Count;
            args.ParameterString1 = $"Started Saving Changes {args.ParameterInt1}";
            progress.Report(args);
            try
            {
                foreach (T t in GetAddedEntities())
                {
                    args.ParameterInt1 = x;
                    progress.Report(args);
                    errorsInfo = await InsertAsync(t);
                    x++;
                }
                foreach (var t in GetModifiedEntities())
                {
                    args.ParameterInt1 = x;
                    progress.Report(args);
                    errorsInfo = await UpdateAsync(t);
                    x++;
                }
                foreach (var t in GetDeletedEntities())
                {
                    args.ParameterInt1 = x;
                    progress.Report(args);
                    errorsInfo = await DeleteAsync(t);
                    x++;
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
                args.ParameterString1 = $"Ended Saving Changes";
                progress.Report(args);
            }
            catch (Exception ex)
            {
                args.ParameterString1 = $"Error Saving Changes {ex.Message}";
                progress.Report(args);
                errorsInfo.Ex = ex;
                DMEEditor.AddLogMessage("UnitofWork", $"Saving and Commiting Changes error {ex.Message}", DateTime.Now, args.ParameterInt1, ex.Message, Errors.Failed);
            }
            
            return await Task.FromResult<IErrorsInfo>(errorsInfo);
        }
        public virtual async Task<ObservableCollection<T>> GetByConditionFromSource(List<AppFilter> filters)
        {
            var retval = DataSource.GetEntityAsync(EntityName, filters).Result;
            if (retval != null)
            {
                if (retval is DataTable)
                {
                    DataTable dataTable = (DataTable)retval;
                    Units = ConvertDataTableToObservable<T>(dataTable);
                }
                if (retval is IList)
                {
                    List<T> list = (List<T>)retval;
                    Units = ConvertList2Observable<T>(list);
                }
            }
            return await Task.FromResult(Units);
        }
        private async Task<IErrorsInfo> UpdateAsync(T doc)
        {
            if (!IsRequirmentsValidated())
            {
                return DMEEditor.ErrorObject;
            }
            if (doc == null)
            {
                DMEEditor.ErrorObject.Flag= Errors.Failed;
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
            if (!string.IsNullOrEmpty(PrimaryKey))
            {
                if (PKProperty == null)
                {
                    PKProperty = doc.GetType().GetProperty(PrimaryKey);
                }
                PKProperty.SetValue(GetPrimaryKey(doc), null);

            }
            if (!string.IsNullOrEmpty(GuidKey))
            {
                if (Guidproperty == null)
                {
                    Guidproperty = doc.GetType().GetProperty(GuidKey);
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
        public virtual int GetSeq(string SeqName)
        {
            int retval = -1;
            if(DataSource.Category== DatasourceCategory.RDBMS && DataSource.DatasourceType== DataSourceType.Oracle )
            {
                string querystring = $"select {SeqName}.nextval from dual";
                string schname = DataSource.Dataconnection.ConnectionProp.SchemaName;
                string userid = DataSource.Dataconnection.ConnectionProp.UserID;
                if (schname != null)
                {
                    if (!schname.Equals(userid, StringComparison.InvariantCultureIgnoreCase))
                    {
                        querystring = $"select {schname}.{SeqName}.nextval from dual";
                    }
                    else
                        querystring = $"select {SeqName}.nextval from dual";
                }
                retval=(int)DataSource.RunQuery(querystring);
            }
            return retval;
        }
        public virtual int FindDocIdx(T doc)
        {
            int retval = -1;

            retval = Units.IndexOf(doc);

            return retval;
        }
        public virtual T GetDocFromList(KeyValuePair<int, int> key)
        {
            return Units[key.Value];
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
        public virtual int GetPrimaryKey(T doc)
        {
            int retval = -1;
            if (DataSource.Category == DatasourceCategory.RDBMS && DataSource.DatasourceType == DataSourceType.Oracle)
            {
                retval = GetSeq(PrimaryKey);
            }
            return retval;
        }
        private  bool IsRequirmentsValidated()
        {
            bool retval = true;
            if (EntityStructure == null)
            {
                retval = false;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Missing Entity Structure";

            }else
             if (EntityStructure.PrimaryKeys.Count==0 )
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
            bool retval= true;
            if(DataSource == null)
            {
                if(!string.IsNullOrEmpty(DatasourceName)) { 
                    DataSource=DMEEditor.GetDataSource(DatasourceName);
                    if (DataSource == null)
                    {
                        DMEEditor.AddLogMessage("Beep", $"Error Opening DataSource in UnitofWork {DatasourceName}", DateTime.Now, -1, DatasourceName, Errors.Failed);
                        retval= false;
                    }
                    else
                        retval = true;
                }
            }
            return retval;
        }
        public static ObservableCollection<T1> ConvertDataTableToObservable<T1>( DataTable table) where T1 : class, new()
        {
            try
            {
                ObservableCollection<T1> list = new ObservableCollection<T1>();

                foreach (var row in table.AsEnumerable())
                {
                    T1 obj = new T1();

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        try
                        {
                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
                            propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    list.Add(obj);
                }

                return list;
            }
            catch
            {
                return null;
            }
        }
        public static ObservableCollection<T1> ConvertList2Observable<T1>(IList table) where T1 : class, new()
        {
            try
            {
                ObservableCollection<T1> list = new ObservableCollection<T1>();

                foreach (var row in table)
                {
                    T1 obj = new T1();

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        try
                        {
                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
                            var vl= propertyInfo.GetValue(row, null);
                            propertyInfo.SetValue(obj, Convert.ChangeType(vl, propertyInfo.PropertyType), null);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    list.Add(obj);
                }

                return list;
            }
            catch
            {
                return null;
            }
        }
    }
    
    public enum EntityState
    {
        Added,
        Modified,
        Deleted
    }
}
