# Managed Forms Query Policy

Status: Stage B increment, not complete authorization or provider qualification.
Scope: FormsManager and platform-neutral UI integration contracts.

## Current Boundary

[ManagedReadPolicy](FormsManager.ManagedReadPolicy.cs) is shared by basic/enhanced
block queries, immediate/deferred detail reads, count and source-level aggregate
queries. Caller, block default and security predicates are ANDed exactly once.
Authorization is checked even when an enhanced query is already in Query mode.
Denied or malformed policies stop before row/scalar execution. Read identity,
field schema and security are checked again after callbacks/awaits.

The default [SecurityManager](Helpers/SecurityManager.cs) owns context and block
policy containers. Getters return copies; modifying them no longer changes the
active policy. Publish updates with SetSecurityContext/SetBlockSecurity. Matching
role grants follow the model's allow-any-role rule, independent of role order.
An admin bypasses coarse permission checks, not configured row predicates.
The optional [IQuerySecuritySnapshotProvider](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IQuerySecuritySnapshotProvider.cs)
captures query permission, clause and values at one monotonically changing
revision. Custom legacy security helpers receive material-policy rechecks but
cannot promise coherent context capture without the optional contract.

## Supported Filter Grammar

Managed default/security clauses support an optional WHERE, parenthesized AND,
declared unqualified fields (including quoted identifiers), comparisons,
IS NULL/IS NOT NULL, BETWEEN/NOT BETWEEN, IN/NOT IN and LIKE/NOT LIKE.
Values are single-quoted escaped strings, invariant finite numbers, Boolean/NULL
literals or named `:parameter` references supplied by RowFilterValues.
Empty strings and NULL remain distinct. Quoted IN items preserve commas, quotes
and empty strings through the shared AppFilter collection decoder.

OR, raw SQL, functions/subqueries, comments, missing parameters, unknown fields,
unsupported/custom value types and trailing/malformed syntax are rejected,
not silently ignored. IN lists containing NULL are rejected because the flat
AppFilter protocol does not faithfully represent their three-valued semantics.
Supported operators on caller filters are normalized against the same closed
protocol; sort direction is ASC/DESC on a declared field.

Bounds: 128 filters, 128 values per IN predicate, 16,384 clause characters,
32 parenthesis levels, 65,536 characters per scalar and 1 MiB combined filter
payload. Numeric conversion is invariant and rejects non-finite/overflow values.
Arbitrary objects are rejected before invoking their formatting callbacks.
See [compiler](Helpers/ManagedFilterCompiler.cs).

Compatibility change: previously permissive complex WHERE fragments now fail.
Rewrite them to this grammar; do not remove a mandatory policy to restore reads.
This intentionally does not change the public legacy QueryBuilder parser.

## Scalar Execution

Providers may implement optional [IParameterizedScalarDataSource](../../../DataManagementModelsStandard/DataBase/IParameterizedScalarDataSource.cs).
It receives a definition with typed parameter values, never interpolated caller
text. Bind every parameter as data and honor cancellation where supported.
Existing IDataSource members are unchanged.

Legacy scalar providers use closed finite numeric/Boolean literals and dialect
specific hex-encoded text/date/GUID literals. Generated entity/column identities
are quoted, not arbitrary SELECT sources. Entity metadata must supply raw
unquoted identity segments (up to three dot-separated segments); prequoted names
or dots embedded inside one identifier are not qualified by this increment.
Text fallback generation exists for SQLite, SQL Server, MySQL/MariaDB and PostgreSQL;
only real SQLite execution is tested here. Other dialects must implement binding
for text/date/GUID filters. Numeric/dialect conversions are not a cross-provider
semantic parity claim.

Count rejects negative, fractional, nonnumeric, non-finite or int-overflow results.
Failure/cancellation/stale policy returns the existing -1 sentinel, not zero;
count does not load block records. A typed long-count API remains Stage F work.
GetBlockAggregateScalarAsync now accepts only COUNT(*), COUNT(field), or numeric
SUM/AVG/MIN/MAX(field), with declared simple field names. Passing raw SQL is no
longer supported. Aggregates apply the same defaults/security restrictions.
Its legacy double/zero failure projection remains ambiguous; use logs for failure
evidence until an additive typed scalar result is implemented.

## Open Gates

[UI buffer authorization](BUFFER-AUTHORIZATION.md) requires accepted managed read
evidence before freshly binding a held buffer after principal/row-policy changes.
Default field-only policies can remask the same authorized buffer; already displayed
text still requires host clearing/repaint, and auxiliary reads/caches remain separate.

- Default datasource basic/enhanced/detail UoWs now use [staged publication](READ-PUBLICATION.md)
  with revision-gated security, registration and query-request checks. Legacy/custom
  UoWs still use Get, which may publish before late rejection.
  General operation/record generations and arbitrary concurrent edits remain open.
- Record groups, LOV data/cache, validation provider lookups and reflective
  provider paging require explicit policy targets/capabilities. They may access
  different entities/datasources, so blindly applying one block's filters would
  be incorrect. They are not qualified as secured by this block-read increment.
- Explicit [bounded provider fetch](PROVIDER-PAGING.md) now reuses the mandatory
  block policy boundary and staged query publication. Its coherent SQLite test
  lane qualifies count/page filters and row/byte bounds, not reflective legacy
  paging, arbitrary plugins or a policy-authorized prefetch/cache.
- [Deferred coordination](DETAIL-COORDINATION.md) captures relationship mappings/
  modes without mutating live configuration, preserves dirty branches/pending
  markers and reports incomplete outcomes. Failed reads do not cascade with stale
  child keys. Broader public-operation scheduling/lifetime remains C/D work.
- Field security, masked presentation/export, sensitive diagnostics/audit data and
  legacy custom helpers need separate conformance work. Host role/context strings
  are not verified identities. Raw UoW/IDataSource access remains a trusted escape
  hatch, not database authorization.
- External provider plugins and real UI adapters remain unqualified. See
  [test/implementation evidence](IMPLEMENTATION-LOG.md).
