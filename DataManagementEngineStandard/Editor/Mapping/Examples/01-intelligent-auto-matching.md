# 01 - Intelligent Auto Matching

## Goal
Generate scored field matches and route decisions (auto-accept, review, reject).

## Example
```csharp
var options = new AutoMatchOptions
{
    AutoAcceptThreshold = 0.90,
    ReviewThreshold = 0.65,
    AmbiguityDelta = 0.05,
    MaxSuggestionsPerField = 3,
    EnableSynonymMatching = true,
    EnableFuzzyMatching = true
};

var report = MappingManager.AutoMapByConventionWithScoring(
    editor,
    "LegacyCustomers",
    "LegacyDb",
    "Customers",
    "MainDb",
    options);

foreach (var item in report.Suggestions.Where(s => s.Decision == MatchConfidenceDecision.ReviewRequired))
{
    Console.WriteLine($"Review: {item.DestinationField} => {item.BestCandidate?.SourceFieldName} ({item.Score:F2})");
}
```

## Outcome
- High-confidence mappings can be accepted automatically.
- Ambiguous/weak matches are surfaced explicitly for operator review.
