using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.Services.Audit;
using TheTechIdea.Beep.Services.Audit.Models;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Telemetry;
using TheTechIdea.Beep.Services.Telemetry.Context;

namespace TheTechIdea.Beep.Services.Examples
{
    /// <summary>
    /// End-to-end sample showing how a FormsManager-style host
    /// registers <c>AddBeepAuditForDesktop</c> and emits per-record
    /// audit events through the unified <see cref="IBeepAudit"/>
    /// pipeline. Mirrors the behaviour the
    /// <c>FormsAuditBridge</c> uses to forward legacy
    /// <c>AuditEntry</c> instances.
    /// </summary>
    /// <remarks>
    /// Demonstrates:
    /// <list type="number">
    ///   <item>DI registration with desktop audit defaults.</item>
    ///   <item>Hash-chain enabled (default for desktop preset).</item>
    ///   <item>Insert / Update / Delete events with field-level
    ///         change-sets keyed to a record.</item>
    ///   <item>End-of-session flush followed by integrity verification.</item>
    /// </list>
    /// </remarks>
    public static class AuditFormsExample
    {
        /// <summary>Suggested entry-point name for the sample.</summary>
        public const string SampleAppName = "BeepAuditFormsSample";

        /// <summary>
        /// Builds a configured <see cref="IServiceProvider"/>, runs the
        /// representative workload, flushes the pipeline, and verifies
        /// the chain integrity before disposing the host.
        /// </summary>
        public static async Task<bool> RunAsync(CancellationToken cancellationToken = default)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddBeepAuditForDesktop(SampleAppName);

            ServiceProvider provider = services.BuildServiceProvider();
            await using (provider.ConfigureAwait(false))
            {
                IBeepAudit audit = provider.GetRequiredService<IBeepAudit>();
                await ExecuteAsync(audit, cancellationToken).ConfigureAwait(false);
                await audit.FlushAsync(cancellationToken).ConfigureAwait(false);
                return await audit.VerifyIntegrityAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Emits a typical Forms session: an Insert into the HR_EMP
        /// block, then an Update on the same record, finally a Delete.
        /// Each event carries a field-change set so verifiers can
        /// reconstruct the record state over time.
        /// </summary>
        public static async Task ExecuteAsync(IBeepAudit audit, CancellationToken cancellationToken = default)
        {
            if (audit is null) { throw new ArgumentNullException(nameof(audit)); }
            if (!audit.IsEnabled) { return; }

            using (BeepActivityScope.Begin("Forms.Session", new Dictionary<string, object>
            {
                ["form"] = "HR.EmployeeMaint"
            }))
            {
                await RecordAsync(audit, BuildInsert(), cancellationToken).ConfigureAwait(false);
                await RecordAsync(audit, BuildUpdate(), cancellationToken).ConfigureAwait(false);
                await RecordAsync(audit, BuildDelete(), cancellationToken).ConfigureAwait(false);
            }
        }

        private static Task RecordAsync(IBeepAudit audit, AuditEvent evt, CancellationToken cancellationToken)
            => audit.RecordAsync(evt, cancellationToken);

        private static AuditEvent BuildInsert()
            => new AuditEvent
            {
                Source = "Forms.Block.HR_EMP",
                EntityName = "HR_EMP",
                RecordKey = "EMP-1042",
                UserId = "u-42",
                UserName = "alice",
                Category = AuditCategory.DataAccess,
                Operation = "Insert",
                Outcome = AuditOutcome.Success,
                FieldChanges = new List<AuditFieldChange>
                {
                    new AuditFieldChange("FirstName", null, "Alice"),
                    new AuditFieldChange("LastName",  null, "Carter"),
                    new AuditFieldChange("Salary",    null, 75000m)
                }
            };

        private static AuditEvent BuildUpdate()
            => new AuditEvent
            {
                Source = "Forms.Block.HR_EMP",
                EntityName = "HR_EMP",
                RecordKey = "EMP-1042",
                UserId = "u-42",
                UserName = "alice",
                Category = AuditCategory.DataAccess,
                Operation = "Update",
                Outcome = AuditOutcome.Success,
                FieldChanges = new List<AuditFieldChange>
                {
                    new AuditFieldChange("Salary", 75000m, 82500m)
                }
            };

        private static AuditEvent BuildDelete()
            => new AuditEvent
            {
                Source = "Forms.Block.HR_EMP",
                EntityName = "HR_EMP",
                RecordKey = "EMP-1042",
                UserId = "u-99",
                UserName = "admin",
                Category = AuditCategory.DataAccess,
                Operation = "Delete",
                Outcome = AuditOutcome.Success,
                Reason = "Termination",
                FieldChanges = new List<AuditFieldChange>()
            };
    }
}
