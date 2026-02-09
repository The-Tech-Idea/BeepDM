# Defaults Resolvers

## Purpose
This folder implements the rule-resolution engine for default values. It includes resolver registration, built-in rule handlers, and resolver health checks.

## Key Files
- `BaseDefaultValueResolver.cs`: Base contract for resolver capability checks and value resolution.
- `DefaultValueResolverManager.cs`: Resolver registry, dispatch, statistics, and validation.
- `DateTimeResolver.cs`, `GuidResolver.cs`, `EnvironmentResolver.cs`: System-derived default providers.
- `ConfigurationResolver.cs`, `DataSourceResolver.cs`, `UserContextResolver.cs`: Context-aware resolution against editor/runtime state.
- `ExpressionResolver.cs`, `FormulaResolver.cs`, `ObjectPropertyResolver.cs`, `SystemInfoResolver.cs`: Computed and reflective resolver set.

## Runtime Flow
1. Rules are routed through `DefaultValueResolverManager`.
2. The manager selects a resolver via `CanHandle`.
3. The selected resolver executes `ResolveValue` against `IPassedArgs` context.
4. Resolution stats and diagnostics are exposed for monitoring.

## Extension Guidelines
- New resolvers should inherit `BaseDefaultValueResolver` and implement `GetExamples` for discoverability.
- Keep `CanHandle` fast and precise to avoid ambiguous resolver selection.
- Use contextual parameters via `IPassedArgs` instead of static globals.

## Testing Focus
- Resolver selection precedence when multiple rules overlap.
- Rule parsing edge cases and malformed expressions.
- Deterministic output for fixed context inputs.
