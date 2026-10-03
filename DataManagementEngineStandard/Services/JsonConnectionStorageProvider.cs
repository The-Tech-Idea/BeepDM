using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Services;
using TheTechIdea.Beep.Services.Persistence;

namespace TheTechIdea.Beep.Winform.Controls
{
    public sealed class JsonConnectionStorageProvider : IConnectionStorageProvider, IDisposable
    {
        private const string CurrentPackageVersion = "2.0";
        private readonly IBeepService _beepService;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private int _disposed;
        private readonly IConnectionSecretProtector _protector;

        public JsonConnectionStorageProvider(IBeepService beepService)
            : this(beepService, (beepService?.Config_editor as IConnectionProtectionContext)?.ConnectionSecretProtector
                ?? TheTechIdea.Beep.Security.ConnectionCredentialProtection.Default)
        { }

        public JsonConnectionStorageProvider(IBeepService beepService, IConnectionSecretProtector protector)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        }

        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        public IReadOnlyList<ConnectionProperties> LoadConnections(ConnectionStorageScope scope, string profileName, bool includePrecedenceChain)
        {
            ThrowIfDisposed();
            var profile = NormalizeProfile(profileName);
            var chain = includePrecedenceChain ? GetReadChain(scope) : new[] { scope };
            var merged = new Dictionary<string, ConnectionProperties>(StringComparer.OrdinalIgnoreCase);
            foreach (var chainScope in Enumerable.Reverse(chain))
                foreach (var record in ReadScopeRecords(chainScope).Where(r => string.Equals(r.ProfileName, profile, StringComparison.OrdinalIgnoreCase)))
                {
                    EnsureConnectionDefaults(record.Connection);
                    merged[GetIdentityKey(record.Connection)] = _protector.Unprotect(record.Connection);
                }
            return merged.Values.OrderBy(c => c.ConnectionName).ToList();
        }

        public bool SaveConnections(ConnectionStorageScope scope, string profileName, IReadOnlyList<ConnectionProperties> connections)
        {
            ThrowIfDisposed();
            var profile = NormalizeProfile(profileName);
            var prepared = (connections ?? Array.Empty<ConnectionProperties>())
                .Select(connection => PrepareForPersist(connection, scope, profile)).ToList();
            return UpdateScopeRecords(scope, records =>
            {
                records.RemoveAll(r => string.Equals(r.ProfileName, profile, StringComparison.OrdinalIgnoreCase));
                records.AddRange(prepared);
                return true;
            }, true);
        }

