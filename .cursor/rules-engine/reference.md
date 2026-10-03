# Rules Engine Reference

## NFEL-1 Profile

Read Engine `Rules/NFEL.md` before changing NFEL semantics. Configure an actual
NfelParser on RuleEngine; wrappers/metadata do not select this execution path.
Registration captures expression text. Actual solves reparse and compare supplied
types/decoded values; changed text or mismatched tokens deny before parameter lookup.
Existing saved NFEL tokens with enclosing quotes/unknown ternary must be explicitly
reparsed and reviewed, not silently accepted. Old token ordinals are preserved;
Power/Question/Colon are appended, not qualified for older engine readers.

NFEL supports arithmetic/unary signs/power, comparisons, Boolean logic and lazy
ternary. Dotted names are literal parameter keys. String literals are decoded;
comparisons are ordinal case-insensitive. Conditions are actual Booleans. Numeric
arithmetic/literals use finite binary64, while integral/Decimal parameter comparisons
are exact. No implicit numeric strings, arbitrary observer conversion, functions,
member getters or prefixed references. Broader numeric profiles are separate work.

Source/token/node/depth bounds are 65,536 / 4,096 / 2,048 / 64; decoded strings are
bounded to 16,384 characters. Successful history evicts oldest entries at 128
structures, 1,048,576 source characters or 32,768 tokens. Returned structures/tokens
do not own the history. Clear is synchronized but does not cancel active parsing.
Allowed tokens apply to both branches even when unused; policy/tree copies survive
caller mutation. Fresh rules are Draft and do not bypass lifecycle minimums.
Timeouts are checked around lookups but cannot interrupt blocking trusted callbacks.
Audit/registration lifecycle, host parameter snapshots, adapters and package-reader
compatibility remain separate gates; parsing is not security isolation.

Use `NfelAdmissionTests` for actual SolveRule and direct evaluation assertions,
not only parser diagnostics. Keep required-default expression ASTs and legacy
SQL/formula/RulesParser dialects separate. Executable custom IRule modules run by
their own SolveRule API, not by automatic expression-engine dispatch.

## 1) Rule Extension Checklist

- Implement `IRule`:
  - stable `RuleText` key
  - `Structure` metadata holder
  - deterministic `SolveRule(...)`
- Add `[Rule(ruleKey: "...", ParserKey = "...")]` attribute.
- Ensure rule returns `(outputs, result)` with predictable keys.
- For expression holders, register actual expression RuleText with RuleEngine.
  Execute custom modules through their own IRule.SolveRule API; expression-engine
  registration does not dispatch to their method bodies.
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
