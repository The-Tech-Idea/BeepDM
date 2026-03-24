# Migration Plan Template

Use this template for real datasource migration runs. Fill all sections before execution.

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

## 2) Datasource Context

- Datasource Name:
- Datasource Type:
- Datasource Category:
- Connection/Profile Reference:
- Provider Capability Notes:
- Target Schema/Namespace:

## 3) Migration Scope

### In Scope
- Entities:
- Columns:
- Indexes/Constraints:
- Other Objects:

### Out of Scope
- 

### Business Reason
- 

## 4) Discovery and Input Mode

- Migration Input Mode: (`explicit types` / `discovery`)
- Assembly Registration Required: (`yes` / `no`)
- Registered Assemblies:
- Namespace Filter (if used):
- Entity Type Count:

## 5) Preflight and Dry-Run

### Preflight Checklist
- [ ] Connection and permissions verified
- [ ] Backup/snapshot completed
- [ ] Restore procedure validated
- [ ] Provider capability checks reviewed
- [ ] Drift check since plan creation completed

### Dry-Run Evidence
- Dry-run Date:
- Dry-run Performed By:
- Summary Output Link:
- DDL Preview Link:
- Detected Risks:

## 6) Operation Plan

| Step # | Operation | Entity/Object | Risk Class (`safe`/`warning`/`high`) | Expected Duration | Owner |
|---|---|---|---|---|---|
| 1 |  |  |  |  |  |
| 2 |  |  |  |  |  |
| 3 |  |  |  |  |  |

## 7) Policy and Risk Assessment

- Overall Risk Score:
- Blocking Issues:
- Warnings:
- Approval Required for High-Risk Ops: (`yes` / `no`)
- Exception Justification (if any):

## 8) Rollback / Compensation Plan

### Rollback Strategy
- Reversible Steps:
- Non-Reversible Steps:
- Compensation Actions:

### Rollback Triggers
- 

### Rollback Execution Owner
- 

## 9) Execution Window

- Planned Start:
- Planned End:
- Maintenance Window:
- Freeze Window Confirmed: (`yes` / `no`)
- Stakeholder Notification Sent: (`yes` / `no`)

## 10) Approvals

| Role | Name | Decision (`approved`/`rejected`) | Date | Notes |
|---|---|---|---|---|
| Migration Owner |  |  |  |  |
| DBA / Platform Owner |  |  |  |  |
| App Owner |  |  |  |  |
| Change Manager |  |  |  |  |

## 11) Execution Log (During Run)

| Timestamp | Step # | Status (`ok`/`warn`/`fail`) | Message | Action Taken |
|---|---|---|---|---|
|  |  |  |  |  |
|  |  |  |  |  |

## 12) Post-Cutover Validation

### Technical Checks
- [ ] Schema matches expected state
- [ ] Migration summary has no unresolved blockers
- [ ] App smoke tests passed
- [ ] Data integrity checks passed
- [ ] Performance baseline acceptable

### KPI Snapshot
- Success Rate:
- Failed Steps:
- Retry Count:
- Rollback Invoked: (`yes` / `no`)
- Mean Step Duration:

## 13) Outcome and Sign-Off

- Final Outcome: (`success` / `partial` / `rolled back` / `failed`)
- Incident Reference (if any):
- Lessons Learned:
- Follow-up Actions:

| Role | Name | Sign-Off Date |
|---|---|---|
| Migration Owner |  |  |
| App Owner |  |  |
| Change Manager |  |  |

---

## Optional Attachments

- Plan Diff from Previous Version:
- Readiness Report Export:
- Policy Evaluation Report:
- Dry-Run Logs:
- Execution Logs:
- Post-Cutover Validation Report:
