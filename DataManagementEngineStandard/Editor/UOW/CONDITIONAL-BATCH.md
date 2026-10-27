# Conditional atomic batches

The optional Models contracts are IConditionalMutationBatchDataSource and IConditionalUnitOfWorkBatchMutation. UnitofWork<T> dispatches this capability to its provider; ordinary Commit and the existing single-row conditional interface keep their contracts. The legacy two-argument ConditionalRowGuard constructor remains available.

A batch contains 1–64 distinct operation instances, 1–128 required observation groups, up to 16 exact cohort-count checks, up to 16 final authority groups and at most 1,800 scalar values. Operations are ordered, guarded and single-row only. Primary keys and all identifiers must resolve through cached declared provider metadata. Cohort predicates are copied equality maps; their expected counts are bounded to 0–128. Final guards should contain stable authority/time fields that the batch itself does not replace.

The exact built-in, clean, undisposed, non-list UnitofWork owner is required. Its first operation must anchor EntityName. For a tenant-scoped owner, every operation match or insert payload must retain the same tenant, and update payloads cannot replace it. Refusal never falls back to ordinary CRUD. The commit admission gate also prevents another operation from sharing that owner. Applied results invalidate its read identity.

The provider owns a new isolated ADO.NET connection and one serializable transaction. It checks observed rows and cohort counts before writing, requires exactly one affected row per operation, rechecks stable authority/time before commit and rolls back all earlier operations on a later mismatch or execution error. UTC expiry uses provider database time; callers must supply actual stored UTC values rather than local presentation timestamps. A failed rollback or uncertain commit is not acknowledged zero work.

| Outcome | ChangedRows | Meaning |
|---|---|---|
| Applied | operation count | Every change and commit were acknowledged; a later diagnostic failure can accompany success. |
| NoMatch | 0 | Observation/operation/final guard mismatch and rollback acknowledged. |
| NotApplied | 0 | Refused before mutation dispatch. |
| RolledBack | 0 | Execution failed or cancelled and rollback acknowledged. |
| Unconfirmed | null | Commit/rollback acknowledgement is uncertain; reload or reconcile before another command. |

The UnitofWork validates batch correlation and exact Applied count. Lost, contradictory or mismatched acknowledgement becomes Unconfirmed. There is no automatic retry, durable idempotency record, distributed transaction, source-document revision contract or app authorization policy in this generic capability. Those remain caller responsibilities and separate acceptance gates.

Current physical qualification is the disposable DoitAll SQLite fixture. SQL Server, Azure SQL and PostgreSQL have parameterized dialect-generation checks; deployed provider, schema, deadlock/restart and package-consumer qualification remain open. Unsupported providers refuse. For the host adoption and current focused evidence see the [DoitAll atomic approval fix document](../../../../Beep.DoitAll/docs/plan/odoo-saas-review/32-atomic-approval-decisions.md).
