# Balanced Recovery Policy Template

Use this template to configure datasource recovery and rejoin behavior for `BalancedDataSource`.

---

## 1) Policy Metadata

- Recovery Policy ID:
- Policy Version:
- Date Created:
- Created By:
- Environment: (`dev` / `test` / `stage` / `prod`)
- Target Datasource Group:
- Change Ticket / Work Item:

## 2) Failure Detection

- Failure Classifiers Enabled:
  - [ ] timeout
  - [ ] network transient
  - [ ] auth failure
  - [ ] connection refused
  - [ ] provider unavailable
  - [ ] custom:
- Failure Threshold (consecutive failures before open):
- Sliding Window Size:
- Error Rate Threshold (%):

## 3) Probe Configuration

- Probe Interval (seconds): **(required)**
- Probe Timeout (seconds):
- Probe Operation Type:
- Probe Query/Action:
- Probe Retries:

## 4) Open -> Half-Open Transition

- Cooldown Before Half-Open (seconds):
- Success Threshold to Enter Half-Open: **(required)**
- Additional Gate (optional):
- Circuit Open Max Duration Before Escalation:

## 5) Half-Open Trial Controls

- Half-Open Trial Count: **(required)**
- Trial Traffic Percent:
- Trial Timeout Override:
- Trial Failure Threshold:
- Trial Success Threshold:

## 6) Half-Open -> Closed Rejoin

- Required Consecutive Successful Trials:
- Rejoin Strategy:
  - [ ] immediate full restore
  - [ ] gradual ramp-up
- Initial Rejoin Weight:
- Rejoin Validation Window:

## 7) Ramp-Up Policy

- Ramp-Up Percent Per Step: **(required)**
- Ramp-Up Step Interval (seconds):
- Max Ramp-Up Duration:
- Auto-Pause on Error Spike: (`yes` / `no`)
- Error Spike Threshold During Ramp:

## 8) Backoff Policy

- Base Backoff (seconds):
- Backoff Strategy: (`linear` / `exponential` / `custom`)
- Backoff Multiplier:
- Jitter Enabled: (`yes` / `no`)
- Max Backoff (seconds): **(required)**

## 9) Pool Refresh/Rehydration

- Purge Broken Connections on Open: (`yes` / `no`)
- Pool Warmup Before Rejoin: (`yes` / `no`)
- Warmup Connection Count:
- Warmup Validation Probe Count:
- Max Stale Connection Age:

## 10) Routing During Recovery

- Route Exclusion While Open: (`yes` / `no`)
- Route Eligibility in Half-Open:
  - [ ] read-only trial
  - [ ] write trial allowed
  - [ ] critical-entity exclusion
- Preferred Workloads During Trial:

## 11) Observability and Alerts

- Emit Recovery State Transitions: (`yes` / `no`)
- Recovery Correlation ID Enabled: (`yes` / `no`)
- Alert on:
  - [ ] prolonged open state
  - [ ] repeated half-open failure
  - [ ] ramp-up rollback event
  - [ ] max backoff reached
- Alert Routing:
  - On-call:
  - Escalation:

## 12) Safety Gates and Hard Stops

- Hard Stop on Consecutive Rejoin Failures:
- Hard Stop on Recovery Latency Exceed:
- Hard Stop on Error Rate During Trial:
- Auto-Revert to Last Known Good Policy: (`yes` / `no`)

## 13) Approval and Sign-Off

| Role | Name | Decision (`approved`/`rejected`) | Date | Notes |
|---|---|---|---|---|
| Proxy/Balanced Owner |  |  |  |  |
| DBA / Platform Owner |  |  |  |  |
| App Owner |  |  |  |  |
| Change Manager |  |  |  |  |

## 14) Validation Checklist

- [ ] Probe interval and timeout validated in test environment.
- [ ] Success thresholds tuned against realistic failure scenarios.
- [ ] Half-open trial behavior verified with synthetic outage.
- [ ] Ramp-up percent and interval validated under load.
- [ ] Max backoff verified and alerting tested.
- [ ] Pool rehydration behavior confirmed.

## 15) Post-Deployment Review

- Recovery Events Observed:
- Mean Time to Recover (MTTR):
- Rejoin Success Rate:
- Rejoin Rollback Count:
- Follow-up Actions:

---

## Optional Attachments

- Recovery Simulation Report:
- Failover Drill Logs:
- Alerting Test Results:
- Before/After KPI Snapshot:
