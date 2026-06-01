# Json

JSON datasource implementation and helper pipeline for file-based JSON data operations.

## Key Files
- `JsonDataSource.cs`: Primary JSON datasource with CRUD and schema inference
- `JsonDataSourceAdvanced.cs`: Advanced features with helper integration
- `JsonExtensions.cs`: Shared JToken/JObject extension methods
- `Helpers/`: CRUD, filtering, graph hydration, schema sync, caching, and async data support

## How It Fits
Adds file/document-style JSON data handling under Beep datasource abstractions. Supports JSONPath queries, deep entity resolution, and schema synchronization. Used for configuration data, document stores, and API response caching.

## Related Documentation
- [JSON DataSource Help](../Help/json-datasource.html)
- [Docs/JsonDataSource.md](../Docs/JsonDataSource.md)
