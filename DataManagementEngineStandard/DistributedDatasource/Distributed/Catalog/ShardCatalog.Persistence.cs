using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Catalog
{
    /// <summary>
    /// <see cref="ShardCatalog"/> partial — persistence to and from
    /// <see cref="IConfigEditor.DataConnections"/> using the shared
    /// <c>BeepDistributedShard</c> driver name. Mirrors the pattern
    /// used by <c>ProxyCluster.SaveNodesToConfig</c> /
    /// <c>LoadLocalNodesFromConfig</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each shard is stored as a <see cref="ConnectionProperties"/>
    /// record with:
    /// </para>
    /// <list type="bullet">
    ///   <item><c>DriverName</c> = <see cref="ShardDriverName"/>.</item>
    ///   <item><c>ConnectionName</c> = <c>{distributionName}/{shardId}</c>.</item>
    ///   <item><c>ParameterList["DistributionName"]</c>, <c>["ShardId"]</c>, <c>["ClusterName"]</c>, <c>["Weight"]</c>, and <c>["Tags"]</c> (CSV of <c>k=v</c>).</item>
    /// </list>
    /// <para>
    /// Loading requires a factory because <see cref="IProxyCluster"/>
    /// instances cannot be reconstructed from config alone — they own
    /// nodes, transports, and credentials that must be wired up by the
    /// host application.
    /// </para>
    /// </remarks>
    public partial class ShardCatalog
    {
        /// <summary>Driver-name discriminator persisted with every shard record.</summary>
        public const string ShardDriverName = "BeepDistributedShard";

        // ── Save ──────────────────────────────────────────────────────────

        /// <summary>
        /// Writes every registered shard to
        /// <see cref="IConfigEditor.DataConnections"/>. Existing records
        /// (matched by <see cref="ConnectionProperties.ConnectionName"/>)
        /// are updated in place; new records are appended. Returns the
        /// number of records written. Returns <c>0</c> when no editor
        /// was supplied to the constructor.
        /// </summary>
        public int SaveToConfig()
        {
            if (_dmeEditor?.ConfigEditor == null) return 0;

            var written = 0;
            foreach (var shard in _shards.Values)
            {
                var cfg = ToConnectionProperties(shard, DistributionName);

                var existing = _dmeEditor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => string.Equals(
                        c.ConnectionName, cfg.ConnectionName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                    _dmeEditor.ConfigEditor.UpdateDataConnection(cfg, existing.GuidID);
                else
                    _dmeEditor.ConfigEditor.AddDataConnection(cfg);
                written++;
            }
            _dmeEditor.ConfigEditor.SaveDataconnectionsValues();
            _dmeEditor.Logger?.WriteLog(
                $"[ShardCatalog] Saved {written} shard record(s) to config (distribution={DistributionName}).");
            return written;
        }

        // ── Load ──────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the catalog from <see cref="IConfigEditor.DataConnections"/>.
        /// For each stored record matching this catalog's
        /// <see cref="DistributionName"/> the supplied
        /// <paramref name="clusterFactory"/> is invoked with the shard
        /// id and the persisted <see cref="ConnectionProperties"/>;
        /// returning <c>null</c> skips that record. Returns the number
        /// of shards added.
        /// </summary>
        /// <param name="clusterFactory">Factory that materialises an <see cref="IProxyCluster"/> for a given persisted shard.</param>
        /// <exception cref="ArgumentNullException"><paramref name="clusterFactory"/> is <c>null</c>.</exception>
        public int LoadFromConfig(Func<string, ConnectionProperties, IProxyCluster> clusterFactory)
        {
            if (clusterFactory == null) throw new ArgumentNullException(nameof(clusterFactory));
            if (_dmeEditor?.ConfigEditor == null) return 0;

            var records = _dmeEditor.ConfigEditor.DataConnections
                .Where(c => string.Equals(c.DriverName, ShardDriverName, StringComparison.OrdinalIgnoreCase)
                            && c.ParameterList != null
                            && c.ParameterList.TryGetValue("DistributionName", out var dn)
                            && string.Equals(dn, DistributionName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var added = 0;
            foreach (var cfg in records)
            {
                if (!cfg.ParameterList.TryGetValue("ShardId", out var shardId) ||
                    string.IsNullOrWhiteSpace(shardId))
                {
                    _dmeEditor.Logger?.WriteLog(
                        $"[ShardCatalog] Skipping record '{cfg.ConnectionName}' — missing ShardId.");
                    continue;
                }

                if (_shards.ContainsKey(shardId)) continue;

                var cluster = clusterFactory(shardId, cfg);
                if (cluster == null) continue;

                var weight = 1;
                if (cfg.ParameterList.TryGetValue("Weight", out var ws)
                    && int.TryParse(ws, out var parsedWeight) && parsedWeight >= 1)
                    weight = parsedWeight;

                IReadOnlyDictionary<string, string> tags = null;
                if (cfg.ParameterList.TryGetValue("Tags", out var tagsCsv))
                    tags = ParseTagCsv(tagsCsv);

                Add(new Shard(shardId, cluster, weight, tags));
                added++;
            }

            _dmeEditor.Logger?.WriteLog(
                $"[ShardCatalog] Loaded {added} shard(s) from config (distribution={DistributionName}).");
            return added;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static ConnectionProperties ToConnectionProperties(Shard shard, string distributionName)
        {
            var cfg = new ConnectionProperties
            {
                ConnectionName = distributionName + "/" + shard.ShardId,
                DriverName     = ShardDriverName
            };
            cfg.ParameterList["DistributionName"] = distributionName;
            cfg.ParameterList["ShardId"]          = shard.ShardId;
            cfg.ParameterList["ClusterName"]      = shard.Cluster?.DatasourceName ?? string.Empty;
            cfg.ParameterList["Weight"]           = shard.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture);
            cfg.ParameterList["Tags"]             = FormatTagCsv(shard.Tags);
            return cfg;
        }

        private static string FormatTagCsv(IReadOnlyDictionary<string, string> tags)
        {
            if (tags == null || tags.Count == 0) return string.Empty;
            return string.Join(";", tags.Select(t => Escape(t.Key) + "=" + Escape(t.Value ?? string.Empty)));

            static string Escape(string s)
                => s.Replace("\\", "\\\\").Replace(";", "\\;").Replace("=", "\\=");
        }

        private static IReadOnlyDictionary<string, string> ParseTagCsv(string csv)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(csv)) return dict;

            foreach (var pair in SplitEscaped(csv, ';'))
            {
                var parts = SplitEscaped(pair, '=').ToArray();
                if (parts.Length != 2) continue;
                dict[Unescape(parts[0])] = Unescape(parts[1]);
            }
            return dict;

            static string Unescape(string s)
                => s.Replace("\\=", "=").Replace("\\;", ";").Replace("\\\\", "\\");
        }

        private static IEnumerable<string> SplitEscaped(string input, char separator)
        {
            var sb = new System.Text.StringBuilder();
            var escape = false;
            foreach (var ch in input)
            {
                if (escape)
                {
                    sb.Append('\\').Append(ch);
                    escape = false;
                }
                else if (ch == '\\')
                {
                    escape = true;
                }
                else if (ch == separator)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
                else
                {
                    sb.Append(ch);
                }
            }
            if (escape) sb.Append('\\');
            yield return sb.ToString();
        }
    }
}
