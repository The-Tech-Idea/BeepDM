using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager : IFormsLovOutcomes
    {
        private readonly AsyncLocal<int> _lovOperationDepth = new();
        public async Task<FormLovResult> ShowLOVWithOutcomeAsync(string blockName, string fieldName,
            string searchText = null, object selectedRecord = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var lifetime = TryEnterCallback() ?? throw new ObjectDisposedException(nameof(FormsManager));
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _operationLifetime.Token);
            var ct = cancellation.Token;
            var result = new FormLovResult { Success = false, FormInstanceId = _commitFormInstanceId,
                BlockName = blockName, FieldName = fieldName };
            var applied = new List<string>();
            if (_lovOperationDepth.Value > 0)
            {
                result.State = FormLovState.Failed;
                result.ErrorMessage = "Awaited nested LOV operations from their own triggers/providers are not supported.";
                return result;
            }
            _lovOperationDepth.Value++;
            try
            {
                if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("LOV field name is required.", nameof(fieldName));
                var target = CaptureRecordTarget(blockName, fieldName, "LOV");
                blockName = target.Registration.Name;
                result.BlockName = blockName;
                result.RegistrationId = target.Registration.Identity;
                result.RequestRevision = target.Request;
                if (selectedRecord != null && target.Record == null) throw new InvalidOperationException("LOV selection has no captured current record.");
                var definition = _lovManager.GetLOV(blockName, fieldName);
                if (definition == null) throw new InvalidOperationException($"No LOV registered for {blockName}.{fieldName}");
                var selection = new LOVDefinition { ReturnField = definition.ReturnField,
                    RelatedFieldMappings = new Dictionary<string, string>(definition.RelatedFieldMappings ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase) };
                void Verify()
                {
                    VerifyRecordTarget(target, ct);
                    if (!ReferenceEquals(_lovManager.GetLOV(blockName, fieldName), definition) ||
                        definition.ReturnField != selection.ReturnField ||
                        (definition.RelatedFieldMappings?.Count ?? 0) != selection.RelatedFieldMappings.Count ||
                        selection.RelatedFieldMappings.Any(p => definition.RelatedFieldMappings == null ||
                            !definition.RelatedFieldMappings.TryGetValue(p.Key, out var value) || value != p.Value))
                        throw new SupersededRecordOperationException();
                    VerifyRecordTarget(target, ct);
                }
                Verify();
                var writes = Array.Empty<KeyValuePair<string, object>>();
                if (selectedRecord != null)
                {
                    var values = _lovManager.GetRelatedFieldValues(selection, selectedRecord)
                        ?? throw new InvalidOperationException("LOV helper returned no selection mapping.");
                    writes = values.Select(p => new KeyValuePair<string, object>(
                        string.Equals(p.Key, "__RETURN_VALUE__", StringComparison.Ordinal) ? fieldName : p.Key,
                        CopyRecordTargetValue(p.Value))).ToArray();
                    if (writes.Any(p => string.IsNullOrWhiteSpace(p.Key) || !target.Values.ContainsKey(p.Key)) ||
                        writes.Select(p => p.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != writes.Length)
                        throw new InvalidOperationException("LOV selection contains uncaptured or duplicate target fields.");
                    Verify();
                }
                var context = TriggerContext.ForItem(TriggerType.WhenLOVValidation, blockName, fieldName, null, null, _dmeEditor);
                var trigger = await _triggerManager.FireBlockTriggerAsync(TriggerType.WhenLOVValidation, blockName, context, ct).ConfigureAwait(false);
                Verify();
                if (trigger == TriggerResult.Cancelled)
                {
                    result.State = FormLovState.Cancelled;
                    result.ErrorMessage = "LOV cancelled by WHEN-LOV-VALIDATION trigger.";
                    return result;
                }
                if (trigger != TriggerResult.Success && trigger != TriggerResult.Skipped)
                    throw new InvalidOperationException($"WHEN-LOV-VALIDATION did not succeed: {trigger}.");
                result.HelperEffectsPossible = true;
                var loaded = await _lovManager.LoadLOVDataAsync(blockName, fieldName, searchText).ConfigureAwait(false);
                result.LoadAcknowledged = loaded != null;
                Verify();
                if (loaded == null || !loaded.Success) throw new InvalidOperationException(loaded?.ErrorMessage ?? "LOV load returned no result.");
                if (selectedRecord != null)
                {
                    foreach (var write in writes)
                    {
                        Verify();
                        result.SelectionEffectsPossible = true;
                        if (!WriteCapturedRecordField(target, write.Key, write.Value, ct, () =>
                        { applied.Add(write.Key); result.AppliedFields = applied.ToArray(); }))
                            throw new InvalidOperationException($"LOV setter did not acknowledge field '{write.Key}'.");
                    }
                    Verify();
                    result.SelectionApplied = true;
                }
                result.Records = new List<object>(loaded.Records ?? new List<object>());
                result.TotalCount = loaded.TotalCount;
                result.FromCache = loaded.FromCache;
                result.LoadTimeMs = loaded.LoadTimeMs;
                Verify();
                result.State = FormLovState.Completed;
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.State = ct.IsCancellationRequested ? FormLovState.Cancelled :
                    ex is SupersededRecordOperationException ? FormLovState.Superseded : FormLovState.Failed;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;
                return result;
            }
            finally { _lovOperationDepth.Value--; }
        }
    }
}
