# Rules Engine Reference

## 1) Rule Extension Checklist

- Implement `IRule`:
  - stable `RuleText` key
  - `Structure` metadata holder
  - deterministic `SolveRule(...)`
- Add `[Rule(ruleKey: "...", ParserKey = "...")]` attribute.
- Ensure rule returns `(outputs, result)` with predictable keys.
- Register with `RuleEngine.RegisterRule(...)`.
- Add tests for:
  - happy path
  - missing parameter handling
  - policy-restricted execution behavior.

## 2) Parser Extension Checklist

- Implement `IRuleParser`.
- Parse invalid input into `ParseDiagnostic` errors rather than raw parser exceptions.
- Emit `ParseResult.Success = false` when diagnostics contain errors.
- Decorate with `[RuleParser(parserKey: "...")]`.
- Register via `RuleParserFactory.RegisterParser(ruleType, parser)`.

## 3) Recommended Execution Policy Profiles

### Dev profile
```csharp
new RuleExecutionPolicy
{
    MaxDepth = 20,
    AllowDeprecatedExecution = true,
    AllowedTokenTypes = null
};
```

### Production profile
```csharp
new RuleExecutionPolicy
{
    MaxDepth = 8,
    MinimumLifecycleState = RuleLifecycleState.Review,
    AllowDeprecatedExecution = false,
    AllowedTokenTypes = new HashSet<TokenType>
    {
        TokenType.Identifier,
        TokenType.EntityField,
        TokenType.StringLiteral,
        TokenType.NumericLiteral,
        TokenType.BooleanLiteral,
        TokenType.Equal,
        TokenType.NotEqual,
        TokenType.GreaterThan,
        TokenType.LessThan,
        TokenType.GreaterEqual,
        TokenType.LessEqual,
        TokenType.And,
        TokenType.Or,
        TokenType.Not
    }
};
```

## 4) Catalog + Governance Pattern

```csharp
var catalog = new RuleCatalog();
var structure = new RuleStructure
{
    Rulename = "CustomerEligibility",
    Expression = ":Age >= 18 && :Country == \"US\"",
    Module = "Onboarding",
    Tags = "eligibility,customer",
    LifecycleState = RuleLifecycleState.Review
};

catalog.Register(structure);
catalog.Promote(structure.GuidID, RuleLifecycleState.Approved);
```

## 5) Built-in Rule Pattern (minimal template)

```csharp
[Rule(ruleKey: "Custom.NormalizeValue", ParserKey = "RulesParser", RuleName = "NormalizeValue")]
public sealed class NormalizeValue : IRule
{
    public string RuleText { get; set; } = "Custom.NormalizeValue";
    public IRuleStructure Structure { get; set; } = new RuleStructure();

    public (Dictionary<string, object> outputs, object result) SolveRule(
        Dictionary<string, object> parameters = null)
    {
        var outputs = new Dictionary<string, object>();
        var input = parameters != null && parameters.TryGetValue("Input", out var v)
            ? v?.ToString() ?? string.Empty
            : string.Empty;
        var normalized = input.Trim().ToUpperInvariant();
        outputs["Normalized"] = normalized;
        return (outputs, normalized);
    }
}
```

## 6) Troubleshooting

- **Duplicate rule registration**
  - Cause: same `RuleText` key registered twice.
  - Fix: use unique keys or unregister first.

- **Parser not found**
  - Cause: missing `RegisterParser` for `ruleType`.
  - Fix: register parser in startup/bootstrap.

- **Lifecycle policy violation**
  - Cause: rule state below policy minimum or deprecated blocked.
  - Fix: promote lifecycle state or relax policy intentionally.

- **Circular reference**
  - Cause: chain of `@RuleRef` leads back to origin.
  - Fix: break recursion path; use flat composition.

- **Operator not allowed**
  - Cause: token blocked by `AllowedTokenTypes`.
  - Fix: adjust policy profile or expression grammar.
