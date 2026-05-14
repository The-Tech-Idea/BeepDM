# Phase 7 — Observability, Reporting, and Audit

## Objective

Produce a complete, tamper-evident `SetupReport` for every wizard run, emit structured telemetry events throughout the setup lifecycle, and export machine-readable and human-readable audit artifacts that can be consumed by CI/CD pipelines, compliance workflows, and operational dashboards.

This phase wires together the observability outputs from `MigrationManager` (`GetMigrationAuditEvents`, `GetMigrationTelemetrySnapshot`) with wizard-level step results, seeder outcomes, and rollback events into a single, hash-verified record.

---

## Scope

- Finalize `SetupReport` schema (step results, migration audit, seeder audit, rollback)
- `SetupTelemetryEvent` enum and structured event model
- `SetupReportExporter` (JSON + Markdown)
- `SetupAuditEntry` per-event audit trail
- `BeepLog` / `IDMLogger` integration throughout wizard lifecycle
- `SetupReport.ContentHash` (SHA-256) for tamper detection
- `ISetupWizard.GetReport()` post-run contract (declared Phase 1, finalized here)

---

## Finalized `SetupReport` Schema

```csharp
namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Immutable, tamper-evident record of a complete wizard run.
    /// Produced by ISetupWizard.GetReport() after Run() or Resume() completes.
    /// </summary>
    public class SetupReport
    {
        // Identity
        public string WizardId { get; set; }
        public string RunId { get; set; } = Guid.NewGuid().ToString("N");
        public string Environment { get; set; }

        // Outcome
        public bool Succeeded { get; set; }
        public string FailureReason { get; set; }

        // Timing
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset FinishedAt { get; set; }
        public TimeSpan TotalElapsed => FinishedAt - StartedAt;

        // Step results
        public IReadOnlyList<SetupStepResult> StepResults { get; set; }

        // Migration subsystem
        public string MigrationPlanId { get; set; }
        public string DryRunReportJson { get; set; }
        public IReadOnlyList<MigrationAuditEvent> MigrationAuditEvents { get; set; }
        public string MigrationTelemetryJson { get; set; }

        // Seeding subsystem
        public IReadOnlyList<SeederAuditEntry> SeederAuditEntries { get; set; }

        // Rollback (populated only when rollback was triggered)
        public string RollbackReportJson { get; set; }

        // Tamper detection
        /// <summary>SHA-256 of the canonical JSON of StepResults + MigrationAuditEvents.</summary>
        public string ContentHash { get; set; }

        // Raw audit trail
        public IReadOnlyList<SetupAuditEntry> AuditTrail { get; set; }
    }

    public class SetupStepResult
    {
        public string StepId { get; set; }
        public string StepName { get; set; }
        public bool Succeeded { get; set; }
        public bool Skipped { get; set; }
        public string Message { get; set; }
        public TimeSpan Elapsed { get; set; }
        public DateTimeOffset ExecutedAt { get; set; }
    }

    public class SeederAuditEntry
    {
        public string SeederId { get; set; }
        public string SeederName { get; set; }
        public bool Succeeded { get; set; }
        public bool Skipped { get; set; }
        public int RecordsInserted { get; set; }
        public TimeSpan Elapsed { get; set; }
        public string Message { get; set; }
    }

    public class SetupAuditEntry
    {
        public SetupTelemetryEvent Event { get; set; }
        public string StepId { get; set; }
        public string Message { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}
```

---

## `SetupTelemetryEvent` Enum

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public enum SetupTelemetryEvent
    {
        WizardStarted,
        WizardCompleted,
        WizardFailed,

        StepStarted,
        StepCompleted,
        StepFailed,
        StepSkipped,

        ConnectionResolved,
        ConnectionOpened,
        ConnectionFailed,

        MigrationPlanBuilt,
        MigrationPolicyEvaluated,
        MigrationPlanApproved,
        MigrationExecutionStarted,
        MigrationExecutionCompleted,
        MigrationExecutionFailed,
        MigrationRolledBack,

        SeederStarted,
        SeederCompleted,
        SeederFailed,
        SeederSkipped,

        PreflightPassed,
        PreflightFailed,
        SchemaHealthCheckPassed,
        SchemaHealthCheckFailed,
        SeedHealthCheckPassed,
        SeedHealthCheckFailed,

        RollbackStarted,
        RollbackCompleted,
        RollbackFailed
    }
}
```

---

## Audit Collector

An in-process collector that accumulates `SetupAuditEntry` records during a wizard run.

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public class SetupAuditCollector
    {
        private readonly List<SetupAuditEntry> _entries = new();
        private readonly IDMLogger _logger;

        public SetupAuditCollector(IDMLogger logger = null)
        {
            _logger = logger;
        }

        public void Emit(SetupTelemetryEvent evt, string stepId, string message,
            Dictionary<string, string> metadata = null)
        {
            var entry = new SetupAuditEntry
            {
                Event = evt,
                StepId = stepId,
                Message = message,
                Timestamp = DateTimeOffset.UtcNow,
                Metadata = metadata ?? new Dictionary<string, string>()
            };
            _entries.Add(entry);
            _logger?.WriteLog($"[Setup:{evt}] step={stepId} {message}");
        }

        public IReadOnlyList<SetupAuditEntry> GetAll() => _entries.AsReadOnly();
    }
}
```

