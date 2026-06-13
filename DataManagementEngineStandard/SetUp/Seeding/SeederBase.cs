using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor.Defaults;
using static TheTechIdea.Beep.SetUp.StepErrorHelpers;

namespace TheTechIdea.Beep.SetUp.Seeding
{
    /// <summary>
    /// Abstract base for seeders. Provides:
    /// <list type="bullet">
    ///   <item>Default idempotency check via row-count query on <see cref="TargetEntityName"/>.</item>
    ///   <item><see cref="DefaultsManager"/> initialisation before <see cref="SeedCore"/> runs.</item>
    ///   <item>Exception catch-all so seeders never propagate throws.</item>
    /// </list>
    /// </summary>
    public abstract class SeederBase : ISeeder
    {
        /// <summary>
        /// Optional logger for diagnostic output. Set via constructor in derived classes.
        /// When null, errors are silently swallowed (backward-compatible).
        /// </summary>
        protected ILogger? Logger { get; set; }

        /// <inheritdoc/>
        public abstract string SeederId { get; }

        /// <inheritdoc/>
        public abstract string SeederName { get; }

        /// <inheritdoc/>
        public virtual IReadOnlyList<string> DependsOn => Array.Empty<string>();

        /// <summary>Name of the primary entity/table written by this seeder, used by the default idempotency check.</summary>
        protected abstract string TargetEntityName { get; }

        /// <summary>
        /// Default idempotency guard: returns <c>true</c> when <see cref="TargetEntityName"/> exists
        /// and contains at least one row.
        /// Override for a more specific check (e.g. look for a sentinel record).
        /// </summary>
        public virtual bool IsAlreadySeeded(IDataSource dataSource, IDMEEditor editor)
        {
            if (!dataSource.CheckEntityExist(TargetEntityName)) return false;
            var rows = dataSource.GetEntity(TargetEntityName, null);
            return rows?.Any() ?? false;
        }

        /// <inheritdoc/>
        public IErrorsInfo Seed(IDataSource dataSource, IDMEEditor editor,
            IProgress<PassedArgs> progress = null)
        {
            // Ensure defaults (audit/timestamp fields) are wired before inserting
            DefaultsManager.Initialize(editor);

            try
            {
                return SeedCore(dataSource, editor, progress);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Seeder '{SeederId}' threw an unhandled exception", SeederId);
                return Fail($"Seeder '{SeederId}' threw an unhandled exception: {ex.Message}", ex);
            }
        }

        /// <summary>Implement the actual insert logic here.</summary>
        protected abstract IErrorsInfo SeedCore(IDataSource dataSource, IDMEEditor editor,
            IProgress<PassedArgs> progress);
    }
}
