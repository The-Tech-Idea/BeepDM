# Mapping Extensions

## Purpose
Fluent extension helpers improving mapper configuration ergonomics and diagnostics for EntityDataMap operations.

## Key Files
- `AutoObjMapperExtensions.cs`: Extension APIs including `FluentConfigurationBuilder` and detailed mapping-result helpers

## Features
- Fluent `FluentConfigurationBuilder` for readable map setup
- Detailed mapping result diagnostics
- Auto-mapping by convention extensions
- Property transformation helpers

## Usage Notes
- Use fluent extensions for readable map setup in application code
- Keep extension methods thin wrappers over core mapper capabilities
- Preserve backward compatibility for public extension method signatures
