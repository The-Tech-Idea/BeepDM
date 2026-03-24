# 05 - Governance, Versioning, and Audit

## Goal
Capture version history, approval state, and audit evidence on mapping updates.

## Example
```csharp
using (MappingManager.BeginGovernanceScope(
    author: "data-eng",
    changeReason: "Rename source CustEmail -> EmailAddress",
    targetState: MappingApprovalState.Review))
{
    // Any save in this scope is stamped with governance metadata.
    MappingManager.SaveEntityMap(editor, "Customers", "MainDb", map);
}

var history = MappingManager.GetMappingVersionHistory(editor, "Customers", "MainDb");
var governance = MappingManager.GetMappingGovernance(editor, "Customers", "MainDb");

MappingManager.UpdateMappingApprovalState(
    editor,
    "Customers",
    "MainDb",
    MappingApprovalState.Approved,
    actor: "release-manager",
    reason: "Passed QA and drift checks");

if (history.Count >= 2)
{
    var previous = history[^2].Version;
    var current = history[^1].Version;
    var diff = MappingManager.BuildMappingVersionDiffText(editor, "Customers", "MainDb", previous, current);
    Console.WriteLine(diff);
}
```

## Outcome
- Sidecar governance file tracks versions and audit entries:
  - `{MappingPath}/MainDb/Customers_Mapping.governance.json`
