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
    public sealed class BeepConnectionRepository : IConnectionCatalogRepository
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
        /// <summary>Observer errors after a committed change; diagnostic observer errors are isolated too.</summary>
        public event EventHandler<ConnectionCatalogNotificationFailureEventArgs>? NotificationFailed;
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
            var profile = ActiveProfileName;
            EventHandler? observers;
            lock (GetScopeLock(scope))
            {
                EnsureConnectionDefaults(connection);
                var changed = _storageProvider.AddOrUpdate(scope, profile, connection, persist);

                if (!changed)
                {
                    return false;
                }

                observers = ConnectionsChanged;
            }
            NotifyCommitted(nameof(AddOrUpdate), scope, observers);
            return true;
        }

        public bool Remove(string connectionName, bool persist = true)
        {
            if (string.IsNullOrWhiteSpace(connectionName))
            {
                return false;
            }

            var scope = ActiveScope;
            var profile = ActiveProfileName;
            EventHandler? observers;
            lock (GetScopeLock(scope))
            {
                var removed = _storageProvider.Remove(scope, profile, connectionName, persist);
                if (!removed)
                {
                    return false;
                }

                observers = ConnectionsChanged;
            }
            NotifyCommitted(nameof(Remove), scope, observers);
            return true;
        }

        public bool Save(List<ConnectionProperties> connections)
        {
            return ((IConnectionCatalogRepository)this).Save(connections ?? new List<ConnectionProperties>());
        }

        bool IConnectionCatalogRepository.Save(IReadOnlyList<ConnectionProperties> connections)
        {
            var scope = ActiveScope;
            var profile = ActiveProfileName;
            EventHandler? observers;
            lock (GetScopeLock(scope))
            {
                var saved = _storageProvider.SaveConnections(scope, profile, connections ?? new List<ConnectionProperties>());
                if (!saved)
                {
                    return false;
                }

                observers = ConnectionsChanged;
            }
            NotifyCommitted(nameof(Save), scope, observers);
            return true;
        }

        public bool Promote(ConnectionStorageScope targetScope, ConnectionConflictPolicy conflictPolicy, out string message)
        {
            var sourceScope = ActiveScope;
            var profile = ActiveProfileName;
            EventHandler? observers;
            lock (GetScopeLock(sourceScope))
            {
                if (!_storageProvider.Promote(sourceScope, targetScope, profile, conflictPolicy, out message)) return false;
                observers = ConnectionsChanged;
            }
            NotifyCommitted(nameof(Promote), sourceScope, observers);
            return true;
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
            var profile = ActiveProfileName;
            EventHandler? observers;
            lock (GetScopeLock(scope))
            {
                if (!_storageProvider.ImportPackage(scope, profile, packagePath, conflictPolicy, importWhenEmptyOnly, out message)) return false;
                observers = ConnectionsChanged;
            }
            NotifyCommitted(nameof(ImportPackage), scope, observers);
            return true;
        }

        private void NotifyCommitted(string operation, ConnectionStorageScope scope, EventHandler? observers)
        {
            if (observers == null) return;
            // The captured list runs outside scope locks. Concurrent changes may notify out of commit order.
            foreach (EventHandler observer in observers.GetInvocationList())
            {
                try { observer(this, EventArgs.Empty); }
                catch (Exception ex)
                {
                    var failure = new ConnectionCatalogNotificationFailureEventArgs(operation, scope, ex.GetType().Name);
                    ReportObserverFailure(failure.ExceptionType);
                    var diagnostics = NotificationFailed;
                    if (diagnostics == null) continue;
                    foreach (EventHandler<ConnectionCatalogNotificationFailureEventArgs> diagnostic in diagnostics.GetInvocationList())
                    {
                        try { diagnostic(this, failure); }
                        catch (Exception diagnosticError) { ReportObserverFailure(diagnosticError.GetType().Name); }
                    }
                }
            }
        }

        private void ReportObserverFailure(string exceptionType)
        {
            // Observer and logger messages may contain credentials. Neither can change the durable result.
            try { _beepService.lg?.WriteLog($"Connection catalog observer failed ({exceptionType}); storage outcome is unchanged."); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Connection catalog observer logging failed ({ex.GetType().Name})."); }
        }

        private object GetScopeLock(ConnectionStorageScope scope)
        {
            if (!_scopeLocks.TryGetValue(scope, out var lockObj))
            {
                throw new ArgumentOutOfRangeException(nameof(scope));
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
