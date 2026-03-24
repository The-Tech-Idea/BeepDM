using System;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.BeepSync;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Runtime holder for the three optional integration engines that
    /// <see cref="BeepSyncManager"/> can use during a sync run.
    ///
    /// All three references are optional.  When null the corresponding
    /// feature is silently skipped, preserving backward compatibility.
    /// </summary>
    public class SyncIntegrationContext
    {
        // ── Rule Engine ──────────────────────────────────────────────────────────

        /// <summary>
        /// Optional Rule Engine reference.
        /// When non-null and <c>schema.RulePolicy.Enabled == true</c>, preflight
        /// and per-record rules are evaluated through this engine.
        /// </summary>
        public TheTechIdea.Beep.Rules.IRuleEngine RuleEngine { get; set; }

        // ── Defaults Manager ─────────────────────────────────────────────────────

        /// <summary>
        /// Optional Defaults Manager reference.
        /// When non-null and <c>schema.DefaultsPolicy.Enabled == true</c>,
        /// <c>DefaultsManager.Apply</c> is called before each destination write.
        /// </summary>
        public IDefaultsManager DefaultsManager { get; set; }

        // ── Mapping Manager ──────────────────────────────────────────────────────

        // MappingManager is accessed via its static facade; no interface reference
        // is needed at runtime.  The fields below track the compiled plan state.

        /// <summary>
        /// Version of the compiled mapping plan active at the start of this run.
        /// Refreshed on <c>MappingManager.MappingUpdated</c> events.
        /// </summary>
        public int MappingPlanVersion { get; set; } = -1;

        /// <summary>
        /// Identifier for the governance scope opened for this run.
        /// Set by <c>BeginGovernanceScope</c> in MappingManager (via string to avoid
        /// a hard dependency on the mapping assembly in the models project).
        /// </summary>
        public string GovernanceScopeId { get; set; }

        // ── Run initiator ────────────────────────────────────────────────────────

        /// <summary>User or service account that initiated the current sync run.</summary>
        public string RunInitiatedBy { get; set; } = Environment.UserName;

        /// <summary>
        /// Correlation identifier propagated to alert records and telemetry.
        /// Defaults to a new GUID per run start.
        /// </summary>
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    }
}
