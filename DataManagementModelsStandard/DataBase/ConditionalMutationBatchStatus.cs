using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Atomic outcomes distinguish acknowledged rollback from uncertain commit.</summary>
    public enum ConditionalMutationBatchStatus
    {
        /// <summary>All operations and commit acknowledged.</summary>
        Applied,
        /// <summary>An observation did not match; rollback acknowledged.</summary>
        NoMatch,
        /// <summary>Refused before mutation dispatch.</summary>
        NotApplied,
        /// <summary>Execution failed or cancelled and rollback acknowledged.</summary>
        RolledBack,
        /// <summary>Commit or rollback may have occurred; reload before another mutation.</summary>
        Unconfirmed
    }
}
