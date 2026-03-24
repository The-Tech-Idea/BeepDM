using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Utilities;
using System.IO;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using System.ComponentModel;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Rules;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Editor.Migration;

namespace TheTechIdea.Beep.Editor.ETL
{
    /// <summary>
    /// Represents an Extract, Transform, Load (ETL) process.
    /// </summary>
    public class ETLEditor : IETL
    {
        /// <summary>
        /// Initializes a new instance of the ETL class.
        /// </summary>
        /// <param name="_DMEEditor">The DME editor to use for the ETL process.</param>
        public ETLEditor(IDMEEditor _DMEEditor)
        {
            DMEEditor = _DMEEditor;
          //  RulesEngine = new RulesEditor(DMEEditor);
        }
        /// <summary>
        /// Event that is raised when a process is passed.
        /// </summary>
        /// 
        public event EventHandler<PassedArgs> PassEvent;
        /// <summary>Gets or sets the DMEEditor instance.</summary>
        /// <value>The DMEEditor instance.</value>
        public IDMEEditor DMEEditor { get { return _DMEEditor; } set { _DMEEditor = value; } } //;RulesEditor = new RulesEditor(value);MoveValidator = new EntityDataMoveValidator(DMEEditor);
        /// <summary>Gets or sets the rules editor.</summary>
        /// <value>The rules editor.</value>
        public IRuleEngine RulesEngine { get; set; }
        /// <summary>Gets or sets the PassedArgs object.</summary>
        /// <value>The PassedArgs object.</value>
        public PassedArgs Passedargs { get; set; }
        /// <summary>Gets or sets the total script steps for the current ETL run.</summary>
        /// <value>Total number of script detail steps being executed.</value>
        public int ScriptCount { get; set; }
        /// <summary>Gets or sets the current script/record index for progress reporting.</summary>
        /// <value>Step index for step-level events and record index for legacy record loops.</value>
        public int CurrentScriptRecord { get; set; }
        /// <summary>Gets or sets the stop error count.</summary>
        /// <value>The stop error count.</value>
        /// <remarks>
        /// The stop error count determines the maximum number of errors allowed before a process is stopped.
        /// The default value is 10.
        /// </remarks>
        public decimal StopErrorCount { get; set; } = 10;
        /// <summary>Gets or sets the list of loaded data logs.</summary>
        /// <value>The list of loaded data logs.</value>
        public List<LoadDataLogResult> LoadDataLogs { get; set; } = new List<LoadDataLogResult>();
        /// <summary>Gets or sets the ETL script for HDR processing.</summary>
        /// <value>The ETL script for HDR processing.</value>
        public ETLScriptHDR Script { get; set; } = new ETLScriptHDR();

        #region "Local Variables"
        private bool stoprun = false;
        private IDMEEditor _DMEEditor;
        private int errorcount = 0;
        private List<DefaultValue> CurrrentDBDefaults = new List<DefaultValue>();
        private bool disposedValue;
        private readonly bool _enableImportingPreflightBridge = true;
        private readonly bool _useUnifiedCopyPipeline = true;
        private readonly bool _enableLegacyCopyFallback = true;
        private readonly bool _useImportingRunForImports = true;
        private readonly bool _enableLegacyImportFallback = true;
        private const int ImportBridgeDefaultBatchSize = 100;
        private const int ImportBridgeMaxBatchSize = 5000;
        private const int ImportBridgeDefaultMaxRetries = 3;
        private const int ImportBridgeMaxRetries = 10;
        private readonly bool _useMigrationSchemaBridge = true;
        private readonly bool _enableLegacySchemaFallback = true;
        private readonly bool _schemaCreateIfMissing = true;
        private readonly bool _schemaAlterIfNeeded = true;
        private readonly bool _schemaAllowDestructiveChanges = false;
        private string _currentRunCorrelationId;
        private DateTime _currentRunStartedAtUtc;
        private int _runStepsTotal;
        private int _runStepsSucceeded;
        private int _runStepsFailed;
        private int _runRecordsProcessed;
        private bool _runCancelled;
        private readonly bool _persistRunEvidence = true;
        private readonly bool _updateEvidenceSummaryReport = true;
        private readonly bool _verboseDiagnostics = false;
        private readonly int _minProgressIntervalMs = 250;
        private string _lastLoadLogLine;
        private DateTime _lastLoadLogAtUtc;
        private DateTime _lastProgressReportAtUtc;

        private sealed class EtlCopyExecutionOptions
        {
            public int BatchSize { get; set; } = 100;
            public bool EnableParallel { get; set; } = false;
            public int MaxRetries { get; set; } = 3;
            public Func<object, object> CustomTransformation { get; set; }
        }
        private sealed class EtlRunSummary
        {
            public string RunId { get; set; }
            public string Operation { get; set; }
            public DateTime StartedAtUtc { get; set; }
            public DateTime EndedAtUtc { get; set; }
            public int StepsTotal { get; set; }
            public int StepsSucceeded { get; set; }
            public int StepsFailed { get; set; }
            public int RecordsProcessed { get; set; }
            public bool Cancelled { get; set; }
            public TimeSpan Elapsed => EndedAtUtc - StartedAtUtc;
        }
        /// <summary>
        /// Progress semantics:
        /// ScriptCount is the total number of script steps in the current run.
        /// CurrentScriptRecord is the current step index for step-level updates, and
        /// may be reused by legacy record loops for record-level updates.
        /// </summary>
        private void BeginRunTelemetry(string operation, int totalSteps)
        {
            _currentRunCorrelationId = Guid.NewGuid().ToString("N");
            _currentRunStartedAtUtc = DateTime.UtcNow;
            _runStepsTotal = Math.Max(0, totalSteps);
            _runStepsSucceeded = 0;
            _runStepsFailed = 0;
            _runRecordsProcessed = 0;
            _runCancelled = false;
            if (Script != null)
            {
                Script.LastRunCorrelationId = _currentRunCorrelationId;
            }
            DMEEditor.AddLogMessage("ETL.Run", $"[{_currentRunCorrelationId}] {operation} started. steps={_runStepsTotal}", DateTime.Now, -1, "ETL", Errors.Ok);
        }
        private void CountStepOutcome(bool succeeded)
        {
            if (succeeded)
            {
                _runStepsSucceeded++;
            }
            else
            {
                _runStepsFailed++;
            }
        }
        private void ReportEtlProgress(IProgress<PassedArgs> progress, string eventType, string message)
        {
            if (progress == null)
            {
                return;
            }

            var nowUtc = DateTime.UtcNow;
            var isHighFrequencyEvent = string.Equals(eventType, "StepUpdate", StringComparison.InvariantCultureIgnoreCase) ||
                                       string.Equals(eventType, "Heartbeat", StringComparison.InvariantCultureIgnoreCase);
            if (!_verboseDiagnostics && isHighFrequencyEvent && (nowUtc - _lastProgressReportAtUtc).TotalMilliseconds < _minProgressIntervalMs)
            {
                return;
            }

            progress.Report(new PassedArgs
            {
                EventType = eventType,
                ParameterInt1 = CurrentScriptRecord,
                ParameterInt2 = ScriptCount,
                Messege = string.IsNullOrWhiteSpace(_currentRunCorrelationId) ? message : $"[{_currentRunCorrelationId}] {message}"
            });
            _lastProgressReportAtUtc = nowUtc;
        }
        private void AddLoadLogLine(string line, string stepId = null)
        {
            var renderedLine = string.IsNullOrWhiteSpace(_currentRunCorrelationId) ? line : $"[{_currentRunCorrelationId}] {line}";
            var nowUtc = DateTime.UtcNow;
            var isCritical = renderedLine?.IndexOf("failed", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                             renderedLine?.IndexOf("summary", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                             renderedLine?.IndexOf("stopped", StringComparison.InvariantCultureIgnoreCase) >= 0;
            if (!_verboseDiagnostics && !isCritical &&
                string.Equals(_lastLoadLogLine, renderedLine, StringComparison.InvariantCulture) &&
                (nowUtc - _lastLoadLogAtUtc).TotalMilliseconds < 1000)
            {
                return;
            }

            if (LoadDataLogs == null)
            {
                LoadDataLogs = new List<LoadDataLogResult>();
            }
            LoadDataLogs.Add(new LoadDataLogResult
            {
                RunID = _currentRunCorrelationId,
                StepID = stepId,
                Date = DateTime.Now,
                InputLine = renderedLine
            });
            _lastLoadLogLine = renderedLine;
            _lastLoadLogAtUtc = nowUtc;
        }
        private void EndRunTelemetry(string operation, bool cancelled)
        {
            _runCancelled = cancelled;
            var summary = new EtlRunSummary
            {
                RunId = _currentRunCorrelationId,
                Operation = operation,
                StartedAtUtc = _currentRunStartedAtUtc,
                EndedAtUtc = DateTime.UtcNow,
                StepsTotal = _runStepsTotal,
                StepsSucceeded = _runStepsSucceeded,
                StepsFailed = _runStepsFailed,
                RecordsProcessed = _runRecordsProcessed,
                Cancelled = _runCancelled
            };
            var summaryLine = $"Summary operation={summary.Operation} steps(total={summary.StepsTotal},ok={summary.StepsSucceeded},failed={summary.StepsFailed}) records={summary.RecordsProcessed} cancelled={summary.Cancelled} elapsedMs={(int)summary.Elapsed.TotalMilliseconds}";
            DMEEditor.AddLogMessage("ETL.Summary", $"[{summary.RunId}] {summaryLine}", DateTime.Now, -1, "ETL", summary.StepsFailed > 0 ? Errors.Warning : Errors.Ok);
            AddLoadLogLine(summaryLine);
            if (Script != null)
            {
                Script.LastRunDateTime = DateTime.Now;
                Script.LastRunCorrelationId = summary.RunId;
                Script.LastRunSummary = summaryLine;
            }
            PersistRunEvidence(summary, summaryLine);
        }
        private void PersistRunEvidence(EtlRunSummary summary, string summaryLine)
        {
            if (!_persistRunEvidence || summary == null || string.IsNullOrWhiteSpace(summary.RunId))
            {
                return;
            }

            try
            {
                var basePath = DMEEditor?.ConfigEditor?.ExePath;
                if (string.IsNullOrWhiteSpace(basePath))
                {
                    return;
                }

                var evidenceDir = Path.Combine(basePath, "Scripts", "ETL_Evidence");
                Directory.CreateDirectory(evidenceDir);
                var evidenceFile = Path.Combine(evidenceDir, $"{summary.RunId}.md");
                var sourceName = Script?.ScriptSource ?? "unknown";
                var destinationName = Script?.ScriptDestination ?? "unknown";

                var content =
                    "# ETL Run Evidence" + Environment.NewLine +
                    Environment.NewLine +
                    $"- RunId: `{summary.RunId}`" + Environment.NewLine +
                    $"- Operation: `{summary.Operation}`" + Environment.NewLine +
                    $"- Source: `{sourceName}`" + Environment.NewLine +
                    $"- Destination: `{destinationName}`" + Environment.NewLine +
                    $"- StartedAtUtc: `{summary.StartedAtUtc:O}`" + Environment.NewLine +
                    $"- EndedAtUtc: `{summary.EndedAtUtc:O}`" + Environment.NewLine +
                    $"- StepsTotal: `{summary.StepsTotal}`" + Environment.NewLine +
                    $"- StepsSucceeded: `{summary.StepsSucceeded}`" + Environment.NewLine +
                    $"- StepsFailed: `{summary.StepsFailed}`" + Environment.NewLine +
                    $"- RecordsProcessed: `{summary.RecordsProcessed}`" + Environment.NewLine +
                    $"- Cancelled: `{summary.Cancelled}`" + Environment.NewLine +
                    $"- Summary: `{summaryLine}`" + Environment.NewLine +
                    Environment.NewLine +
                    "## Log Snippet" + Environment.NewLine;

                var lastLogLines = LoadDataLogs == null
                    ? new List<LoadDataLogResult>()
                    : LoadDataLogs.TakeLast(20).ToList();

                if (lastLogLines.Count == 0)
                {
                    content += "- No log lines were captured." + Environment.NewLine;
                }
                else
                {
                    foreach (var line in lastLogLines)
                    {
                        content += $"- {line.InputLine}" + Environment.NewLine;
                    }
                }

                File.WriteAllText(evidenceFile, content);
                DMEEditor.AddLogMessage("ETL.Summary", $"[{summary.RunId}] Evidence persisted to '{evidenceFile}'.", DateTime.Now, -1, "ETL", Errors.Ok);
                UpdateEvidenceSummaryReport(summary, evidenceFile);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ETL.Summary", $"[{summary.RunId}] Failed to persist run evidence: {ex.Message}", DateTime.Now, -1, "ETL", Errors.Warning);
            }
        }
        private void UpdateEvidenceSummaryReport(EtlRunSummary summary, string evidenceFilePath)
        {
            if (!_updateEvidenceSummaryReport || summary == null)
            {
                return;
            }

            try
            {
                var evidenceDir = Path.GetDirectoryName(evidenceFilePath);
                if (string.IsNullOrWhiteSpace(evidenceDir))
                {
                    return;
                }

                var reportPath = Path.Combine(evidenceDir, "ETL_EVIDENCE_SUMMARY.md");
                var sourceName = Script?.ScriptSource ?? "unknown";
                var destinationName = Script?.ScriptDestination ?? "unknown";
                var outcome = summary.StepsFailed > 0 ? "WARN" : "OK";
                var relativeEvidencePath = Path.GetFileName(evidenceFilePath);
                var row = $"| {DateTime.Now:yyyy-MM-dd HH:mm:ss} | `{summary.RunId}` | `{summary.Operation}` | `{sourceName}` | `{destinationName}` | {summary.StepsTotal} | {summary.StepsSucceeded} | {summary.StepsFailed} | {summary.RecordsProcessed} | {summary.Cancelled} | {outcome} | `{relativeEvidencePath}` |";

                if (!File.Exists(reportPath))
                {
                    var header =
                        "# ETL Evidence Summary" + Environment.NewLine +
                        Environment.NewLine +
                        "Rolling summary of ETL validation evidence runs." + Environment.NewLine +
                        Environment.NewLine +
                        "| Timestamp | RunId | Operation | Source | Destination | StepsTotal | StepsOk | StepsFailed | Records | Cancelled | Outcome | EvidenceFile |" + Environment.NewLine +
                        "|---|---|---|---|---|---:|---:|---:|---:|---|---|---|" + Environment.NewLine;
                    File.WriteAllText(reportPath, header + row + Environment.NewLine);
                }
                else
                {
                    File.AppendAllText(reportPath, row + Environment.NewLine);
                }

                GenerateCurrentWeekEvidenceReport(reportPath);
                GenerateCurrentMonthEvidenceReport(reportPath);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ETL.Summary", $"[{summary.RunId}] Failed to update evidence summary report: {ex.Message}", DateTime.Now, -1, "ETL", Errors.Warning);
            }
        }
        private void GenerateCurrentWeekEvidenceReport(string summaryReportPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(summaryReportPath) || !File.Exists(summaryReportPath))
                {
                    return;
                }

                var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                var lines = File.ReadAllLines(summaryReportPath);
                var weeklyRows = new List<string>();

                foreach (var line in lines)
                {
                    if (!line.StartsWith("| ") || line.StartsWith("|---"))
                    {
                        continue;
                    }

                    var cols = line.Split('|');
                    if (cols.Length < 3)
                    {
                        continue;
                    }

                    var timestampText = cols[1].Trim();
                    if (DateTime.TryParse(timestampText, out var timestamp) && timestamp.Date >= weekStart)
                    {
                        weeklyRows.Add(line);
                    }
                }

                var weeklyPath = Path.Combine(Path.GetDirectoryName(summaryReportPath) ?? string.Empty, "ETL_EVIDENCE_CURRENT_WEEK.md");
                var weeklyHeader =
                    "# ETL Evidence Current Week" + Environment.NewLine +
                    Environment.NewLine +
                    $"Week start: `{weekStart:yyyy-MM-dd}`" + Environment.NewLine +
                    Environment.NewLine +
                    "| Timestamp | RunId | Operation | Source | Destination | StepsTotal | StepsOk | StepsFailed | Records | Cancelled | Outcome | EvidenceFile |" + Environment.NewLine +
                    "|---|---|---|---|---|---:|---:|---:|---:|---|---|---|" + Environment.NewLine;

                File.WriteAllText(weeklyPath, weeklyHeader + string.Join(Environment.NewLine, weeklyRows) + Environment.NewLine);
            }
            catch
            {
                // Keep weekly report generation best-effort only.
            }
        }
        private void GenerateCurrentMonthEvidenceReport(string summaryReportPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(summaryReportPath) || !File.Exists(summaryReportPath))
                {
                    return;
                }

