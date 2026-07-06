using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Utilities;
using DataSourceType = TheTechIdea.Beep.Utilities.DataSourceType;
using static TheTechIdea.Beep.SetUp.StepErrorHelpers;

namespace TheTechIdea.Beep.SetUp.Steps
{
    /// <summary>
    /// Configures, persists, and optionally opens a data connection.
    ///
    /// Execution order
    /// ───────────────
    ///  1. Find the best-matching driver config
    ///     (ConnectionHelper.GetBestMatchingDriver)
    ///  2. Build / resolve connection string template
    ///     (ConnectionHelper.ReplaceValueFromConnectionString)
    ///  3. Normalise file-system paths for file-based datasources
    ///  4. Validate connection string structure (skippable via options)
    ///  5. Report readiness (credentials kept intact in cp.ConnectionString)
    ///  6. Persist: AddDataConnection (new) / UpdateDataConnection (existing)
    ///     + SaveDataconnectionsValues
    ///  7. Optionally open the datasource and populate context.DataSource
    /// </summary>
    public class ConnectionConfigStep : IConnectionConfigStep
    {
        private readonly ConnectionConfigStepOptions _opts;
        private readonly ILogger<ConnectionConfigStep>? _logger;

        public ConnectionConfigStep(ConnectionConfigStepOptions opts, ILogger<ConnectionConfigStep>? logger = null)
        {
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
            _logger = logger;
        }

        /// <summary>
        /// Public accessor for the typed options. UI shells (and other consumers) use this to
        /// read and write the option values directly without going through reflection or a
        /// string-keyed Properties dictionary. The canonical <see cref="Execute"/> pipeline
        /// reads from the same object, so UI mutations take effect immediately.
        /// </summary>
        public ConnectionConfigStepOptions Options => _opts;

        // ── ISetupStep ───────────────────────────────────────────────────────

        public string StepId => "connection-config";
        public string StepName => "Connection Configuration";
        public string Description => $"Configure and persist connection '{_opts.ConnectionProperties?.ConnectionName}'.";
        public IReadOnlyList<string> DependsOn => new[] { "driver-provision" };

        public bool CanSkip(SetupContext context)
        {
            if (context?.Options?.DryRun == true) return true;
            var cp = _opts.ConnectionProperties;
            if (cp == null) return false;

            bool alreadyRegistered = context?.Editor?.ConfigEditor?.DataConnections
                ?.Any(c => string.Equals(c.ConnectionName, cp.ConnectionName,
                    StringComparison.OrdinalIgnoreCase)) == true;

            if (!alreadyRegistered) return false;

            // Skip only when the datasource is already open — avoids reopening on every wizard run
            return context?.DataSource != null
                && context.DataSource.ConnectionStatus == ConnectionState.Open;
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context?.Editor == null)
                return Fail("SetupContext.Editor is required.");

            var cp = _opts.ConnectionProperties;
            if (cp == null)
                return Fail("ConnectionConfigStepOptions.ConnectionProperties must be set.");

            if (string.IsNullOrWhiteSpace(cp.ConnectionName))
                return Fail("ConnectionProperties.ConnectionName must be set.");

            if (cp.DatabaseType == DataSourceType.Unknown || cp.DatabaseType == DataSourceType.NONE)
                return Fail($"ConnectionProperties.DatabaseType is '{cp.DatabaseType}'. " +
                             "Set it to a valid datasource type (e.g. SqlServer, SqlLite, PostgreSql).");

            return Ok();
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            var cp = _opts.ConnectionProperties;
            var editor = context.Editor;
            var baseDir = _opts.BaseDirectory ?? AppContext.BaseDirectory;

            // ── 1. Link to driver ────────────────────────────────────────────
            StepErrorHelpers.Report(progress, 10, "Linking connection to driver…");
            var driver = ConnectionHelper.GetBestMatchingDriver(cp, editor.ConfigEditor);
            if (driver == null)
                return Fail($"No matching driver found for ConnectionName='{cp.ConnectionName}', " +
                             $"DatabaseType='{cp.DatabaseType}'. " +
                             "Run DriverProvisionStep first or verify DataDriversClasses.");

