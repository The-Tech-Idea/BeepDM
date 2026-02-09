# Rdbms Helper

## Purpose
This folder provides relational helper logic shared by SQL-style providers, including core SQL generation, constraints, and transaction support.

## Key Files
- `RdbmsHelper.cs`: Base relational dialect behavior and capability checks.
- `RdbmsHelper.Constraints.cs`: Constraint and DDL-related helper members.
- `RdbmsHelper.Transactions.cs`: Transaction command generation and orchestration helpers.

## Runtime Flow
1. Quote identifiers and map CLR/provider types.
2. Generate provider-safe SQL for schema and data operations.
3. Emit transaction and constraint statements where supported.

## Extension Guidelines
- Always parameterize values; avoid inline literal composition.
- Keep identifier quoting and case behavior provider-aware.
- Validate transaction capability before issuing transactional SQL.
