# File Helper

## Purpose
This folder contains file-format helper logic for file-backed data sources.

## Key Files
- `FileFormatHelper.cs`: File helper for format-aware behavior and conversions.

## Usage Notes
- Keep schema inference and delimiter/format behavior deterministic.
- Validate file-format assumptions before runtime processing.
- Surface parse and serialization errors with actionable diagnostics.
