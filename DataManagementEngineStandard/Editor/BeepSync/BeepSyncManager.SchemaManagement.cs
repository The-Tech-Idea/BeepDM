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

        public void SaveSchemas() => SaveSchemasAsync().GetAwaiter().GetResult();

        public async Task<ObservableBindingList<DataSyncSchema>> LoadSchemasAsync() =>
            SyncSchemas = await _persistenceHelper.LoadSchemasAsync();

        public void LoadSchemas() => LoadSchemasAsync().GetAwaiter().GetResult();
    }
}
