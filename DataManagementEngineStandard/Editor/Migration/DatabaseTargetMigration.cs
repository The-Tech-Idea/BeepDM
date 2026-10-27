using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.SchemaMigration;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>Owns acknowledged catalog-creation intent, identity, ownership and recovery.
    /// Construct through MigrationManager.BindDatabaseTarget. No retry, drop, adoption or CRUD.</summary>
    public sealed class DatabaseTargetMigration
    {
        private sealed record Claim(int Version, string Server, string Database, Guid Owner,
            bool CreationDispatched = false, string Provider = null, string OwnershipScope = null);
        private readonly IDatabaseTargetMigrationProvider _provider;
        private readonly DatabaseTargetMigrationIdentity _identity;
        private readonly string _claimPath;
        private readonly DatabaseTargetLegacyClaimBinding _legacyClaimBinding;
        internal DatabaseTargetMigration(IDatabaseTargetMigrationProvider provider, string claimPath, DatabaseTargetLegacyClaimBinding legacyClaimBinding)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _identity = provider.Identity ?? throw new ArgumentException("A native database identity is required.", nameof(provider));
            if (string.IsNullOrWhiteSpace(_identity.Provider) || string.IsNullOrWhiteSpace(_identity.Server) ||
                string.IsNullOrWhiteSpace(_identity.Database) || string.IsNullOrWhiteSpace(_identity.OwnershipScope))
                throw new ArgumentException("The complete native database identity is required.", nameof(provider));
            _claimPath = Path.GetFullPath(claimPath);
            _legacyClaimBinding = legacyClaimBinding;
        }
        public DatabaseTargetMigrationIdentity Identity => _identity;
        private void CheckTarget()
        {
            if (_provider.Identity != _identity)
                throw new InvalidOperationException("The native database target changed after migration binding.");
        }
        private Claim ReadClaim()
        {
            if (!File.Exists(_claimPath)) return null;
            var claim = JsonSerializer.Deserialize<Claim>(File.ReadAllText(_claimPath));
            // An application must explicitly map its old unscoped receipt to the exact native
            // provider/scope. The framework owns no application names or implicit compatibility.
            var legacy = claim?.Version == 1 && _legacyClaimBinding != null &&
                _identity.Provider == _legacyClaimBinding.Provider && _identity.OwnershipScope == _legacyClaimBinding.OwnershipScope &&
                claim.Provider == null && claim.OwnershipScope == null;
            var current = claim?.Version == 2 && claim.Provider == _identity.Provider && claim.OwnershipScope == _identity.OwnershipScope;
            if (claim == null || (!legacy && !current) || claim.Owner == Guid.Empty ||
                !string.Equals(claim.Server, _identity.Server, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(claim.Database, _identity.Database, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The database migration claim is invalid or belongs to another target. Preserve it for reconciliation.");
            return claim;
        }
        private async Task SaveClaimAsync(Claim claim, bool replace, CancellationToken ct)
        {
            var temporary = _claimPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            var bytes = JsonSerializer.SerializeToUtf8Bytes(claim);
            await using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await file.WriteAsync(bytes, ct).ConfigureAwait(false);
                file.Flush(true);
            }
            ct.ThrowIfCancellationRequested();
            File.Move(temporary, _claimPath, replace);
            if (ReadClaim() != claim) throw new InvalidOperationException("The database migration claim was not acknowledged after saving.");
        }
        private bool DatabaseExists() { CheckTarget(); return _provider.DatabaseExists(); }
        private bool OwnerMatches(Claim claim) { CheckTarget(); return _provider.OwnerMatches(claim.Owner); }
        /// <summary>Read-only status. No claim, catalog or schema is created.</summary>
        public bool Exists
        {
            get { CheckTarget(); var claim = ReadClaim(); return claim != null && DatabaseExists() && OwnerMatches(claim); }
        }
        public Guid Owner { get { CheckTarget(); return ReadClaim()?.Owner ?? Guid.Empty; } }
        public async Task PrepareAsync(bool createIfMissing, bool allowExistingUnowned, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested(); CheckTarget();
            if (allowExistingUnowned && !File.Exists(_claimPath) && DatabaseExists()) return;
            if (createIfMissing) await EnsureCreatedAsync(ct).ConfigureAwait(false);
            else if (!Exists) throw new InvalidOperationException("The owned database target is unavailable. Normal boot will not recreate it.");
            ct.ThrowIfCancellationRequested();
        }
        public async Task EnsureCreatedAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested(); CheckTarget();
            Directory.CreateDirectory(Path.GetDirectoryName(_claimPath));
            await using var lease = new FileStream(_claimPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var claim = ReadClaim();
            if (claim == null)
            {
                claim = new Claim(2, _identity.Server, _identity.Database, Guid.NewGuid(), false, _identity.Provider, _identity.OwnershipScope);
                await SaveClaimAsync(claim, false, ct).ConfigureAwait(false);
            }
            ct.ThrowIfCancellationRequested();
            if (DatabaseExists())
            {
                if (!OwnerMatches(claim))
                    throw new InvalidOperationException("The database already exists without this migration's ownership stamp. No database was adopted or replaced.");
                return;
            }
            if (claim.CreationDispatched)
                throw new InvalidOperationException("An earlier database creation intent may have been dispatched. Reconcile the existing claim and target before retrying; no database was recreated.");
            claim = claim with { CreationDispatched = true };
            await SaveClaimAsync(claim, true, ct).ConfigureAwait(false);
            CheckTarget();
            await _provider.CreateDatabaseAsync(ct).ConfigureAwait(false);
            // Once creation is acknowledged, settle ownership even if the caller cancels.
            // Native operations remain timeout-bounded. Any uncertain outcome keeps dispatched intent.
            CheckTarget();
            await _provider.StampOwnerAsync(claim.Owner, CancellationToken.None).ConfigureAwait(false);
            if (!OwnerMatches(claim)) throw new InvalidOperationException("Native database ownership was not confirmed after creation. Preserve the target for reconciliation.");
            ct.ThrowIfCancellationRequested();
        }
    }
}
