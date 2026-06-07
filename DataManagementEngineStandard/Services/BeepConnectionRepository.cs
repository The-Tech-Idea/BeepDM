using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.Winform.Controls
{
    /// <summary>
    /// Centralized connection persistence helper backed by IBeepService.Config_editor.
    /// Keeps connection CRUD and save/reload behavior consistent across forms.
    /// </summary>
    public sealed class BeepConnectionRepository
    {
        private readonly IBeepService _beepService;
        private readonly IConnectionStorageProvider _storageProvider;
        private readonly Dictionary<ConnectionStorageScope, object> _scopeLocks = new()
        {
            [ConnectionStorageScope.Project] = new object(),
            [ConnectionStorageScope.User] = new object(),
            [ConnectionStorageScope.Machine] = new object()
        };

        public event EventHandler? ConnectionsChanged;
        public ConnectionStorageScope ActiveScope { get; set; } = ConnectionStorageScope.Project;
        public string ActiveProfileName { get; set; } = "Default";
        public bool UseScopePrecedence { get; set; } = true;

        public BeepConnectionRepository(IBeepService beepService, IConnectionStorageProvider? storageProvider = null)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _storageProvider = storageProvider ?? new JsonConnectionStorageProvider(_beepService);
        }

        public IReadOnlyList<ConnectionProperties> LoadConnections()
        {
            var scope = ActiveScope;
            lock (GetScopeLock(scope))
            {
                return _storageProvider.LoadConnections(scope, ActiveProfileName, UseScopePrecedence);
            }
        }

        public bool AddOrUpdate(ConnectionProperties connection, bool persist = true)
        {
            if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionName))
            {
                return false;
            }

            var scope = ActiveScope;
            lock (GetScopeLock(scope))
            {
                EnsureConnectionDefaults(connection);
                var changed = _storageProvider.AddOrUpdate(scope, ActiveProfileName, connection, persist);

                if (!changed)
                {
                    return false;
                }

                ConnectionsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
        }

        public bool Remove(string connectionName, bool persist = true)
        {
            if (string.IsNullOrWhiteSpace(connectionName))
            {
                return false;
            }

            var scope = ActiveScope;
            lock (GetScopeLock(scope))
            {
                var removed = _storageProvider.Remove(scope, ActiveProfileName, connectionName, persist);
                if (!removed)
                {
                    return false;
                }

                ConnectionsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
        }

        public bool Save(List<ConnectionProperties> connections)
        {
            var scope = ActiveScope;
            lock (GetScopeLock(scope))
            {
                var saved = _storageProvider.SaveConnections(scope, ActiveProfileName, connections ?? new List<ConnectionProperties>());
                if (!saved)
                {
                    return false;
                }

                ConnectionsChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
        }

        public bool Promote(ConnectionStorageScope targetScope, ConnectionConflictPolicy conflictPolicy, out string message)
        {
            var sourceScope = ActiveScope;
            lock (GetScopeLock(sourceScope))
            {
                var ok = _storageProvider.Promote(sourceScope, targetScope, ActiveProfileName, conflictPolicy, out message);
                if (ok)
                {
                    ConnectionsChanged?.Invoke(this, EventArgs.Empty);
                }

                return ok;
            }
        }

        public bool ExportPackage(string packagePath, bool includeEncryptedSecretsOnly, out string message)
        {
            var scope = ActiveScope;
            lock (GetScopeLock(scope))
            {
                return _storageProvider.ExportPackage(scope, ActiveProfileName, packagePath, includeEncryptedSecretsOnly, out message);
            }
        }

        public bool ImportPackage(
            string packagePath,
            ConnectionConflictPolicy conflictPolicy,
            bool importWhenEmptyOnly,
            out string message)
        {
            var scope = ActiveScope;
            lock (GetScopeLock(scope))
            {
                var ok = _storageProvider.ImportPackage(scope, ActiveProfileName, packagePath, conflictPolicy, importWhenEmptyOnly, out message);
                if (ok)
                {
                    ConnectionsChanged?.Invoke(this, EventArgs.Empty);
                }

                return ok;
            }
        }

        private object GetScopeLock(ConnectionStorageScope scope)
        {
            if (!_scopeLocks.TryGetValue(scope, out var lockObj))
            {
                lockObj = new object();
                _scopeLocks[scope] = lockObj;
            }

            return lockObj;
        }

        private static void EnsureConnectionDefaults(ConnectionProperties connection)
        {
            if (string.IsNullOrWhiteSpace(connection.GuidID))
            {
                connection.GuidID = Guid.NewGuid().ToString("D");
            }
        }
    }
}
