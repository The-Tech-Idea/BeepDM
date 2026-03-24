# Phase 14 — Data Classification and DLP

| Attribute      | Value                                      |
|----------------|--------------------------------------------|
| Phase          | 14                                         |
| Status         | planned                                    |
| Priority       | Critical (regulatory)                      |
| Dependencies   | Phase 3 (schema inference), Phase 12 (schema registry), Phase 7 (security governance) |
| Est. Effort    | 5 days                                     |

---

## 1. Goal

Automatically **detect, classify, and protect sensitive data in CSV/file sources** before it is persisted or forwarded.  
Callers should be able to configure per-column masking/tokenization policies that fire _as data is read_, not as an afterthought in the target database.

---

## 2. Motivation

| Current state | Enterprise / regulatory requirement |
|---------------|-------------------------------------|
| Raw file data flows unmodified to target | PII must be masked or tokenized at the point of ingestion |
| No awareness of what sensitive data exists in files | Auto-detect SSNs, emails, credit-card numbers, phone numbers in column data |
| Phase 7 adds static path-policy guards | Phase 14 adds _column-value_ analysis and masking (complementary, not redundant) |
| Compliance teams cannot audit what sensitive data was seen | Classification labels must be attached to schema metadata (Phase 12) and logged |

---

## 3. Sensitive Data Patterns

### 3.1 Built-in detection patterns

| Pattern Key | Category | Detection method |
|-------------|----------|-----------------|
| `PII-Email` | Personally Identifiable | Regex `^[^@\s]+@[^@\s]+\.[^@\s]+$` |
| `PII-Phone` | Personally Identifiable | Regex — E.164, US, UK |
| `PII-SSN` | Highly Sensitive | Regex `^\d{3}-\d{2}-\d{4}$` (+ invalid prefix check) |
| `PII-CreditCard` | Highly Sensitive | Luhn algorithm + pattern |
| `PII-IBAN` | Highly Sensitive | ISO 13616 IBAN checksum |
| `PII-Name` | Personally Identifiable | NLP heuristic (high-cardinality, title-case token pairs) |
| `PII-DateOfBirth` | Personally Identifiable | Date column with suspiciously past dates + age range |
| `PII-PostalAddress` | Personally Identifiable | Multi-token matches address keywords |
| `PII-NationalId` | Highly Sensitive | Country-specific patterns (extensible) |
| `Health-Condition` | Special Category (GDPR Art 9) | Keyword list + column name heuristic |
| `Financial-AccountNum` | Sensitive | Numeric, lengths 8-12, column name heuristic |

### 3.2 Detection confidence levels

| Level | Meaning | Action |
|-------|---------|--------|
| HIGH (>=0.85) | Pattern matched AND column name is consistent | Apply policy immediately |
| MEDIUM (0.60–0.84) | Pattern matched but column name ambiguous | Log warning; apply policy; alert steward |
| LOW (<0.60) | Partial heuristic match | Log info; do NOT auto-apply policy |

---

## 4. Contracts

### 4.1 `IDataClassificationEngine`

```csharp
namespace TheTechIdea.Beep.FileManager.Classification
{
    public interface IDataClassificationEngine
    {
        /// <summary>
        /// Samples up to <paramref name="sampleSize"/> values from a column
        /// and returns all detected classifications with confidence scores.
        /// </summary>
        Task<IReadOnlyList<ClassificationHit>> ClassifyColumnAsync(
            string columnName,
            IEnumerable<string> sampleValues,
            int sampleSize = 500,
            CancellationToken ct = default);

        /// <summary>
        /// Classifies all columns in a <see cref="FileSchemaVersion"/> by analysing
        /// the sample data captured during schema inference (Phase 3).
        /// </summary>
        Task<FileClassificationResult> ClassifySchemaAsync(
            FileSchemaVersion schema,
            IDictionary<string, IEnumerable<string>> columnSamples,
            CancellationToken ct = default);
    }

    public sealed record ClassificationHit(
        string PatternKey,
        string Category,
        double Confidence,
        int MatchedSamples,
        int TotalSamples);

    public sealed class FileClassificationResult
    {
        public IReadOnlyDictionary<string, IReadOnlyList<ClassificationHit>> ColumnHits { get; init; }
        public IReadOnlyList<string> HighSensitivityColumns { get; init; }   // >= HIGH confidence
        public bool HasSpecialCategoryData { get; init; }   // GDPR Art 9 — requires extra consent
        public DateTimeOffset ClassifiedAt { get; init; }
    }
}
```

