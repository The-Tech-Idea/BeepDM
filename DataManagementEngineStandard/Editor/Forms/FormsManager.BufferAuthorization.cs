using System;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager;

public partial class FormsManager
{
    private sealed class BufferAuthorization
    {
        internal object SecurityOwner, Identity, LegacyUnits;
        internal long ReadRevision;
        internal IDataSource Source;
        internal IEntityStructure Entity;
        internal string EntityName, BlockEntityName, DataSourceName, Schema, DefaultClause;
        internal bool HasIdentity, ManagedRead;
    }

    private BufferAuthorization CaptureBufferAuthorization(RegistrationLease registration,
        QuerySecuritySnapshot policy, object preparedIdentity = null)
    {
        var unit = registration.Source;
        var evidence = new BufferAuthorization { SecurityOwner = _securityManager,
            ReadRevision = policy.ReadAuthorizationRevision, Source = unit.DataSource,
            Entity = registration.Block.EntityStructure, EntityName = unit.EntityName,
            BlockEntityName = registration.Block.EntityStructure?.EntityName, DefaultClause = registration.Block.DefaultWhereClause,
            DataSourceName = registration.Block.DataSourceName,
            Schema = SchemaSignature(registration.Block.EntityStructure?.Fields), ManagedRead = preparedIdentity != null };
        if (unit is IUnitofWorkReadBufferIdentity buffers && buffers.SupportsReadBufferIdentity)
        {
            evidence.HasIdentity = true;
            if (preparedIdentity != null) evidence.Identity = preparedIdentity;
            else if (!buffers.TryGetReadBufferIdentity(out evidence.Identity)) return null;
        }
        else
        {
            if (preparedIdentity != null) return null;
            evidence.LegacyUnits = unit.Units;
        }
        return evidence;
    }

    private void PrepareInitialBufferAuthorization(RegistrationLease registration)
    {
        var policy = CaptureQuerySecurity(registration.Name);
        if (registration.Source is IUnitofWorkReadBufferIdentity buffers && buffers.SupportsReadBufferIdentity &&
            buffers.RequiresReadAuthorization) return;
        // Compatibility for the original unscoped preloaded buffer only, not certification
        // of externally loaded rows under a configured principal/tenant policy.
        if ((_securityManager == null || _securityManager is SecurityManager) &&
            (_securityManager == null || policy.ReadAuthorizationRevision == 0) && policy.QueryAllowed && string.IsNullOrWhiteSpace(policy.RowFilterClause))
            registration.BufferAuthorization = CaptureBufferAuthorization(registration, policy);
    }

    private bool IsBufferAuthorized(RegistrationLease registration)
    {
        BufferAuthorization evidence;
        lock (_registrationGate)
        {
            if (!CanDispatchRegistration(registration)) return false;
            evidence = registration.BufferAuthorization;
        }
        if (evidence == null || !ReferenceEquals(evidence.SecurityOwner, _securityManager)) return false;
        var policy = CaptureQuerySecurity(registration.Name);
        if (!policy.QueryAllowed || policy.ReadAuthorizationRevision != evidence.ReadRevision || !registration.Block.QueryAllowed)
            return false;
        var unit = registration.Source;
        if (!ReferenceEquals(unit.DataSource, evidence.Source) || unit.EntityName != evidence.EntityName ||
            !ReferenceEquals(registration.Block.EntityStructure, evidence.Entity) ||
            registration.Block.EntityStructure?.EntityName != evidence.BlockEntityName ||
            registration.Block.DefaultWhereClause != evidence.DefaultClause ||
            registration.Block.DataSourceName != evidence.DataSourceName ||
            SchemaSignature(registration.Block.EntityStructure?.Fields) != evidence.Schema) return false;
        if (evidence.HasIdentity)
        {
            if (unit is not IUnitofWorkReadBufferIdentity buffers || !buffers.SupportsReadBufferIdentity ||
                (!evidence.ManagedRead && buffers.RequiresReadAuthorization) ||
                !buffers.TryGetReadBufferIdentity(out var identity) || !ReferenceEquals(identity, evidence.Identity)) return false;
        }
        else if (!ReferenceEquals((object)unit.Units, evidence.LegacyUnits)) return false;
        // All external getters are outside the registration monitor. Recheck the pure
        // publication pointer and policy revision after they have acknowledged.
        var current = CaptureQuerySecurity(registration.Name);
        lock (_registrationGate)
            return CanDispatchRegistration(registration) && ReferenceEquals(registration.BufferAuthorization, evidence) &&
                current.QueryAllowed && current.ReadAuthorizationRevision == evidence.ReadRevision;
    }
}
