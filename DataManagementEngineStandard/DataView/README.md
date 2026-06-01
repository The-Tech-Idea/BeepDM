# DataView

Virtual/federated datasource that unions and joins entities from multiple underlying datasources.

## Key Files
- `DataViewDataSource.Core.cs` — Runtime virtual datasource
- `DataViewDataSource.Query.cs` — Federated query execution
- `DataViewDataSource.JoinGraph.cs` — Join topology between entities
- `DataViewDataSource.EntityLifecycle.cs` — Entity CRUD in views
- `DataViewDataSource.Filters.cs` — Filter propagation across sources
- `DataViewDataSource.Validation.cs` — View validation rules
- `DataViewConnection.cs` — Connection wrapper for views

## How It Fits
Allows querying across multiple datasources as if they were a single source. Supports joins, filters, and entity lifecycle operations. Used for reporting, cross-source data aggregation, and federated views.

## Related Documentation
- [DataViewDataSource Help](../Help/dataviewdatasource.html)
- [Docs/DataView.md](../Docs/DataView.md)
