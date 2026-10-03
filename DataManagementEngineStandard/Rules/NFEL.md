# NFEL-1 Admission And Execution

Use `new RuleEngine(new NfelParser())` to select this profile. A RuleType string,
parser-factory registration or wrapper around a parser does not select the profile.
Other parsers retain their existing evaluator semantics. This is expression
evaluation, not execution of an arbitrary registered IRule.SolveRule implementation.

## Grammar

- Literals: finite invariant binary64 numbers (decimal point and exponent notation),
  single/double-quoted strings, case-insensitive true/false/null.
- Identifiers: ASCII letter/underscore followed by letters/digits/underscores;
  dotted segments follow the same rule. Dots mean literal dictionary keys, not
  object traversal. Lookup uses the supplied Dictionary's comparer.
- Arithmetic: binary `+ - * / % ^` and unary `+ -`.
- Comparisons: `== != < <= > >=`.
- Boolean: `!`/`NOT`, `&&`/`AND`, `||`/`OR`.
- Parentheses and right-associative `condition ? yes : no`.
- Power is right-associative and binds more tightly than unary signs:
  `2^3^2 = 512`, `-2^2 = -4`, `2^-2 = .25`.

Boolean precedence is OR, AND, then comparisons; arithmetic precedence is addition,
multiplication, unary, power. Ternary has the lowest precedence. Full input must
be consumed. Calls, comma lists, prefixed `@rule`/`:field` references, assignment,
member getters and arbitrary functions are not NFEL-1 syntax. Unknown text is never
discarded. Both ternary branches must be syntactically valid.

Quoted strings are decoded, not returned with enclosing quotes. Escapes support
backslash, single/double quote, n/r/t/b/f and four-hex-digit Unicode (`\u0041`).
Raw control characters, invalid escapes and unterminated quotes deny admission.
Doubled-quote SQL escaping is not this profile's escape syntax.

## Typed Values And Lazy Evaluation

Arithmetic retains the historic Double result type. Accepted numeric parameter
types are byte/sbyte/short/ushort/int/uint/long/ulong/float/double/Decimal.
Integral/Decimal parameter-to-parameter comparisons use exact Decimal values;
comparisons involving a floating value or numeric literal use binary64.
Binary64 literals/arithmetic retain IEEE rounding/underflow behavior, not arbitrary
precision or financial Decimal semantics. Overflow/NaN/infinity and division/modulo
by zero are typed failures. Broader mixed-number/exact-literal profiles are not
qualified by this increment.

String comparison is ordinal case-insensitive. String concatenation requires two
strings. Boolean equality requires two Booleans; logical/ternary conditions require
actual Boolean values, not numeric/string truthiness. Null supports equality and
inequality, not arithmetic or ordered comparison. Mixed incomparable types deny.
Other objects, enums and implicit numeric strings are not converted; arbitrary
ToString/IConvertible/property observers are never a scalar conversion mechanism.

AND/OR and ternary evaluate only needed branches. Unused identifiers are not read
and unused division by zero does not run. All tokens, including unused branches,
must satisfy the captured AllowedTokenTypes policy; laziness does not bypass syntax
or token restrictions. Unused branches are not semantically prequalified.

## Owned Admission And Policy

Direct EvaluateExpression captures/validates a private token/tree representation.
SolveRule captures the expression text at registration, reparses it on each solve,
and compares any supplied nonempty token structure with canonical types/decoded
values. Changed RuleText, changed structure Expression or mismatched tokens deny,
rather than silently overriding or ignoring reviewed input. Registration lookup
remains case-insensitive; the original source spelling and parameter keys remain.
Token spans are bounded diagnostics, not semantic identity.

AST and policy copies are private before parameter lookup. Callback edits to caller
tokens or AllowedTokenTypes cannot change that evaluation; later solves revalidate
the supplied structure. Parameters themselves are not an immutable run snapshot;
hosts must coordinate writers, trusted dictionary comparers and integration context.
Registration/unregistration concurrency and observer lifecycle are not qualified
by the parser's synchronized history lock.

Policy fields/token sets are captured per evaluation. Negative limits and invalid
lifecycle profiles deny. Token policies contain only defined kinds and are bounded
by the enum inventory before copying. Hosts coordinate concurrent policy writers.
A fresh unapproved rule has Draft lifecycle; SolveRule
enforces minimum state and deprecated policy even without a supplied structure.
Direct token evaluation has no catalog lifecycle identity. MaxDepth retains its
rule-reference-chain role; NFEL-1 has no rule-reference syntax and always applies
its independent hard syntax/tree-depth cap, including when MaxDepth is zero.

MaxExecutionMs measures admission/evaluation and checks before/after dictionary
lookups and during tree execution. Zero means no soft timeout. A blocking comparer,
rule getter/setter or other trusted callback cannot be interrupted; timeout is
observed after control returns. There is no CancellationToken on these legacy APIs.
Rule registration/audit observers and unknown-key diagnostics retain legacy behavior,
including expression keys and exception propagation; this increment does not
sandbox plugins or isolate all observers. Parameter lookup/NFEL capture failures
and parser diagnostics do not echo raw source, parameter
payloads or observer exception messages.

## Hard Bounds And Retention

NfelParser exposes the bounds as constants:

| Resource | Maximum |
|---|---:|
| Source UTF-16 code units | 65,536 |
| Tokens | 4,096 |
| AST nodes | 2,048 |
| Grammar recursion / AST height | 64 |
| Decoded string / returned concatenation code units | 16,384 |
| Identifier code units | 256 |
| Numeric literal code units | 128 |
| Retained successful structures | 128 |
| Retained source code units | 1,048,576 |
| Retained tokens | 32,768 |

Failed parses are not retained. Oldest successful history entries are evicted to
meet all three retention bounds. Parse results and RuleStructures history views
are independent copies, including tokens/metadata. Clear and history updates are
synchronized; a parse completing after Clear can create new history. Clear does
not cancel in-flight parsing. Eviction cannot invalidate an admitted evaluation.
Caller-held snapshots and concurrent callers' transient allocations are outside
history retention; no process-wide peak-memory benchmark is claimed.

## Compatibility And Evidence

Existing token ordinals are unchanged; Power/Question/Colon are appended. Existing
public interfaces and transport RuleStructure schema remain unchanged. Transport
versions other than the current 1.0 deny; this is not a future/minor-version
reader compatibility claim. Old NFEL
tokens with quoted literals or incorrect subtraction/unknown ternary do not pass
canonical admission. Explicitly reparse reviewed source and update its tokens;
preserve/review lifecycle metadata instead of silently retaining old semantics.
Older engines are not qualified to read the new token kinds. Host/package adapters
and persisted reader compatibility need separate tests before release.

See `tests/FrameworkReliabilityTests/NfelAdmissionTests.cs` for actual parse,
SolveRule/EvaluateExpression, mutation, lazy branch, finite/type/coercion, policy,
lifecycle, diagnostic, hard-bound and synchronized retention regressions.
This profile is separate from required-default expression/formula ASTs. It does
not establish complete sync governance, immutable host context, provider isolation,
all-parser conformance, security sandboxing or full P3-09/P4-07 completion.
