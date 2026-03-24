# MigrationManager Examples

This folder contains end-to-end examples for the current `MigrationManager` API surface.

Execution order:
1. `01-plan-and-policy.md`
2. `02-dryrun-preflight-impact.md`
3. `03-execution-checkpoint-resume.md`
4. `04-rollback-compensation.md`
5. `05-ci-and-artifacts.md`
6. `06-rollout-governance.md`

All examples assume:
- you already initialized `IDMEEditor`
- `IDataSource` is configured and reachable
- `migrationManager` is constructed as:

```csharp
var migrationManager = new MigrationManager(editor, dataSource);
```