---

## `SetupReportExporter`

Exports `SetupReport` to JSON (machine-readable) and Markdown (human-readable approval doc).

```csharp
namespace TheTechIdea.Beep.SetUp
{
    public static class SetupReportExporter
    {
        private static readonly System.Text.Json.JsonSerializerOptions _jsonOpts =
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true };

        /// <summary>Export the full report as indented JSON.</summary>
        public static string ToJson(SetupReport report) =>
            System.Text.Json.JsonSerializer.Serialize(report, _jsonOpts);

        /// <summary>Export the report as a Markdown approval document.</summary>
        public static string ToMarkdown(SetupReport report)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# Setup Run Report");
            sb.AppendLine();
            sb.AppendLine($"| Field | Value |");
            sb.AppendLine($"|---|---|");
            sb.AppendLine($"| Wizard ID | `{report.WizardId}` |");
            sb.AppendLine($"| Run ID | `{report.RunId}` |");
            sb.AppendLine($"| Environment | `{report.Environment}` |");
            sb.AppendLine($"| Started | {report.StartedAt:u} |");
            sb.AppendLine($"| Finished | {report.FinishedAt:u} |");
            sb.AppendLine($"| Total Elapsed | {report.TotalElapsed:mm\\:ss\\.fff} |");
            sb.AppendLine($"| Outcome | **{(report.Succeeded ? "✓ Success" : "✗ Failed")}** |");
            sb.AppendLine($"| Content Hash | `{report.ContentHash}` |");
            sb.AppendLine();

            sb.AppendLine("## Step Results");
            sb.AppendLine();
            sb.AppendLine("| Step | Result | Elapsed | Message |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var r in report.StepResults ?? Enumerable.Empty<SetupStepResult>())
            {
                var status = r.Skipped ? "⏭ Skipped" : (r.Succeeded ? "✓ OK" : "✗ Failed");
                sb.AppendLine($"| {r.StepName} | {status} | {r.Elapsed:mm\\:ss\\.fff} | {r.Message} |");
            }
            sb.AppendLine();

            if (report.SeederAuditEntries?.Count > 0)
            {
                sb.AppendLine("## Seeder Results");
                sb.AppendLine();
                sb.AppendLine("| Seeder | Result | Records | Elapsed |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var s in report.SeederAuditEntries)
                {
                    var status = s.Skipped ? "⏭ Skipped" : (s.Succeeded ? "✓ OK" : "✗ Failed");
                    sb.AppendLine($"| {s.SeederName} | {status} | {s.RecordsInserted} | {s.Elapsed:mm\\:ss\\.fff} |");
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(report.FailureReason))
            {
                sb.AppendLine("## Failure Details");
                sb.AppendLine();
                sb.AppendLine($"> {report.FailureReason}");
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(report.RollbackReportJson))
            {
                sb.AppendLine("## Rollback");
                sb.AppendLine();
                sb.AppendLine("```json");
                sb.AppendLine(report.RollbackReportJson);
                sb.AppendLine("```");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine($"*Generated by BeepDM Setup Wizard on {DateTimeOffset.UtcNow:u}*");

            return sb.ToString();
        }

        /// <summary>
        /// Write report artifacts to a directory.
        /// Creates: setup-report-{runId}.json and setup-report-{runId}.md
        /// </summary>
        public static void WriteToDirectory(SetupReport report, string outputPath)
        {
            Directory.CreateDirectory(outputPath);
            var jsonPath = Path.Combine(outputPath, $"setup-report-{report.RunId}.json");
            var mdPath   = Path.Combine(outputPath, $"setup-report-{report.RunId}.md");
            File.WriteAllText(jsonPath, ToJson(report), System.Text.Encoding.UTF8);
            File.WriteAllText(mdPath, ToMarkdown(report), System.Text.Encoding.UTF8);
        }
    }
}
```

---

## Content Hash Computation

The `ContentHash` must be reproducible and stable so CI pipelines can detect report tampering.

```csharp
// In SetupWizard.BuildReport():
private static string ComputeContentHash(IReadOnlyList<SetupStepResult> steps,
    IReadOnlyList<MigrationAuditEvent> migrationEvents)
{
    var canonical = System.Text.Json.JsonSerializer.Serialize(new
    {
        steps = steps.Select(s => new { s.StepId, s.Succeeded, s.Skipped, s.Message }),
        migration = migrationEvents?.Select(e => new { e.EventType, e.Timestamp, e.Message })
    });
    var bytes = System.Text.Encoding.UTF8.GetBytes(canonical);
    return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
}
```

---

## Logging Integration

Every significant event in the wizard lifecycle should emit a structured log via `IDMLogger`:

```csharp
// Wizard start
editor.Logger?.WriteLog($"[SetupWizard:{wizardId}] Run started. Environment={opts.Environment} DryRun={opts.DryRun}");

