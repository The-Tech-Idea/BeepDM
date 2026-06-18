using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.ConfigUtil
{
    /// <summary>
    /// Catalog-aware connection persistence. Implemented by BeepConnectionRepository
    /// in the engine layer. Declared here so IConfigEditor can reference it without
    /// taking a dependency on the engine project.
    /// </summary>
    public interface IConnectionCatalogRepository
    {
        ConnectionStorageScope ActiveScope { get; set; }
        string ActiveProfileName { get; set; }
        bool UseScopePrecedence { get; set; }

        IReadOnlyList<ConnectionProperties> LoadConnections();
        bool AddOrUpdate(ConnectionProperties connection, bool persist = true);
        bool Remove(string connectionName, bool persist = true);
        bool Promote(ConnectionStorageScope targetScope, ConnectionConflictPolicy conflictPolicy, out string message);
        bool ExportPackage(string packagePath, bool includeEncryptedSecretsOnly, out string message);
        bool ImportPackage(string packagePath, ConnectionConflictPolicy conflictPolicy, bool importWhenEmptyOnly, out string message);
        bool Save(IReadOnlyList<ConnectionProperties> connections);

        event EventHandler? ConnectionsChanged;
    }

    /// <summary>
    /// Lightweight connection lifecycle state (platform-agnostic).
    /// Mirrors BeepConnectionState in the engine layer.
    /// </summary>
    public enum BeepConnectionLifecycle
    {
        Unknown = 0,
        Connected = 1,
        Failed = 2,
        Testing = 3,
        Closed = 4
    }

    /// <summary>
    /// Connection health status for monitoring.
    /// </summary>
    public enum ConnectionHealth
    {
        Unknown = 0,
        Healthy = 1,
        Degraded = 2,
        Unhealthy = 3
    }
}
