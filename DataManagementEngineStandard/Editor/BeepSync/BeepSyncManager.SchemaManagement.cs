using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor
{
    public partial class BeepSyncManager
    {
        public void AddSyncSchema(DataSyncSchema schema) => SyncSchemas.Add(schema);

        public void RemoveSyncSchema(string schemaId)
        {
            var schema = FindSchema(schemaId);
            if (schema != null) SyncSchemas.Remove(schema);
        }

        public void UpdateSyncSchema(DataSyncSchema schema)
        {
            var existing = FindSchema(schema?.Id);
            if (existing != null) SyncSchemas[SyncSchemas.IndexOf(existing)] = schema;
        }

        public void AddFilter(string schemaId, AppFilter filter) =>
            FindSchema(schemaId)?.Filters.Add(filter);

        public void RemoveFilter(string schemaId, string fieldName)
        {
            var schema = FindSchema(schemaId);
            var filter = schema?.Filters?.FirstOrDefault(f => f.FieldName == fieldName);
            if (filter != null) schema.Filters.Remove(filter);
        }

        public void AddFieldMapping(string schemaId, FieldSyncData field) =>
            FindSchema(schemaId)?.MappedFields.Add(field);

        public IErrorsInfo ValidateSchema(DataSyncSchema schema) =>
            _validationHelper.ValidateSchema(schema);

        public async Task SaveSchemasAsync() =>
            await _persistenceHelper.SaveSchemasAsync(SyncSchemas);

        // Task.Run is what makes the sync bridge safe: it starts the async method on a
        // thread-pool thread where SynchronizationContext.Current is null, so the awaits inside
        // resume on the pool. Awaiting the task directly here would post the continuation back
        // to the caller's context — and when the caller is the UI thread, that thread is blocked
        // in GetResult() waiting for the very continuation it must run. That is the deadlock.
        public void SaveSchemas() => Task.Run(() => SaveSchemasAsync()).GetAwaiter().GetResult();

        public async Task<ObservableBindingList<DataSyncSchema>> LoadSchemasAsync() =>
            SyncSchemas = await _persistenceHelper.LoadSchemasAsync();

        public void LoadSchemas() => Task.Run(() => LoadSchemasAsync()).GetAwaiter().GetResult();
    }
}
