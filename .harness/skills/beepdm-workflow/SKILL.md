---
name: beepdm-workflow
description: Use when designing, persisting, or migrating BeepDM workflow definitions — multi-step business processes that compose datasource operations, ETL pipelines, and forms. Hands off to ETL (for data movement), Forms (for human tasks), and Configuration (for persisted definitions) skills.
---

# beepdm-workflow

`WorkFlowEngine` orchestrates **multi-step business processes** in BeepDM. Where ETL moves data and Forms present screens, workflows tie those things together into a process with state, persistence, and migration.

## When to use this skill

- Defining a workflow (sequence of steps, conditions, branches).
- Persisting workflow definitions and instances.
- Migrating workflow definitions when the schema of the workflow DSL changes.
- Composing a workflow that calls into ETL pipelines or Forms.

## Do NOT use this skill for

- Pure data movement with no branching → use **beepdm-etl** (pipelines cover that).
- Master-detail UI flows → use **beepdm-forms**.
- Schema migrations on application data → use **beepdm-migration**.

## File Locations

`DataManagementEngineStandard/Editor/ETL/Engine/Workflow/`:

- `WorkFlowEngine.cs` — runtime executor
- `WorkFlowStorage.cs` — persistence of definitions + instances
- `WorkFlowMigration.cs` — version migration of workflow DSL

## Core Concepts

- **Workflow Definition** — the static graph of steps (serialized via `WorkFlowStorage`).
- **Workflow Instance** — a running execution of a definition with current state.
- **Step** — a unit of work; can call into ETL pipelines, Forms, or arbitrary delegate code.
- **Workflow Migration** — version-to-version upgrade of a definition (e.g. step renamed, branch added).

## How this skill works with the rest of the data-management layer

| Handoff | Direction | What flows |
|---|---|---|
| **beepdm-etl** | → ETL | A workflow step commonly invokes a pipeline by id. The workflow owns the *when*; the pipeline owns the *what*. |
| **beepdm-forms** | ↔ Forms | A workflow can pause for a human task — it shows a Form, waits for input, then continues. Forms do not own the workflow. |
| **beepdm-configuration** | ↔ Config | Workflow definitions may be persisted through `ConfigEditor` (depending on the host app); treat the storage layer as pluggable. |
| **beepdm-migration** | ↔ Migration | When the workflow DSL changes, `WorkFlowMigration` runs the upgrade. It uses migration patterns but lives in a separate file. |
| **beepdm-unitofwork** | ← UoW | A workflow step that mutates entities should use UoW so the change is transactional. |

## Design Rules

- Workflow definitions are **versioned**. Always go through `WorkFlowMigration` for upgrades — never mutate a stored definition in place.
- Steps are **idempotent** where possible; a re-run of a workflow should be safe.
- Use `IErrorsInfo` for expected failures (a step is allowed to say "wait for human"); throw only for unexpected exceptions.
- Workflow storage is pluggable; the engine should not assume a particular backend.

## Cross-references

- See **beepdm-etl** for the data-movement work flows typically compose.
- See **beepdm-forms** for human-task steps in a workflow.
- See **beepdm-migration** for the patterns `WorkFlowMigration` is modeled on.
