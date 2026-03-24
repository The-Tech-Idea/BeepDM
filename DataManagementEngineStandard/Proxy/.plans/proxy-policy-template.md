# Proxy Policy Template

Use this template to define a production-ready `ProxyDataSource` policy profile.

---

## 1) Policy Metadata

- Policy ID:
- Policy Version:
- Date Created:
- Created By:
- Environment: (`dev` / `test` / `stage` / `prod`)
- Change Ticket / Work Item:
- Related Release:
- Policy Status: (`draft` / `review` / `approved` / `deprecated`)

## 2) Proxy Scope

- Proxy Name / Alias:
- Datasource Group:
- Underlying Data Sources:
  - Primary:
  - Secondary:
  - Tertiary:
- Workload Class: (`read-heavy` / `write-heavy` / `mixed`)

## 3) Routing Profile

- Routing Strategy:
  - [ ] weighted-latency-aware
  - [ ] least-outstanding
  - [ ] sticky-key
  - [ ] custom
- Route Weights:
  - Source A:
  - Source B:
  - Source C:
- Read/Write Split Policy:
- Entity-Specific Routing Rules:
- Health Hysteresis Enabled: (`yes` / `no`)

## 4) Retry Profile

- Max Retries:
- Base Delay (ms):
- Backoff Strategy: (`linear` / `exponential` / `custom`)
- Max Backoff (ms):
- Retryable Error Classes:
  - [ ] timeout
  - [ ] transient network
  - [ ] connection reset
  - [ ] provider throttling
  - [ ] other:
- Non-Retryable Error Classes:
- Write Retry Idempotency Guard: (`required` / `optional` / `disabled`)

## 5) Circuit Breaker Profile

- Enabled: (`yes` / `no`)
- Failure Threshold:
- Sliding Window Size:
- Open State Timeout (seconds):
- Half-Open Trial Requests:
- Trip Conditions:
- Reset Conditions:

## 6) Health Check Profile

- Enabled: (`yes` / `no`)
- Interval (ms):
- Timeout (ms):
- Probe Operation:
- Degraded Threshold:
- Unhealthy Threshold:

## 7) Cache Profile

- Caching Enabled: (`yes` / `no`)
- Default TTL:
- Entity-Specific TTL Overrides:
- Cache Key Strategy:
- Max Cache Items:
- Eviction Policy:
- Consistency Mode:
  - [ ] write-through invalidation
  - [ ] stale-while-revalidate
  - [ ] strict no-stale
- Sensitive Entity Cache Disabled List:

## 8) Security and Audit Controls

- Route Allowlist Policy:
- Sensitive Query Redaction:
- Audit Event Level:
- Policy Version Stamped in Logs: (`yes` / `no`)
- Security Exception Process:

## 9) SLO and Alert Thresholds

- Success Rate SLO:
- p95 Latency SLO:
- Failover Rate Threshold:
- Circuit-Open Duration Threshold:
- Error Spike Threshold:
- Cache Hit Ratio Target:
- Alert Routing:
  - On-call:
  - Escalation:

## 10) Capacity and Performance Controls

- Max Concurrency:
- Pool Size per Source:
- Idle Connection Timeout:
- Throttle Policy:
- Overload Behavior: (`queue` / `shed` / `degrade`)
- Expected Peak RPS:

## 11) Validation and Simulation

- Pre-Deployment Checklist:
  - [ ] Policy schema validation passed
  - [ ] Routing simulation passed
  - [ ] Failover simulation passed
  - [ ] Retry/circuit interaction test passed
  - [ ] Cache consistency checks passed
- Simulation Report Link:

## 12) Rollout Plan

- Rollout Mode: (`canary` / `wave` / `full`)
- Wave 1 Scope:
- Wave 2 Scope:
- Wave 3 Scope:
- Hard Stop Triggers:
- Rollback Policy Version:

## 13) Approvals

| Role | Name | Decision (`approved`/`rejected`) | Date | Notes |
|---|---|---|---|---|
| Proxy Owner |  |  |  |  |
| Platform/DBA |  |  |  |  |
| App Owner |  |  |  |  |
| Change Manager |  |  |  |  |

## 14) Post-Deployment Review

- Validation Date:
- Baseline vs Current Metrics:
  - Success Rate:
  - p95 Latency:
  - Failover Count:
  - Circuit Open Time:
  - Cache Hit Ratio:
- Incidents/Exceptions:
- Follow-up Actions:

---

## Optional Attachments

- Policy Diff Report:
- Simulation Output:
- Load Test Summary:
- Security Review Notes:
- Post-Deployment KPI Snapshot:
