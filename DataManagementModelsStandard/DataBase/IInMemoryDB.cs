using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Contract for data sources that can persist all data in-memory (no external DB roundtrip per op).
    ///
    /// Implementers: SQLite (`:memory:`), DuckDB (in-memory mode), LiteDB (MemoryStream),
    /// RocksDB / LevelDB / LMDB (temp-dir fallback), Couchbase Lite (in-memory mode),
    /// Realm (in-memory Realm).
    ///
    /// Lifecycle:
    ///   OpenInMemory(name) → CreateStructure / LoadStructure → LoadData
    ///   SyncData / RefreshData ↔ external sources
    ///   FillFromDataSource / ExportToDataSource for bulk movement
    ///   SaveStructure / LoadData for persistence round-trips
    ///   ResetInMemory to clear data + schema without dropping the connection
    ///   Dispose to release the in-memory engine
    /// </summary>
    public interface IInMemoryDB : IDisposable
    {
        // ── State flags (settable for back-compat with existing implementations; implementations should
        //    raise StateChanged when any flag flips) ──
        bool IsCreated { get; set; }
        bool IsLoaded { get; set; }
        bool IsSaved { get; set; }
        bool IsSynced { get; set; }
        bool IsStructureCreated { get; set; }
        bool IsStructureLoaded { get; set; }

        // ── Connection / open ──
        /// <summary>Opens (or re-opens) the in-memory engine with the given logical database name.</summary>
        IErrorsInfo OpenInMemory(string databaseName);

        /// <summary>Returns the in-memory connection string (e.g. SQLite ":memory:", LiteDB "memory").</summary>
        string GetInMemoryConnectionString();

        /// <summary>Drops all data + schema without closing the connection. Reuse for a fresh in-memory session.</summary>
        IErrorsInfo ResetInMemory();

        // ── Structure ──
        IErrorsInfo CreateStructure(IProgress<PassedArgs>? progress = null, CancellationToken token = default);
        IErrorsInfo LoadStructure(IProgress<PassedArgs>? progress = null, CancellationToken token = default);

        /// <summary>Load structure AND hydrate rows from the backing store.</summary>
        IErrorsInfo LoadStructureWithData(IProgress<PassedArgs>? progress = null, CancellationToken token = default);

        IErrorsInfo SaveStructure();

        // ── Data ──
        IErrorsInfo LoadData(IProgress<PassedArgs>? progress = null, CancellationToken token = default);

        IErrorsInfo SyncData(IProgress<PassedArgs>? progress = null, CancellationToken token = default);
        IErrorsInfo SyncData(string entityName, IProgress<PassedArgs>? progress = null, CancellationToken token = default);

        IErrorsInfo RefreshData(IProgress<PassedArgs>? progress = null, CancellationToken token = default);
        IErrorsInfo RefreshData(string entityName, IProgress<PassedArgs>? progress = null, CancellationToken token = default);

        // ── Bulk import / export ──
        /// <summary>Bulk-copy all rows from <paramref name="source"/> into the in-memory engine.</summary>
        IErrorsInfo FillFromDataSource(IDataSource source, IProgress<PassedArgs>? progress = null, CancellationToken token = default);

        /// <summary>Bulk-copy all in-memory rows into <paramref name="target"/>.</summary>
        IErrorsInfo ExportToDataSource(IDataSource target, IProgress<PassedArgs>? progress = null, CancellationToken token = default);

        // ── Schema cache (built by LoadStructure / CreateStructure) ──
        List<EntityStructure> InMemoryStructures { get; set; }

        // ── Lifecycle events (replace the previous 7-event set with a uniform 3-event contract) ──
        /// <summary>Raised whenever the schema (InMemoryStructures) changes — created, dropped, or modified.</summary>
        event EventHandler<PassedArgs> StructureChanged;

        /// <summary>Raised whenever row data changes — LoadData, SyncData, RefreshData, FillFrom, ExportTo.</summary>
        event EventHandler<PassedArgs> DataChanged;

        /// <summary>Raised whenever any state flag (IsCreated / IsLoaded / IsSaved / IsSynced) flips.</summary>
        event EventHandler<PassedArgs> StateChanged;
    }
}