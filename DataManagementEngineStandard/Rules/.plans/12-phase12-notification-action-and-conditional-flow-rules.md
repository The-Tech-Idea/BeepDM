# Phase 12 — Notification, Action & Conditional-Flow Rules

## Purpose
Add `IRule` implementations that go beyond data transformation into *reactive data automation*:
triggering notifications, routing records, branching pipeline flow, and invoking external
actions — the patterns that make tools like n8n, Zapier, and Apache NiFi so powerful.

---

## Motivation — reactive patterns

| Pattern | n8n equivalent | Apache NiFi | BeepDM gap |
|---|---|---|---|
| Route by value | "Switch" node | RouteOnAttribute | ✅ needed |
| Threshold alert | "IF" + "Email" node | PutEmail | ✅ needed |
| Retry on failure | "RetryOnFail" node | RetryFlowFile | ✅ needed |
| Enrich from lookup | "Merge" / "HTTP Request" | LookupRecord | ✅ needed |
| Rate-gate / debounce | "Wait" node | LimitRate | ✅ needed |
| Event correlation | "Merge Node" | MergeContent | future |
| Schema evolution | — | UpdateAttribute | ✅ needed |

---

## Planned Rule Implementations

### Group F — Conditional Routing Rules

#### `RouteOnCondition`
- **Key:** `Route.OnCondition`
- **Inputs:** `Value`, `Conditions` (JSON array of `{Expression, Label}`), `DefaultLabel`
- **Output:** `string` (matched label) — ETL pipeline branches on this value
- **Use:** Multi-branch routing (replaces cascaded IF nodes); durable label returned
  to ETL engine to select next step

#### `RouteOnFieldValue`
- **Key:** `Route.OnFieldValue`
- **Inputs:** `Value`, `Cases` (comma-separated `value:label` pairs), `DefaultLabel`
- **Output:** `string` (matched label)
- **Use:** Lookup-table routing (e.g. country code → regional processor queue)

#### `EvaluateRuleSet`
- **Key:** `Route.EvaluateRuleSet`
- **Inputs:** `RuleKeys` (comma-separated list), `Parameters`, `StopOnFirstFailure` (bool), `IDMEEditor`
- **Output:** `bool` (all passed), `FailedRules` (list in output dict)
- **Use:** Rule chain / composite guard — n8n conditional node equivalent that
  evaluates multiple DQ rules and returns fail summary

---

### Group G — Data Enrichment Rules

#### `LookupAndEnrich`
- **Key:** `Enrich.LookupAndEnrich`
- **Inputs:** `DataSourceName`, `LookupEntity`, `LookupKeyField`, `LookupKeyValue`,
  `FieldsToReturn` (comma-separated), `IDMEEditor`
- **Output:** `Dictionary<string,object>` with matched fields
- **Use:** Enrich streaming records with reference data (e.g. add customer address
  from CRM lookup during order-processing ETL)

#### `FormatFromTemplate`
- **Key:** `Enrich.FormatFromTemplate`
- **Inputs:** `Template` (string with `{FieldName}` placeholders), `Parameters`
- **Output:** `string`
- **Use:** Build notification body, email subject, audit messages;
  equivalent to n8n "Set node" with expression interpolation

#### `MergeRecords`
- **Key:** `Enrich.MergeRecords`
- **Inputs:** `BaseRecord` (dict), `OverrideRecord` (dict), `ConflictStrategy` ("base"|"override"|"concat")
- **Output:** merged `Dictionary<string,object>`
- **Use:** Late-arriving data merge; partial-update pattern

---

### Group H — Notification & Side-Effect Rules

#### `RaiseAlert`
- **Key:** `Notify.RaiseAlert`
- **Inputs:** `Severity` ("info"|"warning"|"error"|"critical"), `Message`, `Source`,
  optional `Tags` (comma-separated), `IDMEEditor`
- **Output:** `bool` (recorded), `AlertId` (GUID string)
- **Side-effect:** writes alert to `IDMEEditor`'s error/log infrastructure via `AddLogMessage`
- **Use:** DQ failure alerting; pipeline exception surfacing; SLA breach notification

#### `EmitAuditTrail`
- **Key:** `Notify.EmitAuditTrail`
- **Inputs:** `EntityName`, `RecordId`, `Action` ("insert"|"update"|"delete"|"validate"),
  `Before` (dict, optional), `After` (dict, optional), `User` (string), `IDMEEditor`
- **Output:** `string` (audit record GUID)
- **Side-effect:** appends structured audit entry to configured audit sink
- **Use:** Change-data-capture pattern; GDPR audit log; ETL lineage tracking

#### `SendWebhook` *(async stub)*
- **Key:** `Notify.SendWebhook`
- **Inputs:** `Url`, `Payload` (JSON string or dict), `Method` ("POST"|"PUT"),
  `Headers` (comma-separated `key=value`), `TimeoutMs` (int)
