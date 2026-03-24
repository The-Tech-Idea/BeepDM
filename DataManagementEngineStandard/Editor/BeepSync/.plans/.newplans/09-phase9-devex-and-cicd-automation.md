# Phase 9 (Enhanced) — DevEx, CI/CD, and Integration Validation Pipeline

## Supersedes
`../09-phase9-devex-and-cicd-automation.md`

## Objective
Integrate Rule Engine catalog linting, Mapping governance diff, and Defaults profile
export/import into the CI/CD pipeline — making sync schema promotion a gate-protected,
auditable engineering workflow.

---

## Scope
- CLI / programmatic schema validation (dry run with `SyncPreflightReport`).
- Rule catalog linting: unique keys, no deprecated rules, lifecycle minimum.
- Mapping governance diff in PR description generation.
- Defaults profile export for environment promotion (dev → staging → production).
- CI gate: mapping quality score, rule catalog state, defaults profile parity.

---

## File Targets

| File | Change Description |
|---|---|
| `BeepSync/BeepSyncManager.Orchestrator.cs` | Add `ValidateSchemaForCiAsync(schema, strictMode)` returning `SyncCiGateResult` |
| `BeepSync/Helpers/SyncValidationHelper.cs` | Add `LintRuleCatalog(...)`, `ValidateDefaultsProfile(...)`, `ValidateMappingForPromotion(...)` |
| `BeepSync/Models/SyncCiGateResult.cs` *(new)* | CI gate output: pass/fail, per-check results, diff text, remediation steps |

---

## Integration Points: Rule Engine

### 1. Rule Catalog Lint Check
As part of the CI pipeline, lint all rules referenced by the sync schema:
```csharp
public SyncCiGateResult LintRuleCatalog(DataSyncSchema schema, IRuleEngine ruleEngine, IRuleCatalog catalog)
{
    var result = new SyncCiGateResult();
    var allRuleKeys = schema.AllReferencedRuleKeys();  // aggregate from all policy objects

    foreach (var ruleKey in allRuleKeys)
    {
        var catalogEntry = catalog.GetByName(ruleKey);

        if (catalogEntry == null)
        {
            result.AddFailure("RULE-NOT-FOUND",
                $"Rule '{ruleKey}' referenced in schema '{schema.SchemaID}' is not registered in RuleCatalog.");
            continue;
        }

        if (catalogEntry.LifecycleState == RuleLifecycleState.Deprecated)
        {
            result.AddWarning("RULE-DEPRECATED",
                $"Rule '{ruleKey}' is deprecated. Promote or replace before production rollout.");
        }

        // Dry-parse the rule to check for tokenizer issues
        var parseResult = ruleEngine.ParseRule(ruleKey);
        if (parseResult.HasDiagnostics)
            result.AddWarning("RULE-PARSE-DIAGNOSTIC", parseResult.DiagnosticsSummary);
    }

    return result;
}
```

### 2. Rule Uniqueness Enforcement
CI check: verify no two schemas reference the same conflict/CDC rule with conflicting policy configurations:
```csharp
// Find schemas that share a conflict rule key but have different resolution modes
var conflictingSchemas = allSchemas
    .GroupBy(s => s.ConflictPolicy?.ResolutionRuleKey)
    .Where(g => g.Key != null && g.Select(s => s.SyncDirection).Distinct().Count() > 1)
    .ToList();
```

### 3. CI Gate Criteria for Rule Engine
| Check | Pass Condition |
|---|---|
| All referenced rule keys exist in `RuleCatalog` | 0 `RULE-NOT-FOUND` failures |
| No deprecated rules in production-bound schemas | 0 `RULE-DEPRECATED` warnings (strict mode) |
| All rules parse without errors | 0 parse-level errors in dry run |
| `RuleExecutionPolicy.MaxDepth` ≥ 3 for all production schemas | Depth ≥ 3 |

---

## Integration Points: Mapping Manager

### 1. Mapping Governance Diff for PR Description
Generate a human-readable diff between the current and previously approved mapping version
for inclusion in pull request descriptions:
```csharp
var diff = MappingManager.BuildMappingVersionDiffText(
    schema.SourceDataSourceName,
    schema.DestinationDataSourceName,
    schema.SourceEntityName,
    fromVersion: baseline.MappingVersion,
    toVersion:   candidate.MappingVersion);

ciResult.MappingDiffText = diff;
// Caller embeds this in PR body / pipeline summary artifact
```

