using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// <see cref="DistributedSchemaService"/> partial — CreateEntity
    /// broadcast. Handles mode-specific target resolution and the
    /// <see cref="IdentityColumnPolicy"/> check for Sharded entities.
    /// </summary>
    public sealed partial class DistributedSchemaService
    {
        /// <inheritdoc/>
        public Task<SchemaOperationOutcome> CreateEntityAsync(
            EntityStructure   structure,
            CancellationToken cancellationToken = default)
        {
            if (structure == null) throw new ArgumentNullException(nameof(structure));
            if (string.IsNullOrWhiteSpace(structure.EntityName))
                throw new ArgumentException(
                    "EntityStructure.EntityName must be set.",
                    nameof(structure));

            var placement      = ResolvePlacementOrThrow(structure.EntityName);
            var targetShardIds = ComputeTargetShardIds(placement);

            // Phase 12 identity-column check for Sharded entities.
            if (placement.Mode == DistributionMode.Sharded &&
                HasIdentityColumn(structure))
            {
                var message =
                    $"Sharded entity '{structure.EntityName}' declares an identity column " +
                    $"('{GetIdentityColumnName(structure)}'). Per-shard identity columns " +
                    "collide across shards; configure an IDistributedSequenceProvider " +
                    "or switch to a non-identity key.";

                if (_identityPolicy == IdentityColumnPolicy.RejectShardedIdentity)
                {
                    var err = new InvalidOperationException(message);
                    _raisePlacementViolation(structure.EntityName, null, message);
                    return Task.FromResult(new SchemaOperationOutcome(
                        operation:         "CreateEntity",
                        entityName:        structure.EntityName,
                        targetedShardIds:  targetShardIds,
                        succeededShardIds: Array.Empty<string>(),
                        errors:            null,
                        terminalError:     err));
                }

                // WarnOnly — emit a placement-violation warning and proceed.
                _raisePlacementViolation(structure.EntityName, null, "WARN: " + message);
                _raisePassEvent("DistributedSchemaService.CreateEntity: " + message);
            }

            return RunDdlFanOutAsync(
                operation:      "CreateEntity",
                entityName:     structure.EntityName,
                targetShardIds: targetShardIds,
                perShardAction: (cluster, _, __) =>
                {
                    cluster.CreateEntityAs(structure);
                    return Task.CompletedTask;
                },
                cancellationToken: cancellationToken);
        }

        private static bool HasIdentityColumn(EntityStructure structure)
        {
            if (structure.Fields == null || structure.Fields.Count == 0) return false;
            foreach (var f in structure.Fields)
            {
                if (f != null && f.IsAutoIncrement) return true;
            }
            return false;
        }

        private static string GetIdentityColumnName(EntityStructure structure)
            => structure.Fields?
                        .FirstOrDefault(f => f != null && f.IsAutoIncrement)?
                        .FieldName;
    }
}
