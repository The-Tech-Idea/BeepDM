using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;

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
    public class ConnectionConfigStep : ISetupStep
    {
        private readonly ConnectionConfigStepOptions _opts;

        public ConnectionConfigStep(ConnectionConfigStepOptions opts)
        {
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
        }

        // ── ISetupStep ───────────────────────────────────────────────────────

        public string StepId => "connection-config";
        public string StepName => "Connection Configuration";
        public string Description => $"Configure and persist connection '{_opts.ConnectionProperties?.ConnectionName}'.";
        public IReadOnlyList<string> DependsOn => new[] { "driver-provision" };

        public bool CanSkip(SetupContext context)
        {
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

            return Ok();
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            var cp = _opts.ConnectionProperties;
            var editor = context.Editor;
            var baseDir = _opts.BaseDirectory ?? AppContext.BaseDirectory;

            // ── 1. Link to driver ────────────────────────────────────────────
            Report(progress, 10, "Linking connection to driver…");
            var driver = ConnectionHelper.GetBestMatchingDriver(cp, editor.ConfigEditor);
            if (driver == null)
                return Fail($"No matching driver found for ConnectionName='{cp.ConnectionName}', " +
                             $"DatabaseType='{cp.DatabaseType}'. " +
                             "Run DriverProvisionStep first or verify DataDriversClasses.");

            cp.DriverName = driver.PackageName;

            // ── 2. Build connection string ───────────────────────────────────
            Report(progress, 25, "Building connection string…");
            string builtCs = ConnectionHelper.ReplaceValueFromConnectionString(driver, cp, editor);
            if (!string.IsNullOrWhiteSpace(builtCs))
                cp.ConnectionString = builtCs;

            // ── 3. Normalise file paths ──────────────────────────────────────
            ConnectionHelper.NormalizeFilePath(cp, baseDir);

            // ── 4. Validate connection string structure ──────────────────────
            if (!_opts.SkipConnectionStringValidation)
            {
                Report(progress, 40, "Validating connection string…");
                if (!ConnectionHelper.IsConnectionStringValid(cp.ConnectionString, cp.DatabaseType))
                    return Fail($"Connection string for '{cp.ConnectionName}' failed validation " +
                                 $"(DatabaseType={cp.DatabaseType}). " +
                                 "Set SkipConnectionStringValidation=true to bypass.");
            }

            // ── 5. Log masked credential preview (display only; cp.ConnectionString unchanged) ─────
            // SecureConnectionString masks passwords for log/display purposes.
            // We must NOT store the masked string in cp — the real credentials are required
            // for every subsequent OpenDataSource call.
            Report(progress, 55, $"Connection string ready. Driver={cp.DriverName}");

            // ── 6. Persist ───────────────────────────────────────────────────
            Report(progress, 65, "Persisting connection…");

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
                Report(progress, 80, $"Opening connection '{cp.ConnectionName}'…");
                var state = editor.OpenDataSource(cp.ConnectionName);

                if (state != ConnectionState.Open)
                    return Fail($"Failed to open datasource '{cp.ConnectionName}'. " +
                                 $"ConnectionState returned: {state}");

                context.DataSource = editor.GetDataSource(cp.ConnectionName);
            }

            Report(progress, 100, "Connection configured.");
            return Ok($"Connection '{cp.ConnectionName}' configured successfully.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void Report(IProgress<PassedArgs> progress, int pct, string msg) =>
            progress?.Report(new PassedArgs { ParameterInt1 = pct, Messege = msg });

        private static IErrorsInfo Ok(string msg = "Ok") =>
            new ErrorsInfo { Flag = Errors.Ok, Message = msg };

        private static IErrorsInfo Fail(string msg, Exception ex = null) =>
            new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };
    }
}
