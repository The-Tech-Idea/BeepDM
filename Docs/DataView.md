# DataView DataSource Guide

## Overview

DataViewDataSource provides virtual/federated data views that can join, filter, and query across multiple underlying datasources.

## Architecture

```
DataViewDataSource
├── JoinGraph - Join topology between entities
├── EntityLifecycleManager - Entity CRUD in views
├── Query Engine - Federated query execution
└── Validation - View validation rules
```

## Core Components

- **DataViewDataSource** - Runtime virtual datasource
- **DataViewConnection** - Connection wrapper for views
- **JoinGraph** - Defines relationships between view entities
- **EntityLifecycleManager** - Handles entity operations in views

## Creating a View

```csharp
// Create connection
var viewConnection = new DataViewConnection(editor);
viewConnection.AddConnection("CustomersDB", customerDs);
viewConnection.AddConnection("OrdersDB", ordersDs);

// Define entities
var customerEntity = new EntityStructure
{
    EntityName = "CustomerView",
    DataSourceID = "CustomersDB",
    EntityID = "Customers"
};

var orderEntity = new EntityStructure
{
    EntityName = "OrderView", 
    DataSourceID = "OrdersDB",
    EntityID = "Orders"
};

// Define joins
var joinGraph = new JoinGraph();
joinGraph.AddJoin(new EntityJoin
{
    LeftEntity = "CustomerView",
    RightEntity = "OrderView",
    LeftField = "CustomerId",
    RightField = "CustomerId",
    JoinType = JoinType.Left
});

// Create view datasource
var viewDs = new DataViewDataSource("CustomerOrders", viewConnection, editor);
viewDs.AddEntity(customerEntity);
viewDs.AddEntity(orderEntity);
viewDs.SetJoinGraph(joinGraph);
```

## Querying Views

```csharp
// Query the view like any datasource
var results = viewDs.GetEntity("CustomerView", new List<AppFilter>{
    new AppFilter { FieldName = "Country", Operator = "=", FilterValue = "USA" }
});
```

## File Locations

- `DataManagementEngineStandard/DataView/DataViewDataSource.Core.cs`
- `DataManagementEngineStandard/DataView/DataViewDataSource.Query.cs`
- `DataManagementEngineStandard/DataView/DataViewDataSource.JoinGraph.cs`
- `DataManagementEngineStandard/DataView/DataViewConnection.cs`

## Related Documentation

- [Core Architecture](CoreArchitecture.md)
- [Data Source Implementation](HowToCreateNewDataSource.md)
