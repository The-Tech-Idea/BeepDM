# 03 - Quality and Drift Gates

## Goal
Score mapping quality and detect schema drift before execution.

## Example
```csharp
var quality = MappingManager.ValidateMappingWithScore(
    editor,
    map,
    productionThreshold: 80);

Console.WriteLine($"Score: {quality.Score}, Band: {quality.Band}, Issues: {quality.Issues.Count}");

var drift = MappingManager.DetectMappingDrift(
    editor,
    map,
    sourceDataSourceName: "LegacyDb",
    destinationDataSourceName: "MainDb");

foreach (var entry in drift.Entries)
{
    Console.WriteLine($"{entry.Category}: {entry.Description}");
}

var canPromote = MappingManager.EnforceProductionQualityThreshold(quality, minimumScore: 80);
if (!canPromote)
{
    throw new InvalidOperationException("Mapping quality gate failed.");
}
```

## Outcome
- Promotion decisions are based on explicit quality and drift evidence.
