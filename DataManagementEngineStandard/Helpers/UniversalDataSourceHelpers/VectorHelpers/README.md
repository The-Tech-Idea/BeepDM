# Vector Helper

## Purpose
This folder provides helper logic for vector-database style providers, including vector-oriented query behavior and capability checks.

## Key Files
- `VectorDbHelper.cs`: Vector provider helper implementation.

## Usage Notes
- Separate vector similarity capabilities from standard relational features.
- Keep embedding/vector field type handling explicit and validated.
- Ensure capability checks communicate whether ANN/similarity operations are available.