### 4.2 `IDataMaskingEngine`

```csharp
namespace TheTechIdea.Beep.FileManager.Classification
{
    public interface IDataMaskingEngine
    {
        /// <summary>
        /// Applies the masking strategy for <paramref name="patternKey"/> to <paramref name="value"/>.
        /// Returns the masked/tokenized string.
        /// </summary>
        string Mask(string value, string patternKey, ColumnMaskingPolicy policy);
    }
}
```

### 4.3 `ColumnMaskingPolicy`

```csharp
namespace TheTechIdea.Beep.FileManager.Classification
{
    public enum MaskingStrategy
    {
        /// <summary>Replace all characters with ***</summary>
        Redact,
        /// <summary>Show first N characters, mask the rest: john@******.com</summary>
        PartialMask,
        /// <summary>Replace with a deterministic pseudonym (same input → same output, reversible with key)</summary>
        Tokenize,
        /// <summary>Replace with a format-preserving encrypted value (FPE)</summary>
        FormatPreservingEncrypt,
        /// <summary>Replace with a synthetic value of the same type (fake name, fake email)</summary>
        Synthesize,
        /// <summary>Round numeric values (e.g. age 37 → 35, zip 10025 → 10000)</summary>
        Generalize,
        /// <summary>Pass value through unchanged — operator explicitly opts out of masking</summary>
        None
    }

    public sealed class ColumnMaskingPolicy
    {
        public string PatternKey { get; init; }             // which classification this policy applies to
        public MaskingStrategy Strategy { get; init; }
        public int     PartialMaskRevealChars { get; init; } = 4;  // for PartialMask
        public string  TokenizationKeyId { get; init; }             // reference to key in the key store
        public double  GeneralizeRoundTo { get; init; }             // for Generalize: round to this multiple
        public bool    ApplyWhenConfidence { get; init; }           // if true: apply even at MEDIUM confidence
    }
}
```

### 4.4 `IMaskingPolicyStore`

```csharp
namespace TheTechIdea.Beep.FileManager.Classification
{
    public interface IMaskingPolicyStore
    {
        /// <summary>Returns the masking policy for a given pattern key, or null if no policy is registered.</summary>
        ColumnMaskingPolicy GetPolicy(string patternKey);

        /// <summary>Adds or replaces the policy for a pattern key.</summary>
        void SetPolicy(string patternKey, ColumnMaskingPolicy policy);

        /// <summary>Returns all registered policies.</summary>
        IReadOnlyDictionary<string, ColumnMaskingPolicy> GetAll();
    }
}
```

---

## 5. Built-in Masking Implementations

### 5.1 Email — PartialMask

```
john.doe@example.com  →  john***@***.com
```

Rule: reveal local-part up to first 4 chars + `***`, reveal TLD, mask domain.

### 5.2 SSN — Redact

```
123-45-6789  →  ***-**-****
```

### 5.3 Credit Card — PartialMask (PCI-DSS compliant)

```
4111 1111 1111 1234  →  4111 **** **** 1234
```

Show first 6 and last 4 digits only (PCI-DSS BIN + last-4 rule).

### 5.4 Phone — PartialMask

```
+1-212-555-1234  →  +1-***-***-1234
```

Show country code and last 4 digits.

### 5.5 Name — Synthesize

Replace with a random name drawn from a locale-appropriate synthetic name list.  
Same original value (Levenshtein hash) → deterministic replacement when `TokenizationKeyId` is set.

---

## 6. Integration with the Ingestion Pipeline

Classification runs once per file (during `Validating` state):

```
Validating
    ↓
[CSVAnalyser samples up to 500 rows per column]
    ↓
[IDataClassificationEngine.ClassifySchemaAsync(schema, columnSamples)]
    ↓
[Update FileSchemaVersion.Columns[n].DataClassifications in registry (Phase 12)]
    ↓
[IMaskingPolicyStore → resolve masking policy per column]
    ↓
[Attach ColumnMaskingPolicy map to the active ingestion context]
    ↓
Ingesting ──► Apply Mask(value, patternKey, policy) on every row read
```

