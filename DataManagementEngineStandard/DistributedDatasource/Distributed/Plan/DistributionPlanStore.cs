using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Distributed.Plan
{
    /// <summary>
    /// <see cref="IDistributionPlanStore"/> implementation backed by
    /// <see cref="IConfigEditor.DataConnections"/>. One
    /// <see cref="ConnectionProperties"/> record is written per
    /// <see cref="EntityPlacement"/> in the plan; loads regroup
    /// records by plan name and reconstruct the placement set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Persistence schema (in <see cref="ConnectionProperties.ParameterList"/>):
    /// </para>
    /// <list type="bullet">
    ///   <item><c>DistributionName</c> — plan name (group key).</item>
    ///   <item><c>Version</c> — plan version (every record in a group MUST agree).</item>
    ///   <item><c>EntityName</c> — placement entity.</item>
    ///   <item><c>Mode</c> — <see cref="DistributionMode"/> as string.</item>
    ///   <item><c>ShardIds</c> — CSV of shard ids (no quoting; ids must be CSV-safe).</item>
    ///   <item><c>PartitionKind</c> — <see cref="PartitionKind"/> as string.</item>
    ///   <item><c>KeyColumns</c> — CSV of key column names.</item>
    ///   <item><c>ReplicationFactor</c>, <c>WriteQuorum</c> — integers (invariant).</item>
    ///   <item><c>Params</c> — JSON object of partition-function parameters.</item>
    /// </list>
    /// </remarks>
    public sealed class DistributionPlanStore : IDistributionPlanStore
    {
        /// <summary>Driver-name discriminator persisted with every plan record.</summary>
        public const string PlanDriverName = "BeepDistributionPlan";

        private static readonly JsonSerializerOptions ParamsJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        private readonly IDMEEditor _dmeEditor;
        private readonly object     _writeLock = new object();

        /// <summary>Initialises a new store backed by the given editor's <see cref="IConfigEditor"/>.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="dmeEditor"/> or its <c>ConfigEditor</c> is <c>null</c>.</exception>
        public DistributionPlanStore(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            if (_dmeEditor.ConfigEditor == null)
                throw new ArgumentException("DMEEditor.ConfigEditor must be non-null.", nameof(dmeEditor));
        }

        /// <inheritdoc/>
        public DistributionPlan Load(string distributionName)
        {
            if (string.IsNullOrWhiteSpace(distributionName)) return DistributionPlan.Empty;

            var records = SelectPlanRecords(distributionName);
            if (records.Count == 0) return DistributionPlan.Empty;

            var version = records
                .Select(r => TryReadInt(r, "Version", defaultValue: 1))
                .DefaultIfEmpty(1)
                .Max();

            var placements = new Dictionary<string, EntityPlacement>(StringComparer.OrdinalIgnoreCase);
            foreach (var rec in records)
            {
                var placement = TryDeserialisePlacement(rec);
                if (placement != null)
                    placements[placement.EntityName] = placement;
            }

            return new DistributionPlan(distributionName, version, placements, DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void Save(DistributionPlan plan, string distributionName)
        {
            if (plan == null)                                throw new ArgumentNullException(nameof(plan));
            if (string.IsNullOrWhiteSpace(distributionName)) throw new ArgumentException("Distribution name required.", nameof(distributionName));

            lock (_writeLock)
            {
                // 1. Collect existing records for the plan and index them by entity name.
                var existing = SelectPlanRecords(distributionName);
                var existingByEntity = new Dictionary<string, ConnectionProperties>(StringComparer.OrdinalIgnoreCase);
                foreach (var r in existing)
                {
                    if (r.ParameterList.TryGetValue("EntityName", out var en) && !string.IsNullOrWhiteSpace(en))
                        existingByEntity[en] = r;
                }

                // 2. Upsert each placement.
                foreach (var placement in plan.EntityPlacements.Values)
                {
                    var cfg = ToConnectionProperties(placement, distributionName, plan.Version);
                    if (existingByEntity.TryGetValue(placement.EntityName, out var prior))
                    {
                        cfg.GuidID = prior.GuidID;
                        _dmeEditor.ConfigEditor.UpdateDataConnection(cfg, prior.GuidID);
                        existingByEntity.Remove(placement.EntityName);
                    }
                    else
                    {
                        _dmeEditor.ConfigEditor.AddDataConnection(cfg);
                    }
                }

                // 3. Delete any records that no longer have a placement.
                foreach (var orphan in existingByEntity.Values)
                    _dmeEditor.ConfigEditor.RemoveDataConnection(orphan.ConnectionName);

                _dmeEditor.ConfigEditor.SaveDataconnectionsValues();
                _dmeEditor.Logger?.WriteLog(
                    $"[DistributionPlanStore] Saved plan '{distributionName}' v{plan.Version} ({plan.EntityPlacements.Count} entities).");
            }
        }

        /// <inheritdoc/>
        public int Delete(string distributionName)
        {
            if (string.IsNullOrWhiteSpace(distributionName)) return 0;

            lock (_writeLock)
            {
                var records = SelectPlanRecords(distributionName);
                foreach (var r in records)
                    _dmeEditor.ConfigEditor.RemoveDataConnection(r.ConnectionName);
                if (records.Count > 0)
                    _dmeEditor.ConfigEditor.SaveDataconnectionsValues();
                return records.Count;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<string> ListPlanNames()
        {
            var dataConnections = _dmeEditor.ConfigEditor.DataConnections;
            if (dataConnections == null) return Array.Empty<string>();

            return dataConnections
                .Where(c => string.Equals(c.DriverName, PlanDriverName, StringComparison.OrdinalIgnoreCase)
                            && c.ParameterList != null
                            && c.ParameterList.TryGetValue("DistributionName", out _))
                .Select(c => c.ParameterList["DistributionName"])
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private List<ConnectionProperties> SelectPlanRecords(string distributionName)
        {
            var dataConnections = _dmeEditor.ConfigEditor.DataConnections;
            if (dataConnections == null) return new List<ConnectionProperties>();

            return dataConnections
                .Where(c => string.Equals(c.DriverName, PlanDriverName, StringComparison.OrdinalIgnoreCase)
                            && c.ParameterList != null
                            && c.ParameterList.TryGetValue("DistributionName", out var dn)
                            && string.Equals(dn, distributionName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static ConnectionProperties ToConnectionProperties(
            EntityPlacement placement,
            string          distributionName,
            int             planVersion)
        {
            var cfg = new ConnectionProperties
            {
                ConnectionName = distributionName + "/" + placement.EntityName,
                DriverName     = PlanDriverName
            };

            cfg.ParameterList["DistributionName"]  = distributionName;
            cfg.ParameterList["Version"]           = planVersion.ToString(CultureInfo.InvariantCulture);
            cfg.ParameterList["EntityName"]        = placement.EntityName;
            cfg.ParameterList["Mode"]              = placement.Mode.ToString();
            cfg.ParameterList["ShardIds"]          = string.Join(",", placement.ShardIds);
            cfg.ParameterList["PartitionKind"]     = placement.PartitionFunction.Kind.ToString();
            cfg.ParameterList["KeyColumns"]        = string.Join(",", placement.PartitionFunction.KeyColumns);
            cfg.ParameterList["ReplicationFactor"] = placement.ReplicationFactor.ToString(CultureInfo.InvariantCulture);
            cfg.ParameterList["WriteQuorum"]       = placement.WriteQuorum.ToString(CultureInfo.InvariantCulture);
            cfg.ParameterList["Params"]            = SerialiseParams(placement.PartitionFunction.Parameters);
            return cfg;
        }

        private static EntityPlacement TryDeserialisePlacement(ConnectionProperties cfg)
        {
            if (cfg?.ParameterList == null) return null;
            if (!cfg.ParameterList.TryGetValue("EntityName", out var entity) || string.IsNullOrWhiteSpace(entity))
                return null;

            var modeText = TryReadString(cfg, "Mode", DistributionMode.Routed.ToString());
            if (!Enum.TryParse<DistributionMode>(modeText, ignoreCase: true, out var mode))
                mode = DistributionMode.Routed;

            var shardIds = SplitCsv(TryReadString(cfg, "ShardIds", string.Empty));
            if (shardIds.Count == 0) return null;

            var kindText = TryReadString(cfg, "PartitionKind", PartitionKind.None.ToString());
            if (!Enum.TryParse<PartitionKind>(kindText, ignoreCase: true, out var kind))
                kind = PartitionKind.None;

            var keyColumns = SplitCsv(TryReadString(cfg, "KeyColumns", string.Empty));
            var parameters = DeserialiseParams(TryReadString(cfg, "Params", null));

            var partitionFunction = kind == PartitionKind.None
                ? PartitionFunctionRef.None
                : new PartitionFunctionRef(kind, keyColumns, parameters);

            try
            {
                return new EntityPlacement(
                    entity,
                    mode,
                    shardIds,
                    partitionFunction,
                    replicationFactor: TryReadInt(cfg, "ReplicationFactor", 1),
                    writeQuorum:       TryReadInt(cfg, "WriteQuorum",       0));
            }
            catch (ArgumentException)
            {
                // Skip malformed records rather than throwing during load — diagnostics
                // happen via PlacementViolation events when the plan is applied.
                return null;
            }
        }

        private static string SerialiseParams(IReadOnlyDictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0) return "{}";
            var dict = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in parameters)
                dict[kv.Key] = kv.Value;
            return JsonSerializer.Serialize(dict, ParamsJsonOptions);
        }

        private static IReadOnlyDictionary<string, string> DeserialiseParams(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, ParamsJsonOptions);
                if (dict == null || dict.Count == 0) return null;
                return new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static IReadOnlyList<string> SplitCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();
            return csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .Where(s => s.Length > 0)
                      .ToList();
        }

        private static string TryReadString(ConnectionProperties cfg, string key, string defaultValue)
            => cfg.ParameterList != null && cfg.ParameterList.TryGetValue(key, out var v) ? v : defaultValue;

        private static int TryReadInt(ConnectionProperties cfg, string key, int defaultValue)
        {
            var s = TryReadString(cfg, key, null);
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : defaultValue;
        }
    }
}