        public bool AddOrUpdate(ConnectionStorageScope scope, string profileName, ConnectionProperties connection, bool persist)
        {
            ThrowIfDisposed();
            if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionName)) return false;
            var profile = NormalizeProfile(profileName);
            var prepared = PrepareForPersist(connection, scope, profile);
            return UpdateScopeRecords(scope, records =>
            {
                records.RemoveAll(r => string.Equals(r.ProfileName, profile, StringComparison.OrdinalIgnoreCase) &&
                    IsSameIdentity(r.Connection, prepared.Connection));
                records.Add(prepared);
                return true;
            }, persist);
        }

        public bool Remove(ConnectionStorageScope scope, string profileName, string connectionName, bool persist)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(connectionName)) return false;
            var profile = NormalizeProfile(profileName);
            return UpdateScopeRecords(scope, records =>
                records.RemoveAll(r => string.Equals(r.ProfileName, profile, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(r.Connection.ConnectionName, connectionName, StringComparison.OrdinalIgnoreCase)) > 0, persist);
        }

        public async Task<IReadOnlyList<ConnectionProperties>> LoadConnectionsAsync(ConnectionStorageScope scope,
            string profileName, bool includePrecedenceChain, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            var profile = NormalizeProfile(profileName);
            var chain = includePrecedenceChain ? GetReadChain(scope) : new[] { scope };
            var merged = new Dictionary<string, ConnectionProperties>(StringComparer.OrdinalIgnoreCase);
            // Apply lower-precedence scopes first so Project overrides User, then Machine.
            foreach (var chainScope in Enumerable.Reverse(chain))
            {
                var records = await ReadScopeRecordsAsync(chainScope, cancellationToken).ConfigureAwait(false);
                foreach (var record in records.Where(r => string.Equals(r.ProfileName, profile, StringComparison.OrdinalIgnoreCase)))
                {
                    EnsureConnectionDefaults(record.Connection);
                    merged[GetIdentityKey(record.Connection)] = _protector.Unprotect(record.Connection);
                }
            }
            return merged.Values.OrderBy(c => c.ConnectionName).ToList();
        }

        public Task<bool> SaveConnectionsAsync(ConnectionStorageScope scope, string profileName,
            IReadOnlyList<ConnectionProperties> connections, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            var profile = NormalizeProfile(profileName);
            var prepared = (connections ?? Array.Empty<ConnectionProperties>())
                .Select(connection => PrepareForPersist(connection, scope, profile)).ToList();
            return UpdateScopeRecordsAsync(scope, records =>
            {
                records.RemoveAll(r => string.Equals(r.ProfileName, profile, StringComparison.OrdinalIgnoreCase));
                records.AddRange(prepared);
                return true;
            }, true, cancellationToken);
        }

        public Task<bool> AddOrUpdateAsync(ConnectionStorageScope scope, string profileName,
            ConnectionProperties connection, bool persist, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionName)) return Task.FromResult(false);
            var profile = NormalizeProfile(profileName);
            var prepared = PrepareForPersist(connection, scope, profile);
            return UpdateScopeRecordsAsync(scope, records =>
            {
                records.RemoveAll(r => string.Equals(r.ProfileName, profile, StringComparison.OrdinalIgnoreCase) &&
                    IsSameIdentity(r.Connection, prepared.Connection));
                records.Add(prepared);
                return true;
            }, persist, cancellationToken);
        }

        public Task<bool> RemoveAsync(ConnectionStorageScope scope, string profileName,
            string connectionName, bool persist, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(connectionName)) return Task.FromResult(false);
            var profile = NormalizeProfile(profileName);
            return UpdateScopeRecordsAsync(scope, records =>
                records.RemoveAll(r => string.Equals(r.ProfileName, profile, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(r.Connection.ConnectionName, connectionName, StringComparison.OrdinalIgnoreCase)) > 0,
                persist, cancellationToken);
        }

        public bool Promote(ConnectionStorageScope sourceScope, ConnectionStorageScope targetScope,
            string profileName, ConnectionConflictPolicy conflictPolicy, out string message)
        {
            ThrowIfDisposed();
            var profile = NormalizeProfile(profileName);
            var source = ReadScopeRecords(sourceScope)
                .Where(r => string.Equals(r.ProfileName, profile, StringComparison.OrdinalIgnoreCase)).ToList();
            if (source.Count == 0)
            {
                message = "No records found in source scope.";
                return false;
            }
            var actions = new List<string>();
            UpdateScopeRecords(targetScope, target =>
            {
                ApplyIncoming(target, source, targetScope, profile, conflictPolicy, actions);
                return true;
            }, true);
            message = string.Join(Environment.NewLine, actions);
            return true;
        }

        public bool ExportPackage(ConnectionStorageScope scope, string profileName, string packagePath,
            bool includeEncryptedSecretsOnly, out string message)
        {
            ThrowIfDisposed();
            var profile = NormalizeProfile(profileName);
            var records = ReadScopeRecords(scope)
                .Where(r => string.Equals(r.ProfileName, profile, StringComparison.OrdinalIgnoreCase)).ToList();
            if (records.Count == 0)
            {
                message = "No records available to export.";
                return false;
            }
            var package = new ConnectionCatalogPackage
            {
                PackageVersion = CurrentPackageVersion,
                ProfileName = profile, SourceScope = scope.ToString(), ExportedOnUtc = DateTime.UtcNow
            };
            foreach (var record in records)
            {
                var cloned = includeEncryptedSecretsOnly
                    ? CloneRecordForScope(record, scope, profile)
                    : new ConnectionCatalogRecord
                    {
                        Scope = scope.ToString(), ProfileName = profile, SourceStore = record.Scope,
                        SourceProfile = record.ProfileName, ExportedOnUtc = DateTime.UtcNow,
                        PackageVersion = CurrentPackageVersion, Connection = _protector.Redact(record.Connection)
                    };
                package.Records.Add(cloned);
            }
            AtomicFileStore.WriteText(packagePath, JsonSerializer.Serialize(package, _jsonOptions));
            message = $"Exported {package.Records.Count} connection(s).";
            return true;
        }

        public bool ImportPackage(ConnectionStorageScope targetScope, string profileName, string packagePath,
            ConnectionConflictPolicy conflictPolicy, bool importWhenEmptyOnly, out string message)
        {
            ThrowIfDisposed();
            var json = AtomicFileStore.ReadText(packagePath);
            if (json == null)
            {
                message = "Package file does not exist.";
                return false;
            }
            var package = ParsePackage(json);
            if (package.Records.Count == 0)
            {
                message = "Package does not contain records.";
                return false;
            }
            var profile = NormalizeProfile(profileName);
            var actions = new List<string>();
            var applied = UpdateScopeRecords(targetScope, target =>
            {
                if (importWhenEmptyOnly && target.Any(r => string.Equals(r.ProfileName, profile, StringComparison.OrdinalIgnoreCase)))
                    return false;
                ApplyIncoming(target, package.Records, targetScope, profile, conflictPolicy, actions);
                return true;
            }, true);
            message = applied ? string.Join(Environment.NewLine, actions) : "Import skipped because target profile is not empty.";
            return applied;
        }

        private void ApplyIncoming(List<ConnectionCatalogRecord> target, IEnumerable<ConnectionCatalogRecord> source,
            ConnectionStorageScope scope, string profile, ConnectionConflictPolicy policy, List<string> actions)
        {
            foreach (var record in source)
            {
                EnsureConnectionDefaults(record.Connection);
                var existing = target.FirstOrDefault(r => string.Equals(r.ProfileName, profile, StringComparison.OrdinalIgnoreCase) &&
                    (IsSameIdentity(r.Connection, record.Connection) ||
                     string.Equals(r.Connection.ConnectionName, record.Connection.ConnectionName, StringComparison.OrdinalIgnoreCase)));
                if (existing == null)
                {
                    target.Add(CloneRecordForScope(record, scope, profile));
                    actions.Add($"Added:{record.Connection.ConnectionName}");
                }
                else ResolveConflict(target, existing, record, scope, profile, policy, actions);
            }
        }

        private List<ConnectionCatalogRecord> ReadScopeRecords(ConnectionStorageScope scope)
        {
            var json = AtomicFileStore.ReadText(GetCatalogFilePath(scope));
            return json == null ? new List<ConnectionCatalogRecord>() : ParsePackage(json, scope).Records;
        }

        private bool UpdateScopeRecords(ConnectionStorageScope scope, Func<List<ConnectionCatalogRecord>, bool> update, bool persist)
        {
            if (!persist) return update(ReadScopeRecords(scope));
            var changed = false;
            AtomicFileStore.UpdateText(GetCatalogFilePath(scope), current =>
            {
                var records = current == null ? new List<ConnectionCatalogRecord>() : ParsePackage(current, scope).Records;
                ValidateExistingCredentials(records);
                changed = update(records);
                if (!changed) return current;
                return JsonSerializer.Serialize(new ConnectionCatalogPackage
                {
                    PackageVersion = CurrentPackageVersion,
                    SourceScope = scope.ToString(), Records = records, ExportedOnUtc = DateTime.UtcNow
                }, _jsonOptions);
            });
            return changed;
        }

        private async Task<List<ConnectionCatalogRecord>> ReadScopeRecordsAsync(ConnectionStorageScope scope, CancellationToken token)
        {
            var json = await AtomicFileStore.ReadTextAsync(GetCatalogFilePath(scope), token).ConfigureAwait(false);
            return json == null ? new List<ConnectionCatalogRecord>() : ParsePackage(json, scope).Records;
        }

        private async Task<bool> UpdateScopeRecordsAsync(ConnectionStorageScope scope,
            Func<List<ConnectionCatalogRecord>, bool> update, bool persist, CancellationToken token)
        {
            if (!persist)
                return update(await ReadScopeRecordsAsync(scope, token).ConfigureAwait(false));
            var changed = false;
            await AtomicFileStore.UpdateTextAsync(GetCatalogFilePath(scope), current =>
            {
                var records = current == null ? new List<ConnectionCatalogRecord>() : ParsePackage(current, scope).Records;
                ValidateExistingCredentials(records);
                changed = update(records);
                if (!changed) return current;
                return JsonSerializer.Serialize(new ConnectionCatalogPackage
                {
                    PackageVersion = CurrentPackageVersion,
                    SourceScope = scope.ToString(), Records = records, ExportedOnUtc = DateTime.UtcNow
                }, _jsonOptions);
            }, token).ConfigureAwait(false);
            return changed;
        }

        private ConnectionCatalogPackage ParsePackage(string json, ConnectionStorageScope? scope = null)
        {
            using var document = JsonDocument.Parse(json);
            ValidateUniqueProperties(document.RootElement);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty(nameof(ConnectionCatalogPackage.Records), out var records) ||
                records.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("Connection catalog records are missing or malformed.");
            var package = JsonSerializer.Deserialize<ConnectionCatalogPackage>(json, _jsonOptions);
            if (package == null || (package.PackageVersion != "1.0" && package.PackageVersion != CurrentPackageVersion) || package.Records == null ||
                package.Records.Any(r => r?.Connection == null || string.IsNullOrWhiteSpace(r.Connection.ConnectionName) ||
                    string.IsNullOrWhiteSpace(r.ProfileName) || (r.PackageVersion != "1.0" && r.PackageVersion != CurrentPackageVersion) ||
                    (r.PackageVersion == "1.0" && !string.IsNullOrEmpty(r.Connection.ProtectedCredentialPayload))) ||
                (package.PackageVersion == "1.0" && package.Records.Any(r => r.PackageVersion != "1.0")))
                throw new InvalidDataException("Unsupported or malformed connection catalog package.");
            if (scope != null && (!string.Equals(package.SourceScope, scope.ToString(), StringComparison.OrdinalIgnoreCase) ||
                package.Records.Any(r => !string.Equals(r.Scope, scope.ToString(), StringComparison.OrdinalIgnoreCase))))
                throw new InvalidDataException("Connection catalog scope does not match its storage identity.");
            return package;
        }

        private void ValidateExistingCredentials(IEnumerable<ConnectionCatalogRecord> records)
        {
            // Validate before any deletion/replacement. Redacted export deliberately does not require keys.
            foreach (var record in records) _protector.Unprotect(record.Connection);
        }

        private static void ValidateUniqueProperties(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name)) throw new InvalidDataException("Connection catalog contains duplicate JSON properties.");
                    ValidateUniqueProperties(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
                foreach (var item in element.EnumerateArray()) ValidateUniqueProperties(item);
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

        private ConnectionCatalogRecord PrepareForPersist(ConnectionProperties connection, ConnectionStorageScope scope, string profileName)
        {
            var prepared = _protector.Unprotect(connection);
            EnsureConnectionDefaults(prepared);
            var sanitized = _protector.Protect(prepared);
            return new ConnectionCatalogRecord
            {
                Scope = scope.ToString(),
                ProfileName = profileName,
                SourceStore = scope.ToString(),
                SourceProfile = profileName,
                ExportedOnUtc = DateTime.UtcNow,
                PackageVersion = CurrentPackageVersion,
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

        private ConnectionCatalogRecord CloneRecordForScope(ConnectionCatalogRecord source, ConnectionStorageScope scope, string profileName, bool rename = false)
        {
            var plaintext = _protector.Unprotect(source.Connection);
            EnsureConnectionDefaults(plaintext);
            if (rename)
            {
                plaintext.ConnectionName += "_Imported";
                plaintext.GuidID = Guid.NewGuid().ToString("D");
            }
            return new ConnectionCatalogRecord
            {
                Scope = scope.ToString(),
                ProfileName = profileName,
                SourceStore = source.Scope,
                SourceProfile = source.ProfileName,
                ExportedOnUtc = DateTime.UtcNow,
                PackageVersion = CurrentPackageVersion,
                Connection = _protector.Protect(plaintext)
            };
        }

        private void ResolveConflict(
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
                    var renamed = CloneRecordForScope(incoming, targetScope, profileName, rename: true);
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
