using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using TheTechIdea.Beep.Editor.Forms.Helpers;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        private sealed class ManagedReadPlan
        {
            internal DataBlockInfo Block;
            internal IUnitofWork Unit;
            internal List<AppFilter> Filters;
            internal QuerySecuritySnapshot Security;
            internal string PolicySignature;
            internal string DefaultClause;
            internal IDataSource DataSource;
            internal string DataSourceName;
            internal IEntityStructure Entity;
            internal string EntityName;
            internal string UnitEntityName;
            internal string FieldSignature;
            internal List<EntityField> DeclaredFields;
            internal RegistrationLease QueryRegistration;
            internal long QueryRevision;
            internal DataBlockMode? QueryMode;
            internal BoundedPageRequest PageRequest;
            internal BlockConfiguration PageConfiguration;
            internal string PageKeys, PageDefaultOrder;
            internal int PageSize, PageMaxFetch, PageMaxRecords;
        }

        private QuerySecuritySnapshot CaptureQuerySecurity(string blockName)
        {
            if (_securityManager is IQuerySecuritySnapshotProvider provider) return provider.CaptureQuerySecurity(blockName);
            var policy = _securityManager?.GetBlockSecurity(blockName);
            return new QuerySecuritySnapshot(-1, _securityManager?.IsBlockAllowed(blockName, SecurityPermission.Query) ?? true,
                policy?.RowFilterClause ?? string.Empty, new Dictionary<string, object>(policy?.RowFilterValues ?? new Dictionary<string, object>(), StringComparer.OrdinalIgnoreCase));
        }

        private ManagedReadPlan BuildManagedReadPlan(string blockName, List<AppFilter> caller)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(FormsManager));
            var block = GetBlock(blockName);
            if (block?.UnitOfWork == null) throw new InvalidOperationException("Managed read block is missing or has no UoW.");
            var security = CaptureQuerySecurity(blockName);
            if (!security.QueryAllowed)
            {
                _securityManager?.RaiseViolation(blockName, null, SecurityPermission.Query, "Managed query denied by security policy.");
                throw new UnauthorizedAccessException("Query not permitted on this block.");
            }
            if (!block.QueryAllowed) throw new UnauthorizedAccessException("Query not allowed on this block.");
            var entity = block.EntityStructure;
            // This is the query field projection, not a substitute for full metadata graph snapshots.
            var fields = (entity?.Fields ?? Enumerable.Empty<EntityField>()).Select(field => new EntityField
                { FieldName = field.FieldName, Fieldtype = field.Fieldtype, IsKey = field.IsKey }).ToList();
            var plan = new ManagedReadPlan { Block = block, Unit = block.UnitOfWork,
                Security = security, DefaultClause = block.DefaultWhereClause,
                DataSource = block.UnitOfWork.DataSource, DataSourceName = block.DataSourceName,
                Entity = entity, EntityName = entity?.EntityName, DeclaredFields = fields,
                UnitEntityName = block.UnitOfWork.EntityName, FieldSignature = SchemaSignature(fields) };
            plan.Filters = new ManagedFilterCompiler(fields).Compile(caller, plan.DefaultClause, security.RowFilterClause, security.RowFilterValues);
            var policy = new ManagedFilterCompiler(fields).Compile(null, null, security.RowFilterClause, security.RowFilterValues);
            plan.PolicySignature = FilterSignature(policy);
            return plan;
        }

        private void VerifyManagedReadPlan(string blockName, ManagedReadPlan plan)
        {
            if (plan.QueryRegistration != null) VerifyQueryRegistration(plan);
            if (plan.PageRequest != null)
            {
                VerifyPageConfiguration(plan);
                if (PageKeySignature(plan.Entity?.Fields) != plan.PageKeys)
                    throw new InvalidOperationException("Provider page primary keys changed during the operation.");
            }
            if (_disposed || !ReferenceEquals(GetBlock(blockName), plan.Block) ||
                !ReferenceEquals(plan.Block.UnitOfWork, plan.Unit) || plan.Block.DefaultWhereClause != plan.DefaultClause ||
                !ReferenceEquals(plan.Unit.DataSource, plan.DataSource) || plan.Block.DataSourceName != plan.DataSourceName ||
                !ReferenceEquals(plan.Block.EntityStructure, plan.Entity) || plan.Entity?.EntityName != plan.EntityName ||
                plan.Unit.EntityName != plan.UnitEntityName || SchemaSignature(plan.Entity?.Fields) != plan.FieldSignature)
                throw new InvalidOperationException("Managed read target changed during the operation.");
            var current = CaptureQuerySecurity(blockName);
            if (!current.QueryAllowed || !plan.Block.QueryAllowed || current.Revision != plan.Security.Revision)
                throw new UnauthorizedAccessException("Managed query security changed during the operation.");
            var filters = new ManagedFilterCompiler(plan.DeclaredFields).Compile(null, null, current.RowFilterClause, current.RowFilterValues);
            if (FilterSignature(filters) != plan.PolicySignature)
                throw new UnauthorizedAccessException("Managed query restriction changed during the operation.");
        }

        private void PublishManagedRead(string blockName, ManagedReadPlan plan, IUnitofWorkReadStage stage,
            CancellationToken token, Action publishOwnedQueryState = null)
        {
            if (_securityManager is not IQuerySecurityPublication security)
                throw new NotSupportedException("Staged managed reads require a revision-gated security helper.");
            RegistrationLease registration;
            lock (_registrationGate) _registrations.TryGetValue(blockName, out registration);
            var preparedIdentity = (stage as IUnitofWorkReadBufferStage)?.PreparedBufferIdentity;
            var authorization = registration == null || preparedIdentity == null ? null :
                CaptureBufferAuthorization(registration, plan.Security, preparedIdentity);
            VerifyManagedReadPlan(blockName, plan);
            stage.Publish(publish =>
            {
                if (!security.TryPublishQuery(plan.Security.Revision, () =>
                {
                    lock (_registrationGate)
                    {
                        token.ThrowIfCancellationRequested();
                        if (plan.QueryRegistration != null) VerifyQueryRegistration(plan);
                        if (plan.PageRequest != null) VerifyPageConfiguration(plan);
                        if (_disposed || !_registrations.TryGetValue(blockName, out var lease) ||
                            lease.Retired || !lease.Published || !ReferenceEquals(lease.Block, plan.Block) ||
                            !ReferenceEquals(lease.Source, plan.Unit) || !ReferenceEquals(plan.Block.UnitOfWork, plan.Unit) ||
                            !plan.Block.QueryAllowed || plan.Block.DefaultWhereClause != plan.DefaultClause ||
                            !ReferenceEquals(plan.Block.EntityStructure, plan.Entity) || plan.Block.DataSourceName != plan.DataSourceName)
                            throw new InvalidOperationException("Managed read registration changed before publication.");
                        publish();
                        lease.BufferAuthorization = authorization;
                        publishOwnedQueryState?.Invoke();
                    }
                })) throw new UnauthorizedAccessException("Managed query security changed before publication.");
            });
            if (!stage.IsPublished)
                throw new InvalidOperationException("Staged read did not confirm publication; no legacy fallback is permitted.");
        }

        private static string FilterSignature(IEnumerable<AppFilter> filters)
        {
            var text = new StringBuilder();
            foreach (var filter in filters)
                foreach (var value in new[] { filter.FieldName, filter.Operator, filter.FieldType?.FullName, filter.FilterValue, filter.FilterValue1 })
                    text.Append(value?.Length ?? -1).Append(':').Append(value);
            return text.ToString();
        }

        private static string SchemaSignature(IEnumerable<EntityField> fields)
        {
            var text = new StringBuilder();
            foreach (var field in fields ?? Enumerable.Empty<EntityField>())
                foreach (var value in new[] { field.FieldName, field.Fieldtype })
                    text.Append(value?.Length ?? -1).Append(':').Append(value);
            return text.ToString();
        }
    }
}
