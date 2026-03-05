---
name: forms-performance-configuration
description: Detailed guidance for FormsManager performance and configuration behavior using PerformanceManager and ConfigurationManager. Use when tuning cache, collecting metrics, and applying form/block configuration defaults.
---

# Forms Performance And Configuration

Use this skill for performance-sensitive forms and environment-level defaults.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Performance Surface
- `PerformanceManager.CacheBlockInfo(blockName, blockInfo)`
- `PerformanceManager.GetCachedBlockInfo(blockName)`
- `PerformanceManager.OptimizeBlockAccess()`
- `PerformanceManager.ClearCache()`
- `PerformanceManager.GetPerformanceStatistics()`
- `PerformanceManager.PreloadFrequentBlocks(blockNames)`
- `PerformanceManager.GetCacheEfficiencyMetrics()`

## Configuration Surface
- `ConfigurationManager.LoadConfiguration()`
- `ConfigurationManager.SaveConfiguration()`
- `ConfigurationManager.ResetToDefaults()`
- `ConfigurationManager.ValidateConfiguration()`
- `UnitofWorksManagerConfiguration`:
  - `ValidateBeforeCommit`
  - `EnableLogging`
  - `ClearCacheOnFormClose`
  - `BlockConfigurations` and `FormConfigurations`
  - `DefaultSaveOptions` and `DefaultRollbackOptions`

## Tuning Strategy
1. Start with defaults from `UnitofWorksManagerConfiguration.Default`.
2. Enable/validate config early during app bootstrap.
3. Cache block info on registration, not on every random access.
4. Run optimization and metrics review only at controlled points (not per user click).
5. Clear cache on close only when memory pressure or stale-data risk justifies it.

## Config Pattern
```csharp
var cfgManager = new ConfigurationManager();
cfgManager.LoadConfiguration();

if (!cfgManager.ValidateConfiguration())
{
    cfgManager.ResetToDefaults();
}

cfgManager.Configuration.ValidateBeforeCommit = true;
cfgManager.Configuration.ClearCacheOnFormClose = false;
cfgManager.SaveConfiguration();
```

## Metrics Pattern
```csharp
var stats = forms.PerformanceManager.GetPerformanceStatistics();
if (stats.CacheHitRatio < 0.60)
{
    forms.PerformanceManager.OptimizeBlockAccess();
}
```

## Rules
- Keep cache expiration and max size aligned with form size and record churn.
- Avoid unbounded preload sets; limit to highest-frequency blocks.
- Persist configuration changes intentionally; do not silently mutate defaults.
- Review cache hit/miss ratio and optimization cost together, not independently.

## Pitfalls
- Aggressive `ClearCache()` calls can negate performance gains.
- Large preload lists can waste memory and produce placeholder-heavy cache state.
- Invalid config files can silently fall back to defaults if not validated.
- Performance tuning without telemetry often over-optimizes the wrong path.

## Validation Checklist
- Configuration file exists and deserializes cleanly.
- Cache hit ratio is acceptable under realistic workload.
- Optimization cadence is bounded and measurable.
- Close-form cache policy matches product behavior expectations.

