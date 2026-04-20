using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// <see cref="DistributedSchemaService"/> partial — AlterEntity
    /// broadcast. Renders each <see cref="AlterEntityChange"/> into a
    /// minimal ANSI-SQL fragment (so non-RDBMS providers can still
    /// pattern-match on the message) and dispatches it to every
    /// owning shard via <see cref="Proxy.IProxyCluster.RunScript"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// V1 uses a deliberately conservative SQL rendering (double-
    /// quoted identifiers, plain ANSI <c>ADD COLUMN</c> / <c>DROP COLUMN</c>
    /// / <c>ALTER COLUMN</c> / <c>CREATE INDEX</c> / <c>DROP INDEX</c>).
    /// Providers that need dialect-specific rendering should
    /// override <see cref="AlterEntityRenderer"/> via the options
    /// surface in a follow-up — Phase 12 does not block on that.
    /// </para>
    /// <para>
    /// DDL is not atomic across shards. Callers that require
    /// eventual consistency should pair <see cref="AlterEntityAsync"/>
    /// with a follow-up <see cref="DetectSchemaDriftAsync"/> and a
    /// manual repair step on any shard listed in
    /// <see cref="SchemaOperationOutcome.Errors"/>.
    /// </para>
    /// </remarks>
    public sealed partial class DistributedSchemaService
    {
        /// <summary>
        /// Pluggable renderer hook. Defaults to
        /// <see cref="DefaultRenderAlter"/>; callers can override by
        /// constructing the service with
        /// <see cref="WithAlterRenderer"/>.
        /// </summary>
        private Func<AlterEntityChange, string> _alterRenderer;

        /// <summary>Returns a new service that renders alter statements via <paramref name="renderer"/>.</summary>
        public DistributedSchemaService WithAlterRenderer(Func<AlterEntityChange, string> renderer)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            _alterRenderer = renderer;
            return this;
        }

        /// <inheritdoc/>
        public Task<SchemaOperationOutcome> AlterEntityAsync(
            AlterEntityChange change,
            CancellationToken cancellationToken = default)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));

            var placement      = ResolvePlacementOrThrow(change.EntityName);
            var targetShardIds = ComputeTargetShardIds(placement);
            var sql            = (_alterRenderer ?? DefaultRenderAlter)(change);

            return RunDdlFanOutAsync(
                operation:      "AlterEntity",
                entityName:     change.EntityName,
                targetShardIds: targetShardIds,
                perShardAction: (cluster, shardId, _) =>
                {
                    var script = BuildScript(change.EntityName, shardId, sql);
                    var errors = cluster.RunScript(script);
                    ThrowIfErrored("AlterEntity", change.EntityName, shardId, errors);
                    return Task.CompletedTask;
                },
                cancellationToken: cancellationToken);
        }

        // ── ANSI-SQL rendering ────────────────────────────────────────────

        internal static string DefaultRenderAlter(AlterEntityChange change)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            var sb = new StringBuilder();
            switch (change.Kind)
            {
                case AlterEntityChangeKind.AddColumn:
                    sb.Append("ALTER TABLE ").Append(Quote(change.EntityName))
                      .Append(" ADD COLUMN ").Append(RenderColumn(change.Column))
                      .Append(';');
                    break;

                case AlterEntityChangeKind.DropColumn:
                    sb.Append("ALTER TABLE ").Append(Quote(change.EntityName))
                      .Append(" DROP COLUMN ").Append(Quote(change.ColumnName))
                      .Append(';');
                    break;

                case AlterEntityChangeKind.AlterColumn:
                    sb.Append("ALTER TABLE ").Append(Quote(change.EntityName))
                      .Append(" ALTER COLUMN ").Append(RenderColumn(change.Column))
                      .Append(';');
                    break;

                case AlterEntityChangeKind.AddIndex:
                    sb.Append(change.IndexIsUnique ? "CREATE UNIQUE INDEX " : "CREATE INDEX ")
                      .Append(Quote(change.IndexName))
                      .Append(" ON ").Append(Quote(change.EntityName))
                      .Append(" (");
                    for (var i = 0; i < change.IndexColumns.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(Quote(change.IndexColumns[i]));
                    }
                    sb.Append(");");
                    break;

                case AlterEntityChangeKind.DropIndex:
                    sb.Append("DROP INDEX ").Append(Quote(change.IndexName))
                      .Append(" ON ").Append(Quote(change.EntityName))
                      .Append(';');
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported alter change kind '{change.Kind}'.");
            }
            return sb.ToString();
        }

        private static string RenderColumn(EntityField column)
        {
            if (column == null) throw new ArgumentException("Column payload is required.", nameof(column));
            var name = Quote(column.FieldName);
            var type = string.IsNullOrWhiteSpace(column.Fieldtype)
                           ? "VARCHAR(255)"
                           : column.Fieldtype.ToUpperInvariant();

            var lengthSuffix = string.Empty;
            if (column.Size > 0 &&
                (type.Contains("CHAR") || type.Contains("VARCHAR") || type.Contains("BINARY")))
            {
                lengthSuffix = $"({column.Size})";
            }
            else if (column.NumericPrecision > 0 &&
                     (type.Contains("DECIMAL") || type.Contains("NUMERIC")))
            {
                lengthSuffix = column.Size1 > 0
                    ? $"({column.NumericPrecision},{column.Size1})"
                    : $"({column.NumericPrecision})";
            }

            var nullability = column.AllowDBNull ? "NULL" : "NOT NULL";
            return $"{name} {type}{lengthSuffix} {nullability}";
        }

        private static string Quote(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return "\"\"";
            var escaped = identifier.Replace("\"", "\"\"");
            return "\"" + escaped + "\"";
        }

        private static ETLScriptDet BuildScript(
            string          entityName,
            string          shardId,
            string          sql,
            DDLScriptType   scriptType = DDLScriptType.AlterFor)
            => new ETLScriptDet
            {
                SourceEntityName      = entityName,
                DestinationEntityName = entityName,
                ScriptType            = scriptType,
                Ddl                   = sql,
                Active                = true
            };

        private static void ThrowIfErrored(
            string       operation,
            string       entityName,
            string       shardId,
            IErrorsInfo  errors)
        {
            if (errors == null) return;
            if (errors.Flag == Errors.Ok ||
                errors.Flag == Errors.Information ||
                errors.Flag == Errors.Warning) return;

            var message =
                $"{operation} on shard '{shardId}' for entity '{entityName}' failed: " +
                (errors.Message ?? errors.Flag.ToString());
            if (errors.Ex != null) throw new InvalidOperationException(message, errors.Ex);
            throw new InvalidOperationException(message);
        }
    }
}
