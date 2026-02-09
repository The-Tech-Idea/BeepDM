# Universal Helper Analysis

## Purpose
This folder provides analysis utilities used to infer relationships and navigation properties from model metadata.

## Key Files
- `NavigationPropertyDetector.cs`: Detects candidate navigation properties and categorizes navigation semantics.
- `RelationshipInferencer.cs`: Infers relationship cardinality and emits relationship-analysis metadata.

## Runtime Flow
1. Inspect types and entity metadata for relational patterns.
2. Infer one-to-one, one-to-many, or many-to-many characteristics.
3. Emit normalized analysis output for mapping and helper pipelines.

## Extension Guidelines
- Keep inference deterministic for identical metadata inputs.
- Distinguish hard relationships from heuristic suggestions.
- Validate inferred names against existing entity-map conventions.