                var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var lines = File.ReadAllLines(summaryReportPath);
                var monthlyRows = new List<string>();

                foreach (var line in lines)
                {
                    if (!line.StartsWith("| ") || line.StartsWith("|---"))
                    {
                        continue;
                    }

                    var cols = line.Split('|');
                    if (cols.Length < 3)
                    {
                        continue;
                    }

                    var timestampText = cols[1].Trim();
                    if (DateTime.TryParse(timestampText, out var timestamp) && timestamp.Date >= monthStart)
                    {
                        monthlyRows.Add(line);
                    }
                }

                var monthlyPath = Path.Combine(Path.GetDirectoryName(summaryReportPath) ?? string.Empty, "ETL_EVIDENCE_CURRENT_MONTH.md");
                var monthlyHeader =
                    "# ETL Evidence Current Month" + Environment.NewLine +
                    Environment.NewLine +
                    $"Month start: `{monthStart:yyyy-MM-dd}`" + Environment.NewLine +
                    Environment.NewLine +
                    "| Timestamp | RunId | Operation | Source | Destination | StepsTotal | StepsOk | StepsFailed | Records | Cancelled | Outcome | EvidenceFile |" + Environment.NewLine +
                    "|---|---|---|---|---|---:|---:|---:|---:|---|---|---|" + Environment.NewLine;

