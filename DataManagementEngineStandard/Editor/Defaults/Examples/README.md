# Defaults Examples

## Purpose
This folder contains sample code that demonstrates resolver registration, default rule definitions, and default application behavior.

## Key Files
- `DefaultsManagerExamples.cs`: Example flows for creating and testing defaults.
- Embedded sample models (`SampleOrder`, `BusinessLogicResolver`) illustrate custom rule extension.

## How To Use
1. Register built-in and custom resolvers.
2. Define per-field default rules.
3. Apply defaults to sample entities and validate outcomes.

## Extension Guidelines
- Keep examples synchronized with the current resolver interfaces.
- Prefer small, deterministic scenarios that can be promoted into unit tests.
