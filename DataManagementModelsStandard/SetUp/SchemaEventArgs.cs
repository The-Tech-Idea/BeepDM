using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Published by the schema UI step when its migration plan summary changes.
    /// </summary>
    public sealed class SchemaSummaryEventArgs : EventArgs
    {
        public SchemaSummaryEventArgs(int pendingMigrations, int entityTypeCount, string message)
        {
            PendingMigrations = pendingMigrations;
            EntityTypeCount = entityTypeCount;
            Message = message;
        }

        public int PendingMigrations { get; }
        public int EntityTypeCount { get; }
        public string Message { get; }
    }

    /// <summary>
    /// Result of evaluating a schema migration plan. The canonical
    /// <see cref="SchemaSetupStep"/> produces a richer report; this is a UI-side
    /// summary used by the shell to display pending-migration counts and policy state.
    /// </summary>
    public sealed class MigrationSummary
    {
        public int TotalPendingMigrations { get; set; }
        public bool HasPendingMigrations { get; set; }
        public bool IsValid { get; set; }
        public string? PolicyResult { get; set; }
    }
}
