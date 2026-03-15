# ConfigEditor Quick Reference

## Core Facade Methods
```csharp
var config = editor.ConfigEditor;

// Connections
config.LoadDataConnectionsValues();
config.AddDataConnection(props);
config.SaveDataconnectionsValues();

// Drivers
config.LoadConnectionDriversConfigValues();
config.SaveConnectionDriversConfigValues();

// Queries
config.LoadQueryFile();
config.SaveQueryFile();

// Entity structures and mappings
config.LoadDataSourceEntitiesValues("MyDb");
config.SaveDataSourceEntitiesValues(datasourceEntities);
config.SaveMappingValues("Customers", "MyDb", mapping);
```

## Persisted Files (ConfigPath)
- Config.json
- DataConnections.json
- ConnectionConfig.json
- QueryList.json
- CategoryFolders.json
- Reportslist.json
- reportsDefinition.json
- AIScripts.json
- Projects.json

## Managed Paths
- WorkFlow/DataWorkFlow.json
- Mapping/{datasource}/{entity}_Mapping.json
- Entities/{datasource}_entities.json
- Migrations/{datasource}_MigrationHistory.json
