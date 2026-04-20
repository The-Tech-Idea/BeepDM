using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Services.Audit.Bridges
{
    /// <summary>
    /// Convenience extensions over <see cref="IAuditStore"/> that persist
    /// the legacy entry **and** forward an equivalent
    /// <see cref="Models.AuditEvent"/> through <see cref="IBeepAudit"/>.
    /// Use these helpers to migrate call sites from
    /// <c>store.Save(entry)</c> to a dual-write pattern without breaking
    /// existing query / export behaviour.
    /// </summary>
    /// <remarks>
    /// The legacy save runs first and is unconditional; the bridge step
    /// runs only when a non-null <paramref name="bridge"/> is supplied
    /// (typically resolved from DI by <see cref="AuditBridgeRegistry"/>).
    /// Bridge failures are absorbed inside
    /// <see cref="FormsAuditBridge.Forward"/> so callers see the same
    /// behaviour as the original <see cref="IAuditStore.Save"/>.
    /// </remarks>
    public static class AuditStoreSaveExtensions
    {
        /// <summary>
        /// Persists <paramref name="entry"/> through the legacy store and,
        /// when a bridge is supplied, mirrors it into the unified audit
        /// pipeline.
        /// </summary>
        /// <param name="store">Legacy store; may not be <c>null</c>.</param>
        /// <param name="entry">Legacy entry to persist.</param>
        /// <param name="bridge">Optional unified-pipeline bridge.</param>
        public static void SaveAndForward(
            this IAuditStore store,
            AuditEntry entry,
            FormsAuditBridge bridge)
        {
            store?.Save(entry);
            bridge?.Forward(entry);
        }
    }
}
