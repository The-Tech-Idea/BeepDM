using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Editor.UOW;

namespace TheTechIdea.Beep.Editor
{
    public partial class MultiDataSourceUnitOfWork : IDisposable
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly Dictionary<string, UnitOfWorkWrapper> _unitsOfWork = new Dictionary<string, UnitOfWorkWrapper>();
        private readonly Dictionary<string, List<RelationshipMapping>> _relationships = new Dictionary<string, List<RelationshipMapping>>();
        private string _currentParentEntityName;
        private dynamic _currentParentEntity;
        private int _currentParentIndex = -1;

        public event EventHandler<ChildDataChangedEventArgs> ChildDataChanged;

        public MultiDataSourceUnitOfWork(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
        }

        public async Task AddUnitOfWorkAsync<T>(string dataSourceName, string entityName, string primaryKey) where T : Entity, new()
        {
            var unitOfWork = UnitOfWorkFactory.CreateUnitOfWork(typeof(T), _dmeEditor, dataSourceName, entityName, primaryKey);
            var wrapper = new UnitOfWorkWrapper(unitOfWork);
            _unitsOfWork[entityName] = wrapper;

            await wrapper.Get();
            SubscribeToChanges<T>(entityName);

            // Initialize navigation for parent entities
            if (_relationships.ContainsKey(entityName) && _currentParentEntityName == null)
            {
                _currentParentEntityName = entityName;
                await NavigateToFirstAsync<T>(entityName);
            }
        }

        public void AddRelationship(string parentEntityName, string childEntityName, string parentKeyField, string childForeignKeyField,
            RelationshipBehavior deleteBehavior = RelationshipBehavior.CascadeDelete, bool isRequired = false)
        {
            if (!_relationships.ContainsKey(parentEntityName))
                _relationships[parentEntityName] = new List<RelationshipMapping>();

            _relationships[parentEntityName].Add(new RelationshipMapping
            {
                ParentEntityName = parentEntityName,
                ChildEntityName = childEntityName,
                ParentKeyField = parentKeyField,
                ChildForeignKeyField = childForeignKeyField,
                DeleteBehavior = deleteBehavior,
                IsRequired = isRequired
            });
        }

        public ObservableBindingList<T> GetEntities<T>(string entityName) where T : Entity, new()
        {
            var unitOfWork = GetUnitOfWork(entityName);
            return unitOfWork.Units as ObservableBindingList<T>
                ?? throw new InvalidCastException($"Units for {entityName} cannot be cast to ObservableBindingList<{typeof(T).Name}>");
        }

        public async Task<Dictionary<string, ObservableBindingList<TChild>>> GetAllRelatedChildrenAsync<TChild, TParent>(string parentEntityName, TParent parentEntity)
            where TChild : Entity, new()
            where TParent : Entity, new()
        {
            var result = new Dictionary<string, ObservableBindingList<TChild>>();
            if (!_relationships.TryGetValue(parentEntityName, out var mappings))
            {
                _dmeEditor.AddLogMessage("Beep", $"No relationships defined for {parentEntityName}", DateTime.Now, -1, null, Errors.Failed);
                return result;
            }

            var parentKeyValue = typeof(TParent).GetProperty(mappings.First().ParentKeyField)?.GetValue(parentEntity)?.ToString();
            if (string.IsNullOrEmpty(parentKeyValue))
            {
                _dmeEditor.AddLogMessage("Beep", $"Parent key value is null for {mappings.First().ParentKeyField}", DateTime.Now, -1, null, Errors.Failed);
                return result;
            }

            foreach (var mapping in mappings.Where(m => m.ParentEntityName == typeof(TParent).Name))
            {
                var childUnit = GetUnitOfWork(mapping.ChildEntityName);
                var filters = new List<AppFilter>
                {
                    new AppFilter { FieldName = mapping.ChildForeignKeyField, Operator = "=", FilterValue = parentKeyValue }
                };

                dynamic filteredChildren = await childUnit.Get(filters);
                result[mapping.ChildEntityName] = filteredChildren as ObservableBindingList<TChild>
                    ?? throw new InvalidCastException($"Cannot cast filtered children to ObservableBindingList<{typeof(TChild).Name}>");
            }

            return result;
        }