            cp.DriverName = driver.PackageName;

            // ── 2. Build connection string ───────────────────────────────────
            StepErrorHelpers.Report(progress, 25, "Building connection string…");
            string builtCs = ConnectionHelper.ReplaceValueFromConnectionString(driver, cp, editor);
            if (!string.IsNullOrWhiteSpace(builtCs))
                cp.ConnectionString = builtCs;

            // ── 3. Normalise file paths ──────────────────────────────────────
            ConnectionHelper.NormalizeFilePath(cp, baseDir);

            // Validate directory existence for file-based datasources
            if (IsFileBasedDatasource(cp.DatabaseType) && !string.IsNullOrWhiteSpace(cp.FilePath))
            {
                var dir = Path.GetDirectoryName(cp.FilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    _logger?.LogWarning("Directory '{Dir}' for file-based datasource does not exist; it will be created",
                        dir);
                    try { Directory.CreateDirectory(dir); }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to create directory '{Dir}' for file-based datasource", dir);
                        return Fail($"Failed to create directory for file-based datasource '{cp.ConnectionName}': {ex.Message}");
                    }
                }
            }

            // ── 4. Validate connection string structure ──────────────────────
            if (!_opts.SkipConnectionStringValidation)
            {
                StepErrorHelpers.Report(progress, 40, "Validating connection string…");
                if (!ConnectionHelper.IsConnectionStringValid(cp.ConnectionString, cp.DatabaseType))
                    return Fail($"Connection string for '{cp.ConnectionName}' failed validation " +
                                 $"(DatabaseType={cp.DatabaseType}). " +
                                 "Set SkipConnectionStringValidation=true to bypass.");
            }

            // ── 5. Log masked credential preview (display only; cp.ConnectionString unchanged) ─────
            // SecureConnectionString masks passwords for log/display purposes.
            // We must NOT store the masked string in cp — the real credentials are required
            // for every subsequent OpenDataSource call.
            StepErrorHelpers.Report(progress, 55, $"Connection string ready. Driver={cp.DriverName}");

            // ── 6. Persist ───────────────────────────────────────────────────
            StepErrorHelpers.Report(progress, 65, "Persisting connection…");

            // Look up the STORED GuidID so UpdateDataConnection targets the right record.
            // Using cp.GuidID here would be wrong when cp is a new/detached object — it
            // would cause UpdateDataConnection to fall back to Add, creating duplicates.
            var storedConn = editor.ConfigEditor.DataConnections
                ?.FirstOrDefault(c => string.Equals(c.ConnectionName, cp.ConnectionName,
                    StringComparison.OrdinalIgnoreCase));

            bool persisted = storedConn != null
                ? editor.ConfigEditor.UpdateDataConnection(cp, storedConn.GuidID)
                : editor.ConfigEditor.AddDataConnection(cp);

            if (!persisted)
                return Fail($"Failed to {(storedConn != null ? "update" : "add")} connection '{cp.ConnectionName}'.");

            editor.ConfigEditor.SaveDataconnectionsValues();

            // Store in context regardless of whether we open the connection
            context.ConnectionProperties = cp;

            // ── 7. Open connection ───────────────────────────────────────────
            if (_opts.OpenConnection)
            {
                StepErrorHelpers.Report(progress, 80, $"Opening connection '{cp.ConnectionName}'…");
                var state = editor.OpenDataSource(cp.ConnectionName);

                if (state != ConnectionState.Open)
                    return Fail($"Failed to open datasource '{cp.ConnectionName}'. " +
                                 $"ConnectionState returned: {state}");

                context.DataSource = editor.GetDataSource(cp.ConnectionName);
            }

            StepErrorHelpers.Report(progress, 100, "Connection configured.");
            return Ok($"Connection '{cp.ConnectionName}' configured successfully.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static bool IsFileBasedDatasource(DataSourceType databaseType)
        {
            return databaseType == DataSourceType.SqlLite
                || databaseType == DataSourceType.SqlCompact
                || databaseType == DataSourceType.LiteDB
                || databaseType == DataSourceType.VistaDB;
        }
    }
}
