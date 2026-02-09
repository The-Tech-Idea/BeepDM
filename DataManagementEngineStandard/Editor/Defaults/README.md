# Defaults

Default value management and rule resolution subsystem.

## Core Components
- `DefaultsManager` (static facade)
- `Helpers/DefaultValueHelper`, `DefaultValueValidationHelper`
- `Resolvers/*` (rule/value resolvers)
- `Interfaces/IDefaultValueInterfaces`

## Capabilities
- Get/save datasource-level default values.
- Resolve static or rule-based values at runtime.
- Validate defaults and rule syntax before execution.
- Register custom resolvers for project-specific rules.
- Provide templates for common domains (audit, user, inventory, etc.).

## Runtime Use
- Initialize via `DefaultsManager.Initialize(editor)`.
- Resolve with `ResolveDefaultValue(...)` in import/mapping/unit-of-work flows.
- Persist using `SaveDefaults(...)` so settings survive restarts.

## Integration
- `DataImportManager` automatically loads defaults when configuration enables `ApplyDefaults`.
- Mapping and UOW workflows can apply the same resolver pipeline for consistency.