- **Output:** `bool` (success), `StatusCode` (int), `ResponseBody` (string)
- **Note:** Fires `HttpClient` call synchronously (for now); async variant planned in Phase 9
- **Use:** n8n "Webhook" / "HTTP Request" sink; trigger external systems from ETL rules

---

### Group I — Control-Flow & Resilience Rules

#### `ThrottleExecution`
- **Key:** `Flow.Throttle`
- **Inputs:** `BucketKey` (string), `MaxPerMinute` (int), `IDMEEditor`
- **Output:** `bool` (`true` = allowed, `false` = throttled)
- **Side-effect:** increments per-key counter in shared state (in-process for now)
- **Use:** Rate-gate bulk insert rules; prevent DQ cascade on bad batch

#### `RetryWithBackoff`
- **Key:** `Flow.RetryWithBackoff`
- **Inputs:** `RuleKey` (inner rule to retry), `Parameters`, `MaxAttempts` (int, default 3),
  `BaseDelayMs` (int, default 200), `IDMEEditor`
- **Output:** last inner result; `Attempts` (int) in output dict; `FinalSuccess` (bool)
- **Use:** Transient error resilience; DB connection flaps in ETL; mirrors NiFi RetryFlowFile

#### `CircuitBreaker`
- **Key:** `Flow.CircuitBreaker`
- **Inputs:** `CircuitKey`, `RuleKey` (inner rule), `Parameters`, `FailureThreshold` (int, default 5),
  `ResetAfterSeconds` (int, default 60), `IDMEEditor`
- **Output:** `bool` (executed), `CircuitState` ("closed"|"open"|"half-open"), inner result
- **Use:** Stop hammering failing external endpoints; production resilience pattern

---

### Group J — Schema & Metadata Rules

#### `ValidateEntitySchema`
- **Key:** `Schema.ValidateEntitySchema`
- **Inputs:** `DataSourceName`, `EntityName`, `ExpectedFields` (comma-separated),
  `RequiredFields` (comma-separated), `IDMEEditor`
- **Output:** `bool`, `MissingFields` (list), `ExtraFields` (list)
- **Use:** Schema drift detection before ETL loads; equivalent to dbt `dbt_utils.expression_is_true`

#### `InferAndCastFields`
- **Key:** `Schema.InferAndCastFields`
- **Inputs:** `Record` (dict of string→string), `SchemaDef` (JSON `{field: type}` map)
- **Output:** `Dictionary<string,object>` with values cast to correct types; `CastErrors` list
- **Use:** Dynamic schema mapping in CSV / JSON ingestion pipelines

---

## State management for Group I rules

- `ThrottleExecution`, `CircuitBreaker` need shared counters
- Use a lightweight `IRuleStateStore` interface:

```csharp
public interface IRuleStateStore
{
    long Increment(string key);
    long GetCount(string key);
    void Reset(string key);
    void Set(string key, string value);
    string Get(string key);
}
```

- Default implementation: `InMemoryRuleStateStore` (thread-safe, `ConcurrentDictionary`)
- Future: `DistributedRuleStateStore` backed by `IDMEEditor` config / Redis

---

## Integration Path

- `RuleRegistry` optionally accepts `IRuleStateStore` — injected into rules that require it
  via `(IDMEEditor, IRuleStateStore)` constructor detection
- `EmitAuditTrail` and `RaiseAlert` interact only with `IDMEEditor.AddLogMessage` —
  no new dependencies

---

## Acceptance Criteria

- All 12 rules compile with 0 errors
- `EvaluateRuleSet` correctly propagates `StopOnFirstFailure = true`
- `RetryWithBackoff` does not throw; returns `FinalSuccess = false` after max attempts
- `CircuitBreaker` transitions correctly: closed → open (at threshold) → half-open (after reset)
- `ValidateEntitySchema` detects both missing and extra fields
- `FormatFromTemplate` interpolates `{FieldName}` placeholders from parameters dict
- `IRuleStateStore` interface is in Models; `InMemoryRuleStateStore` is in Engine

---

## File Map

```
DataManagementModelsStandard/Rules/
  IRuleStateStore.cs                    (new interface)

DataManagementEngineStandard/Rules/
  BuiltinRules/
    Route/
      RouteOnCondition.cs
      RouteOnFieldValue.cs
      EvaluateRuleSet.cs
    Enrich/
      LookupAndEnrich.cs
      FormatFromTemplate.cs
      MergeRecords.cs
    Notify/
      RaiseAlert.cs
      EmitAuditTrail.cs
      SendWebhook.cs
    Flow/
      ThrottleExecution.cs
      RetryWithBackoff.cs
      CircuitBreaker.cs
    Schema/
      ValidateEntitySchema.cs
      InferAndCastFields.cs
  InMemoryRuleStateStore.cs             (default state store impl)
```