### 2. Mapping Quality CI Gate
```csharp
var quality = MappingManager.ValidateMappingQuality(
    schema.SourceDataSourceName, schema.DestinationDataSourceName, schema.SourceEntityName);

if (quality.QualityScore < schema.MappingPolicy?.MinQualityScore ?? 70)
    ciResult.AddFailure("MAPPING-QUALITY-BELOW-THRESHOLD",
        $"Score {quality.QualityScore} < threshold {schema.MappingPolicy.MinQualityScore}. " +
        "Review auto-map suggestions and resolve low-confidence fields.");

if (quality.HasDriftWarnings)
    ciResult.AddWarning("MAPPING-DRIFT-DETECTED", quality.DriftSummary);
```

### 3. Required Approval State for Production Schemas
```csharp
var mapVersion = MappingManager.GetCurrentMappingVersionMeta(
    schema.SourceDataSourceName, schema.DestinationDataSourceName, schema.SourceEntityName);

if (schema.MappingPolicy?.RequiredApprovalState == MappingApprovalState.Approved
    && mapVersion.ApprovalState != MappingApprovalState.Approved)
{
    ciResult.AddFailure("MAPPING-NOT-APPROVED",
        $"Mapping approval state is '{mapVersion.ApprovalState}'. " +
        "Must be 'Approved' before production schema is eligible for rollout.");
}
```

---

## Integration Points: Defaults Manager

### 1. Profile Export for Environment Promotion
Generate a portable profile export per destination entity for easy promotion:
```csharp
// Export from source environment
var profileJson = DefaultsManager.ExportDefaults(editor, schema.DestinationDataSourceName);

// Import into target environment (CI artifact or manual promotion)
DefaultsManager.ImportDefaults(editor, targetDsName: schema.DestinationDataSourceName,
    json: profileJson, overwrite: false);
// overwrite: false = do not replace manually customized defaults in target
```

### 2. Profile Parity Check
CI verification that the destination entity's `EntityDefaultsProfile` exists and is initialised:
```csharp
var profile = DefaultsManager.GetProfile(
    schema.DestinationDataSourceName, schema.DestinationEntityName);

if (profile == null || profile.Defaults.Count == 0)
    ciResult.AddWarning("DEFAULTS-PROFILE-EMPTY",
        $"No defaults profile found for '{schema.DestinationEntityName}'. " +
        "Destination fields without source mappings will be null on insert.");

// Verify required audit fields are covered
var requiredAuditFields = new[] { "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy" };
var missingAuditDefaults = requiredAuditFields
    .Where(f => profile?.Defaults.All(d => d.FieldName != f) ?? true)
    .ToList();

if (missingAuditDefaults.Any())
    ciResult.AddWarning("DEFAULTS-AUDIT-FIELDS-MISSING",
        $"Audit fields missing from defaults profile: {string.Join(", ", missingAuditDefaults)}");
```

---

## `SyncCiGateResult` Model

```csharp
public class SyncCiGateResult
{
    public string   SchemaId          { get; set; }
    public bool     Passed            { get; set; }
    public string   MappingDiffText   { get; set; }
    public int      MappingScore      { get; set; }
    public List<CiGateItem> Failures  { get; set; } = new();
    public List<CiGateItem> Warnings  { get; set; } = new();

    public void AddFailure(string code, string message)
    {
        Passed = false;
        Failures.Add(new CiGateItem { Code = code, Message = message, Severity = "Error" });
    }

    public void AddWarning(string code, string message)
        => Warnings.Add(new CiGateItem { Code = code, Message = message, Severity = "Warning" });
}

public class CiGateItem
{
    public string Code      { get; set; }
    public string Severity  { get; set; }
    public string Message   { get; set; }
    public string RemediationHint { get; set; }
}
```

---

## CI Pipeline Integration Steps

```
[1] dotnet build                              → compile check
[2] SyncManager.ValidateSchemaForCiAsync()   → SyncCiGateResult
    ├─ LintRuleCatalog(schema, catalog)       → all rule keys valid, none deprecated
    ├─ ValidateMappingForPromotion(schema)    → quality score ≥ threshold, state = Approved
    └─ ValidateDefaultsProfile(schema)        → profile exists, audit fields covered
[3] MappingManager.BuildMappingVersionDiffText → embed diff in PR body
[4] DefaultsManager.ExportDefaults(...)       → upload as CI artifact for env promotion
[5] Gate: SyncCiGateResult.Passed == true    → allow merge; else block
```

---

## Acceptance Criteria
- `ValidateSchemaForCiAsync(schema, strictMode: true)` runs all three integration checks.
- Rule keys missing from `RuleCatalog` produce a CI failure (not warning).
- Mapping diff text is generated and available as a CI artifact.
- Defaults profile is exported as a JSON artifact for environment promotion.
- CI gate blocks merge when mapping approval state is not `Approved` (for production-bound schemas).
- Pipeline runs under 30 seconds for a single schema with a typical field count (≤ 100 fields).
