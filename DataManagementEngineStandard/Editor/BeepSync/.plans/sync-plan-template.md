# Sync Plan Template

Use this template for real BeepSync run planning and execution tracking.

---

## 1) Plan Metadata

- Plan ID:
- Plan Version:
- Date Created:
- Created By:
- Environment: (`dev` / `test` / `stage` / `prod`)
- Change Ticket / Work Item:
- Related Release:
- Correlation ID (reserved for execution):

## 2) Sync Context

- Sync Schema ID:
- Sync Schema Version:
- Sync Direction: (`SourceToDestination` / `DestinationToSource` / `Bidirectional`)
- Sync Type: (`Full` / `Incremental` / `CDC-style`)
- Source Datasource:
- Source Entity:
- Destination Datasource:
- Destination Entity:

## 3) Scope

### In Scope
- Entities:
- Fields:
- Filters:
- Mapping Rules:

### Out of Scope
- 

### Business Goal
- 

## 4) Sync Keys and Incremental Policy

- Source Sync Field:
- Destination Sync Field:
- Watermark Field:
- Watermark Type: (`datetime` / `numeric` / `composite`)
- Replay Window:
- Dedupe Strategy:

## 5) Conflict Resolution (for bidirectional or overlap scenarios)

- Conflict Policy: (`source-wins` / `destination-wins` / `latest-timestamp-wins` / `custom`)
- Tie-Break Rule:
- Quarantine on Unresolved Conflict: (`yes` / `no`)
- Conflict Evidence Retention:

## 6) Validation and Dry-Run

### Pre-Run Validation Checklist
- [ ] Schema validation passed
- [ ] Datasource connectivity validated
- [ ] Source and destination entities exist
- [ ] Field mappings validated
- [ ] Sync keys verified
- [ ] Incremental policy verified

### Dry-Run Evidence
- Dry-run Date:
- Performed By:
- Validation Report Link:
- Translator Output Link:
- Known Warnings:

## 7) DQ and Reconciliation Policy

- Required Field Checks:
- Type Validation Policy:
- Reject Handling:
- Reconciliation Thresholds:
  - Max mismatch count:
  - Max reject ratio:

## 8) Reliability and Retry Policy

- Retry Policy:
  - Max retries:
  - Backoff strategy:
  - Non-retry error classes:
- Idempotency Controls:
- Checkpoint/Resume Enabled: (`yes` / `no`)

## 9) Execution Window and Operations

- Planned Start:
- Planned End:
- Maintenance Window Required: (`yes` / `no`)
- Stakeholder Notification Sent: (`yes` / `no`)
- On-call Owner:

| Step # | Operation | Expected Result | Risk Level | Owner |
|---|---|---|---|---|
| 1 |  |  |  |  |
| 2 |  |  |  |  |
| 3 |  |  |  |  |

## 10) Approvals

| Role | Name | Decision (`approved`/`rejected`) | Date | Notes |
|---|---|---|---|---|
| Sync Owner |  |  |  |  |
| Data Owner |  |  |  |  |
| App Owner |  |  |  |  |
| Change Manager |  |  |  |  |

## 11) Execution Log (During Run)

| Timestamp | Step # | Status (`ok`/`warn`/`fail`) | Message | Action Taken |
|---|---|---|---|---|
|  |  |  |  |  |
|  |  |  |  |  |

## 12) Post-Run Validation

### Technical Checks
- [ ] Sync status is `Success` or approved `Partial`
- [ ] Reconciliation report generated
- [ ] No unresolved blocking conflicts
- [ ] Reject/error channels reviewed

### KPI Snapshot
- Success Rate:
- Records Read:
- Records Written:
- Records Updated:
- Records Skipped:
- Records Rejected:
- Conflict Count:
- Retry Count:
- Freshness Lag:

## 13) Outcome and Sign-Off

- Final Outcome: (`success` / `partial` / `rolled back` / `failed`)
- Incident/Problem Reference:
- Lessons Learned:
- Follow-up Actions:

| Role | Name | Sign-Off Date |
|---|---|---|
| Sync Owner |  |  |
| Data Owner |  |  |
| Change Manager |  |  |

---

## Optional Attachments

- Schema Diff Report:
- Validation Output:
- Reconciliation Report:
- Conflict Log:
- Run Metrics Export:
- Post-Run Summary:
