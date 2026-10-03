using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Schema;
using TheTechIdea.Beep.Services.Persistence;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    public partial class SchemaPersistenceHelper
    {
        public string GetCheckpointFilePath(string schemaId) => Path.Combine(_directoryPath, "checkpoints", "checkpoint-v1-" + IdentityHash(schemaId) + ".json");
        private string VersionDirectory(string schemaId) => Path.Combine(_directoryPath, "versions", "schema-v1-" + IdentityHash(schemaId));
        public string GetVersionFilePath(string schemaId, int version)
        {
            if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
            return Path.Combine(VersionDirectory(schemaId), $"v{version.ToString("D4", CultureInfo.InvariantCulture)}.json");
        }
        private void RejectLegacyArtifact(string schemaId, bool checkpoint)
        {
            ValidateId(schemaId);
            // Probe only a safe single component; hashed paths support arbitrary identities without traversal.
            if (schemaId == "." || schemaId == ".." || schemaId.Contains('/') || schemaId.Contains('\\') ||
                schemaId.Contains(':') || schemaId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return;
            var path = checkpoint ? Path.Combine(_directoryPath, "checkpoints", schemaId + ".json") : Path.Combine(_directoryPath, "versions", schemaId);
            if (checkpoint ? File.Exists(path) : Directory.Exists(path) && Directory.EnumerateFiles(path, "*.json").Any())
                throw new InvalidDataException("A legacy sync artifact requires explicit identity/type migration; original evidence is preserved.");
        }
        public Task SaveVersionedSchemaAsync(DataSyncSchema schema, SyncSchemaVersion version) => SaveVersionedSchemaAsync(schema, version, default);
        public Task SaveVersionedSchemaAsync(DataSyncSchema schema, SyncSchemaVersion version, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(schema); ArgumentNullException.ThrowIfNull(version);
            var snapshot = Clone(version);
            ValidateVersion(snapshot, schema.Id, snapshot.Version);
            RejectLegacyArtifact(schema.Id, false);
            var serialized = WriteArtifact("schema-version", schema.Id, snapshot);
            return ObserveAsync(() => AtomicFileStore.UpdateTextAsync(GetVersionFilePath(schema.Id, snapshot.Version), current =>
            {
                if (current == null) return serialized;
                var saved = ReadArtifact<SyncSchemaVersion>(current, "schema-version", schema.Id);
                ValidateVersion(saved, schema.Id, snapshot.Version);
                if (Serialize(saved) != Serialize(snapshot)) throw new InvalidOperationException("Sync version identity is already bound to different content.");
                return current;
            }, token));
        }
        public Task<List<SyncSchemaVersion>> LoadSchemaVersionsAsync(string schemaId) => LoadSchemaVersionsAsync(schemaId, default);
        public async Task<List<SyncSchemaVersion>> LoadSchemaVersionsAsync(string schemaId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            RejectLegacyArtifact(schemaId, false);
            var directory = VersionDirectory(schemaId);
            if (!Directory.Exists(directory)) return new List<SyncSchemaVersion>();
            var versions = new List<SyncSchemaVersion>();
            foreach (var path in Directory.GetFiles(directory, "v*.json"))
            {
                token.ThrowIfCancellationRequested();
                if (!int.TryParse(Path.GetFileNameWithoutExtension(path).Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                    throw new InvalidDataException("Sync version filename is invalid.");
                var json = await AtomicFileStore.ReadTextAsync(path, token).ConfigureAwait(false);
                if (json == null) throw new InvalidDataException("A sync version disappeared during load.");
                var snapshot = ReadArtifact<SyncSchemaVersion>(json, "schema-version", schemaId);
                ValidateVersion(snapshot, schemaId, number);
                versions.Add(snapshot);
            }
            if (versions.GroupBy(item => item.Version).Any(group => group.Count() > 1)) throw new InvalidDataException("Sync version identity is duplicated.");
            return versions.OrderByDescending(item => item.Version).ToList();
        }
        private static void ValidateVersion(SyncSchemaVersion version, string schemaId, int number)
        {
            ValidateId(schemaId);
            if (version.SchemaId != schemaId || number < 1 || version.Version != number || string.IsNullOrWhiteSpace(version.VersionGuid) ||
                version.SchemaHash == null || version.SchemaHash.Length != 64 || version.SchemaHash.Any(character => !Uri.IsHexDigit(character)))
                throw new InvalidDataException("Sync schema version identity/hash is invalid.");
        }
        public async Task<string> DiffSchemaToPersistedAsync(DataSyncSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);
            var versions = await LoadSchemaVersionsAsync(schema.Id).ConfigureAwait(false);
            if (versions.Count == 0) return "No persisted version found; this is a new schema.";
            var latest = versions[0];
            var hash = new SchemaFingerprinter().ComputeSchemaHash(schema);
            return hash == latest.SchemaHash ? string.Empty : $"Schema differs from persisted version {latest.Version}.";
        }
        public Task SaveCheckpointAsync(SyncCheckpoint checkpoint) => SaveCheckpointAsync(checkpoint, default);
        public Task SaveCheckpointAsync(SyncCheckpoint checkpoint, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(checkpoint);
            var snapshot = Clone(checkpoint);
            ValidateCheckpoint(snapshot, snapshot.SchemaId);
            RejectLegacyArtifact(snapshot.SchemaId, true);
            snapshot.SavedAt = DateTime.UtcNow;
            var serialized = WriteArtifact("checkpoint", snapshot.SchemaId, snapshot);
            return ObserveAsync(() => AtomicFileStore.UpdateTextAsync(GetCheckpointFilePath(snapshot.SchemaId), current =>
            {
                if (current != null)
                {
                    var previous = ReadArtifact<SyncCheckpoint>(current, "checkpoint", snapshot.SchemaId);
                    ValidateCheckpoint(previous, snapshot.SchemaId);
                    if (previous.RunId == snapshot.RunId && previous.SchemaFingerprint != snapshot.SchemaFingerprint)
                        throw new InvalidOperationException("Sync context changed within the saved run.");
                    if (previous.RequiresReconciliation && !snapshot.RequiresReconciliation)
                        throw new InvalidOperationException("Unresolved provider evidence requires explicit recovery before changing its state.");
                    if (previous.RunId != snapshot.RunId && previous.Status != "Completed")
                        throw new InvalidOperationException("An unresolved sync run cannot be replaced by a new run.");
                    if (previous.RunId == snapshot.RunId && snapshot.ProcessedOffset < previous.ProcessedOffset)
                        throw new InvalidOperationException("A sync checkpoint cannot regress its acknowledged offset.");
                    if (previous.RunId == snapshot.RunId && (previous.Status == "Completed" || previous.Status == "Failed") && Serialize(previous) != Serialize(snapshot))
                    {
                        // SavedAt is refreshed for a repeated acknowledgement; the completed payload is otherwise immutable.
                        snapshot.SavedAt = previous.SavedAt;
                        if (Serialize(previous) != Serialize(snapshot)) throw new InvalidOperationException("A terminal sync run cannot be reopened or changed.");
                        return current;
                    }
                }
                return serialized;
            }, token));
        }
        public Task<SyncCheckpoint> LoadCheckpointAsync(string schemaId) => LoadCheckpointAsync(schemaId, default);
        public async Task<SyncCheckpoint> LoadCheckpointAsync(string schemaId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            RejectLegacyArtifact(schemaId, true);
            var json = await AtomicFileStore.ReadTextAsync(GetCheckpointFilePath(schemaId), token).ConfigureAwait(false);
            if (json == null) return null;
            var checkpoint = ReadArtifact<SyncCheckpoint>(json, "checkpoint", schemaId);
            ValidateCheckpoint(checkpoint, schemaId);
            return checkpoint;
        }
        private static void ValidateCheckpoint(SyncCheckpoint checkpoint, string schemaId)
        {
            ValidateId(schemaId);
            if (checkpoint.SchemaId != schemaId || string.IsNullOrWhiteSpace(checkpoint.RunId) || checkpoint.ProcessedOffset < 0 ||
                checkpoint.TotalExpected < 0 || (checkpoint.TotalExpected > 0 && checkpoint.ProcessedOffset > checkpoint.TotalExpected) || checkpoint.AttemptCount < 1 ||
                (checkpoint.Status != "InProgress" && checkpoint.Status != "Running" && checkpoint.Status != "Completed" && checkpoint.Status != "Failed" && checkpoint.Status != "Stale"))
                throw new InvalidDataException("Sync checkpoint identity, status or count is invalid.");
            var evidence = checkpoint.FailureEvidence;
            if (evidence == null) return;
            var counts = new[] { evidence.RecordsAttempted, evidence.RecordsAcknowledged, evidence.RecordsFailed,
                evidence.RecordsSkipped, evidence.WriteAttempts, evidence.RecordsTransformationFailed,
                evidence.RecordsQualityEvaluated, evidence.RecordsQualityRejected, evidence.RecordsQualityEvaluationFailed,
                evidence.RecordsBlocked, evidence.RecordsQuarantined, evidence.RecordsWarned, evidence.RejectStoreFailures };
            if (evidence.FormatVersion != 1 || !Enum.IsDefined(typeof(SyncRunFailureKind), evidence.Kind) ||
                checkpoint.Status != "Failed" || !checkpoint.RequiresReconciliation || counts.Any(count => count < 0) ||
                evidence.RecordsAcknowledged != checkpoint.ProcessedOffset || evidence.RecordsAttempted != checkpoint.TotalExpected ||
                evidence.RecordsAcknowledged > evidence.RecordsAttempted || evidence.RecordsFailed > evidence.RecordsAttempted ||
                (long)evidence.RecordsAcknowledged + evidence.RecordsFailed > evidence.RecordsAttempted ||
                evidence.WriteAttempts < evidence.RecordsAcknowledged || evidence.RecordsBlocked > evidence.RecordsQualityRejected ||
                evidence.RecordsSkipped > evidence.RecordsAttempted || evidence.RecordsTransformationFailed > evidence.RecordsFailed ||
                evidence.RecordsQualityEvaluated > evidence.RecordsAttempted || evidence.RecordsQualityRejected > evidence.RecordsQualityEvaluated ||
                evidence.RecordsQualityEvaluationFailed > evidence.RecordsQualityEvaluated || evidence.RecordsQuarantined > evidence.RecordsQualityRejected ||
                evidence.RecordsWarned > evidence.RecordsQualityEvaluated ||
                evidence.Threshold != null && (evidence.Threshold.RecordsAttempted != evidence.RecordsAttempted || evidence.Threshold.RecordsRejected != evidence.RecordsQualityRejected))
                throw new InvalidDataException("Sync failure evidence is inconsistent or unsupported.");
        }
        public Task ClearCheckpointAsync(string schemaId) => ClearCheckpointAsync(schemaId, default);
        public Task ClearCheckpointAsync(string schemaId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            RejectLegacyArtifact(schemaId, true);
            return ObserveAsync(() => AtomicFileStore.DeleteValidatedAsync(GetCheckpointFilePath(schemaId), current =>
                ValidateCheckpoint(ReadArtifact<SyncCheckpoint>(current, "checkpoint", schemaId), schemaId), token));
        }

        public Task ClearCheckpointForRunAsync(string schemaId, string expectedRunId, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested(); ArgumentException.ThrowIfNullOrWhiteSpace(expectedRunId);
            RejectLegacyArtifact(schemaId, true);
            return ObserveAsync(() => AtomicFileStore.DeleteValidatedAsync(GetCheckpointFilePath(schemaId), current =>
            {
                var checkpoint = ReadArtifact<SyncCheckpoint>(current, "checkpoint", schemaId);
                ValidateCheckpoint(checkpoint, schemaId);
                if (checkpoint.RunId != expectedRunId) throw new InvalidOperationException("Sync checkpoint owner changed before clear.");
            }, token));
        }
    }
}
