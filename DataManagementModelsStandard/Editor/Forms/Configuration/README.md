# Forms Configuration

## Purpose
This folder defines configuration models and manager services that control form behavior, navigation, validation, and runtime performance settings.

## Key Files
- `UnitofWorksManagerConfiguration.cs`: Root configuration aggregate and lookup helpers.
- `ConfigurationManager.cs`: Load/save/reset and validation lifecycle.
- `FormConfiguration.cs`: Form-level settings and metadata.
- `NavigationConfiguration.cs`: Navigation behavior and transitions.
- `ValidationConfiguration.cs`: Validation execution options.
- `PerformanceConfiguration.cs`: Caching and performance tuning settings.

## Runtime Flow
1. Load configuration from persisted source.
2. Resolve block and form configuration at runtime.
3. Validate settings before enabling form operations.
4. Persist changes through `SaveConfiguration`.

## Extension Guidelines
- Keep configuration backward compatible when adding options.
- Validate defaults to prevent runtime null/invalid settings.
- Separate environment-specific values from behavior flags.
