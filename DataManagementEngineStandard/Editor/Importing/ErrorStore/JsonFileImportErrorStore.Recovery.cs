using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor.Importing.ErrorStore
{
    public sealed partial class JsonFileImportErrorStore
    {
        internal static string Context(DataImportConfiguration config) =>
            config.SourceDataSourceName + "/" + config.SourceEntityName + "->" + config.DestDataSourceName + "/" + config.DestEntityName;

        private static InvalidDataException Invalid() => new("Reject identity, state, revision or recovery evidence is invalid; preserve it and reconcile explicitly.");
        private static bool Identity(string value) => Guid.TryParseExact(value, "N", out var id) && id.ToString("N") == value;
        private static bool Text(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 1024;

        private static void ValidateRecord(ImportErrorRecord record)
        {
            var recovery = record.Recovery;
            if (recovery == null) return;
            if (recovery.FormatVersion != 1 || !Identity(recovery.RejectId) || !Text(recovery.RunId) ||
                !Text(recovery.SourceDataSourceName) || !Text(recovery.SourceEntityName) || !Text(recovery.DestinationDataSourceName) ||
                !Text(recovery.DestinationEntityName) || !Enum.IsDefined(typeof(ImportRejectState), recovery.State) || recovery.Revision < 0 ||
                !string.Equals(record.ContextKey, recovery.SourceDataSourceName + "/" + recovery.SourceEntityName + "->" +
                    recovery.DestinationDataSourceName + "/" + recovery.DestinationEntityName, StringComparison.Ordinal)) throw Invalid();
            ImportRejectPayload.Decode(recovery.OriginalDestinationPayload);
            if (recovery.PreparedDestinationPayload != null) ImportRejectPayload.Decode(recovery.PreparedDestinationPayload);
            if (recovery.Revision == 0 && (recovery.State != ImportRejectState.Pending || recovery.PreparedDestinationPayload != null ||
                recovery.OperatorId != null || recovery.ClaimId != null || recovery.ClaimOwner != null || recovery.ClaimedAt != null ||
                recovery.ReconciliationReference != null)) throw Invalid();
            if (recovery.State == ImportRejectState.Prepared &&
                (recovery.ClaimId != null || recovery.ClaimOwner != null || recovery.ClaimedAt != null)) throw Invalid();
            if (recovery.State == ImportRejectState.Dismissed && (!Text(recovery.OperatorId) || recovery.Revision < 1)) throw Invalid();
            if (recovery.State != ImportRejectState.Pending && recovery.State != ImportRejectState.Dismissed &&
                (!Text(recovery.OperatorId) || recovery.PreparedDestinationPayload == null || recovery.Revision < 1)) throw Invalid();
            if ((recovery.State == ImportRejectState.Claimed || recovery.State == ImportRejectState.ReconciliationRequired ||
                recovery.State == ImportRejectState.Acknowledged || recovery.State == ImportRejectState.ReconciledApplied) &&
                (!Identity(recovery.ClaimId) || !Text(recovery.ClaimOwner) || recovery.ClaimedAt == null)) throw Invalid();
            bool applied = recovery.State == ImportRejectState.Acknowledged || recovery.State == ImportRejectState.ReconciledApplied;
            if (record.Replayed != applied || applied != record.ReplayedAt.HasValue ||
                recovery.State == ImportRejectState.Dismissed && !Text(recovery.ReconciliationReference) ||
                recovery.State == ImportRejectState.ReconciledApplied && !Text(recovery.ReconciliationReference)) throw Invalid();
        }

        private static void ValidateIdentities(IReadOnlyList<ImportErrorRecord> records)
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            if (records.Any(record => record.Recovery != null && !identities.Add(record.Recovery.RejectId))) throw Invalid();
        }

        private static void ValidateJson(JsonElement value)
        {
            ImportRejectPayload.CheckDuplicates(value);
            if (!value.TryGetProperty(nameof(ImportErrorRecord.Recovery), out var recovery) || recovery.ValueKind == JsonValueKind.Null) return;
            if (recovery.ValueKind != JsonValueKind.Object) throw Invalid();
            foreach (var name in new[] { "RejectId", "RunId", "SourceDataSourceName", "SourceEntityName", "DestinationDataSourceName", "DestinationEntityName", "OriginalDestinationPayload" })
                if (!recovery.TryGetProperty(name, out var field) || field.ValueKind != JsonValueKind.String) throw Invalid();
            foreach (var name in new[] { "FormatVersion", "State", "Revision" })
                if (!recovery.TryGetProperty(name, out var field) || field.ValueKind != JsonValueKind.Number || !field.TryGetInt32(out _)) throw Invalid();
            if (!value.TryGetProperty("Replayed", out var replayed) || (replayed.ValueKind != JsonValueKind.False && replayed.ValueKind != JsonValueKind.True)) throw Invalid();
        }

        public async Task ValidateRecoveryAsync(string contextKey, CancellationToken token = default) =>
            _ = await _store.ReadAsync(contextKey, token).ConfigureAwait(false);

        public async Task<ImportErrorRecord> LoadRejectAsync(string contextKey, string rejectId, CancellationToken token = default)
        {
            var records = await _store.ReadAsync(contextKey, token).ConfigureAwait(false);
            return records.SingleOrDefault(record => record.Recovery?.RejectId == rejectId) ?? throw Invalid();
        }

        private async Task<ImportErrorRecord> Transition(string context, string id, int revision,
            Action<ImportErrorRecord, ImportRejectRecovery> change, CancellationToken token)
        {
            ImportErrorRecord selected = null;
            await _store.MutateAsync(context, records =>
            {
                selected = records.SingleOrDefault(record => record.Recovery?.RejectId == id) ?? throw Invalid();
                if (selected.Recovery.Revision != revision) throw Invalid();
                change(selected, selected.Recovery);
                selected.Recovery.Revision = checked(revision + 1);
            }, token).ConfigureAwait(false);
            return selected;
        }

        public Task<ImportErrorRecord> PrepareReplayAsync(string contextKey, string rejectId, int expectedRevision,
            string operatorId, object correctedDestinationRecord = null, CancellationToken token = default)
        {
            if (!Text(operatorId)) throw Invalid();
            var correction = correctedDestinationRecord == null ? null : ImportRejectPayload.Capture(correctedDestinationRecord);
            return Transition(contextKey, rejectId, expectedRevision, (_, recovery) =>
            {
                if (recovery.State != ImportRejectState.Pending && recovery.State != ImportRejectState.Prepared) throw Invalid();
                recovery.PreparedDestinationPayload = correction ?? recovery.PreparedDestinationPayload ?? recovery.OriginalDestinationPayload;
                recovery.OperatorId = operatorId; recovery.ClaimId = null; recovery.ClaimOwner = null; recovery.ClaimedAt = null;
                recovery.State = ImportRejectState.Prepared;
            }, token);
        }

        public Task<ImportErrorRecord> ClaimReplayAsync(string contextKey, string rejectId, int expectedRevision,
            string ownerId, CancellationToken token = default)
        {
            if (!Text(ownerId)) throw Invalid();
            return Transition(contextKey, rejectId, expectedRevision, (_, recovery) =>
            {
                if (recovery.State != ImportRejectState.Prepared) throw Invalid();
                recovery.State = ImportRejectState.Claimed; recovery.ClaimId = Guid.NewGuid().ToString("N");
                recovery.ClaimOwner = ownerId; recovery.ClaimedAt = DateTime.UtcNow;
            }, token);
        }

        public async Task CompleteReplayAsync(string contextKey, string rejectId, int expectedRevision, string claimId,
            ImportReplayDisposition disposition, CancellationToken token = default)
        {
            if (!Enum.IsDefined(typeof(ImportReplayDisposition), disposition)) throw Invalid();
            await Transition(contextKey, rejectId, expectedRevision, (record, recovery) =>
            {
                if (recovery.State != ImportRejectState.Claimed || recovery.ClaimId != claimId) throw Invalid();
                recovery.State = disposition switch
                {
                    ImportReplayDisposition.Acknowledged => ImportRejectState.Acknowledged,
                    ImportReplayDisposition.Uncertain => ImportRejectState.ReconciliationRequired,
                    _ => ImportRejectState.Pending
                };
                record.Replayed = disposition == ImportReplayDisposition.Acknowledged;
                record.ReplayedAt = record.Replayed ? DateTime.UtcNow : null;
            }, token).ConfigureAwait(false);
        }

        public async Task ReconcileReplayAsync(string contextKey, string rejectId, int expectedRevision, string claimId,
            string operatorId, ImportReplayProviderConfirmation confirmation, string evidenceReference, CancellationToken token = default)
        {
            if (!Text(operatorId) || !Text(evidenceReference) || !Enum.IsDefined(typeof(ImportReplayProviderConfirmation), confirmation)) throw Invalid();
            await Transition(contextKey, rejectId, expectedRevision, (record, recovery) =>
            {
                if ((recovery.State != ImportRejectState.Claimed && recovery.State != ImportRejectState.ReconciliationRequired) || recovery.ClaimId != claimId) throw Invalid();
                recovery.OperatorId = operatorId; recovery.ReconciliationReference = evidenceReference;
                recovery.State = confirmation == ImportReplayProviderConfirmation.Applied ? ImportRejectState.ReconciledApplied : ImportRejectState.Pending;
                record.Replayed = confirmation == ImportReplayProviderConfirmation.Applied;
                record.ReplayedAt = record.Replayed ? DateTime.UtcNow : null;
            }, token).ConfigureAwait(false);
        }

        public async Task DismissRejectAsync(string contextKey, string rejectId, int expectedRevision,
            string operatorId, string evidenceReference, CancellationToken token = default)
        {
            if (!Text(operatorId) || !Text(evidenceReference)) throw Invalid();
            await Transition(contextKey, rejectId, expectedRevision, (_, recovery) =>
            {
                if (recovery.State != ImportRejectState.Pending && recovery.State != ImportRejectState.Prepared) throw Invalid();
                recovery.State = ImportRejectState.Dismissed; recovery.OperatorId = operatorId; recovery.ReconciliationReference = evidenceReference;
            }, token).ConfigureAwait(false);
        }
    }
}
