---
name: rdbms-feature-helper
description: Guidance for DatabaseFeatureHelper in BeepDM. Use when behavior must change based on sequence, identity, transaction, view, stored-procedure, or capability support across RDBMS providers.
---

# RDBMS Feature Helper

Use this skill when behavior must change based on provider capabilities.

## File Locations
- `DataManagementEngineStandard/Helpers/RDBMSHelpers/DatabaseFeatureHelper.cs`

## Core APIs
- identity/sequence helpers such as `GenerateFetchNextSequenceValueQuery` and `GenerateFetchLastIdentityQuery`
- transaction helpers such as `GetTransactionStatement(...)`
- capability checks such as `SupportsFeature(...)`, `SupportsSequences`, and `SupportsAutoIncrement`
- provider metadata such as `GetMaxIdentifierLength` and `GetDatabaseInfo`

## Working Rules
1. Check support before emitting feature-specific SQL.
2. Keep identifier-length limits in schema-generation flows.
3. Preserve provider-specific transaction and identity semantics.

## Related Skills
- [`rdbms-object-creation-helper`](../rdbms-object-creation-helper/SKILL.md)
- [`rdbms-dml-helper`](../rdbms-dml-helper/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for feature checks and capability-oriented routing.
