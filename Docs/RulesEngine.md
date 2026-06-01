# Rules Engine Guide

## Overview

The Rules Engine provides a powerful, extensible system for defining and executing business rules, data validation, transformations, and workflow logic.

## Core Components

- **RulesEngine** - Main engine orchestrator
- **RulesParser** - Rule text/parser
- **RuleRegistry** - Rule registration and lookup
- **RuleCatalog** - Categorized rule collection
- **Tokenizer** - Expression tokenization
- **InMemoryRuleStateStore** - Rule state persistence

## Built-in Rule Categories

| Category | Purpose |
|----------|---------|
| Compute | Mathematical and logical computations |
| DQ | Data quality checks |
| Date | Date/time operations |
| Enrich | Data enrichment |
| Flow | Control flow |
| Notify | Notifications |
| Numeric | Numeric operations |
| Query | Query building |
| Record | Record-level operations |
| Route | Routing decisions |
| Schema | Schema validation |
| Security | Security checks |
| Transform | Data transformation |

## Built-in Parsers

- **NfelParser** - NFe (Brazilian electronic invoice) parsing
- **SqlWhereParser** - SQL WHERE clause parsing
- **JsonRuleTreeParser** - JSON rule tree parsing
- **FormulaParser** - Mathematical formula parsing
- **CsvColumnParser** - CSV column parsing

## Usage

```csharp
// Create engine
var engine = new RulesEngine(editor);

// Register rules
engine.RegisterRule("ValidateAge", "Age >= 18 AND Age <= 120");
engine.RegisterRule("ComputeDiscount", "Price * (1 - DiscountRate)");

// Execute rule
var result = engine.Execute("ValidateAge", new { Age = 25 });
if (result.Success)
{
    Console.WriteLine("Age is valid");
}

// Execute with context
var context = new RuleContext
{
    Data = customer,
    Parameters = new Dictionary<string, object> { ["MaxDiscount"] = 0.20 }
};
var discount = engine.Execute("ComputeDiscount", context);
```

## Expression Evaluation

```csharp
// Evaluate expressions directly
var value = engine.Evaluate("Price * Quantity * (1 + TaxRate)", order);

// Use in workflows
var workflow = new WorkFlow();
workflow.AddStep(new WorkFlowStep
{
    Action = new RuleAction { RuleName = "ValidateAge" }
});
```

## File Locations

- `DataManagementEngineStandard/Rules/RulesEngine.cs`
- `DataManagementEngineStandard/Rules/RulesParser.cs`
- `DataManagementEngineStandard/Rules/RuleRegistry.cs`
- `DataManagementEngineStandard/Rules/BuiltinRules/`
- `DataManagementEngineStandard/Rules/BuiltinParsers/`

## Related Documentation

- [Core Architecture](CoreArchitecture.md)
- [Workflow](Docs/Workflow.md)
- [ETL Operations](ETL.md)
