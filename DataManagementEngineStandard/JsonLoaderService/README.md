# JsonLoaderService

JSON serialization/deserialization abstraction used by configuration and persistence layers.

## Key Files
- `IEnhancedJsonLoader.cs`: Enhanced JSON loader contract (load, save, serialize, deserialize)
- `JsonLoader.cs`: Implementation wrapping System.Text.Json with error handling and path resolution

## How It Fits
`ConfigUtil` and `ConfigEditor` depend on this service for consistent JSON persistence. All configuration files (DataConnections.json, ConnectionConfig.json, etc.) are loaded and saved through this service. Used by:
- `ConfigEditor` — configuration persistence
- `InMemoryDataSource` — structure data persistence
- `PipelineManager` — pipeline definition storage
- `ObservabilityStore` — run logs and audit trail

## Related Documentation
- [ConfigEditor Help](../Help/configeditor.html)
- [Core Architecture](../Docs/CoreArchitecture.md)
