using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        private readonly object _registrationGate = new();
        private readonly Dictionary<string, RegistrationLease> _registrations = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, RegistrationLease> _pendingRegistrations = new(StringComparer.OrdinalIgnoreCase);

        private sealed class RegistrationLease
        {
            private readonly FormsManager _owner;
            private readonly object _gate = new();
            private readonly List<(string Resource, Action Cleanup)> _cleanup = new();
            private bool _preparing = true, _cleaned;
            internal volatile bool Retired, Published, EverPublished;
            internal readonly string Name;
            internal readonly IUnitofWork Source;
            internal DataBlockInfo Block;
            internal long QueryRevision;
            internal long LocalPageRevision;
            internal long RecordRevision;
            internal long ValidationRevision;
            internal BufferAuthorization BufferAuthorization;
            internal readonly Dictionary<string, long> RecordRequests = new(StringComparer.OrdinalIgnoreCase);
            internal readonly Guid Identity = Guid.NewGuid();
            internal RegistrationLease(FormsManager owner, string name, IUnitofWork source)
            { _owner = owner; Name = name; Source = source; }
            internal void Own(string resource, Action cleanup)
            {
                lock (_gate) _cleanup.Add((resource, cleanup));
            }
            internal void Check()
            {
                if (_owner._disposed) throw new ObjectDisposedException(nameof(FormsManager));
                if (Retired) throw new InvalidOperationException("Block registration was retired during preparation.");
            }
            internal void MarkRetired()
            {
                Retired = true;
                Published = false;
                if (Block != null) Block.IsRegistered = false;
            }
            internal bool FinishPreparation()
            {
                lock (_gate) _preparing = false;
                return CleanupIfRetired();
            }
            internal bool CleanupIfRetired()
            {
                (string Resource, Action Cleanup)[] actions;
                lock (_gate)
                {
                    if (!Retired || _preparing || _cleaned) return true;
                    _cleaned = true;
                    actions = _cleanup.ToArray();
                    _cleanup.Clear();
                }
                var success = true;
                foreach (var action in actions)
                    success = _owner.CleanupAction(action.Resource, action.Cleanup) && success;
                return success;
            }
        }

        private RegistrationLease ReserveRegistration(string name, IUnitofWork source, out RegistrationLease previous)
        {
            lock (_registrationGate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_pendingRegistrations.ContainsKey(name))
                    throw new InvalidOperationException($"Block '{name}' already has a registration change in progress.");
                _registrations.TryGetValue(name, out previous);
                var next = new RegistrationLease(this, previous?.Name ?? name, source);
                _pendingRegistrations[next.Name] = next;
                return next;
            }
        }

        private bool CanDispatchRegistration(RegistrationLease registration)
        {
            lock (_registrationGate)
                return !_disposed && registration.Published && !registration.Retired &&
                    _registrations.TryGetValue(registration.Name, out var current) && ReferenceEquals(current, registration) &&
                    ReferenceEquals(registration.Block.UnitOfWork, registration.Source);
        }

        private void EndRegistration(RegistrationLease registration)
        {
            lock (_registrationGate)
                if (_pendingRegistrations.TryGetValue(registration.Name, out var current) && ReferenceEquals(current, registration))
                    _pendingRegistrations.Remove(registration.Name);
        }

        private void PublishRegistration(RegistrationLease next, RegistrationLease previous)
        {
            lock (_registrationGate)
            {
                next.Check();
                if (!_pendingRegistrations.TryGetValue(next.Name, out var pending) || !ReferenceEquals(pending, next))
                    throw new InvalidOperationException("Registration publication lost its reservation.");
                if (previous != null && (!_registrations.TryGetValue(next.Name, out var current) || !ReferenceEquals(current, previous)))
                    throw new InvalidOperationException("Previous registration changed during preparation.");
                _blocks[next.Name] = next.Block;
                _registrations[next.Name] = next;
                next.Block.IsRegistered = true;
                next.EverPublished = true;
                next.Published = true;
                previous?.MarkRetired();
                if (string.IsNullOrEmpty(_currentBlockName)) _currentBlockName = next.Name;
            }
        }

        private IEntityStructure ResolveRegistrationStructure(IUnitofWork unit, IEntityStructure explicitStructure,
            string sourceName, out IDataSource resolvedSource)
        {
            resolvedSource = null;
            if (explicitStructure != null) return explicitStructure;
            if (unit.EntityStructure != null) return unit.EntityStructure;
            if (string.IsNullOrWhiteSpace(unit.EntityName)) return null;
            resolvedSource = unit.DataSource;
            if (resolvedSource == null)
            {
                var alias = string.IsNullOrWhiteSpace(sourceName) ? unit.DatasourceName : sourceName;
                if (!string.IsNullOrWhiteSpace(alias)) resolvedSource = _dmeEditor.GetDataSource(alias);
            }
            return resolvedSource?.GetEntityStructure(unit.EntityName, false);
        }

        private void RetireRegistrationCache(RegistrationLease registration, RegistrationLease previous)
        {
            DataBlockInfo current;
            lock (_registrationGate) _blocks.TryGetValue(registration.Name, out current);
            if (!registration.EverPublished && !_disposed && previous != null && ReferenceEquals(current, previous.Block))
                _performanceManager.CacheBlockInfo(previous.Name, previous.Block);
            else if (current == null)
                _performanceManager.InvalidateBlockCache(registration.Name);
        }

        private void ClearOwnedCurrentBlockVariables()
        {
            if (!_ownsSystemVariablesManager) return;
            var variables = _systemVariablesManager.GetFormSystemVariables();
            lock (_registrationGate)
                if (_currentBlockName == null)
                {
                    variables.CURRENT_BLOCK = string.Empty;
                    variables.CURRENT_ITEM = string.Empty;
                }
        }
    }
}