        public async Task<IErrorsInfo> InsertAsync<T>(string entityName, T entity) where T : Entity, new()
        {
            var unitOfWork = GetUnitOfWork(entityName);
            var result = await unitOfWork.InsertAsync(entity);

            if (result.Flag == Errors.Ok)
            {
                await HandleRelationshipInsert(entityName, entity);
                if (entityName == _currentParentEntityName)
                {
                    _currentParentEntity = entity;
                    _currentParentIndex = unitOfWork.Units.IndexOf(entity);
                    RaiseChildDataChanged(entityName);
                }
            }
            return result;
        }

        public async Task<IErrorsInfo> UpdateAsync<T>(string entityName, T entity) where T : Entity, new()
        {
            var unitOfWork = GetUnitOfWork(entityName);
            var result = await unitOfWork.UpdateAsync(entity);

            if (result.Flag == Errors.Ok && entityName == _currentParentEntityName)
            {
                RaiseChildDataChanged(entityName);
            }
            return result;
        }

        public async Task<IErrorsInfo> DeleteAsync<T>(string entityName, T entity) where T : Entity, new()
        {
            var unitOfWork = GetUnitOfWork(entityName);
            var result = await unitOfWork.DeleteAsync(entity);

            if (result.Flag == Errors.Ok)
            {
                await HandleRelationshipDelete(entityName, entity);
                if (entityName == _currentParentEntityName && Equals(_currentParentEntity, entity))
                {
                    await NavigateToNextAsync<T>(entityName); // Move to next if current is deleted
                }
            }
            return result;
        }