// Step start
editor.Logger?.WriteLog($"[SetupWizard:{wizardId}] Step '{step.StepId}' starting.");

// Step success
editor.Logger?.WriteLog($"[SetupWizard:{wizardId}] Step '{step.StepId}' completed in {elapsed:mm\\:ss}.");

// Step failure
editor.Logger?.WriteLog($"[SetupWizard:{wizardId}] Step '{step.StepId}' FAILED: {result.Message}");

// Seeder start / finish
editor.Logger?.WriteLog($"[SeedingStep] Seeder '{seeder.SeederId}' starting.");
editor.Logger?.WriteLog($"[SeedingStep] Seeder '{seeder.SeederId}' completed. {count} records inserted.");
```

---

## MigrationManager Observability Integration

After `SchemaSetupStep` executes, harvest migration observability data into the context:

```csharp
// In SchemaSetupStep.Execute, after successful execution:
var migration = new MigrationManager(editor, ds);

var telemetry = migration.GetMigrationTelemetrySnapshot(planResult.PlanId);
context.Properties["MigrationTelemetryJson"] =
    System.Text.Json.JsonSerializer.Serialize(telemetry);

var auditEvents = migration.GetMigrationAuditEvents(planResult.PlanId);
context.Properties["MigrationAuditEvents"] =
    System.Text.Json.JsonSerializer.Serialize(auditEvents);
```

Then in `SetupWizard.BuildReport()`, retrieve and include these in `SetupReport`.

---

## Report Output Modes

| Mode | Trigger | Output |
|---|---|---|
| In-memory only | `SetupOptions.ReportOutputPath = null` | `ISetupWizard.GetReport()` in-process only |
| File export | `ReportOutputPath` set | JSON + MD files in the specified directory |
| CI artifact | `DryRun = true` + pipeline | JSON written to `$GITHUB_STEP_SUMMARY` or similar |
| Blazor/Web | adapter post-hook | JSON returned from `/api/setup/status` endpoint |

---

## File Layout

```
DataManagementEngineStandard/
  SetUp/
    SetupAuditCollector.cs
    SetupAuditEntry.cs
    SetupTelemetryEvent.cs
    SetupReportExporter.cs
    SeederAuditEntry.cs
```

---

## Testing Approach

| Test | Description |
|---|---|
| `SetupReport_ContentHash_ChangesOnStepChange` | Altering any step result changes the hash |
| `SetupReport_ContentHash_StableForSameInput` | Same inputs produce same hash deterministically |
| `SetupReportExporter_ToJson_RoundTrips` | Deserialize JSON back to SetupReport with same fields |
| `SetupReportExporter_ToMarkdown_ContainsAllSteps` | Each step name appears in MD output |
| `SetupReportExporter_WriteToDirectory_CreatesBothFiles` | JSON and MD written to output path |
| `SetupAuditCollector_EmitsLogEntry_PerEvent` | Each Emit call writes to IDMLogger |
| `SetupWizard_IntegratesMigrationAudit_InReport` | MigrationAuditEvents populated in SetupReport |

---

## Acceptance Criteria

- [ ] `SetupReport` includes `MigrationAuditEvents` from `MigrationManager.GetMigrationAuditEvents`.
- [ ] `SetupReport.ContentHash` is SHA-256 of canonical JSON of steps + migration events.
- [ ] `SetupReportExporter.ToMarkdown()` produces a table with all step names and statuses.
- [ ] `SetupReportExporter.WriteToDirectory()` writes both `.json` and `.md` files.
- [ ] Every `SetupTelemetryEvent` variant has a corresponding `SetupAuditCollector.Emit` call in wizard code.
- [ ] All significant events are logged via `IDMLogger.WriteLog`.
- [ ] `SetupReport.SeederAuditEntries` is populated with per-seeder results (including record count).
