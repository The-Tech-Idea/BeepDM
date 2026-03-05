---
name: rdbms-feature-helper
description: Guidance for DatabaseFeatureHelper covering sequence/identity SQL, transaction statements, feature support checks, and database capability metadata.
---

# RDBMS Feature Helper

Use this skill when behavior must change based on datasource capabilities or feature support.

## Core API Surface
- Identity/sequence:
  - `GenerateFetchNextSequenceValueQuery`
  - `GenerateFetchLastIdentityQuery`
- Transactions:
  - `GetTransactionStatement(DataSourceType, TransactionOperation)`
- Capabilities:
  - `SupportsFeature(DataSourceType, DatabaseFeature)`
  - `GetSupportedFeatures(DataSourceType)`
  - `SupportsSequences`, `SupportsAutoIncrement`, `SupportsStoredProcedures`, `SupportsViews`
  - `GetMaxIdentifierLength`, `GetDatabaseInfo`

## Usage Pattern
1. Check support before emitting feature-specific SQL.
2. Generate proper identity/transaction syntax per provider.
3. Use identifier-length limits during schema generation.

## Pitfalls
- Do not assume sequence support for all providers.
- Do not emit feature SQL before checking support flags.

## Integration Points
- [rdbms-object-creation-helper](../rdbms-object-creation-helper/SKILL.md)
- [rdbms-entity-validation](../rdbms-entity-validation/SKILL.md)
