using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Proxy
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Node status event
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised by <see cref="ProxyCluster"/> when a node transitions between
    /// alive and down states.
    /// </summary>
    public sealed class NodeStatusEventArgs : EventArgs
    {
        /// <summary>The node that changed state.</summary>
        public string NodeId { get; }

        /// <summary><c>true</c> = node came back alive; <c>false</c> = node went down.</summary>
        public bool IsAlive { get; }

        /// <summary>Human-readable reason for the state change (probe failure message, etc.).</summary>
        public string Reason { get; }

        /// <summary>UTC timestamp of the state change.</summary>
        public DateTime OccurredAt { get; } = DateTime.UtcNow;

        public NodeStatusEventArgs(string nodeId, bool isAlive, string reason = null)
        {
            NodeId  = nodeId;
            IsAlive = isAlive;
            Reason  = reason;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Cluster policy-changed event
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised by <see cref="ProxyCluster"/> when <see cref="ProxyCluster.ApplyClusterPolicy"/>
    /// successfully commits a new policy.
    /// </summary>
    public sealed class ClusterPolicyChangedEventArgs : EventArgs
    {
        /// <summary>The newly applied policy.</summary>
        public ProxyPolicy NewPolicy { get; }

        /// <summary>UTC timestamp when the policy was applied.</summary>
        public DateTime AppliedAt { get; } = DateTime.UtcNow;

        public ClusterPolicyChangedEventArgs(ProxyPolicy policy)
        {
            NewPolicy = policy ?? throw new ArgumentNullException(nameof(policy));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Affinity-rebalanced event
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised by <see cref="ProxyCluster"/> when entity-affinity assignments are
    /// redistributed following a node-down or node-add event.
    /// </summary>
    public sealed class AffinityRebalancedEventArgs : EventArgs
    {
        /// <summary>
        /// Map of entity name / prefix → new assigned node ID after rebalancing.
        /// </summary>
        public IReadOnlyDictionary<string, string> Reassignments { get; }

        public AffinityRebalancedEventArgs(IReadOnlyDictionary<string, string> reassignments)
        {
            Reassignments = reassignments ?? throw new ArgumentNullException(nameof(reassignments));
        }
    }
}
