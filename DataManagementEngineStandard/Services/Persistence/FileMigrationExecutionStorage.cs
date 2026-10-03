using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.ConfigUtil.Managers;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Services.Persistence
{
    /// <summary>One captured configuration root/loader for history and execution claims.</summary>
    public sealed class FileMigrationExecutionStorage : IMigrationExecutionStorage
    {
        private readonly MigrationHistoryManager _history;
        private readonly FileMigrationExecutionOwnership _ownership;
        public string ScopeIdentity { get; }

        public FileMigrationExecutionStorage(string configurationRoot, IJsonLoader loader, IDMLogger logger = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(configurationRoot);
            ArgumentNullException.ThrowIfNull(loader);
            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(configurationRoot));
            var identity = OperatingSystem.IsWindows() ? root.ToUpperInvariant() : root;
            ScopeIdentity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
            _history = new MigrationHistoryManager(logger, loader, new ConfigandSettings { ConfigPath = root }, null);
            _ownership = new FileMigrationExecutionOwnership(Path.Combine(root, "Migrations", "ExecutionOwnership"));
        }

        public MigrationHistory LoadMigrationHistory(string name) => _history.Load(name);
        public PersistenceWriteResult SaveMigrationHistoryAcknowledged(MigrationHistory history, CancellationToken token = default) =>
            _history.SaveAcknowledged(history, token);
        public PersistenceWriteResult AppendMigrationRecordAcknowledged(string name, DataSourceType type, MigrationRecord record,
            CancellationToken token = default) => _history.AppendAcknowledged(name, type, record, token);
        public MigrationExecutionAdmission TryAcquireMigrationExecution(string target, string executionToken, string planHash,
            CancellationToken token = default) => _ownership.TryAcquireMigrationExecution(target, executionToken, planHash, token);
        public MigrationExecutionClaim ReadMigrationExecutionClaim(string target) => _ownership.ReadMigrationExecutionClaim(target);
        public PersistenceWriteResult ReconcileMigrationExecution(string target, string claim, string actor, string evidence,
            CancellationToken token = default) => _ownership.ReconcileMigrationExecution(target, claim, actor, evidence, token);
    }
}
