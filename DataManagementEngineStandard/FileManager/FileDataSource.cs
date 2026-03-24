using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.FileManager.Contracts;
using System.Threading;
using TheTechIdea.Beep.FileManager.Classification;
using TheTechIdea.Beep.FileManager.Contracts;
using TheTechIdea.Beep.FileManager.Governance;
using TheTechIdea.Beep.FileManager.Readers;
using TheTechIdea.Beep.FileManager.Resilience;
using TheTechIdea.Beep.FileManager.Schema;
using TheTechIdea.Beep.FileManager.Utilities;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Format-agnostic file data source.  The actual file parsing is delegated to
    /// an <see cref="IFileFormatReader"/> resolved at connection-open time via
    /// <see cref="FileReaderFactory"/> using <see cref="DatasourceType"/>.
    ///
    /// Supported formats (built-in): CSV, TSV, JSON, Text.
    /// Additional formats can be registered with <see cref="FileReaderFactory.Register"/>.
    /// </summary>
    [AddinAttribute(
        Category       = DatasourceCategory.FILE,
        DatasourceType = DataSourceType.CSV | DataSourceType.TSV | DataSourceType.Json | DataSourceType.Text,
        FileType       = "csv,tsv,json,txt")]
    public partial class FileDataSource : IDataSource, IIdempotentFileIngester
    {
        // ── IDataSource contract ─────────────────────────────────────────────
        public event EventHandler<PassedArgs> PassEvent;

        public string ColumnDelimiter    { get; set; } = "''";
        public string ParameterDelimiter { get; set; } = ":";
        public string GuidID             { get; set; } = Guid.NewGuid().ToString();
        public DataSourceType   DatasourceType  { get; set; } = DataSourceType.CSV;
        public DatasourceCategory Category      { get; set; } = DatasourceCategory.FILE;
        public IDataConnection  Dataconnection  { get; set; }
        public string           DatasourceName  { get; set; }
        public IErrorsInfo      ErrorObject     { get; set; }
        public string           Id              { get; set; }
        public IDMLogger        Logger          { get; set; }
        public List<string>         EntitiesNames { get; set; } = new();
        public List<EntityStructure> Entities   { get; set; } = new();
        public IDMEEditor       DMEEditor       { get; set; }
        public ConnectionState  ConnectionStatus { get; set; } = ConnectionState.Closed;

        // ── Enterprise service hooks ─────────────────────────────────────────
        public ITenantContext        TenantContext        { get; set; }
        public IIngestionStateStore  IngestionStateStore  { get; set; }
        public IDeadLetterStore      DeadLetterStore      { get; set; }
        public IFileSchemaRegistry   SchemaRegistry       { get; set; }

        // ── Governance / security hooks ──────────────────────────────────────
        public IFileAccessPolicy       AccessPolicy       { get; set; }
        public IRowLevelSecurityFilter RowSecurityFilter  { get; set; }
        public IDataResidencyPolicy    ResidencyPolicy    { get; set; }
        public IDataMaskingEngine      MaskingEngine      { get; set; }
        public IMaskingPolicyStore     MaskingPolicies    { get; set; }

        // ── Validation settings ──────────────────────────────────────────────
        public RowValidationMode ValidationMode { get; set; } = RowValidationMode.Warn;

        // ── Internal state ───────────────────────────────────────────────────
        /// <summary>Pluggable reader resolved from <see cref="DatasourceType"/> on open.</summary>
        private IFileFormatReader _reader;

        private bool _inTransaction;
        private readonly List<Func<IErrorsInfo>> _pendingOperations = new();

        // ── Constructors ─────────────────────────────────────────────────────

        public FileDataSource()
        {
            IngestionStateStore = new InMemoryIngestionStateStore();
            DeadLetterStore     = new InMemoryDeadLetterStore();
        }

        public FileDataSource(string datasourceName, IDMLogger logger, IDMEEditor dmeEditor,
                              DataSourceType datasourceType, IErrorsInfo errors) : this()
        {
            DatasourceName = datasourceName;
            Logger         = logger;
            DMEEditor      = dmeEditor;
            DatasourceType = datasourceType;
            ErrorObject    = errors;

            Dataconnection = new FileConnection(dmeEditor)
            {
                Logger      = logger,
                ErrorObject = errors
            };

            LoadConnectionProperties();
        }

        // ── Startup helpers ──────────────────────────────────────────────────

        private void LoadConnectionProperties()
        {
            if (DMEEditor?.ConfigEditor == null
                || Dataconnection == null
                || string.IsNullOrWhiteSpace(DatasourceName))
                return;

            ConnectionProperties conn = DMEEditor.ConfigEditor.DataConnections?
                .FirstOrDefault(c =>
                    string.Equals(c.ConnectionName, DatasourceName, StringComparison.OrdinalIgnoreCase)
                 || string.Equals(c.FileName,       DatasourceName, StringComparison.OrdinalIgnoreCase));

            if (conn == null) return;
            Dataconnection.ConnectionProp = conn;
        }

        // ── Path resolution ──────────────────────────────────────────────────

        /// <summary>
        /// Resolves the physical file path for an entity, using the reader's
        /// default extension when no explicit extension is present.
        /// </summary>
        internal string ResolveEntityFilePath(string entityName)
        {
            if (Dataconnection?.ConnectionProp == null)
                throw new InvalidOperationException("Connection properties are not initialized.");

            string basePath    = Dataconnection.ConnectionProp.FilePath  ?? string.Empty;
            string defaultFile = Dataconnection.ConnectionProp.FileName  ?? string.Empty;
            string ext         = _reader?.GetDefaultExtension() ?? "csv";

            if (!string.IsNullOrWhiteSpace(entityName)
                && !string.Equals(Path.GetFileNameWithoutExtension(defaultFile),
                                  entityName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(defaultFile, entityName, StringComparison.OrdinalIgnoreCase))
            {
                string candidate = Path.HasExtension(entityName)
                    ? entityName
                    : $"{entityName}.{ext}";
                return Path.Combine(basePath, candidate);
            }

            return Path.Combine(basePath, defaultFile);
        }

        // ── IDataSource helpers ──────────────────────────────────────────────

        public IEnumerable<string> GetEntitesList() => EntitiesNames;

        public Type GetEntityType(string EntityName) => typeof(Dictionary<string, object>);

        public int GetEntityIdx(string entityName) =>
            Entities.FindIndex(e =>
                string.Equals(e.EntityName, entityName, StringComparison.OrdinalIgnoreCase));

        public bool CheckEntityExist(string EntityName)
        {
            string path = ResolveEntityFilePath(EntityName);
            return File.Exists(path);
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return Enumerable.Empty<ChildRelation>();
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            return Enumerable.Empty<RelationShipKeys>();
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            return ErrorObject;
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            return Enumerable.Empty<ETLScriptDet>();
        }

        protected void RaisePassEvent(string message)
        {
            PassEvent?.Invoke(this, new PassedArgs { Messege = message, EventType = "FileDataSource" });
        }

        public void Dispose()
        {
            _pendingOperations.Clear();
            ConnectionStatus = ConnectionState.Closed;
        }

        // ── Static helpers ───────────────────────────────────────────────────

        /// <summary>Normalises a raw header/field name to a valid C#-compatible identifier.</summary>
        internal static string NormalizeFieldName(string source)
            => Regex.Replace(source ?? string.Empty, @"[\s\-\.]+", "_");

        // ── IIdempotentFileIngester ──────────────────────────────────────────

        public async Task<string> IsAlreadyIngestedAsync(IFileIngestionDescriptor descriptor, CancellationToken ct = default)
        {
            if (IngestionStateStore == null || descriptor == null) return null;
            return await IngestionStateStore.FindByChecksumAsync(
                descriptor.FileChecksum, descriptor.TargetEntityName, ct);
        }

        public async Task<IngestionJobStatus> IngestAsync(
            IFileIngestionDescriptor descriptor,
            IProgress<IngestionProgressArgs> progress = null,
            CancellationToken ct = default)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            if (IngestionStateStore == null)
                return new IngestionJobStatus(descriptor.JobId, IngestionState.Failed, 0, 0, DateTimeOffset.UtcNow, "No state store configured.");

            await IngestionStateStore.CreateAsync(descriptor, ct);
            await IngestionStateStore.TransitionAsync(descriptor.JobId, IngestionState.Ingesting, "Starting", ct);

            long rows = 0;
            try
            {
                string filePath = ResolveEntityFilePath(descriptor.TargetEntityName);
                var headers = _reader?.ReadHeaders(filePath) ?? Array.Empty<string>();
                foreach (var _ in _reader?.ReadRows(filePath) ?? Enumerable.Empty<string[]>())
                {
                    ct.ThrowIfCancellationRequested();
                    rows++;
                    await IngestionStateStore.SaveCheckpointAsync(descriptor.JobId, rows * 100, rows, ct);
                    progress?.Report(new IngestionProgressArgs(descriptor.JobId, IngestionState.Ingesting, rows, rows, 0, 100.0 * rows / Math.Max(rows, 1)));
                }

                await IngestionStateStore.TransitionAsync(descriptor.JobId, IngestionState.Complete, $"Ingested {rows} rows.", ct);
                return new IngestionJobStatus(descriptor.JobId, IngestionState.Complete, rows, 0, DateTimeOffset.UtcNow, $"Ingested {rows} rows.");
            }
            catch (OperationCanceledException)
            {
                await IngestionStateStore.TransitionAsync(descriptor.JobId, IngestionState.Suspended, "Cancelled.", ct);
                return new IngestionJobStatus(descriptor.JobId, IngestionState.Suspended, rows, 0, DateTimeOffset.UtcNow, "Cancelled.");
            }
            catch (Exception ex)
            {
                await IngestionStateStore.TransitionAsync(descriptor.JobId, IngestionState.Failed, ex.Message, CancellationToken.None);
                return new IngestionJobStatus(descriptor.JobId, IngestionState.Failed, rows, 0, DateTimeOffset.UtcNow, ex.Message);
            }
        }

        public async Task<IngestionJobStatus> ResumeAsync(
            string jobId,
            IProgress<IngestionProgressArgs> progress = null,
            CancellationToken ct = default)
        {
            if (IngestionStateStore == null)
                return new IngestionJobStatus(jobId, IngestionState.Failed, 0, 0, DateTimeOffset.UtcNow, "No state store configured.");

            var descriptor = await IngestionStateStore.GetDescriptorAsync(jobId, ct);
            if (descriptor == null)
                return new IngestionJobStatus(jobId, IngestionState.Failed, 0, 0, DateTimeOffset.UtcNow, "Job not found.");

            return await IngestAsync(descriptor, progress, ct);
        }
    }
}
