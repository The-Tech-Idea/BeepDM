using System;

namespace TheTechIdea.Beep.ConfigUtil
{
    /// <summary>
    /// Defines logical storage tiers for connection data.
    /// Shared between engine (BeepConnectionRepository) and public API (IConfigEditor).
    /// </summary>
    public enum ConnectionStorageScope
    {
        Project = 0,
        User = 1,
        Machine = 2
    }

    /// <summary>
    /// Controls how the repository resolves naming collisions during import/promote.
    /// </summary>
    public enum ConnectionConflictPolicy
    {
        Replace = 0,
        Rename = 1,
        Skip = 2,
        MergeByGuid = 3
    }
}
