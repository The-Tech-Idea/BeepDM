using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;
using System.Dynamic;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor
{
    public class UnitOfWorkWrapper
    {
        private dynamic _unitOfWork;

        public UnitOfWorkWrapper(object unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public void Clear() => _unitOfWork.Clear();

        public bool IsInListMode
        {
            get => _unitOfWork.IsInListMode;
            set => _unitOfWork.IsInListMode = value;
        }

        public bool IsDirty => _unitOfWork.IsDirty;

        public IDataSource DataSource
        {
            get => _unitOfWork.DataSource;
            set => _unitOfWork.DataSource = value;
        }

        public string DatasourceName
        {
            get => _unitOfWork.DatasourceName;
            set => _unitOfWork.DatasourceName = value;
        }

        public Dictionary<int, string> DeletedKeys
        {
            get => _unitOfWork.DeletedKeys;
            set => _unitOfWork.DeletedKeys = value;
        }

        public List<dynamic> DeletedUnits
        {
            get => _unitOfWork.DeletedUnits;
            set => _unitOfWork.DeletedUnits = value;
        }

        public IDMEEditor DMEEditor => _unitOfWork.DMEEditor;

        public string EntityName
        {
            get => _unitOfWork.EntityName;
            set => _unitOfWork.EntityName = value;
        }

        public EntityStructure EntityStructure
        {
            get => _unitOfWork.EntityStructure;
            set => _unitOfWork.EntityStructure = value;
        }

        public Type EntityType
        {
            get => _unitOfWork.EntityType;
            set => _unitOfWork.EntityType = value;
        }

        public string GuidKey
        {
            get => _unitOfWork.GuidKey;
            set => _unitOfWork.GuidKey = value;
        }

        public string Sequencer
        {
            get => _unitOfWork.Sequencer;
            set => _unitOfWork.Sequencer = value;
        }

        public Dictionary<int, string> InsertedKeys
        {
            get => _unitOfWork.InsertedKeys;
            set => _unitOfWork.InsertedKeys = value;
        }

        public string PrimaryKey
        {
            get => _unitOfWork.PrimaryKey;
            set => _unitOfWork.PrimaryKey = value;
        }

        public dynamic Units
        {
            get => _unitOfWork.Units;
            set => _unitOfWork.Units = value;
        }

        public Dictionary<int, string> UpdatedKeys
        {
            get => _unitOfWork.UpdatedKeys;
            set => _unitOfWork.UpdatedKeys = value;
        }

        public bool IsIdentity
        {
            get => _unitOfWork.IsIdentity;
            set => _unitOfWork.IsIdentity = value;
        }
        public double GetLastIdentity() => _unitOfWork.GetLastIdentity();

        public IEnumerable<int> GetAddedEntities() => _unitOfWork.GetAddedEntities();

        public async Task<dynamic> GetQueryAsync(string query)
        {
            return await _unitOfWork.GetQuery(query);
        }


        public async Task<dynamic> Ge(List<AppFilter> filters)
        {
            return await _unitOfWork.Get(filters);
        }

        public IEnumerable<dynamic> GetDeletedEntities()
        {
            return _unitOfWork.GetDeletedEntities();
        }

        public dynamic Get(int key)
        {
            return _unitOfWork.Get(key);
        }

        public dynamic GetIDValue(dynamic entity)
        {
            return _unitOfWork.GetIDValue(entity);
        }

        public int Getindex(string id)
        {
            return _unitOfWork.Getindex(id);
        }

        public int Getindex(dynamic entity)
        {
            return _unitOfWork.Getindex(entity);
        }

        public IEnumerable<int> GetModifiedEntities()
        {
            return _unitOfWork.GetModifiedEntities();
        }

        public int GetPrimaryKeySequence(dynamic doc)
        {
            return _unitOfWork.GetPrimaryKeySequence(doc);
        }

        public int GetSeq(string SeqName)
        {
            return _unitOfWork.GetSeq(SeqName);
        }

        public dynamic Read(string id)
        {
            return _unitOfWork.Read(id);
        }


        public void Create(dynamic entity)
        {
            _unitOfWork.Create(entity);
        }

        public ErrorsInfo Delete(string id)
        {
            return _unitOfWork.Delete(id);
        }

        public ErrorsInfo Delete(dynamic doc)
        {
            return _unitOfWork.Delete(doc);
        }

        public ErrorsInfo Update(dynamic entity)
        {
            return _unitOfWork.Update(entity);
        }

        public ErrorsInfo Update(string id, dynamic entity)
        {
            return _unitOfWork.Update(id, entity);
        }

        public int DocExist(dynamic doc)
        {
            return _unitOfWork.DocExist(doc);
        }

        public int DocExistByKey(dynamic doc)
        {
            return _unitOfWork.DocExistByKey(doc);
        }

        public int FindDocIdx(dynamic doc)
        {
            return _unitOfWork.FindDocIdx(doc);
        }

        public dynamic Get(string PrimaryKeyid)
        {
            return _unitOfWork.Get(PrimaryKeyid);
        }



    }
}