                File.WriteAllText(monthlyPath, monthlyHeader + string.Join(Environment.NewLine, monthlyRows) + Environment.NewLine);
            }
            catch
            {
                // Keep monthly report generation best-effort only.
            }
        }
        private IList<object> NormalizeSourceRows(object sourceData, EntityStructure sourceEntity)
        {
            if (sourceData == null)
            {
                return new List<object>();
            }

            if (sourceData is DataTable table)
            {
                DMTypeBuilder.CreateNewObject(DMEEditor, null, sourceEntity.EntityName, sourceEntity.Fields);
                return DMEEditor.Utilfunction.GetListByDataTable(table, DMTypeBuilder.MyType, sourceEntity);
            }

            if (sourceData is IBindingListView listView)
            {
                var rows = new List<object>();
                foreach (var row in listView)
                {
                    rows.Add(row);
                }
                return rows;
            }

            if (sourceData is IEnumerable<object> typedEnumerable)
            {
                return typedEnumerable as IList<object> ?? typedEnumerable.ToList();
            }

            if (sourceData is System.Collections.IEnumerable nonGenericEnumerable)
            {
                var rows = new List<object>();
                foreach (var row in nonGenericEnumerable)
                {
                    rows.Add(row);
                }
                return rows;
            }

            return new List<object> { sourceData };
        }
        #endregion
        #region "Create Scripts"
        /// <summary>Creates the header of an ETL script.</summary>
        /// <param name="Srcds">The data source object.</param>
        /// <param name="progress">The progress object to report progress.</param>
        /// <param name="token">The cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when Srcds is null.</exception>
        public void CreateScriptHeader(IDataSource Srcds, IProgress<PassedArgs> progress, CancellationToken token)
        {
            int i = 0;
            Script = new ETLScriptHDR();
            Script.ScriptSource = Srcds.DatasourceName;
            List<EntityStructure> ls = new List<EntityStructure>();
            Srcds.GetEntitesList();
            foreach (string item in Srcds.EntitiesNames)
            {
                ls.Add(Srcds.GetEntityStructure(item, true));
            }
            Script.ScriptDetails = DMEEditor.ETL.GetCreateEntityScript(Srcds, ls, progress, token);
            foreach (var item in ls)
            {

                ETLScriptDet upscript = new ETLScriptDet();
                upscript.SourceDataSourceName = item.DataSourceID;
                upscript.SourceEntityName = item.EntityName;
                upscript.SourceDataSourceEntityName = item.EntityName;
                upscript.DestinationDataSourceEntityName = item.EntityName;
                upscript.DestinationEntityName = item.EntityName;
                upscript.DestinationDataSourceName = Srcds.DatasourceName;
                upscript.ScriptType = DDLScriptType.CopyData;
                Script.ScriptDetails.Add(upscript);
                i += 1;
            }
        }
        /// <summary>Generates a list of ETL script details for creating entities from a data source.</summary>
        /// <param name="ds">The data source to retrieve entities from.</param>
        /// <param name="entities">The list of entities to create scripts for.</param>
        /// <param name="progress">An object to report progress during the script generation.</param>
        /// <param name="token">A cancellation token to cancel the script generation.</param>
        /// <returns>A list of ETL script details for creating entities.</returns>
        /// <remarks>If an error occurs during the process, a log message will be added and an empty list will be returned.</remarks>
        public List<ETLScriptDet> GetCreateEntityScript(IDataSource ds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();

            try
            {
                List<EntityStructure> ls = new List<EntityStructure>();
                foreach (string item in entities)
                {
                    EntityStructure t1 = ds.GetEntityStructure(item, true); ;// t.Result;
                    ls.Add(t1);
                }
                rt.AddRange(GetCreateEntityScript(ds, ls, progress, token,copydata));

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            // Script.ScriptDetails.AddRange(rt);
            return rt;
        }
        /// <summary>Generates an ETL script detail object based on the provided parameters.</summary>
        /// <param name="item">The entity structure object representing the source entity.</param>
        /// <param name="destSource">The name of the destination data source.</param>
        /// <param name="scriptType">The type of Ddl script.</param>
        /// <returns>An ETLScriptDet object representing the generated script.</returns>
        private ETLScriptDet GenerateScript(EntityStructure item, string destSource, DDLScriptType scriptType)
        {
            ETLScriptDet upscript = new ETLScriptDet();
            upscript.SourceDataSourceName = item.SourceDataSourceID;
            upscript.SourceEntityName = item.EntityName;
            upscript.SourceDataSourceEntityName = string.IsNullOrEmpty(item.DatasourceEntityName)? item.EntityName:item.DatasourceEntityName ;
            upscript.DestinationDataSourceEntityName = string.IsNullOrEmpty(item.DatasourceEntityName) ? item.EntityName : item.DatasourceEntityName;
            upscript.DestinationEntityName = item.EntityName;
            upscript.DestinationDataSourceName = destSource;
            upscript.SourceEntity = item;
            upscript.ScriptType = scriptType;
            return upscript;
        }
        /// <summary>Generates a list of ETL script details for creating entities.</summary>
        /// <param name="Dest">The destination data source.</param>
        /// <param name="entities">The list of entity structures.</param>
        /// <param name="progress">An object for reporting progress.</param>
        /// <param name="token">A cancellation token to cancel the operation.</param>
        /// <returns>A list of ETL script details for creating entities.</returns>
        /// <remarks>
        /// This method generates ETL script details for creating entities based on the provided destination data source and entity structures.
        /// It reports progress using the provided progress object and can be cancelled using the cancellation token.
        /// </remarks>
        public List<ETLScriptDet> GetCreateEntityScript(IDataSource Dest, List<EntityStructure> entities, IProgress<PassedArgs> progress, CancellationToken token,bool copydata=false)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            int i = 0;
            List<ETLScriptDet> retval = new List<ETLScriptDet>();

            try
            {
                //rt = Dest.GetCreateEntityScript(entities);
                foreach (EntityStructure item in entities)
                {
                    ETLScriptDet copyscript = GenerateScript(item, Dest.DatasourceName, DDLScriptType.CreateEntity);
                    copyscript.Id = i;
                    copyscript.CopyData = copydata;
                    copyscript.IsCreated = false;
                    copyscript.IsModified = false;
                    copyscript.IsDataCopied = false;
                    copyscript.Failed = false;
                    copyscript.ErrorMessage = "";
                  
                    copyscript.Active = true;
                    copyscript.Mapping = new EntityDataMap_DTL();
                    copyscript.Tracking = new List<SyncErrorsandTracking>();

                    retval.Add(copyscript);
                    i++;
                }

                DMEEditor.AddLogMessage("Success", $"Generated Script", DateTime.Now, 0, null, Errors.Ok);

            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return retval;

        }
        /// <summary>Generates a script for copying data entities.</summary>
        /// <param name="Dest">The destination data source.</param>
        /// <param name="entities">The list of entity structures.</param>
        /// <param name="progress">An object to report progress.</param>
        /// <param name="token">A cancellation token to cancel the operation.</param>
        /// <returns>A list of ETLScriptDet objects representing the generated script.</returns>
        public List<ETLScriptDet> GetCopyDataEntityScript(IDataSource Dest, List<EntityStructure> entities, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            int i = 0;

            List<ETLScriptDet> retval = new List<ETLScriptDet>();

            try
            {
                // Generate Create Table First
                foreach (EntityStructure sc in entities)
                {
                    ETLScriptDet copyscript = GenerateScript(sc, Dest.DatasourceName, DDLScriptType.CopyData);
                    copyscript.Id = i;
                    i++;
                    //Script.ScriptDetails.Add(copyscript);
                    retval.Add(copyscript);
                }
                i += 1;
                DMEEditor.AddLogMessage("Success", $"Generated Script", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return retval;

        }
        #endregion "Create Scripts"
        #region "Copy Data"
        /// <summary>Copies the structure of specified entities from a source data source to a destination data source.</summary>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="entities">A list of entity names to copy.</param>
        /// <param name="progress">An object to report progress during the copy operation.</param>
        /// <param name="token">A cancellation token to cancel the copy operation.</param>
        /// <param name="CreateMissingEntity">A flag indicating whether to create missing entities in the destination data source.</param>
        /// <returns>An object containing information about any errors that occurred during the copy operation
        public IErrorsInfo CopyEntitiesStructure(IDataSource sourceds, IDataSource destds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true)
        {
            try
            {
                var ls = from e in sourceds.Entities
                         from r in entities
                         where e.EntityName == r
                         select e;
                string entname = "";
                foreach (EntityStructure item in ls)
                {
                    CopyEntityStructure(sourceds, destds, item.EntityName, item.EntityName, progress, token, CreateMissingEntity);
                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Copies the structure of an entity from a source data source to a destination data source.</summary>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="srcentity">The name of the source entity.</param>
        /// <param name="destentity">The name of the destination entity.</param>
        /// <param name="progress">An object to report progress during the copy operation.</param>
        /// <param name="token">A cancellation token to cancel the copy operation.</param>
        /// <param name="CreateMissingEntity">A flag indicating whether to create the destination entity if it doesn't exist
        public IErrorsInfo CopyEntityStructure(IDataSource sourceds, IDataSource destds, string srcentity, string destentity, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true)
        {
            try
            {
                EntityStructure item = sourceds.GetEntityStructure(srcentity, true);
                if (item != null)
                {
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {
                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.DisableFKConstraints(item);
                    }
                    if (destds.CreateEntityAs(item))
                    {
                        DMEEditor.AddLogMessage("Success", $"Creating Entity  {item.EntityName} on {destds.DatasourceName}", DateTime.Now, 0, null, Errors.Ok);
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Fail", $"Error : Could not Create  Entity {item.EntityName} on {destds.DatasourceName}", DateTime.Now, 0, null, Errors.Failed);
                    }
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {

                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.EnableFKConstraints(item);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error Could not Create  Entity {srcentity} on {destds.DatasourceName} ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Copies data from a source data source to a destination data source.</summary>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="progress">An object to report progress during the copy operation.</param>
        /// <param name="token">A cancellation token to cancel the copy operation.</param>
        /// <param name="CreateMissingEntity">A flag indicating whether to create missing entities in the destination data source.</param>
        /// <param name="map_DTL">An optional mapping object to map entity data between the source and destination data sources.</param>
        /// <returns>An object containing information about any errors
        public IErrorsInfo CopyDatasourceData(IDataSource sourceds, IDataSource destds, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null)
        {
            try
            {
                foreach (EntityStructure item in sourceds.Entities)
                {
                    CopyEntityData(sourceds, destds, item.EntityName, item.EntityName, progress, token, CreateMissingEntity);
                }

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Copies data from source data source to destination data source for specified entities.</summary>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="entities">The list of entities to copy.</param>
        /// <param name="progress">The progress object to report progress.</param>
        /// <param name="token">The cancellation token to cancel the operation.</param>
        /// <param name="CreateMissingEntity">Flag indicating whether to create missing entities in the destination data source.</param>
        /// <param name="map_DTL">The mapping object for entity data transfer.</param>
        /// <returns>An object containing
        public IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<string> entities, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null)
        {
            try
            {
                var ls = from e in sourceds.Entities
                         from r in entities
                         where e.EntityName == r
                         select e;

                foreach (EntityStructure item in ls)
                {
                    if (item.EntityName != item.DatasourceEntityName && !string.IsNullOrEmpty(item.DatasourceEntityName))
                    {
                        CopyEntityData(sourceds, destds, item.DatasourceEntityName, item.EntityName, progress, token, CreateMissingEntity);
                    }
                    else
                        CopyEntityData(sourceds, destds, item.EntityName, item.EntityName, progress, token, CreateMissingEntity);

                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Copies entity data from a source data source to a destination data source.</summary>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="srcentity">The name of the source entity.</param>
        /// <param name="destentity">The name of the destination entity.</param>
        /// <param name="progress">An object to report progress during the copy operation.</param>
        /// <param name="token">A cancellation token to cancel the copy operation.</param>
        /// <param name="CreateMissingEntity">A flag indicating whether to create the destination entity if it doesn't exist.</param>
        public IErrorsInfo CopyEntityData(IDataSource sourceds, IDataSource destds, string srcentity, string destentity, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null)
        {
            try
            {
                errorcount = 0;
                EntityStructure item = sourceds.GetEntityStructure(srcentity, true);
                if (item != null)
                {
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {
                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.DisableFKConstraints(item);
                    }
                    if (destds.CheckEntityExist(destentity))
                    {
                        var srcTb = sourceds.GetEntity(item.EntityName, null);
                        var srcList = NormalizeSourceRows(srcTb, item);
                        if (srcList.Count > 0)
                        {
                            ScriptCount += srcList.Count();
                            var lastProgressReport = 0;
                            foreach (var r in srcList)
                            {
                                CurrentScriptRecord += 1;
                                // DMEEditor.ErrorObject=destds.InsertEntity(item.EntityName, r);
                                InsertEntity(destds, item, item.EntityName, null, r, progress, token);
                                token.ThrowIfCancellationRequested();
                                if (progress != null && (CurrentScriptRecord - lastProgressReport >= 100 || CurrentScriptRecord == ScriptCount))
                                {
                                    PassedArgs ps = new PassedArgs { ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, Messege = DMEEditor.ErrorObject.Message };
                                    progress.Report(ps);
                                    lastProgressReport = CurrentScriptRecord;
                                }

                            }
                        }
                        if (progress != null)
                        {
                            PassedArgs ps = new PassedArgs { ParameterString1 = $"Ended Copying Data from {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount };
                            progress.Report(ps);

                        }
                    }
                    else
                    {
                        DMEEditor.AddLogMessage("Copy Data", $"Error Could not Copy Entity Date {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ", DateTime.Now, 0, null, Errors.Failed);
                    }
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {

                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.EnableFKConstraints(item);
                    }
                }
                else
                    DMEEditor.AddLogMessage("Copy Data", $"Error Could not Find Entity  {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ", DateTime.Now, 0, null, Errors.Failed);

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error copying Data {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Copies data from source to destination entities based on provided scripts.</summary>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="scripts">The list of ETL scripts.</param>
        /// <param name="progress">The progress object to report progress.</param>
        /// <param name="token">The cancellation token to cancel the operation.</param>
        /// <param name="CreateMissingEntity">Flag indicating whether to create missing entities.</param>
        /// <param name="map_DTL">The entity data map for data transformation and mapping.</param>
        /// <returns>An object containing information about any errors that
        public IErrorsInfo CopyEntitiesData(IDataSource sourceds, IDataSource destds, List<ETLScriptDet> scripts, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null)
        {
            try
            {
                string srcentityname = "";
                foreach (ETLScriptDet s in scripts.Where(i => i.ScriptType == DDLScriptType.CopyData))
                {
                    if (s.SourceEntityName != s.SourceDataSourceEntityName && !string.IsNullOrEmpty(s.SourceDataSourceEntityName))
                    {
                        srcentityname = s.SourceDataSourceEntityName;
                    }
                    else
                        srcentityname = s.SourceEntityName;
                    CopyEntityData(sourceds, destds, srcentityname, s.SourceEntityName, progress, token, CreateMissingEntity);
                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error copying Data ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        #endregion "Copy Data"
        #region "Run Scripts"
        /// <summary>Runs a child script asynchronously.</summary>
        /// <param name="ParentScript">The parent script.</param>
        /// <param name="srcds">The data source for the source.</param>
        /// <param name="destds">The data source for the destination.</param>
        /// <param name="progress">The progress object to report progress.</param>
        /// <param name="token">The cancellation token to cancel the operation.</param>
        /// <returns>An object containing information about any errors that occurred during the execution of the child script.</returns>
        public async Task<IErrorsInfo> RunChildScriptAsync(ETLScriptDet ParentScript, IDataSource srcds, IDataSource destds, IProgress<PassedArgs> progress, CancellationToken token)
        {

            if (ParentScript.CopyDataScripts.Count > 0)
            {
                for (int i = 0; i < ParentScript.CopyDataScripts.Count; i++)
                {
                    ETLScriptDet sc = ParentScript.CopyDataScripts[i];
                    destds = DMEEditor.GetDataSource(sc.DestinationDataSourceName);
                    srcds = DMEEditor.GetDataSource(sc.SourceDataSourceName);
                    if (destds != null && srcds != null)
                    {
                        DMEEditor.OpenDataSource(sc.DestinationDataSourceName);
                        DMEEditor.OpenDataSource(sc.SourceDataSourceName);
                        if (destds.ConnectionStatus == ConnectionState.Open)
                        {
                            if (sc.ScriptType == DDLScriptType.CopyData)
                            {
                                SendMessege(progress, token, null, sc, $"Started Coping Data for Entity  {sc.DestinationEntityName}  in {sc.DestinationDataSourceName}");
                                DMEEditor.ErrorObject = await ExecuteCopyStepAsync(sc, srcds, destds, progress, token);
                                if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                                {
                                    SendMessege(progress, token, null, sc, $"Error in Coping Data for Entity  {sc.DestinationEntityName}");
                                }
                                else
                                {
                                    SendMessege(progress, token, null, sc, $"Finished Coping Data for Entity  {sc.DestinationEntityName}");
                                }
                            }
                        }
                        else
                        {
                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                            DMEEditor.ErrorObject.Message = $" Could not Connect to on the Data Dource  {sc.SourceDataSourceName}";
                            errorcount = (int)StopErrorCount;
                            SendMessege(progress, token, null, sc);
                        }
                    }
                }
            }
            return DMEEditor.ErrorObject;
        }
        private IErrorsInfo FailPreflight(string message, IProgress<PassedArgs> progress = null)
        {
            DMEEditor.ErrorObject.Flag = Errors.Failed;
            DMEEditor.ErrorObject.Message = message;
            DMEEditor.AddLogMessage("ETL.Preflight", message, DateTime.Now, -1, "ETL", Errors.Failed);
            LoadDataLogs.Add(new LoadDataLogResult { InputLine = $"Preflight failed: {message}" });
            progress?.Report(new PassedArgs { EventType = "PreflightFailed", Messege = message });
            return DMEEditor.ErrorObject;
        }
        private IErrorsInfo PassPreflight(string message, IProgress<PassedArgs> progress = null)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            DMEEditor.ErrorObject.Message = message;
            DMEEditor.AddLogMessage("ETL.Preflight", message, DateTime.Now, -1, "ETL", Errors.Ok);
            LoadDataLogs.Add(new LoadDataLogResult { InputLine = $"Preflight passed: {message}" });
            progress?.Report(new PassedArgs { EventType = "PreflightPassed", Messege = message });
            return DMEEditor.ErrorObject;
        }
        private string BuildValidationErrorsMessage(IErrorsInfo validationResult)
        {
            if (validationResult == null)
            {
                return "Unknown validation error.";
            }
            if (validationResult.Errors == null || validationResult.Errors.Count == 0)
            {
                return validationResult.Message;
            }
            var details = string.Join("; ", validationResult.Errors
                .Where(e => !string.IsNullOrWhiteSpace(e.Message))
                .Select(e => e.Message));
            return string.IsNullOrWhiteSpace(details) ? validationResult.Message : details;
        }
        private async Task<IErrorsInfo> TryRunImportingPreflightAsync(ETLScriptDet step, IProgress<PassedArgs> progress, CancellationToken token)
        {
            if (!_enableImportingPreflightBridge || step == null)
            {
                return DMEEditor.ErrorObject;
            }

            try
            {
                token.ThrowIfCancellationRequested();
                using var importManager = new DataImportManager(DMEEditor);
                var config = importManager.CreateImportConfiguration(
                    step.SourceDataSourceEntityName ?? step.SourceEntityName,
                    step.SourceDataSourceName,
                    step.DestinationEntityName,
                    step.DestinationDataSourceName);
                config.SourceEntityName = step.SourceDataSourceEntityName ?? step.SourceEntityName;
                config.DestEntityName = step.DestinationEntityName;
                config.SourceDataSourceName = step.SourceDataSourceName;
                config.DestDataSourceName = step.DestinationDataSourceName;
                config.CreateDestinationIfNotExists = true;
                config.AddMissingColumns = true;

                var preflight = await importManager.RunMigrationPreflightAsync(
                    config,
                    msg =>
                    {
                        LoadDataLogs.Add(new LoadDataLogResult { InputLine = $"Importing preflight: {msg}" });
                        progress?.Report(new PassedArgs { EventType = "PreflightInfo", Messege = msg });
                    });

                if (preflight?.Flag == Errors.Failed)
                {
                    return FailPreflight($"Importing preflight failed for entity '{step.DestinationEntityName}': {preflight.Message}", progress);
                }
            }
            catch (OperationCanceledException)
            {
                return FailPreflight("Preflight cancelled.", progress);
            }
            catch (Exception ex)
            {
                return FailPreflight($"Importing preflight exception: {ex.Message}", progress);
            }

            return DMEEditor.ErrorObject;
        }
        private async Task<IErrorsInfo> PreflightCreateScriptAsync(IProgress<PassedArgs> progress, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (Script == null)
                {
                    return FailPreflight("Script is null.", progress);
                }
                if (Script.ScriptDetails == null || Script.ScriptDetails.Count == 0)
                {
                    return FailPreflight("Script details are missing.", progress);
                }

                var validator = new ETLValidator(DMEEditor);
                var copySteps = Script.ScriptDetails
                    .Where(s => s != null && s.ScriptType == DDLScriptType.CopyData && s.Active)
                    .ToList();

                foreach (var step in copySteps)
                {
                    token.ThrowIfCancellationRequested();
                    if (string.IsNullOrWhiteSpace(step.SourceDataSourceName) || string.IsNullOrWhiteSpace(step.DestinationDataSourceName))
                    {
                        return FailPreflight("A CopyData step is missing source or destination datasource name.", progress);
                    }
                    if (string.IsNullOrWhiteSpace(step.SourceEntityName) && string.IsNullOrWhiteSpace(step.SourceDataSourceEntityName))
                    {
                        return FailPreflight($"CopyData step '{step.Id}' is missing source entity name.", progress);
                    }
                    if (string.IsNullOrWhiteSpace(step.DestinationEntityName))
                    {
                        return FailPreflight($"CopyData step '{step.Id}' is missing destination entity name.", progress);
                    }

                    var srcEntity = string.IsNullOrWhiteSpace(step.SourceDataSourceEntityName) ? step.SourceEntityName : step.SourceDataSourceEntityName;
                    var srcds = DMEEditor.GetDataSource(step.SourceDataSourceName);
                    var destds = DMEEditor.GetDataSource(step.DestinationDataSourceName);

                    if (srcds == null || destds == null)
                    {
                        return FailPreflight($"Could not resolve source/destination datasource for step '{step.Id}'.", progress);
                    }

                    DMEEditor.OpenDataSource(step.SourceDataSourceName);
                    DMEEditor.OpenDataSource(step.DestinationDataSourceName);
                    var consistency = validator.ValidateEntityConsistency(srcds, destds, srcEntity, step.DestinationEntityName);
                    if (consistency.Flag == Errors.Failed)
                    {
                        return FailPreflight(
                            $"Entity consistency validation failed for '{srcEntity}' -> '{step.DestinationEntityName}': {BuildValidationErrorsMessage(consistency)}",
                            progress);
                    }
                }

                if (copySteps.Count > 0)
                {
                    var importingPreflight = await TryRunImportingPreflightAsync(copySteps.First(), progress, token);
                    if (importingPreflight.Flag == Errors.Failed)
                    {
                        return importingPreflight;
                    }
                }

                return PassPreflight("Create-script preflight completed successfully.", progress);
            }
            catch (OperationCanceledException)
            {
                return FailPreflight("Preflight cancelled.", progress);
            }
            catch (Exception ex)
            {
                return FailPreflight($"Create-script preflight exception: {ex.Message}", progress);
            }
        }
        private async Task<IErrorsInfo> PreflightImportScriptAsync(IProgress<PassedArgs> progress, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (Script?.ScriptDetails == null || Script.ScriptDetails.Count == 0)
                {
                    return FailPreflight("Import script details are missing.", progress);
                }

                var step = Script.ScriptDetails.FirstOrDefault(s => s != null && s.Active);
                if (step == null)
                {
                    return FailPreflight("No active import step found.", progress);
                }

                if (step.Mapping == null)
                {
                    return FailPreflight("Import step mapping is missing.", progress);
                }

                var validator = new ETLValidator(DMEEditor);
                var mappedEntityValidation = validator.ValidateMappedEntity(step.Mapping);
                if (mappedEntityValidation.Flag == Errors.Failed)
                {
                    return FailPreflight($"Mapped entity validation failed: {BuildValidationErrorsMessage(mappedEntityValidation)}", progress);
                }

                var map = new EntityDataMap
                {
                    EntityName = step.DestinationEntityName,
                    EntityDataSource = step.DestinationDataSourceName,
                    EntityFields = step.Mapping.EntityFields ?? new List<EntityField>(),
                    MappedEntities = new List<EntityDataMap_DTL> { step.Mapping }
                };
                var mapValidation = validator.ValidateEntityMapping(map);
                if (mapValidation.Flag == Errors.Failed)
                {
                    return FailPreflight($"Mapping validation failed: {BuildValidationErrorsMessage(mapValidation)}", progress);
                }

                var srcEntity = string.IsNullOrWhiteSpace(step.SourceDataSourceEntityName) ? step.SourceEntityName : step.SourceDataSourceEntityName;
                var srcds = DMEEditor.GetDataSource(step.SourceDataSourceName);
                var destds = DMEEditor.GetDataSource(step.DestinationDataSourceName);
                if (srcds == null || destds == null)
                {
                    return FailPreflight("Could not resolve source/destination datasource for import preflight.", progress);
                }

                DMEEditor.OpenDataSource(step.SourceDataSourceName);
                DMEEditor.OpenDataSource(step.DestinationDataSourceName);
                var consistency = validator.ValidateEntityConsistency(srcds, destds, srcEntity, step.DestinationEntityName);
                if (consistency.Flag == Errors.Failed)
                {
                    return FailPreflight($"Import entity consistency validation failed: {BuildValidationErrorsMessage(consistency)}", progress);
                }

                var importingPreflight = await TryRunImportingPreflightAsync(step, progress, token);
                if (importingPreflight.Flag == Errors.Failed)
                {
                    return importingPreflight;
                }

                return PassPreflight("Import-script preflight completed successfully.", progress);
            }
            catch (OperationCanceledException)
            {
                return FailPreflight("Import preflight cancelled.", progress);
            }
            catch (Exception ex)
            {
                return FailPreflight($"Import preflight exception: {ex.Message}", progress);
            }
        }
        private async Task<IErrorsInfo> ExecuteCopyStepAsync(
            ETLScriptDet step,
            IDataSource sourceDs,
            IDataSource destDs,
            IProgress<PassedArgs> progress,
            CancellationToken token,
            EtlCopyExecutionOptions options = null)
        {
            options ??= new EtlCopyExecutionOptions();
            if (!_useUnifiedCopyPipeline)
            {
                return RunCopyEntityScript(
                    step,
                    sourceDs,
                    destDs,
                    string.IsNullOrWhiteSpace(step.SourceDataSourceEntityName) ? step.SourceEntityName : step.SourceDataSourceEntityName,
                    step.DestinationEntityName,
                    progress,
                    token,
                    true,
                    step.Mapping);
            }

            try
            {
                var srcEntity = string.IsNullOrWhiteSpace(step.SourceDataSourceEntityName) ? step.SourceEntityName : step.SourceDataSourceEntityName;
                var copier = new ETLDataCopier(DMEEditor);
                var copyResult = await copier.CopyEntityDataAsync(
                    sourceDs,
                    destDs,
                    srcEntity,
                    step.DestinationEntityName,
                    progress,
                    token,
                    step.Mapping,
                    options.CustomTransformation,
                    options.BatchSize,
                    options.EnableParallel,
                    options.MaxRetries);

                if (copyResult.Flag == Errors.Failed && _enableLegacyCopyFallback)
                {
                    DMEEditor.AddLogMessage("ETL.Copy", $"Unified pipeline failed; using legacy fallback for {step.DestinationEntityName}.", DateTime.Now, -1, "ETL", Errors.Warning);
                    return RunCopyEntityScript(
                        step,
                        sourceDs,
                        destDs,
                        srcEntity,
                        step.DestinationEntityName,
                        progress,
                        token,
                        true,
                        step.Mapping);
                }

                return copyResult;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ETL.Copy", $"Unified copy execution exception for {step?.DestinationEntityName}: {ex.Message}", DateTime.Now, -1, "ETL", Errors.Failed);
                if (_enableLegacyCopyFallback && step != null && sourceDs != null && destDs != null)
                {
                    var srcEntity = string.IsNullOrWhiteSpace(step.SourceDataSourceEntityName) ? step.SourceEntityName : step.SourceDataSourceEntityName;
                    return RunCopyEntityScript(
                        step,
                        sourceDs,
                        destDs,
                        srcEntity,
                        step.DestinationEntityName,
                        progress,
                        token,
                        true,
                        step.Mapping);
                }
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
                return DMEEditor.ErrorObject;
            }
        }
        private EntityDataMap BuildEntityMapFromScriptStep(ETLScriptDet step)
        {
            var mapping = new EntityDataMap
            {
                EntityName = step.DestinationEntityName,
                EntityDataSource = step.DestinationDataSourceName,
                EntityFields = step.Mapping?.EntityFields ?? new List<EntityField>(),
                MappedEntities = new List<EntityDataMap_DTL>()
            };

            if (step.Mapping != null)
            {
                mapping.MappedEntities.Add(step.Mapping);
            }

            return mapping;
        }
        private DataImportConfiguration BuildImportConfigurationFromEtlScript(
            ETLScriptHDR script,
            ETLScriptDet detail,
            IProgress<PassedArgs> progress,
            CancellationToken token)
        {
            if (script == null)
            {
                throw new InvalidOperationException("ETL script header is not available.");
            }
            if (detail == null)
            {
                throw new InvalidOperationException("ETL script detail is not available.");
            }

            var sourceDs = DMEEditor.GetDataSource(detail.SourceDataSourceName);
            var destDs = DMEEditor.GetDataSource(detail.DestinationDataSourceName);
            if (sourceDs == null || destDs == null)
            {
                throw new InvalidOperationException($"Unable to resolve source/destination datasource for '{detail.DestinationEntityName}'.");
            }

            using var importManager = new DataImportManager(DMEEditor);
            var srcEntity = string.IsNullOrWhiteSpace(detail.SourceDataSourceEntityName) ? detail.SourceEntityName : detail.SourceDataSourceEntityName;
            var config = importManager.CreateImportConfiguration(
                srcEntity,
                detail.SourceDataSourceName,
                detail.DestinationEntityName,
                detail.DestinationDataSourceName);

            config.SourceEntityName = srcEntity;
            config.DestEntityName = detail.DestinationEntityName;
            config.SourceDataSourceName = detail.SourceDataSourceName;
            config.DestDataSourceName = detail.DestinationDataSourceName;
            config.SourceData = sourceDs;
            config.DestData = destDs;
            config.CreateDestinationIfNotExists = true;
            config.AddMissingColumns = true;
            config.RunMigrationPreflight = _enableImportingPreflightBridge;
            config.OnBatchError = BatchErrorStrategy.Retry;
            config.BatchSize = Math.Min(ImportBridgeMaxBatchSize, Math.Max(1, ImportBridgeDefaultBatchSize));
            config.MaxRetries = Math.Min(ImportBridgeMaxRetries, Math.Max(1, ImportBridgeDefaultMaxRetries));
            config.Mapping = BuildEntityMapFromScriptStep(detail);
            if (detail.FilterConditions != null && detail.FilterConditions.Count > 0)
            {
                config.SourceFilters = detail.FilterConditions;
            }
            if (detail.Mapping?.SelectedDestFields != null && detail.Mapping.SelectedDestFields.Count > 0)
            {
                config.SelectedFields = detail.Mapping.SelectedDestFields.Select(f => f.FieldName).ToList();
            }

            if (string.IsNullOrWhiteSpace(config.SourceDataSourceName) ||
                string.IsNullOrWhiteSpace(config.DestDataSourceName) ||
                string.IsNullOrWhiteSpace(config.SourceEntityName) ||
                string.IsNullOrWhiteSpace(config.DestEntityName))
            {
                throw new InvalidOperationException("Import bridge configuration is missing required source/destination names.");
            }
            return config;
        }
        private IProgress<IPassedArgs>? CreateImportProgressAdapter(IProgress<PassedArgs> progress)
        {
            if (progress == null)
            {
                return null;
            }

            return new Progress<IPassedArgs>(p =>
            {
                if (p is PassedArgs pa)
                {
                    progress.Report(pa);
                }
                else
                {
                    progress.Report(new PassedArgs
                    {
                        EventType = p?.EventType,
                        Messege = p?.Messege,
                        ParameterInt1 = p?.ParameterInt1 ?? 0,
                        ParameterInt2 = p?.ParameterInt2 ?? 0
                    });
                }
            });
        }
        private async Task<IErrorsInfo> ExecuteImportViaImportingAsync(
            ETLScriptDet step,
            IDataSource sourceDs,
            IDataSource destDs,
            IProgress<PassedArgs> progress,
            CancellationToken token)
        {
            try
            {
                var runId = string.IsNullOrWhiteSpace(_currentRunCorrelationId) ? Guid.NewGuid().ToString("N") : _currentRunCorrelationId;
                using var importManager = new DataImportManager(DMEEditor);
                var config = BuildImportConfigurationFromEtlScript(DMEEditor.ETL.Script, step, progress, token);
                var importProgress = CreateImportProgressAdapter(progress);
                DMEEditor.AddLogMessage(
                    "ETL.ImportBridge",
                    $"[{runId}] Importing manager started for '{step.DestinationEntityName}' ({config.SourceDataSourceName}.{config.SourceEntityName} -> {config.DestDataSourceName}.{config.DestEntityName}).",
                    DateTime.Now,
                    -1,
                    "ETL",
                    Errors.Ok);
                var result = await importManager.RunImportAsync(config, importProgress, token);
                var status = importManager.GetImportStatus();
                _runRecordsProcessed += Math.Max(0, status.RecordsProcessed);
                if (LoadDataLogs == null)
                {
                    LoadDataLogs = new List<LoadDataLogResult>();
                }
                AddLoadLogLine($"ImportBridge Status: processed={status.RecordsProcessed}, blocked={status.RecordsBlocked}, quarantined={status.RecordsQuarantined}, state={status.State}", step?.Id.ToString());
                if (result.Flag == Errors.Failed)
                {
                    DMEEditor.AddLogMessage("ETL.ImportBridge", $"[{runId}] Importing manager failed for '{step.DestinationEntityName}': {result.Message}", DateTime.Now, -1, "ETL", Errors.Failed);
                }
                else
                {
                    DMEEditor.AddLogMessage("ETL.ImportBridge", $"[{runId}] Importing manager completed for '{step.DestinationEntityName}'.", DateTime.Now, -1, "ETL", Errors.Ok);
                }
                return result;
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("ETL.ImportBridge", $"Importing manager exception for '{step?.DestinationEntityName}': {ex.Message}", DateTime.Now, -1, "ETL", Errors.Failed);
                return DMEEditor.ErrorObject;
            }
        }
        private Task<IErrorsInfo> EnsureDestinationEntityAsync(
            IDataSource destinationDataSource,
            EntityStructure desiredEntity,
            ETLScriptDet scriptDetail,
            bool createIfMissing,
            bool alterIfNeeded)
        {
            if (destinationDataSource == null || desiredEntity == null || scriptDetail == null)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Schema ensure skipped because datasource, entity, or script detail is null.";
                return Task.FromResult(DMEEditor.ErrorObject);
            }

            try
            {
                if (_useMigrationSchemaBridge)
                {
                    var migrationManager = new MigrationManager(DMEEditor, destinationDataSource);
                    var ensureResult = migrationManager.EnsureEntity(desiredEntity, createIfMissing, alterIfNeeded);
                    if (ensureResult.Flag == Errors.Ok)
                    {
                        scriptDetail.IsCreated = true;
                        scriptDetail.IsModified = alterIfNeeded;
                        scriptDetail.Failed = false;
                        scriptDetail.ErrorMessage = string.Empty;
                        DMEEditor.AddLogMessage("ETL.Schema", $"Migration ensure succeeded for '{desiredEntity.EntityName}'.", DateTime.Now, -1, "ETL", Errors.Ok);
                        return Task.FromResult(ensureResult);
                    }

                    DMEEditor.AddLogMessage("ETL.Schema", $"Migration ensure failed for '{desiredEntity.EntityName}': {ensureResult.Message}", DateTime.Now, -1, "ETL", Errors.Failed);
                    if (!_enableLegacySchemaFallback)
                    {
                        scriptDetail.Failed = true;
                        scriptDetail.ErrorMessage = ensureResult.Message;
                        return Task.FromResult(ensureResult);
                    }
                }

                if (createIfMissing)
                {
                    var fallbackResult = destinationDataSource.CreateEntityAs(desiredEntity);
                    if (fallbackResult)
                    {
                        scriptDetail.IsCreated = true;
                        scriptDetail.IsModified = false;
                        scriptDetail.Failed = false;
                        scriptDetail.ErrorMessage = string.Empty;
                        DMEEditor.ErrorObject.Flag = Errors.Ok;
                        DMEEditor.ErrorObject.Message = $"Legacy schema fallback created '{desiredEntity.EntityName}'.";
                        DMEEditor.AddLogMessage("ETL.Schema", $"Legacy schema fallback created '{desiredEntity.EntityName}'.", DateTime.Now, -1, "ETL", Errors.Warning);
                        return Task.FromResult(DMEEditor.ErrorObject);
                    }
                }

                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = $"Unable to ensure destination entity '{desiredEntity.EntityName}'.";
                scriptDetail.Failed = true;
                scriptDetail.ErrorMessage = DMEEditor.ErrorObject.Message;
                return Task.FromResult(DMEEditor.ErrorObject);
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = ex.Message;
                scriptDetail.Failed = true;
                scriptDetail.ErrorMessage = ex.Message;
                DMEEditor.AddLogMessage("ETL.Schema", $"EnsureDestinationEntity failed for '{desiredEntity?.EntityName}': {ex.Message}", DateTime.Now, -1, "ETL", Errors.Failed);
                return Task.FromResult(DMEEditor.ErrorObject);
            }
        }
        private Task<IErrorsInfo> ApplySchemaDeltaIfNeededAsync(
            IDataSource destinationDataSource,
            EntityStructure desiredEntity,
            ETLScriptDet scriptDetail)
        {
            if (!_schemaAlterIfNeeded)
            {
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "Schema delta policy disabled.";
                return Task.FromResult(DMEEditor.ErrorObject);
            }

            if (_schemaAllowDestructiveChanges)
            {
                DMEEditor.AddLogMessage("ETL.Schema", "Destructive schema changes are enabled by policy; no destructive actions are executed in this phase.", DateTime.Now, -1, "ETL", Errors.Warning);
            }

            return EnsureDestinationEntityAsync(
                destinationDataSource,
                desiredEntity,
                scriptDetail,
                createIfMissing: false,
                alterIfNeeded: true);
        }
        /// <summary>Runs a create script and updates data.</summary>
        /// <param name="progress">An object that reports the progress of the operation.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An object containing information about any errors that occurred during the operation.</returns>
        /// <remarks>
        /// This method runs a create script and updates data. It connects to the specified data sources, performs the necessary operations, and reports progress using the provided progress object. If the operation is cancelled using the provided cancellation token, the method will stop and return the current error information.
        /// </remarks>
        public async Task<IErrorsInfo> RunCreateScript(IProgress<PassedArgs> progress, CancellationToken token, bool copydata = true, bool useEntityStructure = true)
        {
            #region "Update Data code "
            var preflight = await PreflightCreateScriptAsync(progress, token);
            if (preflight.Flag == Errors.Failed)
            {
                return preflight;
            }



            int numberToCompute = 0;

            IDataSource destds = null;
            IDataSource srcds = null;
            LoadDataLogs = new List<LoadDataLogResult>();
            numberToCompute = DMEEditor.ETL.Script.ScriptDetails.Count();
            List<ETLScriptDet> crls = DMEEditor.ETL.Script.ScriptDetails.Where(i => i.ScriptType == DDLScriptType.CreateEntity).ToList();
            List<ETLScriptDet> copudatals = DMEEditor.ETL.Script.ScriptDetails.Where(i => i.ScriptType == DDLScriptType.CopyData).ToList();
            List<ETLScriptDet> AlterForls = DMEEditor.ETL.Script.ScriptDetails.Where(i => i.ScriptType == DDLScriptType.AlterFor).ToList();
            // Run Scripts-----------------

            numberToCompute = DMEEditor.ETL.Script.ScriptDetails.Count;
            int p1 = DMEEditor.ETL.Script.ScriptDetails.Where(u => u.ScriptType == DDLScriptType.CreateEntity).Count();
            ScriptCount = p1;
            CurrentScriptRecord = 0;
            errorcount = 0;
            stoprun = false;
            bool CreateSuccess;
            EntityStructure entitystr;
            BeginRunTelemetry("RunCreateScript", DMEEditor.ETL.Script.ScriptDetails.Count);
            var schemaEntitiesProcessed = 0;
            var schemaEntitiesCreated = 0;
            var schemaEntitiesAltered = 0;
            var schemaEntitiesFailed = 0;
            foreach (ETLScriptDet sc in DMEEditor.ETL.Script.ScriptDetails.OrderBy(p => p.Id))
            {
                if (token.IsCancellationRequested)
                {
                    _runCancelled = true;
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "RunCreateScript cancelled by token.";
                    ReportEtlProgress(progress, "RunCancelled", DMEEditor.ErrorObject.Message);
                    break;
                }
                CreateSuccess = true;
                destds = DMEEditor.GetDataSource(sc.DestinationDataSourceName);
                srcds = DMEEditor.GetDataSource(sc.SourceDataSourceName);
                CurrentScriptRecord += 1;
                if (errorcount == StopErrorCount)
                {
                    return DMEEditor.ErrorObject;
                }
                if (destds != null)
                {
                    DMEEditor.OpenDataSource(sc.DestinationDataSourceName);
                    if (stoprun == false)
                    {
                        if (destds.ConnectionStatus == ConnectionState.Open)
                        {
                            switch (sc.ScriptType)
                            {
                                case DDLScriptType.CopyEntities:
                                    break;
                                case DDLScriptType.SyncEntity:
                                    break;
                                case DDLScriptType.CompareEntity:
                                    break;
                                case DDLScriptType.CreateEntity:
                                    if (sc.ScriptType == DDLScriptType.CreateEntity)
                                    {
                                        schemaEntitiesProcessed++;
                                        if (!useEntityStructure || sc.SourceEntity == null)
                                        {
                                            entitystr = (EntityStructure)srcds.GetEntityStructure(sc.SourceDataSourceEntityName, false).Clone();
                                        }
                                        else
                                        {
                                            entitystr=sc.SourceEntity;
                                        }
                                        
                                        if (sc.SourceDataSourceEntityName != sc.DestinationEntityName)
                                        {
                                            entitystr.EntityName = sc.DestinationEntityName;
                                            entitystr.DatasourceEntityName = sc.DestinationEntityName;
                                            entitystr.OriginalEntityName = sc.DestinationEntityName;
                                        }
                                       
                                        SendMessege(progress, token, entitystr, sc, $"Creating Entity  {entitystr.EntityName} ");
                                        DMEEditor.ErrorObject = await EnsureDestinationEntityAsync(
                                            destds,
                                            entitystr,
                                            sc,
                                            createIfMissing: _schemaCreateIfMissing,
                                            alterIfNeeded: _schemaAlterIfNeeded);
                                        if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                                        {
                                            schemaEntitiesCreated++;
                                            if (_schemaAlterIfNeeded)
                                            {
                                                schemaEntitiesAltered++;
                                            }
                                            SendMessege(progress, token, entitystr, sc, $"Schema ensured for Entity  {entitystr.EntityName} ");
                                            sc.Active = true;
                                            sc.IsCreated = true;
                                            LoadDataLogs.Add(new LoadDataLogResult { InputLine = $"Schema ensure succeeded for {entitystr.EntityName}" });
                                            if (sc.CopyDataScripts.Count > 0 && sc.CopyData && sc.IsCreated)
                                            {
                                                SendMessege(progress, token, entitystr, sc, $"Started  Coping Data From {entitystr.EntityName} ");
                                                var t=await RunChildScriptAsync(sc, srcds, destds, progress, token);
                                               
                                                CreateSuccess = true;
                                            }
                                        }
                                        else
                                        {
                                            schemaEntitiesFailed++;
                                            if (string.IsNullOrWhiteSpace(DMEEditor.ErrorObject.Message))
                                            {
                                                DMEEditor.ErrorObject.Message = $"Failed in Creating Entity   {entitystr.EntityName} ";
                                            }
                                            SendMessege(progress, token, entitystr, sc, $"Failed in Ensuring Entity   {entitystr.EntityName} ");
                                            sc.Active = false;
                                            sc.Failed = true;
                                            sc.ErrorMessage = DMEEditor.ErrorObject.Message;
                                            LoadDataLogs.Add(new LoadDataLogResult { InputLine = $"Schema ensure failed for {entitystr.EntityName}: {DMEEditor.ErrorObject.Message}" });
                                            CreateSuccess= false;
                                        }
                                    }
                                    break;
                                case DDLScriptType.AlterPrimaryKey:
                                    break;
                                case DDLScriptType.AlterFor:
                                    if (_schemaAllowDestructiveChanges)
                                    {
                                        DMEEditor.AddLogMessage("ETL.Schema", $"AlterFor for '{sc.DestinationEntityName}' skipped: destructive actions are not implemented in this phase.", DateTime.Now, -1, "ETL", Errors.Warning);
                                    }
                                    else
                                    {
                                        if (!useEntityStructure || sc.SourceEntity == null)
                                        {
                                            entitystr = (EntityStructure)srcds.GetEntityStructure(sc.SourceDataSourceEntityName, false).Clone();
                                        }
                                        else
                                        {
                                            entitystr = sc.SourceEntity;
                                        }

                                        if (sc.SourceDataSourceEntityName != sc.DestinationEntityName)
                                        {
                                            entitystr.EntityName = sc.DestinationEntityName;
                                            entitystr.DatasourceEntityName = sc.DestinationEntityName;
                                            entitystr.OriginalEntityName = sc.DestinationEntityName;
                                        }

                                        DMEEditor.ErrorObject = await ApplySchemaDeltaIfNeededAsync(destds, entitystr, sc);
                                        if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                                        {
                                            sc.IsModified = true;
                                            sc.Failed = false;
                                            sc.ErrorMessage = string.Empty;
                                            schemaEntitiesAltered++;
                                            LoadDataLogs.Add(new LoadDataLogResult { InputLine = $"Schema delta applied for {entitystr.EntityName}" });
                                        }
                                        else
                                        {
                                            sc.Failed = true;
                                            sc.ErrorMessage = DMEEditor.ErrorObject.Message;
                                            schemaEntitiesFailed++;
                                            LoadDataLogs.Add(new LoadDataLogResult { InputLine = $"Schema delta failed for {entitystr.EntityName}: {DMEEditor.ErrorObject.Message}" });
                                        }
                                    }
                                    break;
                                case DDLScriptType.AlterUni:
                                    break;
                                case DDLScriptType.DropTable:
                                    break;
                                case DDLScriptType.EnableCons:
                                    break;
                                case DDLScriptType.DisableCons:
                                    break;
                                case DDLScriptType.CopyData:
                                    if (sc.ScriptType == DDLScriptType.CopyData)
                                    {
                                        if(CreateSuccess==false)
                                        {
                                            SendMessege(progress, token, null, sc, $"Cannot Copy Data for Failed  Entity   {sc.DestinationEntityName} ");
                                            break;
                                        }
                                        SendMessege(progress, token, null, sc, $"Started Coping Data for Entity  {sc.DestinationEntityName}  in {sc.DestinationDataSourceName}");
                                        DMEEditor.ErrorObject = await ExecuteCopyStepAsync(sc, srcds, destds, progress, token);
                                        if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                                        {
                                            sc.Failed = true;
                                            SendMessege(progress, token, null, sc, $"Failed in Coping Data for Entity  {sc.DestinationEntityName}");
                                        }
                                        else
                                        {
                                            sc.Failed = false;
                                            SendMessege(progress, token, null, sc, $"Finished in Coping Data for Entity  {sc.DestinationEntityName}");
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                            if (!sc.Failed && DMEEditor.ErrorObject.Flag != Errors.Failed)
                            {
                                CountStepOutcome(true);
                            }

                        }
                        else
                        {
                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                            DMEEditor.ErrorObject.Message = $" Could not Connect to on the Data Dources {sc.DestinationDataSourceName} or {sc.SourceDataSourceName}";
                            SendMessege(progress, token, null, sc);
                        }
                    }
                }
            }
            var schemaSummary = $"ETL Schema Summary: processed={schemaEntitiesProcessed}, created={schemaEntitiesCreated}, altered={schemaEntitiesAltered}, failed={schemaEntitiesFailed}";
            DMEEditor.AddLogMessage("ETL.Schema", schemaSummary, DateTime.Now, -1, "ETL", schemaEntitiesFailed > 0 ? Errors.Warning : Errors.Ok);
            LoadDataLogs.Add(new LoadDataLogResult { InputLine = schemaSummary });
            EndRunTelemetry("RunCreateScript", _runCancelled || token.IsCancellationRequested);
            // SaveETL(destds.DatasourceName);
            #endregion
            return DMEEditor.ErrorObject;
        }
        /// <summary>Runs a script to copy an entity from a source data source to a destination data source.</summary>
        /// <param name="sc">The ETL script details.</param>
        /// <param name="sourceds">The source data source.</param>
        /// <param name="destds">The destination data source.</param>
        /// <param name="srcentity">The name of the source entity.</param>
        /// <param name="destentity">The name of the destination entity.</param>
        /// <param name="progress">An object to report progress.</param>
        /// <param name="token">A cancellation token to cancel the operation.</param>
        /// <param name="CreateMissingEntity">A flag indicating
        private IErrorsInfo RunCopyEntityScript(ETLScriptDet sc, IDataSource sourceds, IDataSource destds, string srcentity, string destentity, IProgress<PassedArgs> progress, CancellationToken token, bool CreateMissingEntity = true, EntityDataMap_DTL map_DTL = null)
        {
            try
            {
                errorcount = 0;
                EntityStructure srcentitystructure = sourceds.GetEntityStructure(srcentity, true);
                EntityStructure destEntitystructure = destds.GetEntityStructure(destentity, true);
                if (srcentitystructure != null && destEntitystructure!=null)
                {
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {
                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.DisableFKConstraints(destEntitystructure);
                    }
                   
                        string querystring = null;
                        List<AppFilter> filters = null;
                        List<EntityField> SelectedFields = null;
                        List<EntityField> SourceFields = null;
                        if (map_DTL != null)
                        {
                            SelectedFields = map_DTL.SelectedDestFields;
                            SourceFields = map_DTL.EntityFields;
                            //querystring = "Select ";
                            //foreach (Mapping_rep_fields mp in map_DTL.FieldMapping)
                            //{
                            //    querystring += mp.FromFieldName + " ,";
                            //}
                            //querystring = querystring.Remove(querystring.Length - 1);
                            //querystring += $" from {map_DTL.EntityName} ";
                            querystring = srcentitystructure.EntityName;
                        }
                        else
                        {
                            querystring = srcentitystructure.EntityName;
                            filters = null;
                            SelectedFields = srcentitystructure.Fields;
                            SourceFields = srcentitystructure.Fields;
                        }
                        SendMessege(progress, token, null, sc, $"Getting Data for  {srcentity}"); ;
                        var srcTb = sourceds.GetEntity(querystring, filters);
                        SendMessege(progress, token, null, sc, $"Finish Getting Data for  {srcentity}"); ;
                        var srcList = NormalizeSourceRows(srcTb, srcentitystructure);
                        if (srcList.Count > 0)
                        {
                            SendMessege(progress, token, null, sc, $"Data fetched {ScriptCount} Record"); ;
                            int i=0;
                            ScriptCount += srcList.Count();
                            foreach (var r in srcList)
                            {
                                i++;
                                DMEEditor.ErrorObject = InsertEntity(destds, destEntitystructure, destentity, map_DTL, r, progress, token); ;
                               // SendMessege(progress, token, null, sc, $"Data Inserted for {destEntitystructure.EntityName} Record {i}"); ;
                                token.ThrowIfCancellationRequested();

                            }
                        }

                   
                
                    if (destds.Category == DatasourceCategory.RDBMS)
                    {
                        IRDBSource rDB = (IRDBSource)destds;
                        rDB.EnableFKConstraints(srcentitystructure);
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Copy Data", $"Error Could not Find Entity  {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ", DateTime.Now, 0, null, Errors.Failed);
                    errorcount = (int)StopErrorCount;
                    SendMessege(progress, token, null, sc, $"Error Could not Find Entity  {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} "); ;
                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error copying Data {srcentity} on {sourceds.DatasourceName} to {srcentity} on {destds.DatasourceName} ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Loads an ETL (Extract, Transform, Load) script from a specified data source.</summary>
        /// <param name="DatasourceName">The name of the data source.</param>
        /// <returns>An object containing information about any errors that occurred during the loading process.</returns>
        /// <remarks>
        /// This method loads an ETL script from the specified data source. It first creates a directory for the script if it doesn't already exist.
        /// Then, it constructs the file path for the script and checks if the file exists. If the file exists, it deserializes the script object from the file.
        /// If any errors occur during the loading process, a log message is added to the DMEEditor and the
        public IErrorsInfo LoadETL(string DatasourceName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                var scriptManager = new ETLScriptManager(DMEEditor);
                var loadedScript = scriptManager.LoadScriptByDataSource(DatasourceName);
                if (loadedScript != null)
                {
                    if (string.IsNullOrWhiteSpace(loadedScript.ScriptFormatVersion))
                    {
                        loadedScript.ScriptFormatVersion = "1.0-legacy";
                    }
                    if (loadedScript.ScriptSchemaVersion <= 0)
                    {
                        loadedScript.ScriptSchemaVersion = 1;
                    }
                    Script = loadedScript;
                    DMEEditor.AddLogMessage("ETL", $"Loaded canonical ETL script for {DatasourceName}.", DateTime.Now, 0, null, Errors.Ok);
                    return DMEEditor.ErrorObject;
                }

                // Legacy compatibility path: preserve old datasource-folder convention.
                string dbpath = DMEEditor.ConfigEditor.ExePath + "\\Scripts\\" + DatasourceName;
                Directory.CreateDirectory(dbpath);
                string filepath = Path.Combine(dbpath, "createscripts.json");
                if (File.Exists(filepath))
                {
                    var legacyScript = DMEEditor.ConfigEditor.JsonLoader.DeserializeSingleObject<ETLScriptHDR>(filepath);
                    if (legacyScript != null)
                    {
                        legacyScript.ScriptSource = string.IsNullOrWhiteSpace(legacyScript.ScriptSource) ? DatasourceName : legacyScript.ScriptSource;
                        legacyScript.ScriptFormatVersion = string.IsNullOrWhiteSpace(legacyScript.ScriptFormatVersion) ? "1.0-legacy" : legacyScript.ScriptFormatVersion;
                        legacyScript.ScriptSchemaVersion = legacyScript.ScriptSchemaVersion <= 0 ? 1 : legacyScript.ScriptSchemaVersion;
                        Script = legacyScript;
                        DMEEditor.AddLogMessage("ETL", $"Loaded legacy ETL script for {DatasourceName}.", DateTime.Now, 0, null, Errors.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not Load InMemory Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Saves the ETL (Extract, Transform, Load) configuration for a given datasource.</summary>
        /// <param name="DatasourceName">The name of the datasource.</param>
        /// <returns>An object containing information about any errors that occurred during the save operation.</returns>
        /// <remarks>
        /// This method creates a directory for the specified datasource if it doesn't already exist.
        /// It then saves the ETL configuration as a JSON file in the created directory.
        /// If any errors occur during the save operation, a log message is added and the error object is returned.
        /// </remarks>
        public IErrorsInfo SaveETL(string DatasourceName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                var scriptManager = new ETLScriptManager(DMEEditor);
                Script.ScriptSource = string.IsNullOrWhiteSpace(Script.ScriptSource) ? DatasourceName : Script.ScriptSource;
                Script.ScriptName = string.IsNullOrWhiteSpace(Script.ScriptName) ? DatasourceName : Script.ScriptName;
                Script.ScriptFormatVersion = string.IsNullOrWhiteSpace(Script.ScriptFormatVersion) ? "2.0" : Script.ScriptFormatVersion;
                Script.ScriptSchemaVersion = Script.ScriptSchemaVersion <= 0 ? 2 : Script.ScriptSchemaVersion;

                DMEEditor.ErrorObject = scriptManager.ValidateScript(Script);
                if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                {
                    DMEEditor.AddLogMessage("ETL", $"ETL script validation failed before save for {DatasourceName}: {DMEEditor.ErrorObject.Message}", DateTime.Now, 0, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                DMEEditor.ErrorObject = scriptManager.SaveScript(DatasourceName, Script);
                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                {
                    DMEEditor.AddLogMessage("ETL", $"Saved canonical ETL script for {DatasourceName}.", DateTime.Now, 0, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Could not save InMemory Structure for {DatasourceName}- {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        #endregion "Run Scripts"
        #region "Import Methods"
        /// <summary>Creates an import script based on the provided entity mappings.</summary>
        /// <param name="mapping">The main entity data map.</param>
        /// <param name="SelectedMapping">The selected entity data map.</param>
        /// <returns>An object containing information about any errors that occurred during the script creation.</returns>
        /// <remarks>
        /// This method generates an import script by populating the necessary properties of the ETLScriptHDR and ETLScriptDet objects.
        /// It sets the script source to the entity data source of the selected mapping, initializes error and script count variables,
        /// clears the load data logs, and adds a new ETLScriptDet object to the ScriptDetails list with the necessary properties.
        /// If any
        public IErrorsInfo CreateImportScript(EntityDataMap mapping, EntityDataMap_DTL SelectedMapping)
        {
            try
            {
                Script = new ETLScriptHDR();
                Script.ScriptSource = SelectedMapping.EntityDataSource;
                errorcount = 0;
                ScriptCount = 0;
                LoadDataLogs.Clear();
                Script.ScriptDetails.Add(new ETLScriptDet() { Active = true, DestinationDataSourceName = mapping.EntityDataSource, DestinationDataSourceEntityName = mapping.EntityName, DestinationEntityName = mapping.EntityName,ScriptType= DDLScriptType.CopyData, Mapping = SelectedMapping, SourceDataSourceName = SelectedMapping.EntityDataSource, SourceDataSourceEntityName = SelectedMapping.EntityName, SourceEntityName = SelectedMapping.EntityName });
                DMEEditor.AddLogMessage("OK", $"Generated Copy Data script", DateTime.Now, -1, "CopyDatabase", Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error Generating Copy Data script ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>Runs an import script and returns information about any errors that occurred.</summary>
        /// <param name="progress">An object that reports the progress of the import script.</param>
        /// <param name="token">A cancellation token that can be used to cancel the import script.</param>
        /// <returns>An object containing information about any errors that occurred during the import script.</returns>
        public async Task<IErrorsInfo> RunImportScript(IProgress<PassedArgs> progress, CancellationToken token,bool useEntityStructure = true)
        {
            var preflight = await PreflightImportScriptAsync(progress, token);
            if (preflight.Flag == Errors.Failed)
            {
                return preflight;
            }
            IDataSource destds = null;
            IDataSource srcds = null;
            ScriptCount = 1;
            CurrentScriptRecord = 0;
            errorcount = 0;
            stoprun = false;
            EntityStructure entitystr;
            BeginRunTelemetry("RunImportScript", ScriptCount);
            CurrentScriptRecord += 1;
            LoadDataLogs = new List<LoadDataLogResult>();
            ETLScriptDet sc = DMEEditor.ETL.Script.ScriptDetails.First();
            if (sc != null)
            {
                destds = DMEEditor.GetDataSource(sc.DestinationDataSourceName);
                srcds = DMEEditor.GetDataSource(sc.SourceDataSourceName);
                if (errorcount == StopErrorCount)
                {
                    EndRunTelemetry("RunImportScript", _runCancelled || token.IsCancellationRequested);
                    return DMEEditor.ErrorObject;
                }
                if (destds != null)
                {

                    DMEEditor.OpenDataSource(sc.DestinationDataSourceName);
                    if (stoprun == false)
                    {
                        if (destds.ConnectionStatus == ConnectionState.Open)
                        {
                            if (sc.ScriptType == DDLScriptType.CopyData)
                            {
                                if (_useImportingRunForImports)
                                {
                                    DMEEditor.ErrorObject = await ExecuteImportViaImportingAsync(sc, srcds, destds, progress, token);
                                    if (DMEEditor.ErrorObject.Flag == Errors.Ok || !_enableLegacyImportFallback)
                                    {
                                        if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                                        {
                                            CountStepOutcome(true);
                                            sc.Failed = false;
                                            sc.ErrorMessage = string.Empty;
                                            SendMessege(progress, token, null, sc, $"Import completed via Importing bridge for {sc.DestinationEntityName}");
                                        }
                                        else
                                        {
                                            CountStepOutcome(false);
                                        }
                                        EndRunTelemetry("RunImportScript", _runCancelled || token.IsCancellationRequested);
                                        return DMEEditor.ErrorObject;
                                    }
                                    DMEEditor.AddLogMessage("ETL.ImportBridge", $"Falling back to legacy import path for '{sc.DestinationEntityName}'.", DateTime.Now, -1, "ETL", Errors.Warning);
                                }
                                CurrrentDBDefaults = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == destds.DatasourceName)].DatasourceDefaults;
                                if (!useEntityStructure || sc.SourceEntity == null)
                                {
                                    entitystr = (EntityStructure)srcds.GetEntityStructure(sc.SourceDataSourceEntityName, false).Clone();
                                }
                                else
                                {
                                    entitystr = sc.SourceEntity;
                                }

                              

                                sc.ErrorMessage = DMEEditor.ErrorObject.Message;
                               
                                sc.Active = false;
                                SendMessege(progress, token, null, sc, "Starting Import Entities Script");

                                if (errorcount == StopErrorCount)
                                {
                                    return DMEEditor.ErrorObject;
                                }
                                DMEEditor.ErrorObject = await ExecuteCopyStepAsync(
                                    sc,
                                    srcds,
                                    destds,
                                    progress,
                                    token,
                                    new EtlCopyExecutionOptions
                                    {
                                        BatchSize = 100,
                                        EnableParallel = false,
                                        MaxRetries = 3
                                    });
                                if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                                {
                                    sc.Failed = true;
                                    sc.ErrorMessage = DMEEditor.ErrorObject.Message;
                                    SendMessege(progress, token, null, sc, $"Import copy failed for {sc.DestinationEntityName}");
                                }
                                else
                                {
                                    CountStepOutcome(true);
                                    sc.Failed = false;
                                    sc.ErrorMessage = string.Empty;
                                    SendMessege(progress, token, null, sc, $"Import copy completed for {sc.DestinationEntityName}");
                                }
                            }
                        }
                        else
                        {
                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                            DMEEditor.ErrorObject.Message = $" Could not Connect to on the Data Dources {sc.DestinationDataSourceName} or {sc.SourceDataSourceName}";
                            SendMessege(progress, token);
                        }

                    }
                }
            }
            EndRunTelemetry("RunImportScript", _runCancelled || token.IsCancellationRequested);
            return DMEEditor.ErrorObject;

        }
        #endregion
        /// <summary>Inserts an entity into a destination data source.</summary>
        /// <param name="destds">The destination data source.</param>
        /// <param name="destEntitystructure">The structure of the destination entity.</param>
        /// <param name="destentity">The name of the destination entity.</param>
        /// <param name="map_DTL">The mapping details for the entity.</param>
        /// <param name="r">The object representing the entity to be inserted.</param>
        /// <param name="progress">An object to report progress during the insertion process.</param>
        /// <param name="token">A cancellation token to cancel the insertion process.</param>
        /// <returns>An object containing information about any errors
        public IErrorsInfo InsertEntity(IDataSource destds, EntityStructure destEntitystructure, string destentity, EntityDataMap_DTL map_DTL, object r, IProgress<PassedArgs> progress, CancellationToken token)
        {
            object retval = r;
            try
            {
                if (map_DTL != null)
                {
                    retval = DMEEditor.Utilfunction.MapObjectToAnother(DMEEditor, destentity, map_DTL, r);
                }
                if (CurrrentDBDefaults.Count > 0)
                {
                    foreach (DefaultValue _defaultValue in CurrrentDBDefaults.Where(p => p.propertyType == DefaultValueType.Rule))
                    {
                        if (destEntitystructure.Fields.Any(p => p.FieldName.Equals(_defaultValue.PropertyName, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            string FieldName = _defaultValue.PropertyName;
                            DMEEditor.Passedarguments.DatasourceName = destds.DatasourceName;
                            DMEEditor.Passedarguments.CurrentEntity = destentity;
                            ObjectItem ob = DMEEditor.Passedarguments.Objects.Find(p => p.Name == destentity);
                            if (ob != null)
                            {
                                DMEEditor.Passedarguments.Objects.Remove(ob);
                            }
                            DMEEditor.Passedarguments.Objects.Add(new ObjectItem() { Name = destentity, obj = retval });
                            DMEEditor.Passedarguments.ParameterString1 = $":{_defaultValue.Rule}.{FieldName}.{_defaultValue.PropertyValue}";
                            //var value = RulesEngine.SolveRule(_defaultValue.Rule,DMEEditor.Passedarguments);
                            //if (value != null)
                            //{
                            //    DMEEditor.Utilfunction.SetFieldValueFromObject(FieldName, retval, value);
                            //}
                        }
                    }
                }
                if (destEntitystructure.Relations.Any())
                {
                    foreach (RelationShipKeys item in destEntitystructure.Relations) // .Where(p => !p.RelatedEntityID.Equals(destEntitystructure.EntityName, StringComparison.InvariantCultureIgnoreCase)
                    {
                        //if (destEntitystructure.Fields.Any(p => p.FieldName.Equals(item.EntityColumnID, StringComparison.InvariantCultureIgnoreCase)))
                        //{
                        if (!string.IsNullOrEmpty(item.RelatedEntityID))
                        {
                            EntityStructure refentity = (EntityStructure)destds.GetEntityStructure(item.RelatedEntityID, true).Clone();
                            if (DMEEditor.Utilfunction.GetFieldValueFromObject(item.EntityColumnID, retval) != null)
                            {
                                if (EntityDataMoveValidator.TrueifParentExist(DMEEditor, destds, destEntitystructure, retval, item.EntityColumnID, DMEEditor.Utilfunction.GetFieldValueFromObject(item.EntityColumnID, retval)) == EntityValidatorMesseges.MissingRefernceValue)
                                {
                                    LoadDataLogs.Add(new LoadDataLogResult() { InputLine = $"Inserting Parent for  Record {CurrentScriptRecord}  in {item.RelatedEntityID}" });
                                    //---- insert Parent Key ----
                                    object parentob = DMEEditor.Utilfunction.GetEntityObject(DMEEditor, item.RelatedEntityID, refentity.Fields);
                                    if (parentob != null)
                                    {
                                        var refval = DMEEditor.Utilfunction.GetFieldValueFromObject(item.EntityColumnID, retval);
                                        DMEEditor.Utilfunction.SetFieldValueFromObject(item.RelatedEntityColumnID, parentob, refval);
                                        //DMEEditor.ErrorObject = destds.InsertEntity(item.ParentEntityID, parentob);
                                        DMEEditor.ErrorObject = InsertEntity(destds, refentity, item.RelatedEntityID, null, parentob, progress, token); ;
                                        token.ThrowIfCancellationRequested();
                                        SendMessege(progress, token, refentity);
                                    }
                                }
                                //};
                            }

                        }
                    }
                }
                CurrentScriptRecord += 1;
                _runRecordsProcessed += 1;
                 SendMessege(progress, token, destEntitystructure, null, $"Inserting Record {CurrentScriptRecord} ");
                if (_runRecordsProcessed % 250 == 0)
                {
                    ReportEtlProgress(progress, "Heartbeat", $"Heartbeat entity={destentity} recordsProcessed={_runRecordsProcessed}");
                    AddLoadLogLine($"Heartbeat entity={destentity} recordsProcessed={_runRecordsProcessed}");
                }
                DMEEditor.ErrorObject = destds.InsertEntity(destEntitystructure.EntityName, retval);
                token.ThrowIfCancellationRequested();

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("ETL", $"Failed to Insert Entity {destentity} :{ex.Message}", DateTime.Now, CurrentScriptRecord, ex.Message, Errors.Failed);

            }


            return DMEEditor.ErrorObject;
        }
        /// <summary>Sends a message and updates progress based on the result.</summary>
        /// <param name="progress">An object that reports progress updates.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <param name="refentity">An optional reference to an entity structure.</param>
        /// <param name="sc">An optional ETL script detail.</param>
        /// <param name="messege">An optional message to send.</param>
        /// <remarks>
        /// If the error flag is set to "Failed" in the DMEEditor.ErrorObject, a SyncErrorsandTracking object is created and the error count is incremented.
        /// If the error flag is not
        private void SendMessege(IProgress<PassedArgs> progress, CancellationToken token, EntityStructure refentity = null, ETLScriptDet sc = null, string messege = null)
        {
            if (DMEEditor.ErrorObject.Flag == Errors.Failed)
            {

                SyncErrorsandTracking tr = new SyncErrorsandTracking();
                errorcount++;
                tr.ErrorMessage = DMEEditor.ErrorObject.Message;
                
                tr.RunDate = DateTime.Now;
                tr.SourceEntityName = refentity == null ? null : refentity.EntityName;
                tr.CurrentRecordIndex = CurrentScriptRecord;
                tr.SourceDataSourceName = refentity == null ? null : refentity.DataSourceID;
                if (sc != null)
                {
                    tr.ParentScriptId = sc.Id;
                    tr.Script = sc.ScriptType.ToString();
                    sc.Tracking.Add(tr);
                    sc.LastRunCorrelationId = _currentRunCorrelationId;
                    sc.LastEventType = "StepFailed";
                    sc.LastEventMessage = DMEEditor.ErrorObject.Message;
                    sc.LastUpdatedUtc = DateTime.UtcNow;
                }

                AddLoadLogLine($"Failed {CurrentScriptRecord} - {messege} : {tr.ErrorMessage}", sc?.Id.ToString());
                ReportEtlProgress(progress, "StepFailed", DMEEditor.ErrorObject.Message);
                CountStepOutcome(false);
                if (errorcount > StopErrorCount)
                {
                    stoprun = true;
                    ReportEtlProgress(progress, "RunStopped", $"StopErrorCount reached ({StopErrorCount}).");
                    AddLoadLogLine($"Run stopped after reaching StopErrorCount={StopErrorCount}", sc?.Id.ToString());

                }
            }
            else
            {
                if (sc != null)
                {
                    sc.LastRunCorrelationId = _currentRunCorrelationId;
                    sc.LastEventType = "StepUpdate";
                    sc.LastEventMessage = messege;
                    sc.LastUpdatedUtc = DateTime.UtcNow;
                }
                AddLoadLogLine($"{messege}", sc?.Id.ToString());
                ReportEtlProgress(progress, "StepUpdate", messege);
            }
        }

        /// <summary>
        /// Lightweight result contract used by UI wizards that call CopyEntity directly.
        /// </summary>
        public sealed class EtlCopyResult
        {
            public int RecordCount { get; set; }
            public string Message { get; set; } = string.Empty;
            public Errors Flag { get; set; } = Errors.Unknown;
        }

        /// <summary>
        /// Copies one entity from source to destination and returns a UI-friendly summary.
        /// </summary>
        public EtlCopyResult CopyEntity(
            IDataSource sourceDs,
            IDataSource destDs,
            string sourceEntity,
            string destinationEntity,
            IProgress<PassedArgs> progress,
            CancellationToken token)
        {
            var result = new EtlCopyResult();

            if (sourceDs == null || destDs == null)
            {
                result.Flag = Errors.Failed;
                result.Message = "Source or destination datasource is null.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(sourceEntity) || string.IsNullOrWhiteSpace(destinationEntity))
            {
                result.Flag = Errors.Failed;
                result.Message = "Source or destination entity is missing.";
                return result;
            }

            try
            {
                if (sourceDs.ConnectionStatus != ConnectionState.Open)
                {
                    sourceDs.Openconnection();
                }
                if (destDs.ConnectionStatus != ConnectionState.Open)
                {
                    destDs.Openconnection();
                }

                var sourcePayload = sourceDs.GetEntity(sourceEntity, null);
                var candidateCount = CountRows(sourcePayload);

                var copier = new ETLDataCopier(DMEEditor);
                var copyInfo = copier.CopyEntityDataAsync(
                    sourceDs,
                    destDs,
                    sourceEntity,
                    destinationEntity,
                    progress,
                    token).GetAwaiter().GetResult();

                result.Flag = copyInfo?.Flag ?? Errors.Unknown;
                result.RecordCount = result.Flag == Errors.Ok ? candidateCount : 0;
                result.Message = copyInfo?.Message
                                 ?? (result.Flag == Errors.Ok
                                     ? $"Copied {result.RecordCount} records from {sourceEntity} to {destinationEntity}."
                                     : $"Copy failed for {sourceEntity} -> {destinationEntity}.");
            }
            catch (OperationCanceledException)
            {
                result.Flag = Errors.Warning;
                result.Message = "Copy cancelled.";
            }
            catch (Exception ex)
            {
                result.Flag = Errors.Failed;
                result.Message = ex.Message;
            }

            return result;
        }

        private static int CountRows(object? payload)
        {
            if (payload == null)
            {
                return 0;
            }

            if (payload is DataTable table)
            {
                return table.Rows.Count;
            }

            if (payload is IBindingListView bindingListView)
            {
                return bindingListView.Count;
            }

            if (payload is IEnumerable<object> typed)
            {
                return typed.Count();
            }

            if (payload is System.Collections.ICollection collection)
            {
                return collection.Count;
            }

            if (payload is System.Collections.IEnumerable enumerable)
            {
                int count = 0;
                foreach (var _ in enumerable) count++;
                return count;
            }

            return 1;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    LoadDataLogs = null;
                    Script = null;
                    CurrrentDBDefaults = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ETL()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}
