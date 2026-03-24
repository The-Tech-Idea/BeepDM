using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Routing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// A single routing rule in a <see cref="BrokerRoutingTable"/>.
    /// Topics matching <see cref="TopicPattern"/> (glob syntax) are dispatched to
    /// <see cref="BrokerName"/>.  Lower <see cref="Priority"/> values are evaluated first.
    /// </summary>
    public sealed class BrokerRoute
    {
        /// <summary>
        /// Glob pattern matched against the topic name.
        /// <c>*</c> matches any sequence of characters; <c>?</c> matches exactly one character;
        /// all other characters (including <c>.</c>) are treated as literals.
        /// </summary>
        public string TopicPattern { get; init; }

        /// <summary>Name of the target broker adapter.</summary>
        public string BrokerName { get; init; }

        /// <summary>Evaluation order — lower values are checked first.</summary>
        public int Priority { get; init; }

        /// <summary>Optional human-readable description of the routing intent.</summary>
        public string? Description { get; init; }

        /// <summary>When <c>true</c>, this route is used as the fallback when no other matches.</summary>
        public bool IsDefault { get; init; }
    }

    /// <summary>
    /// Immutable set of routing rules that maps topic names to broker adapter names.
    /// Build via <see cref="Create"/>.
    /// </summary>
    public sealed class BrokerRoutingTable
    {
        /// <summary>Routes ordered by <see cref="BrokerRoute.Priority"/> ascending (lowest first).</summary>
        public IReadOnlyList<BrokerRoute> Routes { get; private init; }

        /// <summary>Fallback broker name used when no route pattern matches.</summary>
        public string DefaultBrokerName { get; private init; }

        private BrokerRoutingTable() { }

        /// <summary>
        /// Returns the broker name for <paramref name="topicName"/>.
        /// Evaluates patterns in priority order; falls back to <see cref="DefaultBrokerName"/>.
        /// </summary>
        public string Resolve(string topicName)
        {
            foreach (var route in Routes)
            {
                if (GlobMatch(route.TopicPattern, topicName))
                    return route.BrokerName;
            }
            return DefaultBrokerName;
        }

        /// <summary>
        /// Creates a <see cref="BrokerRoutingTable"/> from the supplied routes.
        /// Routes are automatically sorted by <see cref="BrokerRoute.Priority"/> ascending.
        /// </summary>
        public static BrokerRoutingTable Create(IEnumerable<BrokerRoute> routes, string defaultBrokerName)
            => new BrokerRoutingTable
            {
                Routes            = routes.OrderBy(r => r.Priority).ToList(),
                DefaultBrokerName = defaultBrokerName ?? throw new ArgumentNullException(nameof(defaultBrokerName))
            };

        // Simple back-tracking glob implementation (*, ?, literal)
        private static bool GlobMatch(string pattern, string input)
        {
            int p = 0, s = 0, starP = -1, starS = 0;

            while (s < input.Length)
            {
                if (p < pattern.Length && (pattern[p] == '?' || pattern[p] == input[s]))
                {
                    p++; s++;
                }
                else if (p < pattern.Length && pattern[p] == '*')
                {
                    starP = p++;
                    starS = s;
                }
                else if (starP != -1)
                {
                    p = starP + 1;
                    s = ++starS;
                }
                else return false;
            }

            while (p < pattern.Length && pattern[p] == '*') p++;
            return p == pattern.Length;
        }
    }

    // ── Mirror config ─────────────────────────────────────────────────────────

    /// <summary>Configuration for a topic mirroring job managed by <c>FederatedBrokerAdapter</c>.</summary>
    public sealed class TopicMirrorConfig
    {
        /// <summary>Unique name for this mirroring job — used to start/stop it.</summary>
        public string MirrorName { get; init; }

        /// <summary>Name of the source broker adapter.</summary>
        public string SourceBrokerName { get; init; }

        /// <summary>Name of the target broker adapter where events are republished.</summary>
        public string TargetBrokerName { get; init; }

        /// <summary>Glob filter applied to topic names on the source broker.</summary>
        public string TopicFilter { get; init; }

        /// <summary>Consumer group used when reading from the source broker.</summary>
        public string ConsumerGroupId { get; init; }

        /// <summary>Optional named transformation pipeline applied before republishing. Not yet implemented.</summary>
        public string? TransformPipeline { get; init; }

        /// <summary>When <c>true</c>, the source partition key is preserved on republish.</summary>
        public bool PreservePartitionKey { get; init; } = true;

        /// <summary>When <c>true</c>, mirroring begins from offset 0 (earliest).</summary>
        public bool StartFromEarliest { get; init; }

        /// <summary>Whether this mirror configuration should be activated immediately.</summary>
        public bool IsActive { get; init; }
    }

    // ── Federation interface ──────────────────────────────────────────────────

    /// <summary>
    /// Manages a collection of named <see cref="IBrokerAdapter"/> instances and routes
    /// publish/subscribe calls to the correct adapter based on topic name patterns.
    /// Implemented by <c>FederatedBrokerAdapter</c>.
    /// </summary>
    public interface IBrokerFederation
    {
        /// <summary>Resolves the adapter to use for <paramref name="topicName"/> per the routing table.</summary>
        IBrokerAdapter ResolveAdapter(string topicName);

        /// <summary>Returns the adapter registered under <paramref name="brokerName"/>.</summary>
        IBrokerAdapter GetAdapterByName(string brokerName);

        /// <summary>Adds or replaces an adapter in the federation under <paramref name="brokerName"/>.</summary>
        void RegisterAdapter(string brokerName, IBrokerAdapter adapter);

        /// <summary>Names of all currently registered adapters.</summary>
        IEnumerable<string> RegisteredBrokerNames { get; }

        /// <summary>The active routing table used to resolve adapter names from topic patterns.</summary>
        BrokerRoutingTable RoutingTable { get; }
    }
}
