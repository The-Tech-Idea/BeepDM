using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor
{
    public class EntityUpdateInsertLog
    {
        [JsonPropertyName("ID")]
        public int Id { get; set; }
        [JsonPropertyName("RecordID")]
        public int RecordId { get; set; }
        public string RecordGuidKey { get; set; }
        public string GuidKey { get; set; } = Guid.NewGuid().ToString();
        public Dictionary<string, object> UpdatedFields { get; set; }
        public DateTime LogDateandTime { get; set; }
        public string LogUser { get; set; }
        public LogAction LogAction { get; set; }
        public string LogEntity { get; set; }
        public Tracking TrackingRecord { get; set; }

        // Fields merged from UnitofWork duplicate definition
        public DateTime Timestamp { get; set; }
        public string EntityName { get; set; }
        public string Operation { get; set; }
        public string EntityId { get; set; }
        public Dictionary<string, object> Changes { get; set; }
        public string UserName { get; set; }

        public EntityUpdateInsertLog()
        {
        }
        public EntityUpdateInsertLog(Dictionary<string, object> updatedFields, DateTime logDateandTime, string logUser, LogAction logAction, string logEntity, string recordGuidKey)
        {
            UpdatedFields = updatedFields;
            LogDateandTime = logDateandTime;
            LogUser = logUser;
            LogAction = logAction;
            LogEntity = logEntity;
            RecordGuidKey = recordGuidKey;
        }
        public EntityUpdateInsertLog(Dictionary<string, object> updatedFields, DateTime logDateandTime, string logUser, LogAction logAction, string logEntity)
        {
            UpdatedFields = updatedFields;
            LogDateandTime = logDateandTime;
            LogUser = logUser;
            LogAction = logAction;
            LogEntity = logEntity;
        }
        public EntityUpdateInsertLog(Dictionary<string, object> updatedFields, DateTime logDateandTime, string logUser, LogAction logAction)
        {
            UpdatedFields = updatedFields;
            LogDateandTime = logDateandTime;
            LogUser = logUser;
            LogAction = logAction;
        }
        public EntityUpdateInsertLog(Dictionary<string, object> updatedFields, DateTime logDateandTime, string logUser)
        {
            UpdatedFields = updatedFields;
            LogDateandTime = logDateandTime;
            LogUser = logUser;
        }
        public EntityUpdateInsertLog(Dictionary<string, object> updatedFields, DateTime logDateandTime)
        {
            UpdatedFields = updatedFields;
            LogDateandTime = logDateandTime;
        }
        public EntityUpdateInsertLog(Dictionary<string, object> updatedFields)
        {
            UpdatedFields = updatedFields;
        }
    }
}
