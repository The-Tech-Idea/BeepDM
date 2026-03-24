using System;
using System.Data;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Helpers.ConnectionHelpers
{
    /// <summary>
    /// Determines whether a live <see cref="IDataSource"/> is reachable and responding.
    /// Works purely through the <see cref="IDataSource"/> contract — no driver-specific
    /// assembly references required.  The probe strategy is chosen based on
    /// <see cref="IDataSource.Category"/> and <see cref="IDataSource.DatasourceType"/>
    /// so that each technology is tested in the most appropriate way.
    /// </summary>
    public static class ProxyLivenessHelper
    {
        // ── Public entry point ──────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when <paramref name="ds"/> is reachable and responding.
        /// Never throws — all exceptions are swallowed and mapped to <c>false</c>.
        /// </summary>
        /// <param name="ds">Live datasource instance managed by DMEEditor.</param>
        /// <param name="timeoutMs">
        /// Advisory timeout in milliseconds.  The probe is synchronous; this governs
        /// how long the upstream caller should wait before declaring the source dead.
        /// </param>
        public static bool IsAlive(IDataSource ds, int timeoutMs = 5_000)
        {
            if (ds == null) return false;

            try
            {
                // 1. Ensure the connection is open
                if (ds.ConnectionStatus != ConnectionState.Open)
                {
                    ds.Openconnection();
                    if (ds.ConnectionStatus != ConnectionState.Open)
                        return false;
                }

                // 2. Category-specific liveness probe
                return ds.Category switch
                {
                    DatasourceCategory.RDBMS      => ProbeRdbms(ds),
                    DatasourceCategory.NOSQL       => ProbeMetadata(ds),
                    DatasourceCategory.FILE        => ProbeMetadata(ds),
                    DatasourceCategory.WEBAPI      => ProbeMetadata(ds),
                    DatasourceCategory.CLOUD       => ProbeMetadata(ds),
                    DatasourceCategory.VIEWS       => ProbeMetadata(ds),
                    DatasourceCategory.VectorDB    => ProbeMetadata(ds),
                    DatasourceCategory.MLModel     => ProbeMetadata(ds),
                    DatasourceCategory.IoT         => ProbeConnectionState(ds),
                    DatasourceCategory.STREAM      => ProbeConnectionState(ds),
                    DatasourceCategory.QUEUE       => ProbeConnectionState(ds),
                    DatasourceCategory.Blockchain  => ProbeConnectionState(ds),
                    DatasourceCategory.Connector   => ProbeConnectionState(ds),
                    DatasourceCategory.Workflow    => ProbeMetadata(ds),
                    DatasourceCategory.INMEMORY    => ProbeInMemory(ds),
                    DatasourceCategory.NONE        => ProbeConnectionState(ds),
                    _                              => ProbeConnectionState(ds)
                };
            }
            catch
            {
                return false;
            }
        }

        // ── Probe strategies ─────────────────────────────────────────────

        /// <summary>
        /// RDBMS: open + execute the cheapest possible read-only query for this engine.
        /// Falls back to connection-state check if <see cref="IDataSource.ExecuteSql"/> throws.
        /// </summary>
        private static bool ProbeRdbms(IDataSource ds)
        {
            var sql = GetRdbmsPingQuery(ds.DatasourceType);
            try
            {
                var result = ds.ExecuteSql(sql);
                // ExecuteSql returns IErrorsInfo; Flag == Failed means the DB rejected it
                return result == null || result.Flag != Errors.Failed;
            }
            catch
            {
                // If ExecuteSql itself throws, fall back to connection state
                return ds.ConnectionStatus == ConnectionState.Open;
            }
        }

        /// <summary>
        /// Queryable types (NoSQL, File, WebAPI, Cloud, VectorDB, …):
        /// call <see cref="IDataSource.GetEntitesList"/> — a lightweight metadata read.
        /// If the source is truly up this will succeed (even returning an empty list).
        /// Falls back to connection state on exception.
        /// </summary>
        private static bool ProbeMetadata(IDataSource ds)
        {
            try
            {
                var _ = ds.GetEntitesList();
                return true;  // any result (including empty enumerable) means the source responded
            }
            catch
            {
                return ds.ConnectionStatus == ConnectionState.Open;
            }
        }

        /// <summary>
        /// Stream / Queue / IoT / Blockchain / Connector:
        /// these are event-driven; there is no meaningful "query" to run.
        /// Connection-state is the best available signal.
        /// </summary>
        private static bool ProbeConnectionState(IDataSource ds)
            => ds.ConnectionStatus == ConnectionState.Open;

        /// <summary>
        /// In-memory sources (DuckDB in-memory, Realm in-memory, etc.) live inside the
        /// process and cannot go "down" as long as the process is alive.
        /// Only mark dead if the connection is explicitly <see cref="ConnectionState.Broken"/>.
        /// </summary>
        private static bool ProbeInMemory(IDataSource ds)
            => ds.ConnectionStatus != ConnectionState.Broken;

        // ── RDBMS ping queries ────────────────────────────────────────────

        /// <summary>
        /// Returns the cheapest read-only probe statement for the given RDBMS engine.
        /// Every statement here reads zero rows and incurs minimal server load.
        /// </summary>
        private static string GetRdbmsPingQuery(DataSourceType type) => type switch
        {
            // Standard ANSI / popular RDBMS
            DataSourceType.SqlServer    => "SELECT 1",
            DataSourceType.Mysql        => "SELECT 1",
            DataSourceType.Postgre      => "SELECT 1",
            DataSourceType.SqlLite      => "SELECT 1",
            DataSourceType.SqlCompact   => "SELECT 1",
            DataSourceType.VistaDB      => "SELECT 1",
            DataSourceType.DuckDB       => "SELECT 1",
            DataSourceType.MariaDB      => "SELECT 1",            // MySQL-compatible
            DataSourceType.Cockroach    => "SELECT 1",            // PostgreSQL-compatible
            DataSourceType.AzureSQL     => "SELECT 1",            // SQL Server-compatible
            DataSourceType.AWSRDS       => "SELECT 1",            // multi-engine RDS
            DataSourceType.Spanner      => "SELECT 1",            // Google Cloud Spanner
            DataSourceType.TerraData    => "SELECT 1",
            DataSourceType.Vertica      => "SELECT 1",
            DataSourceType.TimeScale    => "SELECT 1",            // PostgreSQL extension
            DataSourceType.Presto       => "SELECT 1",
            DataSourceType.Trino        => "SELECT 1",
            DataSourceType.Firebolt     => "SELECT 1",
            DataSourceType.Hologres     => "SELECT 1",
            DataSourceType.Supabase     => "SELECT 1",            // PostgreSQL-compatible
            DataSourceType.ClickHouse   => "SELECT 1",            // ClickHouse SQL syntax
            DataSourceType.ADO          => "SELECT 1",

            // Oracle — requires FROM clause
            DataSourceType.Oracle       => "SELECT 1 FROM DUAL",

            // SAP HANA — requires FROM DUMMY table
            DataSourceType.Hana         => "SELECT 1 FROM DUMMY",

            // IBM DB2 family
            DataSourceType.DB2          => "SELECT 1 FROM SYSIBM.SYSDUMMY1",

            // Firebird — requires FROM clause
            DataSourceType.FireBird     => "SELECT 1 FROM RDB$DATABASE",

            // Cloud RDBMS (same SQL syntax as their base engine)
            DataSourceType.AWSRedshift  => "SELECT 1",
            DataSourceType.SnowFlake    => "SELECT 1",
            DataSourceType.GoogleBigQuery => "SELECT 1",
            DataSourceType.DataBricks   => "SELECT 1",

            // ODBC / generic
            DataSourceType.ODBC         => "SELECT 1",
            DataSourceType.OLEDB        => "SELECT 1",

            // Default — works for most ANSI-compliant RDBMSes
            _                           => "SELECT 1"
        };
    }
}
