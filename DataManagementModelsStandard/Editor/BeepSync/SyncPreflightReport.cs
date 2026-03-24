using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Result of a sync preflight check run before executing any data movement.
    /// Returned by <c>BeepSyncManager.RunPreflightAsync</c>.
    /// </summary>
    public class SyncPreflightReport
    {
        /// <summary>Schema ID that was checked.</summary>
        public string PlanId { get; set; }

        /// <summary>
        /// True when no Error-severity issues were found across all integration channels.
        /// </summary>
        public bool IsApproved { get; set; } = true;

        /// <summary>Mapping quality score returned by MappingManager (0–100). -1 when not evaluated.</summary>
        public int MappingScore { get; set; } = -1;

        /// <summary>Current mapping approval state string as returned by MappingManager.</summary>
        public string MappingState { get; set; }

        /// <summary>True when all rule-engine preflight checks passed (or rule engine is disabled).</summary>
        public bool RulesPassed { get; set; } = true;

        /// <summary>True when a defaults profile was found for the destination entity.</summary>
        public bool DefaultsReady { get; set; } = true;

        /// <summary>UTC timestamp at which the preflight ran.</summary>
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>All issues collected across Schema, Rules, Defaults, and Mapping channels.</summary>
        public List<SyncPreflightIssue> Issues { get; set; } = new List<SyncPreflightIssue>();

        // ── Helpers ──────────────────────────────────────────────────────────────

        public void AddError(string code, string channel, string message)
        {
            IsApproved = false;
            Issues.Add(new SyncPreflightIssue
            {
                Code = code,
                Channel = channel,
                Severity = "Error",
                Message = message
            });
        }

        public void AddWarning(string code, string channel, string message)
        {
            Issues.Add(new SyncPreflightIssue
            {
                Code = code,
                Channel = channel,
                Severity = "Warning",
                Message = message
            });
        }

        public void AddInfo(string code, string channel, string message)
        {
            Issues.Add(new SyncPreflightIssue
            {
                Code = code,
                Channel = channel,
                Severity = "Info",
                Message = message
            });
        }
    }

    /// <summary>A single issue raised during preflight validation.</summary>
    public class SyncPreflightIssue
    {
        /// <summary>Short machine-readable code, e.g. "MAPPING-BELOW-THRESHOLD".</summary>
        public string Code { get; set; }

        /// <summary>Integration channel that raised the issue: "Schema" | "Rules" | "Defaults" | "Mapping".</summary>
        public string Channel { get; set; }

        /// <summary>Human severity level: "Error" | "Warning" | "Info".</summary>
        public string Severity { get; set; }

        /// <summary>Human-readable description of the issue.</summary>
        public string Message { get; set; }
    }
}
