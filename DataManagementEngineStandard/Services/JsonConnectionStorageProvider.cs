using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Winform.Controls
{
    public sealed class JsonConnectionStorageProvider : IConnectionStorageProvider, IDisposable
    {
        private readonly IBeepService _beepService;
        private readonly object _syncRoot = new();
        private readonly SemaphoreSlim _asyncLock = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public JsonConnectionStorageProvider(IBeepService beepService)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
        }

        public void Dispose()
        {
            _asyncLock?.Dispose();
        }

        public IReadOnlyList<ConnectionProperties> LoadConnections(ConnectionStorageScope scope, string profileName, bool includePrecedenceChain)
        {
            lock (_syncRoot)
            {
                var selectedProfile = NormalizeProfile(profileName);
                var chain = includePrecedenceChain ? GetReadChain(scope) : new[] { scope };
                var merged = new Dictionary<string, ConnectionProperties>(StringComparer.OrdinalIgnoreCase);

                foreach (var chainScope in chain)
                {
                    foreach (var record in ReadScopeRecords(chainScope)
                                 .Where(r => string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase)
                                          && r.Connection != null))
                    {
                        EnsureConnectionDefaults(record.Connection);
                        var key = GetIdentityKey(record.Connection);
                        merged[key] = ConnectionSecretProtector.Decrypt(record.Connection);
                    }
                }

                return merged.Values.OrderBy(c => c.ConnectionName).ToList();
            }
        }

        public bool SaveConnections(ConnectionStorageScope scope, string profileName, IReadOnlyList<ConnectionProperties> connections)
        {
            lock (_syncRoot)
            {
                var selectedProfile = NormalizeProfile(profileName);
                var records = ReadScopeRecords(scope)
                    .Where(r => !string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var connection in connections ?? Array.Empty<ConnectionProperties>())
                {
                    var prepared = PrepareForPersist(connection, scope, selectedProfile);
                    records.Add(prepared);
                }

                WriteScopeRecords(scope, records);
                return true;
            }
        }

        public bool AddOrUpdate(ConnectionStorageScope scope, string profileName, ConnectionProperties connection, bool persist)
        {
            if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionName))
            {
                return false;
            }

            lock (_syncRoot)
            {
                var selectedProfile = NormalizeProfile(profileName);
                var records = ReadScopeRecords(scope);
                var prepared = PrepareForPersist(connection, scope, selectedProfile);

                var existing = records.FirstOrDefault(r =>
                    string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase) &&
                    IsSameIdentity(r.Connection, prepared.Connection));

                if (existing != null)
                {
                    records.Remove(existing);
                }

                records.Add(prepared);
                if (persist)
                {
                    WriteScopeRecords(scope, records);
                }

                return true;
            }
        }

        public bool Remove(ConnectionStorageScope scope, string profileName, string connectionName, bool persist)
        {
            if (string.IsNullOrWhiteSpace(connectionName))
            {
                return false;
            }

            lock (_syncRoot)
            {
                var selectedProfile = NormalizeProfile(profileName);
                var records = ReadScopeRecords(scope);
                var existing = records.FirstOrDefault(r =>
                    string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(r.Connection.ConnectionName, connectionName, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    return false;
                }

                records.Remove(existing);
                if (persist)
                {
                    WriteScopeRecords(scope, records);
                }

                return true;
            }
        }

        public bool Promote(ConnectionStorageScope sourceScope, ConnectionStorageScope targetScope, string profileName, ConnectionConflictPolicy conflictPolicy, out string message)
        {
            lock (_syncRoot)
            {
                var selectedProfile = NormalizeProfile(profileName);
                var source = ReadScopeRecords(sourceScope)
                    .Where(r => string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var target = ReadScopeRecords(targetScope);

                if (source.Count == 0)
                {
                    message = "No records found in source scope.";
                    return false;
                }

                var actionLog = new List<string>();
                foreach (var sourceRecord in source)
                {
                    var existing = target.FirstOrDefault(r =>
                        string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase) &&
                        (IsSameIdentity(r.Connection, sourceRecord.Connection) ||
                         string.Equals(r.Connection.ConnectionName, sourceRecord.Connection.ConnectionName, StringComparison.OrdinalIgnoreCase)));

                    if (existing == null)
                    {
                        target.Add(CloneRecordForScope(sourceRecord, targetScope, selectedProfile));
                        actionLog.Add($"Added:{sourceRecord.Connection.ConnectionName}");
                        continue;
                    }

                    ResolveConflict(target, existing, sourceRecord, targetScope, selectedProfile, conflictPolicy, actionLog);
                }

                WriteScopeRecords(targetScope, target);
                message = string.Join(Environment.NewLine, actionLog);
                return true;
            }
        }

        public bool ExportPackage(ConnectionStorageScope scope, string profileName, string packagePath, bool includeEncryptedSecretsOnly, out string message)
        {
            lock (_syncRoot)
            {
                var selectedProfile = NormalizeProfile(profileName);
                var records = ReadScopeRecords(scope)
                    .Where(r => string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (records.Count == 0)
                {
                    message = "No records available to export.";
                    return false;
                }

                var package = new ConnectionCatalogPackage
                {
                    PackageVersion = "1.0",
                    ProfileName = selectedProfile,
                    SourceScope = scope.ToString(),
                    ExportedOnUtc = DateTime.UtcNow
                };

                foreach (var record in records)
                {
                    var cloned = CloneRecordForScope(record, scope, selectedProfile);
                    if (!includeEncryptedSecretsOnly)
                    {
                        cloned.Connection = ConnectionSecretProtector.StripSecrets(ConnectionSecretProtector.Decrypt(cloned.Connection));
                    }

                    package.Records.Add(cloned);
                }

                EnsureDirectory(packagePath);
                File.WriteAllText(packagePath, JsonSerializer.Serialize(package, _jsonOptions));
                message = $"Exported {package.Records.Count} connection(s).";
                return true;
            }
        }

        public bool ImportPackage(ConnectionStorageScope targetScope, string profileName, string packagePath, ConnectionConflictPolicy conflictPolicy, bool importWhenEmptyOnly, out string message)
        {
            lock (_syncRoot)
            {
                if (!File.Exists(packagePath))
                {
                    message = "Package file does not exist.";
                    return false;
                }

                var package = JsonSerializer.Deserialize<ConnectionCatalogPackage>(File.ReadAllText(packagePath), _jsonOptions);
                if (package?.Records == null || package.Records.Count == 0)
                {
                    message = "Package does not contain records.";
                    return false;
                }

                var selectedProfile = NormalizeProfile(profileName);
                var target = ReadScopeRecords(targetScope);
                if (importWhenEmptyOnly && target.Any(r => string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase)))
                {
                    message = "Import skipped because target profile is not empty.";
                    return false;
                }

                var actionLog = new List<string>();
                foreach (var sourceRecord in package.Records)
                {
                    sourceRecord.Connection ??= new ConnectionProperties();
                    EnsureConnectionDefaults(sourceRecord.Connection);
                    var existing = target.FirstOrDefault(r =>
                        string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase) &&
                        (IsSameIdentity(r.Connection, sourceRecord.Connection) ||
                         string.Equals(r.Connection.ConnectionName, sourceRecord.Connection.ConnectionName, StringComparison.OrdinalIgnoreCase)));

                    if (existing == null)
                    {
                        target.Add(CloneRecordForScope(sourceRecord, targetScope, selectedProfile));
                        actionLog.Add($"Added:{sourceRecord.Connection.ConnectionName}");
                        continue;
                    }

                    ResolveConflict(target, existing, sourceRecord, targetScope, selectedProfile, conflictPolicy, actionLog);
                }

                WriteScopeRecords(targetScope, target);
                message = string.Join(Environment.NewLine, actionLog);
                return true;
            }
        }

        public async Task<IReadOnlyList<ConnectionProperties>> LoadConnectionsAsync(ConnectionStorageScope scope, string profileName, bool includePrecedenceChain, CancellationToken cancellationToken = default)
        {
            await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var selectedProfile = NormalizeProfile(profileName);
                var chain = includePrecedenceChain ? GetReadChain(scope) : new[] { scope };
                var merged = new Dictionary<string, ConnectionProperties>(StringComparer.OrdinalIgnoreCase);

                foreach (var chainScope in chain)
                {
                    var records = await ReadScopeRecordsAsync(chainScope, cancellationToken).ConfigureAwait(false);
                    foreach (var record in records
                        .Where(r => string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase)
                                 && r.Connection != null))
                    {
                        EnsureConnectionDefaults(record.Connection);
                        var key = GetIdentityKey(record.Connection);
                        merged[key] = ConnectionSecretProtector.Decrypt(record.Connection);
                    }
                }

                return merged.Values.OrderBy(c => c.ConnectionName).ToList();
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        public async Task<bool> SaveConnectionsAsync(ConnectionStorageScope scope, string profileName, IReadOnlyList<ConnectionProperties> connections, CancellationToken cancellationToken = default)
        {
            await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var selectedProfile = NormalizeProfile(profileName);
                var records = (await ReadScopeRecordsAsync(scope, cancellationToken).ConfigureAwait(false))
                    .Where(r => !string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var connection in connections ?? Array.Empty<ConnectionProperties>())
                {
                    var prepared = PrepareForPersist(connection, scope, selectedProfile);
                    records.Add(prepared);
                }

                await WriteScopeRecordsAsync(scope, records, cancellationToken).ConfigureAwait(false);
                return true;
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        public async Task<bool> AddOrUpdateAsync(ConnectionStorageScope scope, string profileName, ConnectionProperties connection, bool persist, CancellationToken cancellationToken = default)
        {
            if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionName))
            {
                return false;
            }

            await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var selectedProfile = NormalizeProfile(profileName);
                var records = await ReadScopeRecordsAsync(scope, cancellationToken).ConfigureAwait(false);
                var prepared = PrepareForPersist(connection, scope, selectedProfile);

                var existing = records.FirstOrDefault(r =>
                    string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase) &&
                    IsSameIdentity(r.Connection, prepared.Connection));

                if (existing != null)
                {
                    records.Remove(existing);
                }

                records.Add(prepared);
                if (persist)
                {
                    await WriteScopeRecordsAsync(scope, records, cancellationToken).ConfigureAwait(false);
                }

                return true;
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        public async Task<bool> RemoveAsync(ConnectionStorageScope scope, string profileName, string connectionName, bool persist, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(connectionName))
            {
                return false;
            }

            await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var selectedProfile = NormalizeProfile(profileName);
                var records = await ReadScopeRecordsAsync(scope, cancellationToken).ConfigureAwait(false);
                var existing = records.FirstOrDefault(r =>
                    string.Equals(r.ProfileName, selectedProfile, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(r.Connection.ConnectionName, connectionName, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    return false;
                }

                records.Remove(existing);
                if (persist)
                {
                    await WriteScopeRecordsAsync(scope, records, cancellationToken).ConfigureAwait(false);
                }

                return true;
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        private async Task<List<ConnectionCatalogRecord>> ReadScopeRecordsAsync(ConnectionStorageScope scope, CancellationToken cancellationToken)
        {
            var path = GetCatalogFilePath(scope);
            if (!File.Exists(path))
            {
                return new List<ConnectionCatalogRecord>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                var loaded = JsonSerializer.Deserialize<ConnectionCatalogPackage>(json, _jsonOptions);
                return StripNullConnections(loaded?.Records);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JsonConnectionStorageProvider.ReadScopeRecords] {scope}: {ex.GetType().Name} - {ex.Message}");
                return new List<ConnectionCatalogRecord>();
            }
        }

        private async Task WriteScopeRecordsAsync(ConnectionStorageScope scope, List<ConnectionCatalogRecord> records, CancellationToken cancellationToken)
        {
            var path = GetCatalogFilePath(scope);
            var package = new ConnectionCatalogPackage
            {
                SourceScope = scope.ToString(),
                Records = records ?? new List<ConnectionCatalogRecord>(),
                ExportedOnUtc = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(package, _jsonOptions);
            await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
        }

        private static string NormalizeProfile(string profileName)
        {
            return string.IsNullOrWhiteSpace(profileName) ? "Default" : profileName.Trim();
        }

        private static ConnectionStorageScope[] GetReadChain(ConnectionStorageScope selectedScope)
        {
            return selectedScope switch
            {
                ConnectionStorageScope.Project => new[] { ConnectionStorageScope.Project, ConnectionStorageScope.User, ConnectionStorageScope.Machine },
                ConnectionStorageScope.User => new[] { ConnectionStorageScope.User, ConnectionStorageScope.Machine },
                _ => new[] { ConnectionStorageScope.Machine }
            };
        }

        private string GetCatalogFilePath(ConnectionStorageScope scope)
        {
            var appRepoName = string.IsNullOrWhiteSpace(_beepService.AppRepoName) ? "BeepPlatformConnections" : _beepService.AppRepoName;
            var baseDirectory = string.IsNullOrWhiteSpace(_beepService.BeepDirectory) ? AppContext.BaseDirectory : _beepService.BeepDirectory;
            var directory = Path.Combine(baseDirectory, "ConnectionCatalogs", appRepoName);
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, $"{scope.ToString().ToLowerInvariant()}.connections.json");
        }

        private List<ConnectionCatalogRecord> ReadScopeRecords(ConnectionStorageScope scope)
        {
            var path = GetCatalogFilePath(scope);
            if (!File.Exists(path))
            {
                return new List<ConnectionCatalogRecord>();
            }

            try
            {
                var loaded = JsonSerializer.Deserialize<ConnectionCatalogPackage>(File.ReadAllText(path), _jsonOptions);
                return loaded?.Records ?? new List<ConnectionCatalogRecord>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JsonConnectionStorageProvider.ReadScopeRecords] {scope}: {ex.GetType().Name} - {ex.Message}");
                return new List<ConnectionCatalogRecord>();
            }
        }

        private void WriteScopeRecords(ConnectionStorageScope scope, List<ConnectionCatalogRecord> records)
        {
            var path = GetCatalogFilePath(scope);
            var package = new ConnectionCatalogPackage
            {
                SourceScope = scope.ToString(),
                Records = records ?? new List<ConnectionCatalogRecord>(),
                ExportedOnUtc = DateTime.UtcNow
            };

            File.WriteAllText(path, JsonSerializer.Serialize(package, _jsonOptions));
        }

        private static ConnectionCatalogRecord PrepareForPersist(ConnectionProperties connection, ConnectionStorageScope scope, string profileName)
        {
            var sanitized = ConnectionSecretProtector.Encrypt(connection);
            EnsureConnectionDefaults(sanitized);
            return new ConnectionCatalogRecord
            {
                Scope = scope.ToString(),
                ProfileName = profileName,
                SourceStore = scope.ToString(),
                SourceProfile = profileName,
                ExportedOnUtc = DateTime.UtcNow,
                PackageVersion = "1.0",
                Connection = sanitized
            };
        }

        private static void EnsureConnectionDefaults(ConnectionProperties connection)
        {
            if (connection == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(connection.GuidID))
            {
                connection.GuidID = Guid.NewGuid().ToString("D");
            }

            connection.ParameterList ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private static string GetIdentityKey(ConnectionProperties connection)
        {
            if (!string.IsNullOrWhiteSpace(connection.GuidID))
            {
                return "guid:" + connection.GuidID.Trim();
            }

            return "name:" + (connection.ConnectionName ?? string.Empty).Trim();
        }

        private static bool IsSameIdentity(ConnectionProperties left, ConnectionProperties right)
        {
            if (!string.IsNullOrWhiteSpace(left.GuidID) && !string.IsNullOrWhiteSpace(right.GuidID))
            {
                return string.Equals(left.GuidID, right.GuidID, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(left.ConnectionName, right.ConnectionName, StringComparison.OrdinalIgnoreCase);
        }

        private static ConnectionCatalogRecord CloneRecordForScope(ConnectionCatalogRecord source, ConnectionStorageScope scope, string profileName)
        {
            return new ConnectionCatalogRecord
            {
                Scope = scope.ToString(),
                ProfileName = profileName,
                SourceStore = source.Scope,
                SourceProfile = source.ProfileName,
                ExportedOnUtc = DateTime.UtcNow,
                PackageVersion = source.PackageVersion,
                Connection = ConnectionSecretProtector.Encrypt(ConnectionSecretProtector.Decrypt(source.Connection))
            };
        }

        private static void ResolveConflict(
            ICollection<ConnectionCatalogRecord> targetRecords,
            ConnectionCatalogRecord existing,
            ConnectionCatalogRecord incoming,
            ConnectionStorageScope targetScope,
            string profileName,
            ConnectionConflictPolicy conflictPolicy,
            ICollection<string> actionLog)
        {
            switch (conflictPolicy)
            {
                case ConnectionConflictPolicy.Skip:
                    actionLog.Add($"Skipped:{incoming.Connection.ConnectionName}");
                    break;
                case ConnectionConflictPolicy.Rename:
                {
                    var renamed = CloneRecordForScope(incoming, targetScope, profileName);
                    renamed.Connection.ConnectionName = $"{incoming.Connection.ConnectionName}_Imported";
                    renamed.Connection.GuidID = Guid.NewGuid().ToString("D");
                    targetRecords.Add(renamed);
                    actionLog.Add($"Renamed:{incoming.Connection.ConnectionName}");
                    break;
                }
                case ConnectionConflictPolicy.MergeByGuid:
                    if (!string.IsNullOrWhiteSpace(existing.Connection.GuidID) &&
                        string.Equals(existing.Connection.GuidID, incoming.Connection.GuidID, StringComparison.OrdinalIgnoreCase))
                    {
                        targetRecords.Remove(existing);
                        targetRecords.Add(CloneRecordForScope(incoming, targetScope, profileName));
                        actionLog.Add($"Merged:{incoming.Connection.ConnectionName}");
                    }
                    else
                    {
                        actionLog.Add($"SkippedMerge:{incoming.Connection.ConnectionName}");
                    }
                    break;
                default:
                    targetRecords.Remove(existing);
                    targetRecords.Add(CloneRecordForScope(incoming, targetScope, profileName));
                    actionLog.Add($"Replaced:{incoming.Connection.ConnectionName}");
                    break;
            }
        }

        private static void EnsureDirectory(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static List<ConnectionCatalogRecord> StripNullConnections(List<ConnectionCatalogRecord>? records)
        {
            if (records == null)
                return new List<ConnectionCatalogRecord>();

            return records.Where(r => r?.Connection != null).ToList();
        }
    }
}
