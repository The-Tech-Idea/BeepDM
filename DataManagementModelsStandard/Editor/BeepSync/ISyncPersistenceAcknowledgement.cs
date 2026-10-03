using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>Optional acknowledged/cancellable sync storage; acknowledgement is not a provider transaction guarantee.</summary>
    public interface ISyncPersistenceAcknowledgement
    {
        Task<PersistenceWriteResult> SaveSchemasAcknowledgedAsync(IEnumerable<DataSyncSchema> schemas, CancellationToken token = default);
        Task<PersistenceWriteResult> SaveSchemaAcknowledgedAsync(DataSyncSchema schema, CancellationToken token = default);
        Task<PersistenceWriteResult> SaveCheckpointAcknowledgedAsync(SyncCheckpoint checkpoint, CancellationToken token = default);
    }
}
