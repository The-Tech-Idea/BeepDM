# Streaming Helper

## Purpose
This folder contains helper functionality for streaming-oriented data sources where append/read patterns differ from transactional stores.

## Key Files
- `StreamingHelper.cs`: Streaming provider helper implementation.

## Usage Notes
- Model operations around stream append/read semantics.
- Expose unsupported random-update semantics through capability checks.
- Keep ordering and offset handling predictable for replay scenarios.
