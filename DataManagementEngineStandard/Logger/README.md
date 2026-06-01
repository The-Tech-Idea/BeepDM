# Logger

Logging bridge helpers for DataManagementEngineStandard.

## Key Files
- `LogAndError.cs` — Centralized logging and error reporting pattern

## How It Fits
Centralizes logging and error reporting patterns used across all engine operations. Both DMEEditor and ConfigEditor delegate logging to `IDMLogger` which is implemented in this module. The `LogAndError` pattern provides consistent error enrichment with operation context, timestamps, and severity levels.

## Features
- Centralized `IDMLogger` implementation
- `LogAndError` pattern for enriched error reporting
- Per-operation logging with context and severity
- Timestamp and source tracking

## Related Documentation
- [Core Architecture](../Docs/CoreArchitecture.md)
- [ConfigEditor](../Docs/Configuration.md)
