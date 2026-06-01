# Report Engine

## Overview

Reporting support services for generating and managing report data and output from BeepDM data operations.

## Key Files

- `ReportDataManager.cs` - Report data orchestration and query building
- `ReportOutput.cs` - Report output model and formatting logic

## Features

- Report data extraction from multiple datasources
- Output formatting and export
- Report template support
- Integration with BeepDM query engine

## Usage

```csharp
var reportManager = new ReportDataManager(editor);

// Define report query
var report = reportManager.CreateReport("SalesReport");
report.AddDataSource("SalesDB", "Orders");
report.AddFields("OrderId", "CustomerName", "Total", "OrderDate");
report.AddFilter(new AppFilter { FieldName = "OrderDate", Operator = ">=", FilterValue = "2024-01-01" });

// Generate output
var output = await reportManager.GenerateAsync(report);
Console.WriteLine($"Records: {output.RecordCount}");
```

## How It Fits

Supports report-oriented workflows:
- Business intelligence reporting
- Data export operations
- Dashboard data feeds
- Scheduled report generation

## Related Documentation

- [Core Architecture](../Docs/CoreArchitecture.md)
- [Unit of Work Pattern](../Docs/UnitOfWork.md)
