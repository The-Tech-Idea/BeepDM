using System.Collections.Generic;
using System.Reflection;

namespace TheTechIdea.Beep.SetUp.Seeding
{
    /// <summary>
    /// Single unit of seed work. Implementations insert reference data or initial records
    /// into a datasource. Each seeder is idempotent — it checks
    /// <see cref="IsAlreadySeeded"/> before writing.
    /// </summary>
    public interface ISeeder
    {
        /// <summary>Stable unique identifier (e.g. <c>"roles-seeder"</c>).</summary>
        string SeederId { get; }

        /// <summary>Human-readable display name shown in progress output.</summary>
        string SeederName { get; }

        /// <summary>
        /// SeedIds of seeders that must execute before this one.
        /// Declare foreign-key–style dependencies here so the registry can topologically sort them.
        /// </summary>
        IReadOnlyList<string> DependsOn { get; }

        /// <summary>
        /// Returns <c>true</c> when this seeder's data is already present in the datasource.
        /// When true, <see cref="SeedingStep"/> will skip this seeder.
        /// </summary>
        bool IsAlreadySeeded(IDataSource dataSource, IDMEEditor editor);

        /// <summary>
        /// Insert seed data into <paramref name="dataSource"/>. Must not throw.
        /// Returns <c>Errors.Ok</c> on success.
        /// </summary>
        IErrorsInfo Seed(IDataSource dataSource, IDMEEditor editor,
            System.IProgress<PassedArgs> progress = null);
    }
}
