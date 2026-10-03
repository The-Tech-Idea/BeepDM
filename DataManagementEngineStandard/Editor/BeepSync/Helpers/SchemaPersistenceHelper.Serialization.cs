using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using TheTechIdea.Beep.Services.Persistence;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    public partial class SchemaPersistenceHelper
    {
        private static JsonSerializer Serializer() => JsonSerializer.Create(new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None, ObjectCreationHandling = ObjectCreationHandling.Replace,
            ReferenceLoopHandling = ReferenceLoopHandling.Error, DateParseHandling = DateParseHandling.None,
            MaxDepth = 64, CheckAdditionalContent = true, ContractResolver = new CursorContracts(),
            Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        });
        private sealed class CursorContracts : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization)
            {
                var property = base.CreateProperty(member, serialization);
                if ((member.DeclaringType == typeof(WatermarkPolicy) && member.Name == nameof(WatermarkPolicy.LastWatermarkValue)) ||
                    (member.DeclaringType == typeof(SyncCheckpoint) && member.Name == nameof(SyncCheckpoint.LastProcessedKeyValue)))
                    property.Converter = new CursorConverter();
                return property;
            }
        }
        private sealed class CursorConverter : JsonConverter
        {
            public override bool CanConvert(Type type) => type == typeof(object);
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
                writer.WriteRawValue(System.Text.Json.JsonSerializer.Serialize(TypedCursorCodec.Encode(value, 0)));
            public override object ReadJson(JsonReader reader, Type type, object existing, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                var value = JToken.Load(reader);
                if (value is not JObject) throw new InvalidDataException("Untyped legacy sync cursors require explicit migration.");
                var encoded = System.Text.Json.JsonSerializer.Deserialize<TypedCursorValue>(value.ToString(Formatting.None));
                return TypedCursorCodec.Decode(encoded, 0);
            }
        }
        private static string Serialize(object value)
        {
            using var buffer = new StringWriter(CultureInfo.InvariantCulture);
            using (var writer = new JsonTextWriter(buffer)) Serializer().Serialize(writer, value);
            return buffer.ToString();
        }
        private static JToken Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("Existing sync snapshot is empty.");
            using var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 64, DateParseHandling = DateParseHandling.None };
            var value = JToken.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read()) throw new InvalidDataException("Sync snapshot contains trailing content.");
            return value;
        }
        private static T Clone<T>(T value) => Parse(Serialize(value)).ToObject<T>(Serializer());
        private static string WriteSchemas(List<DataSyncSchema> schemas) => Serialize(new SchemaEnvelope { FormatVersion = 1, Schemas = schemas });
        private static List<DataSyncSchema> ReadSchemas(string json)
        {
            var value = Parse(json);
            JArray records;
            if (value is JArray legacy) records = legacy;
            else
            {
                if (value is not JObject root || root["FormatVersion"]?.Type != JTokenType.Integer ||
                    (int)root["FormatVersion"] != 1 || root["Schemas"] is not JArray)
                    throw new InvalidDataException("Unsupported sync schema envelope.");
                records = (JArray)root["Schemas"];
            }
            foreach (var record in records)
            {
                if (record is not JObject item) throw new InvalidDataException("Sync schema record is not an object.");
                // The model constructor creates an ID; persisted evidence must supply its own.
                var identities = item.Properties().Where(property => StringComparer.OrdinalIgnoreCase.Equals(property.Name, nameof(DataSyncSchema.Id))).ToList();
                if (identities.Count != 1 || identities[0].Value.Type != JTokenType.String)
                    throw new InvalidDataException("Sync schema identity is missing, ambiguous or not a string.");
                ValidateId(identities[0].Value.Value<string>());
            }
            var schemas = records.ToObject<List<DataSyncSchema>>(Serializer());
            ValidateSchemas(schemas);
            return schemas;
        }
        private static void ValidateSchemas(IEnumerable<DataSyncSchema> schemas)
        {
            if (schemas == null) throw new InvalidDataException("Sync schemas are missing.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var schema in schemas)
            {
                if (schema == null) throw new InvalidDataException("Sync schema is null.");
                ValidateId(schema.Id);
                if (!ids.Add(schema.Id)) throw new InvalidDataException("Sync schema identity is duplicated.");
            }
        }
        private static void ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || Encoding.UTF8.GetByteCount(id) > 1024)
                throw new InvalidDataException("Sync storage identity is missing or too long.");
        }
        private static string IdentityHash(string id)
        {
            ValidateId(id);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id))).ToLowerInvariant();
        }
        internal static string ComputeExecutionFingerprint(DataSyncSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);
            var intent = new object[]
            {
                "BeepSyncContext.v1", schema.Id, schema.SourceDataSourceName, schema.DestinationDataSourceName,
                schema.SourceEntityName, schema.DestinationEntityName, schema.SourceKeyField, schema.DestinationKeyField,
                schema.SourceSyncDataField, schema.DestinationSyncDataField, schema.SyncType, schema.SyncDirection,
                schema.WatermarkPolicy?.WatermarkMode, schema.WatermarkPolicy?.WatermarkField,
                schema.Filters, schema.MappedFields, schema.RulePolicy, schema.DefaultsPolicy, schema.MappingPolicy,
                schema.ConflictPolicy, schema.DqPolicy, schema.CurrentSchemaVersion?.VersionGuid
            };
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Serialize(intent)))).ToLowerInvariant();
        }
        private sealed class SchemaEnvelope
        {
            public int FormatVersion { get; set; }
            public List<DataSyncSchema> Schemas { get; set; }
        }
        private sealed class ArtifactEnvelope<T>
        {
            public int FormatVersion { get; set; }
            public string ArtifactKind { get; set; }
            public string SchemaId { get; set; }
            public T Snapshot { get; set; }
        }
        private static string WriteArtifact<T>(string kind, string schemaId, T snapshot) =>
            Serialize(new ArtifactEnvelope<T> { FormatVersion = 1, ArtifactKind = kind, SchemaId = schemaId, Snapshot = snapshot });
        private static T ReadArtifact<T>(string json, string kind, string schemaId) where T : class
        {
            var value = Parse(json);
            if (value is not JObject root || root["FormatVersion"]?.Type != JTokenType.Integer || (int)root["FormatVersion"] != 1 ||
                root["ArtifactKind"]?.Value<string>() != kind || root["SchemaId"]?.Value<string>() != schemaId || root["Snapshot"] is not JObject)
                throw new InvalidDataException("Sync artifact version, kind or schema identity is invalid.");
            var snapshot = (JObject)root["Snapshot"];
            var strings = kind == "checkpoint" ? new[] { "SchemaId", "RunId", "Status", "SavedAt" } : new[] { "SchemaId", "VersionGuid", "SchemaHash", "SavedAt" };
            var integers = kind == "checkpoint" ? new[] { "ProcessedOffset", "TotalExpected", "AttemptCount" } : new[] { "Version" };
            if (strings.Any(name => snapshot[name]?.Type != JTokenType.String || string.IsNullOrWhiteSpace(snapshot[name].Value<string>())) ||
                integers.Any(name => snapshot[name]?.Type != JTokenType.Integer))
                throw new InvalidDataException("Required sync artifact fields are missing; constructor defaults are not persisted evidence.");
            if (kind == "checkpoint" && snapshot["FailureEvidence"] != null && snapshot["FailureEvidence"].Type != JTokenType.Null)
            {
                if (snapshot["FailureEvidence"] is not JObject evidence)
                    throw new InvalidDataException("Sync failure evidence is not an object.");
                var counts = new[] { "FormatVersion", "RecordsAttempted", "RecordsAcknowledged", "RecordsFailed", "RecordsSkipped",
                    "WriteAttempts", "RecordsTransformationFailed", "RecordsQualityEvaluated", "RecordsQualityRejected",
                    "RecordsQualityEvaluationFailed", "RecordsBlocked", "RecordsQuarantined", "RecordsWarned", "RejectStoreFailures" };
                if (counts.Any(name => evidence[name]?.Type != JTokenType.Integer) || evidence["Kind"]?.Type != JTokenType.String ||
                    evidence["HasUncertainWrites"]?.Type != JTokenType.Boolean || evidence["QualityAdmissionFailed"]?.Type != JTokenType.Boolean ||
                    evidence["TransformationAdmissionFailed"] != null && evidence["TransformationAdmissionFailed"].Type != JTokenType.Boolean ||
                    evidence.Properties().Any(property => !counts.Contains(property.Name, StringComparer.Ordinal) &&
                        property.Name != "Kind" && property.Name != "HasUncertainWrites" && property.Name != "QualityAdmissionFailed" &&
                        property.Name != "TransformationAdmissionFailed" && property.Name != "Threshold"))
                    throw new InvalidDataException("Required sync failure evidence fields are missing or malformed.");
                if (evidence["Threshold"] != null && evidence["Threshold"].Type != JTokenType.Null)
                {
                    if (evidence["Threshold"] is not JObject threshold || threshold["Outcome"]?.Type != JTokenType.String ||
                        threshold["FailureMode"]?.Type != JTokenType.String || threshold["RecordsAttempted"]?.Type != JTokenType.Integer ||
                        threshold["RecordsRejected"]?.Type != JTokenType.Integer ||
                        (threshold["MaxRejectRate"]?.Type != JTokenType.Float && threshold["MaxRejectRate"]?.Type != JTokenType.Integer))
                        throw new InvalidDataException("Required sync threshold evidence fields are missing or malformed.");
                }
            }
            return root["Snapshot"].ToObject<T>(Serializer()) ?? throw new InvalidDataException("Sync artifact is missing.");
        }
    }
}