Masking is applied **in the reader layer** — raw values never reach the target store.

---

## 7. Audit Logging for Classified Data Access

Every time a classified column is **read** (even for schema analysis):

```csharp
public sealed record ClassifiedColumnAccessEvent(
    string JobId,
    string AccessedBy,          // service principal or user
    string SourceSystem,
    string EntityName,
    string ColumnName,
    string ClassificationLabel,
    AccessPurpose Purpose,      // SchemaAnalysis | Ingestion | Query | Export
    DateTimeOffset AccessedAt,
    bool WasMasked);
```

This audit record must be written to `IDMEEditor.Logger` with severity `Audit` AND to a tamper-evident append-only store (separate from normal application logs).

---

## 8. Default Policy Table

| Classification | Default strategy | Notes |
|---------------|-----------------|-------|
| `PII-Email` | `PartialMask` | Reveal local-part prefix + TLD |
| `PII-SSN` | `Redact` | All digits masked |
| `PII-CreditCard` | `PartialMask` | PCI-DSS BIN + last-4 rule |
| `PII-Phone` | `PartialMask` | Country code + last 4 |
| `PII-IBAN` | `Tokenize` | Deterministic token |
| `PII-Name` | `Synthesize` | Random locale name |
| `PII-DateOfBirth` | `Generalize` | Round to year |
| `PII-NationalId` | `Redact` | Full redaction |
| `Health-Condition` | `Redact` | GDPR Art 9 special category |
| `Financial-AccountNum` | `Tokenize` | Deterministic token |

All default policies are **overridable** via `IMaskingPolicyStore.SetPolicy(...)`.

---

## 9. Acceptance Criteria

| # | Criterion | Test |
|---|-----------|------|
| 1 | `ClassifyColumnAsync` detects `PII-Email` with HIGH confidence on a 200-row sample containing valid emails | Unit |
| 2 | SSN `123-45-6789` → `***-**-****` with `Redact` strategy | Unit |
| 3 | Credit card `4111111111111234` → `411111******1234` | Unit |
| 4 | Masking is applied per-value in `GetEntity` when a `ColumnMaskingPolicy` is present | Integration |
| 5 | A `ClassifiedColumnAccessEvent` is logged for every column with `HIGH` classification during ingestion | Integration |
| 6 | Setting `MaskingStrategy.None` for a pattern key disables masking for that column | Unit |
| 7 | `HasSpecialCategoryData = true` if any column has `Health-Condition` or `PII-NationalId` | Unit |
| 8 | Two identical raw values produce the same tokenized output when `TokenizationKeyId` is the same | Unit |

---

## 10. Deliverables

| Artifact | Location |
|----------|----------|
| `Classification/IDataClassificationEngine.cs` | `FileManager/Classification/` |
| `Classification/IDataMaskingEngine.cs` | `FileManager/Classification/` |
| `Classification/ColumnMaskingPolicy.cs` | `FileManager/Classification/` |
| `Classification/IMaskingPolicyStore.cs` | `FileManager/Classification/` |
| `Classification/SensitiveDataPatterns.cs` | `FileManager/Classification/` |
| `Classification/RegexClassificationEngine.cs` | `FileManager/Classification/Implementations/` |
| `Classification/BuiltinMaskingStrategies.cs` | `FileManager/Classification/Implementations/` |
| `Classification/DefaultMaskingPolicyStore.cs` | `FileManager/Classification/Implementations/` |
| `Classification/ClassifiedColumnAccessEvent.cs` | `FileManager/Classification/` |
| Unit tests | `tests/FileManager/ClassificationTests.cs` |

---

## 11. Enterprise Standards Traceability

| Standard | Clause | Addressed |
|----------|--------|-----------|
| GDPR Art. 5(1)(f) | Integrity and confidentiality | Masking at ingestion boundary |
| GDPR Art. 9 | Special category data | `HasSpecialCategoryData` flag + default `Redact` |
| PCI-DSS Req. 3.4 | PAN masking / truncation | Built-in credit card partial mask |
| HIPAA §164.514(b) | De-identification | `Synthesize` + `Generalize` strategies |
| NIST SP 800-188 | De-identification of government datasets | `FormatPreservingEncrypt` strategy |
| CCPA §1798.100 | Consumer data rights audit trail | `ClassifiedColumnAccessEvent` log |
