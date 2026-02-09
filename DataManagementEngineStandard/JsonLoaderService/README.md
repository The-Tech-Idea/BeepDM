# JsonLoaderService

JSON serialization/deserialization abstraction used by configuration and persistence layers.

## Key Files
- `IEnhancedJsonLoader.cs`: abstraction contract.
- `JsonLoader.cs`: implementation.

## How It Fits
`ConfigUtil` and other modules depend on this service for consistent JSON persistence behavior.

