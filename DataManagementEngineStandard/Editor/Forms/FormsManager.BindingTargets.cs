using System;
using TheTechIdea.Beep.Editor.Forms.Hosts;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager;

public partial class FormsManager : IFormsBindingTargets
{
    public bool TryCaptureBindingTarget(string blockName, out FormBindingTarget target)
    {
        target = null;
        using var admission = TryEnterCallback();
        if (admission == null) return false;
        try
        {
            if (_securityManager != null && _securityManager is not IQuerySecuritySnapshotProvider) return false;
            var securityRevision = (_securityManager as IQuerySecuritySnapshotProvider)?.SecurityRevision;
            RegistrationLease registration;
            lock (_registrationGate) _registrations.TryGetValue(blockName, out registration);
            if (registration == null || !IsBufferAuthorized(registration)) return false;
            var record = CaptureRecordTarget(blockName, null, "Binding", trackRequest: false);
            target = new FormBindingTarget(this, record, _commitFormInstanceId, record.Registration.Identity,
                record.Registration.Name, record.Record, securityRevision);
            if (!IsBindingTargetCurrent(target)) { target = null; return false; }
            return true;
        }
        catch { target = null; return false; }
    }

    public bool IsBindingTargetCurrent(FormBindingTarget target, bool includeRecord = true)
    {
        if (target == null || !ReferenceEquals(target.Owner, this) || target.Evidence is not RecordTarget record) return false;
        using var admission = TryEnterCallback();
        if (admission == null) return false;
        if (!includeRecord) return CanDispatchRegistration(record.Registration);
        try
        {
            return BindingPolicyCurrent(target) && IsBufferAuthorized(record.Registration) &&
                RecordTargetCurrentSafely(record, default) && BindingPolicyCurrent(target);
        }
        catch { return false; }
    }

    private bool BindingPolicyCurrent(FormBindingTarget target) => target.SecurityRevision == null ? _securityManager == null :
        _securityManager is IQuerySecuritySnapshotProvider provider && provider.SecurityRevision == target.SecurityRevision;

    public FormViewDeliveryResult ApplyBindingValue(FormBindingTarget target, string fieldName, object value)
    {
        var result = new FormViewDeliveryResult { State = FormViewDeliveryState.Superseded };
        using var admission = TryEnterCallback();
        if (admission == null || !IsBindingTargetCurrent(target)) return result;
        try
        {
            if (string.IsNullOrWhiteSpace(fieldName) || target.Record == null) throw new EditorDeniedException();
            var captured = (RecordTarget)target.Evidence;
            // A successful write must invalidate older public tokens, not mutate their private evidence.
            var write = new RecordTarget { Registration = captured.Registration, Record = captured.Record,
                Units = captured.Units, Field = fieldName, Revision = captured.Revision, QueryRevision = captured.QueryRevision,
                UnitRevision = captured.UnitRevision, HasUnitRevision = captured.HasUnitRevision, Mode = captured.Mode,
                TrackRequest = false };
            foreach (var pair in captured.Values) write.Values.Add(pair.Key, CopyRecordTargetValue(pair.Value));
            foreach (var pair in captured.Items) write.Items.Add(pair.Key, pair.Value);
            VerifyEditorEditable(write);
            VerifyRecordTarget(write, _operationLifetime.Token);
            if (!BindingPolicyCurrent(target)) throw new SupersededRecordOperationException();
            result.EffectsPossible = true;
            if (!WriteCapturedRecordField(write, fieldName, CopyRecordTargetValue(value), _operationLifetime.Token,
                    () => result.Acknowledged = true)) throw new InvalidOperationException("Binding setter did not acknowledge the write.");
            VerifyEditorEditable(write);
            VerifyRecordTarget(write, _operationLifetime.Token);
            if (!BindingPolicyCurrent(target)) throw new SupersededRecordOperationException();
            result.State = FormViewDeliveryState.Delivered;
        }
        catch (Exception ex)
        {
            result.Exception = ex;
            result.State = _disposed ? FormViewDeliveryState.Cancelled :
                ex is SupersededRecordOperationException ? FormViewDeliveryState.Superseded :
                ex is EditorDeniedException ? FormViewDeliveryState.Rejected : FormViewDeliveryState.Failed;
        }
        return result;
    }
}
