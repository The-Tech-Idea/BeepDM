---
name: forms-performance-configuration
description: Detailed guidance for FormsManager performance and configuration behavior in BeepDM. Use when tuning block caching, collecting metrics, or applying form and block configuration defaults through PerformanceManager and ConfigurationManager.
---

# Forms Performance And Configuration

Use this skill for performance-sensitive forms and environment-level defaults.

## File Locations
- `DataManagementEngineStandard/Editor/Forms/Helpers/PerformanceManager.cs`
- `DataManagementEngineStandard/Editor/Forms/Configuration/ConfigurationManager.cs`
- `DataManagementEngineStandard/Editor/Forms/Configuration/UnitofWorksManagerConfiguration.cs`
- `DataManagementEngineStandard/Editor/Forms/Configuration/FormConfiguration.cs`
- `DataManagementEngineStandard/Editor/Forms/Configuration/NavigationConfiguration.cs`
- `DataManagementEngineStandard/Editor/Forms/Configuration/PerformanceConfiguration.cs`

## Core Surface
- cache operations on `PerformanceManager`
- metrics and efficiency retrieval
- configuration load/save/reset/validate operations on `ConfigurationManager`

## Working Rules
1. Start from defaults and validate before persisting config.
2. Cache block info intentionally, not opportunistically on every path.
3. Tune with telemetry, not guesses.
4. Keep close-form cache policy aligned with actual UX and stale-data risk.

## Related Skills
- [`forms`](../forms/SKILL.md)
- [`forms-helper-managers`](../forms-helper-managers/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for config patterns, metrics usage, and validation checks.
