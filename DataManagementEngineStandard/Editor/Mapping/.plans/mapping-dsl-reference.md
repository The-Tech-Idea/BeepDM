# Mapping DSL Reference

## Purpose
This document defines a practical Mapping DSL for `MappingManager` conditions and transforms, with concrete syntax and examples.

It is intended as a planning/reference artifact for phases:
- `05-phase5-rule-based-and-conditional-mapping.md`
- `03-phase3-conversion-and-transform-pipeline.md`
- `09-phase9-etl-import-sync-integration.md`

## DSL Design Goals
- Readable and explicit rules.
- Deterministic execution order.
- Easy interoperability with existing BeepDM mapping/default pipelines.
- Safe defaults for null/type handling.

## Rule Model

Each field mapping can optionally include:
- `source`: source field/path
- `target`: destination field/path
- `when`: condition expression (optional)
- `transform`: one or more transform operations
- `onNull`: null policy
- `onError`: error policy

### Canonical Rule Shape (YAML-like)
```text
map:
  source: SourceField
  target: TargetField
  when: <condition-expression>
  transform: <transform-expression or pipeline>
  onNull: preserve|nullToDefault|skip
  onError: fail|warn|fallback(<value>)
```

## Expression Syntax

Two equivalent syntaxes are supported conceptually:

1. Function style
- `EQ(Source.Status, 'Active')`
- `CONCAT(TRIM(Source.FirstName), ' ', TRIM(Source.LastName))`

2. Dot style
- `EQ.Source.Status.Active`
- `CONCAT.TRIM.Source.FirstName.' '.TRIM.Source.LastName`

## Condition Operators
- `EQ`, `NE`
- `GT`, `GTE`, `LT`, `LTE`
- `IN`, `NOTIN`
- `ISNULL`, `NOTNULL`
- `STARTSWITH`, `ENDSWITH`, `CONTAINS`
- `AND`, `OR`, `NOT`

## Transform Operators
- String: `TRIM`, `UPPER`, `LOWER`, `REPLACE`, `SUBSTR`, `PADLEFT`, `PADRIGHT`
- Numeric: `TOINT`, `TODECIMAL`, `ROUND`, `ABS`, `ADD`, `SUB`, `MUL`, `DIV`
- Date/time: `TODATE`, `FORMATDATE`, `NOW`
- Null/value: `COALESCE`, `DEFAULT`
- Lookup/query: `LOOKUP`, `QUERYSCALAR`
- Structure: `MAPOBJECT`, `MAPLIST`, `SPLIT`, `JOIN`
- Utility: `HASH`, `GUID`, `CONCAT`

## Execution Semantics
- `when` is evaluated first.
- If `when` is false:
  - mapping step is skipped (unless explicitly configured otherwise).
- If true:
  - transform pipeline executes left-to-right.
- Null/error policies apply at the end of each transform step and final assignment.

## Null and Error Policies
- `onNull: preserve` keeps null.
- `onNull: nullToDefault` substitutes `DEFAULT(...)`.
- `onNull: skip` does not set target field.
- `onError: fail` fails the map execution.
- `onError: warn` logs warning and continues.
- `onError: fallback(x)` uses fallback value `x`.

## Field Path Notation
- Flat field: `Source.Email`
- Nested object: `Source.Address.City`
- Collection item (planned): `Source.Items[*].Sku`

## 50 Practical Examples

## A) Basic Field-to-Field (1-8)
1. `map source=Source.Name target=Target.Name`
2. `map source=Source.Email target=Target.Email`
3. `map source=Source.Phone target=Target.ContactPhone`
4. `map source=Source.Code target=Target.ExternalCode`
5. `map source=Source.CustomerId target=Target.CustomerId`
6. `map source=Source.Country target=Target.Country`
7. `map source=Source.Zip target=Target.PostalCode`
8. `map source=Source.IsActive target=Target.IsEnabled`

## B) Conditional Mapping (9-18)
9. `when EQ(Source.Status,'Active') map Source.Status -> Target.Status`
10. `when NE(Source.Type,'Internal') map Source.Type -> Target.Type`
11. `when GT(Source.Score,70) map Source.Score -> Target.PassScore`
12. `when GTE(Source.Age,18) map Source.Age -> Target.AdultAge`
13. `when LT(Source.Balance,0) map Source.Balance -> Target.NegativeBalance`
14. `when IN(Source.Country,'US','CA','MX') map Source.Country -> Target.RegionCountry`
15. `when NOTNULL(Source.ManagerId) map Source.ManagerId -> Target.OwnerId`
16. `when ISNULL(Source.MiddleName) map DEFAULT('') -> Target.MiddleName`
17. `when AND(EQ(Source.Status,'Active'),GT(Source.Score,80)) map Source.Tier -> Target.Tier`
18. `when OR(EQ(Source.Channel,'Web'),EQ(Source.Channel,'Mobile')) map Source.Channel -> Target.Channel`

