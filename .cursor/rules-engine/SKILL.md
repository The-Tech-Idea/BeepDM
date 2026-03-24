---
name: rules-engine
description: Guidance for implementing and using the BeepDM Rules engine (RuleEngine, RuleParser, RuleCatalog, and built-in/custom IRule modules). Use when adding rule logic, parser extensions, execution policies, or rule governance workflows in DataManagementEngineStandard/Rules and DataManagementModelsStandard/Rules.
---

# Rules Engine Guide

Use this skill when working on `DataManagementEngineStandard/Rules` and `DataManagementModelsStandard/Rules`.

## Use this skill when
- Creating or updating `IRule` implementations (built-in or custom).
- Extending parser/tokenization behavior (`Tokenizer`, `RuleParser`, parser factory).
- Adding execution-policy constraints (`RuleExecutionPolicy`) or lifecycle governance.
- Wiring rule catalogs, parser registration, or audit events.

## Do not use this skill when
- The task is generic datasource behavior. Use [`idatasource`](../idatasource/SKILL.md).
- The task is mapping-only transformation design. Use [`mapping`](../mapping/SKILL.md).
- The task is ETL orchestration without rule-engine changes. Use [`etl`](../etl/SKILL.md).

## Responsibilities
- Keep parser/tokenizer deterministic and diagnostics-first (`ParseResult`, `ParseDiagnostic`).
- Keep execution safe via `RuleExecutionPolicy` (depth, token constraints, lifecycle).
- Keep rule registration deterministic (no duplicate keys or parser collisions).
- Keep custom rule behavior isolated in `IRule` classes (avoid hardcoding rule logic in engine core).

## Core API Surface
- Engine:
  - `RuleEngine.RegisterRule(...)`
  - `RuleEngine.SolveRule(...)`
  - `RuleEngine.EvaluateExpression(...)`
  - `RuleEngine.RuleEvaluated` (audit event)
- Parsing:
  - `RuleParser.ParseRule(...)`
  - `Tokenizer.Tokenize()`
  - `RuleParserFactory.RegisterParser(...)`, `GetParser(...)`
- Catalog and governance:
  - `RuleCatalog.Register(...)`, `GetByGuid(...)`, `GetByName(...)`, `Promote(...)`
  - `IRuleStructure.LifecycleState`, `SchemaVersion`, `Touch()`

## Typical Usage Pattern
1. Create or load an `IRule` (`RuleText`, optional `Structure` metadata).
2. Parse it with `RuleParser` (or parser selected via `RuleParserFactory`).
3. Register it in `RuleEngine`.
4. Evaluate with `SolveRule` using parameters and an explicit `RuleExecutionPolicy`.
5. Subscribe to `RuleEvaluated` for operational audit/telemetry.

## Creating a Custom Rule
1. Add a class under `DataManagementEngineStandard/Rules/BuiltinRules/...` (or custom module).
2. Implement `IRule` with stable `RuleText` key and `SolveRule(...)`.
3. Add `[Rule(...)]` metadata for discovery/catalog semantics.
4. Return deterministic `outputs` and `result` from `SolveRule`.
5. Register rule instance in bootstrap/composition root via `RuleEngine.RegisterRule`.

## Creating a Custom Parser
1. Implement `IRuleParser`.
2. Decorate parser with `[RuleParser(parserKey: "...")]`.
3. Register parser in `RuleParserFactory.RegisterParser(ruleType, parser)`.
4. Ensure parse failures are diagnostics-driven, not exception-driven, for invalid user input.

## Validation and Safety
- Enforce policy on every evaluation path (`AllowedTokenTypes`, `MaxDepth`, lifecycle minimum).
- Detect and block circular rule references.
- Keep tokenizer and parser recoverable with structured diagnostics.
- Use explicit rule keys and parser keys to prevent duplicate registration ambiguity.

## Pitfalls
- Registering multiple rules with the same `RuleText`.
- Using permissive policy in production without token restrictions.
- Throwing raw exceptions for parse errors instead of returning diagnostics.
- Mutating shared parameter dictionaries in custom rules without intent.

## File Locations
- `DataManagementEngineStandard/Rules/RulesEngine.cs`
- `DataManagementEngineStandard/Rules/RulesEngine.ExpressionEvaluation.cs`
- `DataManagementEngineStandard/Rules/RulesParser.cs`
- `DataManagementEngineStandard/Rules/Tokenizer.cs`
- `DataManagementEngineStandard/Rules/RuleCatalog.cs`
- `DataManagementEngineStandard/Rules/RuleParserFactory.cs`
- `DataManagementModelsStandard/Rules/IRuleEngine.cs`
- `DataManagementModelsStandard/Rules/IRule.cs`
- `DataManagementModelsStandard/Rules/IRuleStructure.cs`
- `DataManagementModelsStandard/Rules/RuleExecutionPolicy.cs`

## Example
```csharp
var parser = new RuleParser();
var engine = new RuleEngine(parser);

engine.RuleEvaluated += (_, e) =>
{
    Console.WriteLine($"{e.RuleKey} success={e.Success} elapsed={e.Elapsed.TotalMilliseconds}ms");
};

var rule = new TheTechIdea.Beep.Rules.BuiltinRules.Record.SetFieldValue();
engine.RegisterRule(rule);

var policy = new RuleExecutionPolicy
{
    MaxDepth = 10,
    AllowDeprecatedExecution = false
};

var parameters = new Dictionary<string, object>
{
    ["FieldName"] = "Status",
    ["Value"] = "Processed"
};

var (_, result) = engine.SolveRule(rule.RuleText, parameters, policy);
```

## Related Skills
- [`mapping`](../mapping/SKILL.md)
- [`etl`](../etl/SKILL.md)
- [`beepdm`](../beepdm/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for extension templates, policy profiles, and troubleshooting.
