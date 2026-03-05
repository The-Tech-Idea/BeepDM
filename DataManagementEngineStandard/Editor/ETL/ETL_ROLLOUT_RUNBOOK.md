# ETL Rollout Runbook

Operational rollout guidance for integrated ETL behavior (Mapping + Migration + Importing bridges).

## Feature Toggle Baseline

Validate toggles before each rollout wave:

- Import bridge: `_useImportingRunForImports`
- Import fallback: `_enableLegacyImportFallback`
- Copy pipeline bridge: `_useUnifiedCopyPipeline`
- Copy fallback: `_enableLegacyCopyFallback`
- Migration schema bridge: `_useMigrationSchemaBridge`
- Schema fallback: `_enableLegacySchemaFallback`

## Rollout Waves

### Wave 1: Internal validation

1. Run mandatory matrix scenarios on test datasets.
2. Verify run correlation, summary lines, and tracking coherence.
3. Confirm fallback paths execute cleanly when bridge path fails.

Exit criteria:

- All blocking scenarios pass.
- No unresolved blocking defects.

### Wave 2: Pilot workloads

1. Select low-risk ETL workloads with representative volume.
2. Enable integrated bridges with fallback still on.
3. Track error rates, retries, and run duration for 3-5 cycles.

Exit criteria:

- Stable success rate and no major regression.
- Telemetry remains actionable and non-noisy.

### Wave 3: Broader adoption

1. Expand to all default ETL workloads.
2. Keep rollback path available.
3. Review summary metrics weekly during initial adoption.

Exit criteria:

- No critical incidents.
- Stakeholder approval for steady-state operation.

## Rollback Criteria

Trigger rollback if any of the following is observed:

- Repeatable correctness mismatch (row/entity mismatch)
- Severe runtime regression (material duration increase)
- Unrecoverable integration failure without fallback recovery
- Operational telemetry is insufficient for triage

## Rollback Steps

1. Disable integration bridges and keep legacy paths enabled.
2. Re-run failed workload with legacy path.
3. Capture run id, summary, and failure logs for analysis.
4. File remediation actions and re-validate in internal wave.

## Troubleshooting Matrix

| Symptom | Probable Cause | Action |
|---|---|---|
| Import path fails quickly | Invalid import config or mapping | Validate preflight + mapping details |
| Run halts early | `StopErrorCount` reached | Inspect tracking entries and offending entity |
| Slow throughput | Overly chatty progress/logging or provider constraints | Use heartbeat-only progress and verify provider capabilities |
| Schema step fails | Provider DDL limitation or invalid entity shape | Verify migration policy and datasource support |

## Run Evidence Template

Record for each validation/pilot run:

- Run correlation id
- Entry point (`RunCreateScript` / `RunImportScript`)
- Provider and datasource names
- Steps (total/succeeded/failed)
- Records processed
- Cancellation/stop status
- Final outcome and notes
