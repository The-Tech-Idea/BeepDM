using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Services.Persistence;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil.Managers
{
    /// <summary>Coordinated, acknowledged migration history snapshots for local cooperating writers.</summary>
    public class MigrationHistoryManager
    {
        private readonly IDMLogger _logger;
        private readonly IJsonLoader _jsonLoader;
        private ConfigandSettings _config;

        public ConfigandSettings Config { get => _config; set => _config = value; }

        public MigrationHistoryManager(IDMLogger logger, IJsonLoader jsonLoader, ConfigandSettings config, ConfigPathManager pathManager)
        {
            _logger = logger;
            _jsonLoader = jsonLoader;
            _config = config;
        }

        private IJsonSnapshotCodec Codec => _jsonLoader as IJsonSnapshotCodec
            ?? throw new NotSupportedException("Migration history requires an IJsonSnapshotCodec loader. Legacy void serialization is not acknowledged.");

        public MigrationHistory Load(string dataSourceName)
        {
            try
            {
                var name = NormalizeName(dataSourceName);
                var paths = GetPaths(name);
                return ReadHistory(AtomicFileStore.ReadText(paths.current)
                    ?? AtomicFileStore.ReadText(paths.legacy), name);
            }
            catch (Exception ex)
            {
                Report(ex);
                throw;
            }
        }

        /// <summary>Legacy signature retained; failures now propagate instead of being logged as success.</summary>
        public void Save(MigrationHistory history) => SaveAcknowledged(history).ThrowIfNotSaved();

        public void AppendRecord(string dataSourceName, DataSourceType dataSourceType, MigrationRecord record) =>
            AppendAcknowledged(dataSourceName, dataSourceType, record).ThrowIfNotSaved();

        /// <summary>Explicit whole-history replacement, not optimistic merging of a stale caller snapshot.</summary>
        public PersistenceWriteResult SaveAcknowledged(MigrationHistory history, CancellationToken token = default) =>
            WriteAcknowledged(() =>
            {
                token.ThrowIfCancellationRequested();
                ArgumentNullException.ThrowIfNull(history);
                var name = NormalizeName(history.DataSourceName);
                var copy = Codec.DeserializeSnapshot<MigrationHistory>(Codec.SerializeSnapshot(history));
                ValidateHistory(copy, name);
                copy.DataSourceName = name;
                copy.StorageFormatVersion = 1;
                ValidateHistory(copy, name);
                var serialized = Codec.SerializeSnapshot(copy);
                var paths = GetPaths(name);
                AtomicFileStore.UpdateText(paths.current, current =>
                {
                    var previous = ReadHistory(current ?? AtomicFileStore.ReadText(paths.legacy), name);
                    ValidateType(previous, copy.DataSourceType);
                    return serialized;
                }, token);
            });

        public PersistenceWriteResult AppendAcknowledged(string dataSourceName, DataSourceType dataSourceType,
            MigrationRecord record, CancellationToken token = default) =>
            WriteAcknowledged(() =>
            {
                token.ThrowIfCancellationRequested();
                ArgumentNullException.ThrowIfNull(record);
                var name = NormalizeName(dataSourceName);
                // Freeze caller-owned payload before waiting for the file lease.
                var copy = Codec.DeserializeSnapshot<MigrationRecord>(Codec.SerializeSnapshot(record));
                ValidateRecord(copy);
                var paths = GetPaths(name);
                AtomicFileStore.UpdateText(paths.current, current =>
                {
                    var history = ReadHistory(current ?? AtomicFileStore.ReadText(paths.legacy), name);
                    ValidateType(history, dataSourceType);
                    history.DataSourceName = name;
                    history.DataSourceType = dataSourceType;
                    history.StorageFormatVersion = 1;
                    history.Migrations.Add(copy);
                    return Codec.SerializeSnapshot(history);
                }, token);
            });

        private PersistenceWriteResult WriteAcknowledged(Action write)
        {
            try
            {
                write();
                return new PersistenceWriteResult(PersistenceWriteStatus.Saved);
            }
            catch (OperationCanceledException ex)
            {
                return new PersistenceWriteResult(PersistenceWriteStatus.Cancelled, ex);
            }
            catch (Exception ex)
            {
                Report(ex);
                return new PersistenceWriteResult(ex is NotSupportedException
                    ? PersistenceWriteStatus.Unsupported : PersistenceWriteStatus.Failed, ex);
            }
        }

        private MigrationHistory ReadHistory(string json, string name)
        {
            if (json == null)
                return new MigrationHistory { DataSourceName = name, StorageFormatVersion = 1 };
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("Existing migration history is empty; preserve it for recovery.");
            using var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 64, DateParseHandling = DateParseHandling.None };
            var document = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read())
                throw new InvalidDataException("Migration history contains additional JSON content.");
            if (document["DataSourceName"]?.Type != JTokenType.String || document["Migrations"] is not JArray)
                throw new InvalidDataException("Migration history is missing identity or records.");
            var history = Codec.DeserializeSnapshot<MigrationHistory>(json);
            ValidateHistory(history, name);
            return history;
        }

        private static void ValidateHistory(MigrationHistory history, string name)
        {
            if (history == null || !string.Equals(history.DataSourceName, name, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Migration history datasource identity does not match its storage key.");
            if (history.StorageFormatVersion is not (0 or 1))
                throw new NotSupportedException("Migration history storage version is unsupported.");
            if (history.Migrations == null)
                throw new InvalidDataException("Migration history records are missing.");
            foreach (var record in history.Migrations) ValidateRecord(record);
        }

        private static void ValidateRecord(MigrationRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.MigrationId))
                throw new InvalidDataException("Migration history contains a record without identity.");
        }

        private static void ValidateType(MigrationHistory history, DataSourceType type)
        {
            if (history.DataSourceType != DataSourceType.Unknown && type != history.DataSourceType)
                throw new InvalidDataException("Migration history datasource type does not match the requested target.");
        }

        private static string NormalizeName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return name.Trim();
        }

        private (string current, string legacy) GetPaths(string name)
        {
            var basePath = _config?.ConfigPath;
            if (string.IsNullOrWhiteSpace(basePath)) basePath = _config?.ExePath;
            if (string.IsNullOrWhiteSpace(basePath)) basePath = AppDomain.CurrentDomain.BaseDirectory;
            var directory = Path.Combine(basePath, "Migrations");
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name.ToUpperInvariant())));
            var legacyName = new StringBuilder(name.Length);
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var ch in name) legacyName.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
            return (Path.Combine(directory, $"history-v1-{hash}.json"),
                Path.Combine(directory, $"{legacyName}_migrations.json"));
        }

        private void Report(Exception ex)
        {
            // Do not leak record contents/connection details or let logging hide the primary failure.
            try { _logger?.WriteLog($"Migration history persistence failed ({ex.GetType().Name})."); }
            catch (Exception logError) { System.Diagnostics.Debug.WriteLine($"Migration history logging failed ({logError.GetType().Name})."); }
        }
    }
}
