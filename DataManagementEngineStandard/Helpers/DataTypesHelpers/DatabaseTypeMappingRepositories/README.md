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

## Large variable-string storage

`DataTypeMappingLookup.ResolveStringLength` owns the provider length rule shared by configured lookups and `RdbmsHelper` migration type mapping. For PostgreSQL, sized `varchar(N)` / `character varying(N)` above 10,485,760 characters resolve to the corresponding unbounded variable type. Ordinary sized fields stay sized, preferred `text` stays text, and fixed `char` semantics do not change. SQL Server retains its existing nvarchar/varchar MAX rules. Applications declare CLR metadata and byte/semantic admission; they must not pre-map columns or reduce payload fidelity to work around the type owner.

Reference: [PostgreSQL18 character types](https://www.postgresql.org/docs/current/datatype-character.html), checked2026-10-28. `LargeStringStorageShapeTests` and `EntityTemporalRepresentationTests` run on net8/net9/net10 under Windows, alongside existing decimal controls. Returned-type tests are not evidence of a live migration, provider transaction or package-consumer deployment.