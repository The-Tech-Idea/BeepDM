using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    public enum FormBlockCommitState
    {
        Unattempted, Prepared, Committed, RolledBack, FailedBeforeWrite, PartiallyApplied, Unknown
    }

    public enum FormDataSourceCommitState { Unattempted, Opened, Independent, Committed, RolledBack, Unknown }

    public sealed class FormDataSourceCommitResult
    {
        public System.Guid InstanceId { get; internal set; }
        public string DataSourceName { get; internal set; }
        public FormDataSourceCommitState State { get; internal set; }
    }

    public sealed class FormBlockCommitResult
    {
        public System.Guid FormInstanceId { get; internal set; }
        public string FormName { get; internal set; }
        public string BlockName { get; internal set; }
        public string DataSourceName { get; internal set; }
        public FormBlockCommitState State { get; internal set; }
        public string Message { get; internal set; }
        /// <summary>Present only when provider reconciliation is required; never blindly replay.</summary>
        public IUnitofWorkCommitStage Reconciliation { get; internal set; }
    }

    /// <summary>Durability and notification outcomes returned through the legacy error contract.</summary>
    public sealed class FormCommitResult : ErrorsInfo
    {
        public IReadOnlyList<FormBlockCommitResult> Blocks { get; internal set; } = new List<FormBlockCommitResult>();
        public IReadOnlyList<FormDataSourceCommitResult> DataSources { get; internal set; } = new List<FormDataSourceCommitResult>();
        public bool UsesIndependentCommits { get; internal set; }
        public bool RequiresReconciliation => Blocks.Any(b => b.State == FormBlockCommitState.Unknown ||
            b.State == FormBlockCommitState.PartiallyApplied || (b.Reconciliation != null && !b.Reconciliation.IsResolved)) ||
            DataSources.Any(d => d.State == FormDataSourceCommitState.Unknown);
        public bool HasPartialCommit => Blocks.Any(b => b.State == FormBlockCommitState.Committed) && !AllWritesCommitted;
        public bool AllWritesCommitted => Blocks.Count > 0 && Blocks.All(b => b.State == FormBlockCommitState.Committed);
    }
}
