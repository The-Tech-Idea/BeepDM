---
name: forms-performance-configuration
description: FormsManager local and bounded provider paging, performance and configuration guidance for BeepDM. Use when changing page outcomes, row/byte bounds, page/count arithmetic, staged fetch, caches, telemetry or configuration defaults.
---

# Forms Performance And Configuration

Use this skill for performance-sensitive forms and environment-level defaults.

## File Locations
- `DataManagementEngineStandard/Editor/Forms/Helpers/PerformanceManager.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.LocalPaging.cs`
- `DataManagementEngineStandard/Editor/Forms/FormsManager.ProviderPaging.cs`
- `DataManagementEngineStandard/Editor/UOW/UnitofWork.PageRead.cs`
- `DataManagementModelsStandard/DataBase/IBoundedPagedDataSource.cs`
- `DataManagementEngineStandard/Editor/Forms/Helpers/PagingManager.cs`
- `DataManagementModelsStandard/Editor/Forms/Interfaces/IFormsLocalPaging.cs`
- `DataManagementModelsStandard/Editor/Forms/Models/PerformanceModels.cs`
- `DataManagementModelsStandard/Editor/Forms/Configuration/`

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
Read `LOCAL-PAGING.md` in the checkout. LoadPageAsync is local navigation, not
provider fetching. Prefer typed outcomes, exact long arithmetic, authorized buffer
checks and acknowledged publication. Zero size disables; stored zero is known.
Read `PROVIDER-PAGING.md` for explicit FetchPageWithOutcomeAsync, required optional
capabilities, request byte/row bounds, complete key ordering and policy publication.
Inspect RecordsPublished separately from ProviderPage count evidence. No unbounded
fallback, raw order fragments or silent out-of-range re-fetch. Prefetch/cache is
still unimplemented; settings alone do not activate it.
Keep providers/getters/observers outside helper/security/registration monitors.

Use [`reference.md`](./reference.md) for config patterns, metrics usage, and validation checks.
