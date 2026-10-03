using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        private readonly AsyncLocal<RecordWriteObservation> _recordWriteObservation = new();

        private sealed class RecordTarget
        {
            internal RegistrationLease Registration;
            internal object Record, Units;
            internal string Field, RequestKey;
            internal long Request, Revision, QueryRevision, UnitRevision;
            internal long ValidationRevision;
            internal bool HasUnitRevision, ForValidation, TrackRequest = true;
            internal DataBlockMode Mode;
            internal readonly Dictionary<string, object> Values = new(StringComparer.OrdinalIgnoreCase);
            internal readonly Dictionary<string, object> Items = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class RecordWriteObservation
        {
            internal RecordTarget Target;
            internal string Field;
            internal bool Active = true, Foreign;
            internal int Events;
        }

        private void ObserveRecordChange(RegistrationLease registration, object record = null, string field = null)
        {
            lock (_registrationGate)
            {
                if (!CanDispatchRegistration(registration)) return;
                registration.RecordRevision++;
                var write = _recordWriteObservation.Value;
                if (write?.Active == true && ReferenceEquals(write.Target.Registration, registration))
                    if (!ReferenceEquals(write.Target.Record, record) ||
                        !string.Equals(write.Field, field, StringComparison.OrdinalIgnoreCase) || ++write.Events > 1)
                        write.Foreign = true;
            }
        }

        private static object CopyRecordTargetValue(object value) => value is byte[] bytes ? bytes.ToArray() : value;

        private static bool SameRecordTargetValue(object expected, object value) => expected is byte[] bytes
            ? value is byte[] current && bytes.SequenceEqual(current)
            : expected == null ? value == null : expected is string || expected.GetType().IsValueType
                ? expected.Equals(value) : ReferenceEquals(expected, value);

        private RecordTarget CaptureRecordTarget(string blockName, string field, string operation, bool trackRequest = true)
        {
            RecordTarget target;
            lock (_registrationGate)
            {
                if (_disposed || string.IsNullOrWhiteSpace(blockName) ||
                    !_registrations.TryGetValue(blockName, out var registration) || !CanDispatchRegistration(registration))
                    throw new InvalidOperationException("Record operation has no current block registration.");
                var key = operation + ":" + field;
                registration.RecordRequests.TryGetValue(key, out var request);
                if (trackRequest) registration.RecordRequests[key] = ++request;
                target = new RecordTarget { Registration = registration, Field = field, RequestKey = key,
                    Request = request, Revision = registration.RecordRevision, QueryRevision = registration.QueryRevision,
                    Mode = registration.Block.Mode, TrackRequest = trackRequest };
                target.ForValidation = operation == "Validation";
                if (target.ForValidation) target.ValidationRevision = ++registration.ValidationRevision;
            }
            var unit = target.Registration.Source;
            if (unit is IUnitofWorkRecordRevision revisions && revisions.SupportsRecordRevision)
            {
                target.HasUnitRevision = true;
                if (!revisions.TryGetRecordRevision(out target.UnitRevision))
                    throw new InvalidOperationException("Record revision source is unavailable or disposed.");
            }
            target.Units = unit.Units;
            target.Record = unit.CurrentItem;
            var fields = (target.Registration.Block.EntityStructure?.Fields?.Select(f => f.FieldName) ?? Enumerable.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n)).ToArray();
            var itemNames = fields.Concat(_itemPropertyManager?.GetAllItems(target.Registration.Name)?.Select(i => i.ItemName) ?? Enumerable.Empty<string>())
                .Concat(string.IsNullOrEmpty(field) ? Array.Empty<string>() : new[] { field })
                .Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var name in itemNames) target.Items[name] = _itemPropertyManager?.GetItem(target.Registration.Name, name);
            if (target.Record != null)
            {
                foreach (var name in fields.Concat(string.IsNullOrEmpty(field) ? Array.Empty<string>() : new[] { field }).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!RecordPropertyAccessor.TryGetValue(target.Record, name, out var value, _dmeEditor))
                        throw new InvalidOperationException($"Cannot capture record field '{name}'.");
                    target.Values[name] = CopyRecordTargetValue(value);
                }
            }
            VerifyRecordTarget(target, default);
            return target;
        }

        private bool RecordTargetIdentityCurrent(RecordTarget target)
        {
            lock (_registrationGate)
                return target != null && CanDispatchRegistration(target.Registration) &&
                    target.Registration.QueryRevision == target.QueryRevision &&
                    target.Registration.Block.Mode == target.Mode &&
                    (!target.ForValidation || target.Registration.ValidationRevision == target.ValidationRevision) &&
                    (!target.TrackRequest || target.Registration.RecordRequests.TryGetValue(target.RequestKey, out var request) && request == target.Request);
        }

        private bool RecordTargetCurrent(RecordTarget target, CancellationToken ct, bool ignoreRevision = false)
        {
            if (ct.IsCancellationRequested || !RecordTargetIdentityCurrent(target)) return false;
            var unit = target.Registration.Source;
            // Getters/helpers can call host code: read outside ownership monitors and recheck afterward.
            if (!ReferenceEquals(unit.Units, target.Units) || !ReferenceEquals(unit.CurrentItem, target.Record)) return false;
            foreach (var item in target.Items)
                if (!ReferenceEquals(_itemPropertyManager?.GetItem(target.Registration.Name, item.Key), item.Value)) return false;
            foreach (var pair in target.Values)
                if (!RecordPropertyAccessor.TryGetValue(target.Record, pair.Key, out var value, _dmeEditor) || !SameRecordTargetValue(pair.Value, value)) return false;
            if (!ignoreRevision && target.HasUnitRevision &&
                (unit is not IUnitofWorkRecordRevision revisions || !revisions.TryGetRecordRevision(out var revision) || revision != target.UnitRevision)) return false;
            lock (_registrationGate)
                return !ct.IsCancellationRequested && RecordTargetIdentityCurrent(target) &&
                    (ignoreRevision || target.Registration.RecordRevision == target.Revision);
        }

        private void VerifyRecordTarget(RecordTarget target, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!RecordTargetCurrent(target, ct)) throw new SupersededRecordOperationException();
        }

        private bool RecordTargetCurrentSafely(RecordTarget target, CancellationToken ct)
        {
            try { return RecordTargetCurrent(target, ct); }
            catch { return false; }
        }

        private sealed class SupersededRecordOperationException : InvalidOperationException
        {
            internal SupersededRecordOperationException() : base("Record, registration, field or request changed while the operation was pending.") { }
        }

        private bool WriteCapturedRecordField(RecordTarget target, string field, object value, CancellationToken ct,
            Action acknowledged)
        {
            VerifyRecordTarget(target, ct);
            if (!target.Values.ContainsKey(field)) throw new InvalidOperationException($"Record target field '{field}' was not captured.");
            var prior = _recordWriteObservation.Value;
            var write = new RecordWriteObservation { Target = target, Field = field };
            _recordWriteObservation.Value = write;
            try
            {
                if (!SetFieldValue(target.Record, field, value)) return false;
                acknowledged();
                if (!RecordPropertyAccessor.TryGetValue(target.Record, field, out var accepted, _dmeEditor))
                    throw new SupersededRecordOperationException();
                target.Values[field] = CopyRecordTargetValue(accepted);
                // Keep observation active through getters; reentrant or independent ABA is not an own-write acknowledgement.
                if (write.Foreign || !RecordTargetCurrent(target, ct, ignoreRevision: true))
                    throw new SupersededRecordOperationException();
                if (target.HasUnitRevision)
                {
                    if (!((IUnitofWorkRecordRevision)target.Registration.Source).TryGetRecordRevision(out var revision) ||
                        revision != target.UnitRevision + write.Events)
                        throw new SupersededRecordOperationException();
                    target.UnitRevision = revision;
                }
                lock (_registrationGate)
                {
                    if (write.Foreign || target.Registration.RecordRevision != target.Revision + write.Events)
                        throw new SupersededRecordOperationException();
                    target.Revision = target.Registration.RecordRevision;
                }
                VerifyRecordTarget(target, ct);
                if (write.Foreign) throw new SupersededRecordOperationException();
                return true;
            }
            finally { write.Active = false; _recordWriteObservation.Value = prior; }
        }
    }
}
