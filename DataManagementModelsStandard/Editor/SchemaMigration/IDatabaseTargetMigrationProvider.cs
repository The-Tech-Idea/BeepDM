using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.SchemaMigration
{
    /// <summary>Immutable catalog identity captured before any provisioning work.</summary>
    public sealed record DatabaseTargetMigrationIdentity(string Provider, string Server, string Database, string OwnershipScope);

    /// <summary>Explicit compatibility mapping for an existing unscoped v1 receipt.</summary>
    public sealed record DatabaseTargetLegacyClaimBinding(string Provider, string OwnershipScope);

    /// <summary>Optional native catalog capability used only by MigrationManager's target lifecycle.
    /// Entity/schema migrations still use ISchemaMigrationProvider. No caller-supplied SQL is accepted.</summary>
    public interface IDatabaseTargetMigrationProvider
    {
        DatabaseTargetMigrationIdentity Identity { get; }
        bool DatabaseExists();
        bool OwnerMatches(Guid owner);
        Task CreateDatabaseAsync(CancellationToken cancellationToken);
        Task StampOwnerAsync(Guid owner, CancellationToken cancellationToken);
    }
}