        public async Task<IErrorsInfo> CommitAsync()
        {
            IErrorsInfo finalResult = new ErrorsInfo { Flag = Errors.Ok };
            var committedUnits = new List<UnitOfWorkWrapper>();

            try
            {
                foreach (var unit in _unitsOfWork.Values)
                {
                    if (unit.IsDirty && !ValidateEntityChanges(unit.EntityName, out string errorMessage))
                    {
                        finalResult.Flag = Errors.Failed;
                        finalResult.Message = $"Validation failed for {unit.EntityName}: {errorMessage}";
                        return finalResult;
                    }

                    var result = await unit.Commit();
                    if (result.Flag != Errors.Ok)
                    {
                        finalResult.Flag = Errors.Failed;
                        finalResult.Message += $"Commit failed for {unit.EntityName}: {result.Message}\n";
                        throw new Exception($"Partial commit detected for {unit.EntityName}");
                    }
                    committedUnits.Add(unit);
                }
            }
            catch (Exception ex)
            {
                finalResult.Flag = Errors.Failed;
                finalResult.Message += $"Rolling back due to error: {ex.Message}\n";

                foreach (var unit in committedUnits)
                {
                    await unit.Rollback();
                }
                _dmeEditor.AddLogMessage("Beep", $"Commit failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return finalResult;
        }

        public async Task NavigateToParentAsync<T>(string parentEntityName, T parentEntity) where T : Entity, new()
        {
            var unitOfWork = GetUnitOfWork(parentEntityName);
            int index = unitOfWork.Units.IndexOf(parentEntity);
            if (index == -1)
            {
                _dmeEditor.AddLogMessage("Beep", $"Entity not found in {parentEntityName}", DateTime.Now, -1, null, Errors.Failed);
                return;
            }

            if (_currentParentEntityName != parentEntityName || !Equals(_currentParentEntity, parentEntity))
            {
                await SaveAndValidateChildrenAsync(parentEntityName);
                _currentParentEntityName = parentEntityName;
                _currentParentEntity = parentEntity;
                _currentParentIndex = index;
                unitOfWork.MoveTo(index); // Sync with UnitOfWork navigation
                RaiseChildDataChanged(parentEntityName);
            }
        }

        public async Task NavigateToNextAsync<T>(string parentEntityName) where T : Entity, new()
        {
            var unitOfWork = GetUnitOfWork(parentEntityName);
            if (_currentParentIndex < unitOfWork.Units.Count - 1)
            {
                await SaveAndValidateChildrenAsync(parentEntityName);
                _currentParentIndex++;
                _currentParentEntity = unitOfWork.Units[_currentParentIndex];
                unitOfWork.MoveTo(_currentParentIndex);
                RaiseChildDataChanged(parentEntityName);
            }
        }

        public async Task NavigateToPreviousAsync<T>(string parentEntityName) where T : Entity, new()
        {
            var unitOfWork = GetUnitOfWork(parentEntityName);
            if (_currentParentIndex > 0)
            {
                await SaveAndValidateChildrenAsync(parentEntityName);
                _currentParentIndex--;
                _currentParentEntity = unitOfWork.Units[_currentParentIndex];
                unitOfWork.MoveTo(_currentParentIndex);
                RaiseChildDataChanged(parentEntityName);
            }
        }

        public async Task NavigateToFirstAsync<T>(string parentEntityName) where T : Entity, new()
        {
            var unitOfWork = GetUnitOfWork(parentEntityName);
            if (unitOfWork.Units.Count > 0)
            {
                await SaveAndValidateChildrenAsync(parentEntityName);
                _currentParentIndex = 0;
                _currentParentEntity = unitOfWork.Units[_currentParentIndex];
                unitOfWork.MoveTo(_currentParentIndex);
                RaiseChildDataChanged(parentEntityName);
            }
        }

        public async Task NavigateToLastAsync<T>(string parentEntityName) where T : Entity, new()
        {
            var unitOfWork = GetUnitOfWork(parentEntityName);
            if (unitOfWork.Units.Count > 0)
            {
                await SaveAndValidateChildrenAsync(parentEntityName);
                _currentParentIndex = unitOfWork.Units.Count - 1;
                _currentParentEntity = unitOfWork.Units[_currentParentIndex];
                unitOfWork.MoveTo(_currentParentIndex);
                RaiseChildDataChanged(parentEntityName);
            }
        }

        private async Task SaveAndValidateChildrenAsync(string parentEntityName)
        {
            if (_relationships.TryGetValue(parentEntityName, out var mappings))
            {
                foreach (var mapping in mappings)
                {
                    var childUnit = GetUnitOfWork(mapping.ChildEntityName);
                    if (childUnit.IsDirty)
                    {
                        if (!ValidateEntityChanges(mapping.ChildEntityName, out string errorMessage))
                            throw new InvalidOperationException($"Cannot navigate: {errorMessage}");

                        await childUnit.Commit();
                    }
                }
            }
        }

        private void SubscribeToChanges<T>(string entityName) where T : Entity, new()
        {
            var units = GetEntities<T>(entityName);
            units.CollectionChanged += (s, e) =>
            {
                if (_relationships.ContainsKey(entityName) && _currentParentEntityName == entityName)
                {
                    RaiseChildDataChanged(entityName);
                }
            };
        }

        private async Task HandleRelationshipInsert<T>(string entityName, T entity) where T : Entity, new()
        {
            if (_relationships.TryGetValue(entityName, out var mappings))
            {
                var parentKeyValue = typeof(T).GetProperty(mappings.First().ParentKeyField)?.GetValue(entity)?.ToString();
                if (string.IsNullOrEmpty(parentKeyValue))
                {
                    _dmeEditor.AddLogMessage("Beep", $"Parent key value is null for {mappings.First().ParentKeyField}", DateTime.Now, -1, null, Errors.Failed);
                    return;
                }

                foreach (var mapping in mappings)
                {
                    var childUnit = GetUnitOfWork(mapping.ChildEntityName);
                    var filters = new List<AppFilter>
                    {
                        new AppFilter { FieldName = mapping.ChildForeignKeyField, Operator = "IS NULL", FilterValue = null }
                    };

                    dynamic children = await childUnit.Get(filters);
                    foreach (var child in children)
                    {
                        var fkProperty = child.GetType().GetProperty(mapping.ChildForeignKeyField);
                        if (fkProperty != null && fkProperty.GetValue(child) == null)
                        {
                            fkProperty.SetValue(child, parentKeyValue);
                            await childUnit.UpdateAsync(child);
                        }
                    }
                }
            }
        }

        private async Task HandleRelationshipDelete<T>(string entityName, T entity) where T : Entity, new()
        {
            if (_relationships.TryGetValue(entityName, out var mappings))
            {
                var parentKeyValue = typeof(T).GetProperty(mappings.First().ParentKeyField)?.GetValue(entity)?.ToString();
                if (string.IsNullOrEmpty(parentKeyValue))
                {
                    _dmeEditor.AddLogMessage("Beep", $"Parent key value is null for {mappings.First().ParentKeyField}", DateTime.Now, -1, null, Errors.Failed);
                    return;
                }

                foreach (var mapping in mappings)
                {
                    var childUnit = GetUnitOfWork(mapping.ChildEntityName);
                    var filters = new List<AppFilter>
                    {
                        new AppFilter { FieldName = mapping.ChildForeignKeyField, Operator = "=", FilterValue = parentKeyValue }
                    };

                    dynamic children = await childUnit.Get(filters);
                    var childList = children.ToList();

                    switch (mapping.DeleteBehavior)
                    {
                        case RelationshipBehavior.CascadeDelete:
                            foreach (var child in childList)
                                await childUnit.DeleteAsync(child);
                            break;
                        case RelationshipBehavior.SetNull:
                            foreach (var child in childList)
                            {
                                var fkProperty = child.GetType().GetProperty(mapping.ChildForeignKeyField);
                                if (fkProperty != null && fkProperty.PropertyType.IsClass)
                                {
                                    fkProperty.SetValue(child, null);
                                    await childUnit.UpdateAsync(child);
                                }
                            }
                            break;
                        case RelationshipBehavior.Restrict:
                            if (childList.Any())
                                throw new InvalidOperationException($"Cannot delete {entityName} due to existing {mapping.ChildEntityName} records.");
                            break;
                    }
                }
            }
        }

        private bool ValidateEntityChanges(string entityName, out string errorMessage)
        {
            var unitOfWork = GetUnitOfWork(entityName);
            errorMessage = string.Empty;
            var errors = new List<string>();

            foreach (var entity in unitOfWork.Units)
            {
                var properties = entity.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(entity)?.ToString();
                    if (prop.Name != unitOfWork.PrimaryKey && string.IsNullOrWhiteSpace(value))
                    {
                        errors.Add($"{prop.Name} is required for {entityName}.");
                    }
                }

                if (_relationships.TryGetValue(entityName, out var mappings))
                {
                    foreach (var mapping in mappings.Where(m => m.IsRequired))
                    {
                        var childUnit = GetUnitOfWork(mapping.ChildEntityName);
                        var parentKeyValue = entity.GetType().GetProperty(mapping.ParentKeyField)?.GetValue(entity)?.ToString();
                        if (string.IsNullOrEmpty(parentKeyValue))
                        {
                            errors.Add($"Parent key {mapping.ParentKeyField} is null for {entityName}.");
                            continue;
                        }

                        var filters = new List<AppFilter>
                        {
                            new AppFilter { FieldName = mapping.ChildForeignKeyField, Operator = "=", FilterValue = parentKeyValue }
                        };

                        dynamic children = childUnit.Get(filters).Result;
                        if (!children.Any())
                        {
                            errors.Add($"At least one {mapping.ChildEntityName} is required for {entityName} with {mapping.ParentKeyField} = {parentKeyValue}.");
                        }
                    }
                }
            }

            if (errors.Any())
            {
                errorMessage = string.Join("\n", errors);
                return false;
            }
            return true;
        }

        private void RaiseChildDataChanged(string parentEntityName)
        {
            if (_relationships.TryGetValue(parentEntityName, out var mappings))
            {
                foreach (var mapping in mappings)
                {
                    ChildDataChanged?.Invoke(this, new ChildDataChangedEventArgs
                    {
                        ParentEntityName = parentEntityName,
                        ChildEntityName = mapping.ChildEntityName
                    });
                }
            }
        }

        private UnitOfWorkWrapper GetUnitOfWork(string entityName)
        {
            if (_unitsOfWork.TryGetValue(entityName, out var unitOfWork))
                return unitOfWork;
            throw new KeyNotFoundException($"No UnitOfWork found for entity: {entityName}");
        }

        public void Dispose()
        {
            foreach (var unit in _unitsOfWork.Values)
            {
                ((IDisposable)unit).Dispose();
            }
        }

        public dynamic CurrentParentEntity => _currentParentEntity;
    }

    public enum RelationshipBehavior
    {
        CascadeDelete,
        SetNull,
        Restrict
    }

    public class RelationshipMapping
    {
        public string ParentEntityName { get; set; }
        public string ChildEntityName { get; set; }
        public string ParentKeyField { get; set; }
        public string ChildForeignKeyField { get; set; }
        public RelationshipBehavior DeleteBehavior { get; set; }
        public bool IsRequired { get; set; }
    }

    public class ChildDataChangedEventArgs : EventArgs
    {
        public string ParentEntityName { get; set; }
        public string ChildEntityName { get; set; }
    }

    public static class Extensions
    {
        public static ObservableBindingList<T> ToObservableBindingListTEDO<T>(this IEnumerable<T> source) where T : Entity, new()
        {
            return new ObservableBindingList<T>(source.ToList());
        }
    }
}