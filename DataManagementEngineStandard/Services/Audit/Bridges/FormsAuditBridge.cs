using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Services.Audit.Models;
using LegacyAuditEntry = TheTechIdea.Beep.Editor.Forms.Models.AuditEntry;
using LegacyAuditFieldChange = TheTechIdea.Beep.Editor.Forms.Models.AuditFieldChange;
using UnifiedAuditEvent = TheTechIdea.Beep.Services.Audit.Models.AuditEvent;
using UnifiedAuditFieldChange = TheTechIdea.Beep.Services.Audit.Models.AuditFieldChange;

namespace TheTechIdea.Beep.Services.Audit.Bridges
{
    /// <summary>
    /// Adapts a forms-level <see cref="LegacyAuditEntry"/> stream into the
    /// unified <see cref="IBeepAudit"/> pipeline. The bridge is invoked by
    /// the <see cref="AuditStoreSaveExtensions.SaveAndForward"/> helper or by
    /// callers that subscribe to <see cref="IAuditManager"/> commit hooks
    /// directly.
    /// </summary>
    /// <remarks>
    /// The bridge is **append-only** — it does not write back into the
    /// legacy <see cref="IAuditStore"/>. The legacy store keeps its own
    /// life-cycle and the unified pipeline gains a parallel, signed copy
    /// suitable for compliance reporting and tamper-evident replay.
    /// Failures inside <see cref="ForwardAsync"/> are swallowed so a
    /// misbehaving sink can never break the legacy commit path.
    /// </remarks>
    public sealed class FormsAuditBridge
    {
        private const string SourcePrefix = "Forms.Block.";

        private readonly IBeepAudit _audit;

        /// <summary>Creates a new bridge that forwards into the supplied beep audit pipeline.</summary>
        /// <param name="audit">Unified audit pipeline; must not be <c>null</c>.</param>
        public FormsAuditBridge(IBeepAudit audit)
        {
            _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        }

        /// <summary>Synchronous forward used from non-async code paths.</summary>
        /// <param name="entry">Legacy entry produced by <see cref="AuditManager"/>.</param>
        public void Forward(LegacyAuditEntry entry)
        {
            try
            {
                ForwardAsync(entry, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch
            {
                // legacy path must never throw
            }
        }

        /// <summary>Async forward; preferred when the caller is already async.</summary>
        public async Task ForwardAsync(LegacyAuditEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry is null) return;
            if (!_audit.IsEnabled) return;

            UnifiedAuditEvent unified = MapToUnified(entry);
            try
            {
                await _audit.RecordAsync(unified, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // never propagate from the bridge; legacy store has the canonical copy
            }
        }

        private static UnifiedAuditEvent MapToUnified(LegacyAuditEntry entry)
        {
            UnifiedAuditEvent unified = new UnifiedAuditEvent
            {
                EventId = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
                TimestampUtc = entry.Timestamp == default ? DateTime.UtcNow : entry.Timestamp,
                Source = string.Concat(SourcePrefix, entry.BlockName ?? string.Empty),
                EntityName = entry.BlockName,
                RecordKey = entry.RecordKey,
                UserName = entry.UserName,
                Category = AuditCategory.DataAccess,
                Operation = entry.Operation.ToString(),
                Outcome = MapOutcome(entry.Operation),
                Properties = BuildProperties(entry),
                FieldChanges = MapFieldChanges(entry.FieldChanges)
            };
            return unified;
        }

        private static AuditOutcome MapOutcome(AuditOperation op)
        {
            switch (op)
            {
                case AuditOperation.Rollback: return AuditOutcome.Compensated;
                default:                      return AuditOutcome.Success;
            }
        }

        private static IDictionary<string, object> BuildProperties(LegacyAuditEntry entry)
        {
            Dictionary<string, object> bag = new Dictionary<string, object>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(entry.FormName))
            {
                bag["forms.formName"] = entry.FormName;
            }
            if (!string.IsNullOrEmpty(entry.BeforeImage))
            {
                bag["forms.beforeImage"] = entry.BeforeImage;
            }
            if (!string.IsNullOrEmpty(entry.AfterImage))
            {
                bag["forms.afterImage"] = entry.AfterImage;
            }
            return bag.Count == 0 ? null : bag;
        }

        private static IList<UnifiedAuditFieldChange> MapFieldChanges(List<LegacyAuditFieldChange> source)
        {
            if (source is null || source.Count == 0)
            {
                return new List<UnifiedAuditFieldChange>();
            }
            List<UnifiedAuditFieldChange> mapped = new List<UnifiedAuditFieldChange>(source.Count);
            foreach (LegacyAuditFieldChange fc in source)
            {
                if (fc is null) continue;
                mapped.Add(new UnifiedAuditFieldChange(fc.FieldName, fc.OldValue, fc.NewValue));
            }
            return mapped;
        }
    }
}
