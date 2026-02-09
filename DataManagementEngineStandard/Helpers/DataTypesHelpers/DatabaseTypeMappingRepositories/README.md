# Database Type Mapping Repositories

## Purpose
This folder contains segmented partial mapping repositories that translate between CLR field types and provider-specific database types across a wide provider matrix.

## Structure
- Files are grouped by domain/provider families (`SqlServer`, `Oracle`, `PostgreMySQL`, `NoSQL`, `Graph`, `Vector`, `Streaming`, `Cloud`, `IoT`, and others).
- Each partial file contributes mappings to the same repository surface.
- Segmentation keeps provider logic maintainable while preserving one lookup entry point.

## Usage Notes
- Use this repository as the canonical type-translation source when generating DDL and validating field compatibility.
- Keep mapping keys normalized to avoid duplicate provider aliases.
- Update both forward (CLR to provider) and reverse (provider to CLR) mappings together.

## Testing Focus
- Numeric precision/scale mappings across providers.
- Date/time and binary edge-case conversions.
- Backward compatibility when introducing new provider aliases.
