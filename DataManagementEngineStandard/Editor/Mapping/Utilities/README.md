# Mapping Utilities

## Purpose
Stateless utility functions used by AutoObjMapper for reusable mapping operations.

## Key Files
- `AutoObjMapperUtilities.cs`: Shared utility methods for mapping engine components

## Functions
- Property matching and comparison utilities
- Type conversion helpers for field mapping
- Naming convention normalization
- Circular reference detection

## Usage Notes
- Keep utility methods stateless and side-effect free
- Promote frequently reused logic from core files into this utility layer
- Ensure utility behavior remains compatible with mapper configuration rules
