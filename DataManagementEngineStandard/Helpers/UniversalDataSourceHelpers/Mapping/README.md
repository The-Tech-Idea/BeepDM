# Universal Helper Mapping

## Purpose
This folder contains mapping bridges that convert relationship-analysis output into entity metadata structures used by the editor and data-source layers.

## Key Files
- `RelationshipToEntityMapper.cs`: Translates inferred relationships into `EntityStructure`-compatible references.

## Usage Notes
- Use this mapper after analysis helpers have produced normalized relationship metadata.
- Keep generated entity relationship names stable for repeatable outputs.
- Validate mapped relations against existing primary/foreign key constraints before persistence.
