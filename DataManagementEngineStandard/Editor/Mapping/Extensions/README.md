# Mapping Extensions

## Purpose
This folder provides fluent extension helpers that improve mapper configuration ergonomics and diagnostics.

## Key Files
- `AutoObjMapperExtensions.cs`: Extension APIs including `FluentConfigurationBuilder` and detailed mapping-result helpers.

## Usage Notes
- Use fluent extensions for readable map setup in application code.
- Keep extension methods thin wrappers over core mapper capabilities.
- Preserve backward compatibility for public extension method signatures.