## C) String Transforms (19-30)
19. `transform TRIM(Source.Name) -> Target.Name`
20. `transform UPPER(Source.CountryCode) -> Target.CountryCode`
21. `transform LOWER(Source.Email) -> Target.Email`
22. `transform REPLACE(Source.Phone,'-','') -> Target.Phone`
23. `transform SUBSTR(Source.Sku,0,8) -> Target.SkuShort`
24. `transform PADLEFT(Source.Sequence,6,'0') -> Target.SequenceCode`
25. `transform PADRIGHT(Source.Branch,4,'X') -> Target.BranchCode`
26. `transform CONCAT(Source.FirstName,' ',Source.LastName) -> Target.FullName`
27. `transform CONCAT(UPPER(Source.LastName),', ',Source.FirstName) -> Target.DisplayName`
28. `transform JOIN(SPLIT(Source.Tags,';'),'|') -> Target.Tags`
29. `transform REPLACE(TRIM(Source.City),'  ',' ') -> Target.City`
30. `transform HASH(LOWER(Source.Email)) -> Target.EmailHash`

## D) Numeric Transforms (31-38)
31. `transform TOINT(Source.Quantity) -> Target.Quantity`
32. `transform TODECIMAL(Source.Price) -> Target.Price`
33. `transform ROUND(Source.Amount,2) -> Target.Amount`
34. `transform ABS(Source.Delta) -> Target.AbsoluteDelta`
35. `transform ADD(Source.Subtotal,Source.Tax) -> Target.Total`
36. `transform SUB(Source.Total,Source.Discount) -> Target.NetTotal`
37. `transform MUL(Source.UnitPrice,Source.Qty) -> Target.LineTotal`
38. `transform DIV(Source.Total,Source.Count) -> Target.Average`

## E) Date/Time Transforms (39-43)
39. `transform TODATE(Source.OrderDate,'yyyy-MM-dd') -> Target.OrderDate`
40. `transform FORMATDATE(Source.OrderDate,'yyyyMMdd') -> Target.OrderDateKey`
41. `transform NOW('utc') -> Target.ProcessedAtUtc`
42. `transform FORMATDATE(NOW('local'),'yyyy-MM-dd HH:mm:ss') -> Target.ProcessedAtLocal`
43. `transform TODATE(Source.BirthDate,'MM/dd/yyyy') -> Target.BirthDate`

## F) Null/Fallback Handling (44-47)
44. `transform COALESCE(Source.NickName,Source.FirstName,'Unknown') -> Target.DisplayName`
45. `onNull nullToDefault transform DEFAULT('N/A') -> Target.Comment`
46. `onNull skip map Source.OptionalField -> Target.OptionalField`
47. `onError fallback('0') transform TOINT(Source.UnstableInt) -> Target.SafeInt`

## G) Lookup/Query Mapping (48-50)
48. `transform LOOKUP('Departments','Name','Id',Source.DepartmentId) -> Target.DepartmentName`
49. `transform QUERYSCALAR('SELECT Region FROM Customers WHERE Id=@Id', Id=Source.CustomerId) -> Target.Region`
50. `when NOTNULL(Source.ProductCode) transform LOOKUP('Products','Id','Code',Source.ProductCode) -> Target.ProductId`

## Additional Composite Examples

### Composite-1: Customer Full Name with Safety
```text
when NOTNULL(Source.FirstName)
transform CONCAT(TRIM(Source.FirstName),' ',TRIM(COALESCE(Source.LastName,'')))
onError warn
-> Target.FullName
```

### Composite-2: Financial Net Amount
```text
when AND(NOTNULL(Source.Amount),NOTNULL(Source.Fee))
transform ROUND(SUB(TODECIMAL(Source.Amount),TODECIMAL(Source.Fee)),2)
onError fail
-> Target.NetAmount
```

### Composite-3: Country Normalization
```text
when NOTNULL(Source.Country)
transform UPPER(TRIM(Source.Country))
transform REPLACE($prev,'USA','US')
-> Target.CountryCode
```

### Composite-4: Nested Address Map
```text
map Source.Address.City -> Target.PrimaryAddress.City
map Source.Address.Zip -> Target.PrimaryAddress.PostalCode
transform UPPER(Source.Address.Country) -> Target.PrimaryAddress.CountryCode
```

## Validation Checklist
- Operator exists and is supported.
- Required argument count matches operator contract.
- Field paths are resolvable in source schema.
- Destination field type compatibility is validated.
- Null/error policies are explicitly defined for risky conversions.

## Suggested Governance Rules
- Auto-accept only mappings with confidence >= configured threshold.
- Require review for rules using `QUERYSCALAR` and `LOOKUP`.
- Disallow `onError warn` for critical financial/identity fields.
- Require explicit null policy for non-nullable destination fields.

## Testing Guidance
- Unit test each transform operator with normal/null/error inputs.
- Test condition truth tables (`AND`/`OR`/`NOT`).
- Validate mapping outputs against sample source snapshots.
- Add drift tests for renamed/removed source fields.

## Compatibility Notes
- Existing `FieldMapping` entries remain valid.
- DSL adoption can be incremental:
  - begin with condition-only rules,
  - then add transform pipelines,
  - then enable strict validation in production.
