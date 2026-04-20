using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// <see cref="DistributedSchemaService"/> partial — DropEntity
    /// broadcast. Renders a conservative <c>DROP TABLE</c> statement
    /// and dispatches it to every owning shard. Uses the same
    /// <see cref="RunDdlFanOutAsync"/> helper as CreateEntity /
    /// AlterEntity so the audit surface stays consistent.
    /// </summary>
    public sealed partial class DistributedSchemaService
    {
        /// <inheritdoc/>
        public Task<SchemaOperationOutcome> DropEntityAsync(
            string            entityName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));

            var placement      = ResolvePlacementOrThrow(entityName);
            var targetShardIds = ComputeTargetShardIds(placement);
            var sql            = "DROP TABLE " + QuoteIdent(entityName) + ";";

            return RunDdlFanOutAsync(
                operation:      "DropEntity",
                entityName:     entityName,
                targetShardIds: targetShardIds,
                perShardAction: (cluster, shardId, _) =>
                {
                    var script = new ETLScriptDet
                    {
                        SourceEntityName      = entityName,
                        DestinationEntityName = entityName,
                        ScriptType            = DDLScriptType.DropTable,
                        Ddl                   = sql,
                        Active                = true
                    };
                    IErrorsInfo errors = cluster.RunScript(script);
                    ThrowIfErrored("DropEntity", entityName, shardId, errors);
                    return Task.CompletedTask;
                },
                cancellationToken: cancellationToken);
        }

        private static string QuoteIdent(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return "\"\"";
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }
    }
}
