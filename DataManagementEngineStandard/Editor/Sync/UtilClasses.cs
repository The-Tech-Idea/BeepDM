using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.Sync
{
   

    // Assuming these interfaces and classes are defined elsewhere in the project:
    // IDataSource, IDMEEditor, DataSyncSchema, AppFilter, FieldSyncData, SyncRunData, IErrorsInfo, ErrorsInfo, Errors,
    // PassedArgs, EntityUpdateInsertLog, LogAction, SyncErrorsandTracking, ObservableBindingList, etc.

    public interface ISchemaRepository
    {
        ObservableBindingList<DataSyncSchema> LoadSchemas();
        void SaveSchemas(ObservableBindingList<DataSyncSchema> schemas);
    }

    public class JsonSchemaRepository : ISchemaRepository
    {
        private readonly string _filePath;

        public JsonSchemaRepository(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            _filePath = Path.Combine(directoryPath, "SyncSchemas.json");
        }

        public ObservableBindingList<DataSyncSchema> LoadSchemas()
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<ObservableBindingList<DataSyncSchema>>(json);
            }
            return new ObservableBindingList<DataSyncSchema>();
        }

        public void SaveSchemas(ObservableBindingList<DataSyncSchema> schemas)
        {
            string json = JsonConvert.SerializeObject(schemas, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }
    }

    public interface ISyncValidator
    {
        IErrorsInfo ValidateSchema(DataSyncSchema schema);
    }

    public class SchemaValidator : ISyncValidator
    {
        public IErrorsInfo ValidateSchema(DataSyncSchema schema)
        {
            IErrorsInfo err = new ErrorsInfo { Flag = Errors.Ok };

            void AddError(string message)
            {
                err.Flag = Errors.Failed;
                err.Errors.Add(new ErrorsInfo { Message = message });
            }

            if (string.IsNullOrEmpty(schema.SourceDataSourceName))
                AddError("Source Data Source Name is empty");

            if (string.IsNullOrEmpty(schema.DestinationDataSourceName))
                AddError("Destination Data Source Name is empty");

            if (string.IsNullOrEmpty(schema.SourceEntityName))
                AddError("Source Entity Name is empty");

            if (string.IsNullOrEmpty(schema.DestinationEntityName))
                AddError("Destination Entity Name is empty");

            if (string.IsNullOrEmpty(schema.SourceSyncDataField))
                AddError("Source Sync Data Field is empty");

            if (string.IsNullOrEmpty(schema.DestinationSyncDataField))
                AddError("Destination Sync Data Field is empty");

            if (string.IsNullOrEmpty(schema.SyncType))
                AddError("Sync Type is empty");

            if (string.IsNullOrEmpty(schema.SyncDirection))
                AddError("Sync Direction is empty");

            return err;
        }
    }

    public interface IFieldMapper
    {
        void MapFields(object source, object destination, IEnumerable<FieldSyncData> mappedFields);
    }

    public class ReflectionFieldMapper : IFieldMapper
    {
        public void MapFields(object source, object destination, IEnumerable<FieldSyncData> mappedFields)
        {
            foreach (var field in mappedFields)
            {
                var sourceProperty = source.GetType().GetProperty(field.SourceField);
                var destProperty = destination.GetType().GetProperty(field.DestinationField);
                if (sourceProperty != null && destProperty != null && destProperty.CanWrite)
                {
                    var sourceValue = sourceProperty.GetValue(source);
                    destProperty.SetValue(destination, sourceValue);
                }
            }
        }
    }

}
