# Mapping Utilities

## Purpose
This folder contains utility-level functions used by AutoObjMapper for reusable mapping operations that do not belong in core configuration or runtime classes.

## Key Files
- `AutoObjMapperUtilities.cs`: Shared utility methods consumed by mapping engine components.

## Usage Notes
- Keep utility methods stateless and side-effect free.
- Promote frequently reused logic from core files into this utility layer.
- Ensure utility behavior remains compatible with mapper configuration rules.
