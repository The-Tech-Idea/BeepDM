# BeepSync Guide

## Overview

BeepSync Manager provides comprehensive data synchronization between datasources with support for CDC (Change Data Capture), bulk sync, conflict resolution, schema governance, and retry/checkpoint mechanisms.

## Core Capabilities

- **CDC (Change Data Capture)** - Real-time change detection
- **Bulk Sync** - High-volume data transfer
- **Conflict Resolution** - Handle conflicts during bidirectional sync
- **Schema Governance** - Schema versioning and validation
- **Retry & Checkpoint** - Fault-tolerant execution
- **Incremental Sync** - Delta-based synchronization

## Key Classes

- `BeepSyncManager` - Main synchronization orchestrator
- `SyncConfig` - Configuration for sync operations
- `SyncResult` - Result of sync execution
- `ConflictResolver` - Conflict resolution strategies

## Usage

```csharp
// Create sync manager
var syncManager = new BeepSyncManager(editor);

// Configure sync
var config = new SyncConfig
{
    SourceConnection = "SourceDB",
    TargetConnection = "TargetDB",
    SourceEntity = "Customers",
    TargetEntity = "Customers",
    SyncMode = SyncMode.Incremental,
    ConflictResolution = ConflictResolutionStrategy.SourceWins,
    EnableCDC = true,
    BatchSize = 1000
};

// Execute sync
var result = await syncManager.ExecuteSync(config);
Console.WriteLine($"Success: {result.Success}, Records: {result.RecordsProcessed}");
```

## Conflict Resolution Strategies

- **SourceWins** - Source data always takes precedence
- **TargetWins** - Target data always takes precedence
- **Timestamp** - Most recent timestamp wins
- **Custom** - User-defined resolution logic

## File Locations

- `DataManagementEngineStandard/Editor/BeepSync/BeepSyncManager.Core.cs`
- `DataManagementEngineStandard/Editor/BeepSync/Helpers/`
- `DataManagementEngineStandard/Editor/BeepSync/Models/`

## Related Documentation

- [Core Architecture](CoreArchitecture.md)
- [ETL Operations](ETL.md)
- [Unit of Work Pattern](UnitOfWork.md)
