using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Distributed.Schema;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — Phase 12 schema
    /// surface. Exposes the
    /// <see cref="IDistributedSchemaService"/> orchestrator and
    /// forwards the <see cref="IDataSource"/> DDL members (originally
    /// stubbed in <c>DistributedDataSource.IDataSource.cs</c>) into
    /// that service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The service is created lazily through
    /// <see cref="EnsureSchemaServiceInitialized"/>. Callers that only
    /// need the high-level <see cref="IDistributedSchemaService"/>
    /// surface should prefer the typed methods
    /// (<see cref="CreateEntityDistributedAsync"/>,
    /// <see cref="AlterEntityDistributedAsync"/>, etc.).
    /// </para>
    /// <para>
    /// The classic <see cref="IDataSource"/> overloads
    /// (<see cref="CreateEntityAs"/>,
    /// <see cref="CreateEntities"/>,
    /// <see cref="RunScript"/>,
    /// <see cref="GetCreateEntityScript"/>) preserve their synchronous
    /// signatures by blocking on the async service; Phase 12 logs the
    /// blocking call through <see cref="PassEvent"/> so mixed-mode
    /// callers remain discoverable.
    /// </para>
    /// </remarks>
    public partial class DistributedDataSource
    {
        private IDistributedSchemaService _schemaService;

        /// <summary>
        /// Lazy accessor for the distributed schema service. Safe to
        /// call concurrently with other operations; the first caller
        /// wins the construction race under
        /// <see cref="_schemaServiceLock"/>.
        /// </summary>
        public IDistributedSchemaService SchemaService
        {
            get
            {
                EnsureSchemaServiceInitialized();
                return _schemaService;
            }
        }

        private readonly object _schemaServiceLock = new object();

        private void EnsureSchemaServiceInitialized()
        {
            if (_schemaService != null) return;
            lock (_schemaServiceLock)
            {
                if (_schemaService != null) return;
                _schemaService = new DistributedSchemaService(
                    getCurrentPlan:          () => _plan,
                    resolveShard:            ResolveShardForSchema,
                    raisePlacementViolation: (entity, shard, reason) =>
                        RaisePlacementViolation(entity, shard, reason),
                    raisePassEvent:          RaisePassEventSafe,
                    identityPolicy:          _options.IdentityColumnPolicy);
            }
        }

        private Proxy.IProxyCluster ResolveShardForSchema(string shardId)
        {
            if (string.IsNullOrWhiteSpace(shardId)) return null;
            return _shards.TryGetValue(shardId, out var cluster) ? cluster : null;
        }

        // ── IDataSource DDL members (Phase 12) ────────────────────────────

        /// <inheritdoc/>
        public bool CreateEntityAs(EntityStructure entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var outcome = SchemaService
                .CreateEntityAsync(entity)
                .GetAwaiter()
                .GetResult();
            if (outcome.TerminalError != null) throw outcome.TerminalError;
            return outcome.IsFullySucceeded;
        }

        /// <inheritdoc/>
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            var errors = new ErrorsInfo { Flag = Errors.Ok };
            if (entities == null || entities.Count == 0) return errors;

            var aggregated = new List<Exception>();
            foreach (var entity in entities)
            {
                if (entity == null) continue;
                try
                {
                    var outcome = SchemaService
                        .CreateEntityAsync(entity)
                        .GetAwaiter()
                        .GetResult();
                    if (outcome.TerminalError != null)      aggregated.Add(outcome.TerminalError);
                    foreach (var err in outcome.Errors)     aggregated.Add(err.Value);
                }
                catch (Exception ex)
                {
                    aggregated.Add(ex);
                }
            }

            if (aggregated.Count == 0) return errors;

            errors.Flag    = Errors.Failed;
            errors.Ex      = aggregated[0];
            errors.Message = "One or more entities failed to create across shards: " +
                             string.Join("; ", aggregated.Select(e => e.Message));
            return errors;
        }

        /// <inheritdoc/>
        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            if (entities == null || entities.Count == 0)
                return Array.Empty<ETLScriptDet>();

            var results = new List<ETLScriptDet>();
            foreach (var entity in entities)
            {
                if (entity == null || string.IsNullOrWhiteSpace(entity.EntityName)) continue;
                var cluster = ResolveSchemaScriptShard(entity.EntityName);
                if (cluster == null) continue;
                try
                {
                    var scripts = cluster.GetCreateEntityScript(new List<EntityStructure> { entity });
                    if (scripts != null) results.AddRange(scripts);
                }
                catch (Exception ex)
                {
                    RaisePassEventSafe(
                        $"DistributedDataSource.GetCreateEntityScript '{entity.EntityName}' failed: {ex.Message}");
                }
            }
            return results;
        }

        /// <inheritdoc/>
        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            var errors = new ErrorsInfo { Flag = Errors.Ok };
            if (dDLScripts == null)
            {
                errors.Flag    = Errors.Failed;
                errors.Message = "RunScript received a null script.";
                return errors;
            }

            var entityName = dDLScripts.DestinationEntityName ?? dDLScripts.SourceEntityName;
            if (string.IsNullOrWhiteSpace(entityName))
            {
                errors.Flag    = Errors.Failed;
                errors.Message = "RunScript requires SourceEntityName or DestinationEntityName.";
                return errors;
            }

            if (!_plan.TryGetPlacement(entityName, out var placement))
            {
                RaisePlacementViolation(entityName, null,
                    $"RunScript cannot route: no placement for '{entityName}'.");
                errors.Flag    = Errors.Failed;
                errors.Message = $"No placement for entity '{entityName}'.";
                return errors;
            }

            var aggregated = new List<Exception>();
            foreach (var shardId in placement.ShardIds)
            {
                var cluster = ResolveShardForSchema(shardId);
                if (cluster == null)
                {
                    aggregated.Add(new InvalidOperationException(
                        $"Shard '{shardId}' is not registered."));
                    continue;
                }
                try
                {
                    var perShard = cluster.RunScript(dDLScripts);
                    if (perShard != null && perShard.Flag != Errors.Ok
                        && perShard.Flag != Errors.Information
                        && perShard.Flag != Errors.Warning)
                    {
                        aggregated.Add(perShard.Ex
                                       ?? new InvalidOperationException(perShard.Message ?? perShard.Flag.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    aggregated.Add(ex);
                }
            }

            if (aggregated.Count == 0) return errors;
            errors.Flag    = Errors.Failed;
            errors.Ex      = aggregated[0];
            errors.Message = "RunScript failed on one or more shards: " +
                             string.Join("; ", aggregated.Select(e => e.Message));
            return errors;
        }

        private Proxy.IProxyCluster ResolveSchemaScriptShard(string entityName)
        {
            if (!_plan.TryGetPlacement(entityName, out var placement)) return null;
            foreach (var shardId in placement.ShardIds)
            {
                var cluster = ResolveShardForSchema(shardId);
                if (cluster != null) return cluster;
            }
            return null;
        }

        // ── High-level typed surface (opt-in) ─────────────────────────────

        /// <summary>
        /// Typed wrapper that creates <paramref name="structure"/> on
        /// every owning shard and returns the full outcome (including
        /// per-shard errors).
        /// </summary>
        public async System.Threading.Tasks.Task<SchemaOperationOutcome> CreateEntityDistributedAsync(
            EntityStructure structure,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var entityName = structure?.EntityName ?? string.Empty;
            EnsureAccess(entityName, Security.DistributedAccessKind.Ddl, principal: null);
            var outcome = await SchemaService.CreateEntityAsync(structure, cancellationToken).ConfigureAwait(false);
            RaiseAuditEvent(
                kind:       Audit.DistributedAuditEventKind.DDLBroadcast,
                operation:  "CreateEntity",
                entityName: entityName,
                shardIds:   outcome?.TargetedShardIds,
                message:    outcome == null ? "no outcome" : $"fullySucceeded={outcome.IsFullySucceeded}; errors={outcome.Errors.Count}",
                error:      outcome?.TerminalError);
            return outcome;
        }

        /// <summary>
        /// Typed wrapper that broadcasts <paramref name="change"/> to
        /// every owning shard and returns the full outcome.
        /// </summary>
        public async System.Threading.Tasks.Task<SchemaOperationOutcome> AlterEntityDistributedAsync(
            AlterEntityChange change,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var entityName = change?.EntityName ?? string.Empty;
            EnsureAccess(entityName, Security.DistributedAccessKind.Ddl, principal: null);
            var outcome = await SchemaService.AlterEntityAsync(change, cancellationToken).ConfigureAwait(false);
            RaiseAuditEvent(
                kind:       Audit.DistributedAuditEventKind.DDLBroadcast,
                operation:  "AlterEntity",
                entityName: entityName,
                shardIds:   outcome?.TargetedShardIds,
                message:    outcome == null ? "no outcome" : $"fullySucceeded={outcome.IsFullySucceeded}; errors={outcome.Errors.Count}; changeKind={change?.Kind}",
                error:      outcome?.TerminalError);
            return outcome;
        }

        /// <summary>
        /// Typed wrapper that drops <paramref name="entityName"/> on
        /// every owning shard and returns the full outcome.
        /// </summary>
        public async System.Threading.Tasks.Task<SchemaOperationOutcome> DropEntityDistributedAsync(
            string entityName,
            System.Threading.CancellationToken cancellationToken = default)
        {
            EnsureAccess(entityName ?? string.Empty, Security.DistributedAccessKind.Ddl, principal: null);
            var outcome = await SchemaService.DropEntityAsync(entityName, cancellationToken).ConfigureAwait(false);
            RaiseAuditEvent(
                kind:       Audit.DistributedAuditEventKind.DDLBroadcast,
                operation:  "DropEntity",
                entityName: entityName,
                shardIds:   outcome?.TargetedShardIds,
                message:    outcome == null ? "no outcome" : $"fullySucceeded={outcome.IsFullySucceeded}; errors={outcome.Errors.Count}",
                error:      outcome?.TerminalError);
            return outcome;
        }

        /// <summary>
        /// Runs a schema-drift scan across every shard in the active
        /// plan and returns the aggregated report.
        /// </summary>
        public System.Threading.Tasks.Task<SchemaDriftReport> DetectSchemaDriftAsync(
            System.Threading.CancellationToken cancellationToken = default)
            => SchemaService.DetectSchemaDriftAsync(cancellationToken);
    }
}
