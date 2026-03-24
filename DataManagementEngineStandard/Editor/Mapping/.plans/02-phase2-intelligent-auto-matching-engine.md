# Phase 2 - Intelligent Auto-Matching Engine

## Objective
Upgrade convention mapping to a weighted, explainable auto-match engine comparable to enterprise suites.

## Scope
- Multi-strategy field matching and score ranking.
- Explainable match output with reasons and confidence.

## File Targets
- `Mapping/MappingManager.Conventions.cs`
- `Mapping/MappingManager.cs`

## Planned Enhancements
- Matching strategies:
  - exact/case-insensitive/prefix (existing)
  - normalized token matching
  - synonym dictionary support
  - optional phonetic/fuzzy-distance matching
- Weighted confidence score per candidate.
- Top-N match suggestions with explainability (why matched).
- Confidence thresholds:
  - auto-accept
  - review-required
  - reject

## Acceptance Criteria
- Auto-map returns confidence scores for each field pairing.
- Ambiguous matches are surfaced for manual review.
- Match explainability is available for UI/tooling consumption.

## Risks and Mitigations
- Risk: over-aggressive fuzzy matches.
  - Mitigation: threshold tuning + explicit review band.
